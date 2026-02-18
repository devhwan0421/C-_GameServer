using Serilog;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;

public class UserSession
{
    public int SessionId { get; set; }
    public int AccountId { get; set; }
    private Socket _socket;
    private GameLogicThread _gameLogicThread;
    private PacketHandler _handler;

    private SocketAsyncEventArgs _recvArgs;
    private SocketAsyncEventArgs _sendArgs;

    private RecvBuffer _recvBuffer;
    private Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>(); //보내기 대기 중인 패킷들을 저장하는 큐
    private List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>(); //보내기 대기 중인 패킷들을 모아두는 리스트

    private bool _isPendingSend = false;

    private const int MAX_SEND_SIZE = 65536;
    private ArraySegment<byte> _reservePacket;

    private object _lock = new object();
    private readonly SemaphoreSlim _packetSemaphore = new SemaphoreSlim(1, 1);

    private int _isDisconnected = 0;

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        PropertyNameCaseInsensitive = true
    };

    public Player MyPlayer { get; set; }

    public UserSession(Socket socket, GameLogicThread gameLogicThread, PacketHandler handler, int sessionId)
    {
        _socket = socket;
        _gameLogicThread = gameLogicThread;
        _handler = handler;
        this.SessionId = sessionId;

        byte[] rented = ArrayPool<byte>.Shared.Rent(65536);
        _recvBuffer = new RecvBuffer(new ArraySegment<byte>(rented, 0, 65536));

        _recvArgs = new SocketAsyncEventArgs();
        _sendArgs = new SocketAsyncEventArgs();

        _recvArgs.Completed += OnIOCompleted; //IOCP 콜백 등록
        _sendArgs.Completed += OnIOCompleted;
    }

    public void Start() => RegisterRecv();

    private void RegisterRecv()
    {
        if (_isDisconnected == 1) return;

        if (_recvBuffer.FreeSize < _recvBuffer.UnderlyingArray.Length /4)
            _recvBuffer.Clean();

        ArraySegment<byte> writeSegment = _recvBuffer.WriteSegment;

        _recvArgs.SetBuffer(writeSegment.Array, writeSegment.Offset, writeSegment.Count);

        bool pending = _socket.ReceiveAsync(_recvArgs);
        if (!pending)
        {
            OnIOCompleted(null, _recvArgs);
        }
    }

    private void OnIOCompleted(object sender, SocketAsyncEventArgs args)
    {
        if (args.LastOperation == SocketAsyncOperation.Receive)
        {
            ProcessReceive(args);
        }
        else if (args.LastOperation == SocketAsyncOperation.Send)
        {
            ProcessSend(args);
        }
    }

    private void ProcessReceive(SocketAsyncEventArgs args)
    {
        if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
        {
            //수신 성공 시 커서만 이동
            _recvBuffer.OnWrite(args.BytesTransferred);

            //루프를 돌며 완성된 패킷이 있는지 체크
            //한번에 여러 패킷이 올 수 있으므로 루프 처리
            while (true)
            {
                ArraySegment<byte> data = _recvBuffer.ReadSegment;

                //헤더 도착 체크
                if (data.Count < 4) break;

                ushort size = BitConverter.ToUInt16(data.Array, data.Offset);

                if (size < 4) break;

                ushort id = BitConverter.ToUInt16(data.Array, data.Offset + 2);

                //패킷이 다 안 왔으면 탈출
                if (data.Count < size) break;

                //바디 추출
                string json = Encoding.UTF8.GetString(data.Array, data.Offset + 4, size - 4);

                ThroughputMonitor.IncReceived();
                _gameLogicThread.Enqueue(async () =>
                {
                    //_handler.OnRecvPacket(this, (PacketID)id, json);
                    await this.ProcessPacketAsync((PacketID)id, json);
                });

                //처리한 패킷만큼 읽기 커서 이동
                _recvBuffer.OnRead(size);
            }

            //다시 수신 대기
            RegisterRecv();
        }
        else
        {
            Console.WriteLine($"ProcessReceive에서 종료 {MyPlayer.Nickname}({MyPlayer.UserId})");
            _= DisConnect();
        }
    }

    public async Task ProcessPacketAsync(PacketID id, string json)
    {
        await _packetSemaphore.WaitAsync();

        try
        {
            await _handler.OnRecvPacket(this, id, json);
        }
        finally
        {
            _packetSemaphore.Release();
        }
    }
    /*public async Task ProcessPacketAsync(PacketID id, string json)
    {
        await _packetSemaphore.WaitAsync(TimeSpan.FromSeconds(5)); //5초가 지나면 패킷이 무시될 수 있음. 상태 불일치 문제가 생길 수 있음
        try
        {
            await _handler.OnRecvPacket(this, id, json);
        }
        finally
        {
            _packetSemaphore.Release();
        }
    }*/

    public void Send<T>(T packet) where T : IPacket
    {
        string json = JsonSerializer.Serialize(packet, _jsonOptions);
        //Console.WriteLine($"[Debug] Sending Packet: ID={(ushort)packet.PacketId}, JSON={json}"); // 로그 추가
        ushort packetId = (ushort)packet.PacketId;
        Send(packetId, json);
    }
    public void Send(ushort packetId, string json)
    {
        ArraySegment<byte> sendBuff = PacketSerializer.Serialize(packetId, json);
        lock (_lock)
        {
            if(_isDisconnected == 1) return;

            _sendQueue.Enqueue(sendBuff);
            //Console.WriteLine($"[Debug] Send Queued: ID={packetId}, QueueCount={_sendQueue.Count}"); // 로그 추가
            if (_pendingList.Count == 0)
            {
                RegisterSend();
            }
        }
    }

    public void Send(ArraySegment<byte> sendBuff)
    {
        if (_isDisconnected == 1) return;

        bool pushSend = false;

        lock (_lock)
        {
            _sendQueue.Enqueue(sendBuff);

            if (_isPendingSend == false)
            {
                _isPendingSend = true;
                pushSend = true;
            }
        }

        if (pushSend)
        {
            Task.Run(() => RegisterSend());
        }
    }
    /*public void Send(ArraySegment<byte> sendBuff)
    {
        lock (_lock)
        {
            if (_isDisconnected == 1) return;

            _sendQueue.Enqueue(sendBuff);

            if (_pendingList.Count == 0)
            {
                RegisterSend();
            }
        }
    }*/

    private void RegisterSend()
    {
        lock (_lock)
        {
            _pendingList.Clear();
            int totalPacketSize = 0;

            while (totalPacketSize < MAX_SEND_SIZE)
            {
                ArraySegment<byte> packet;

                if (_reservePacket.Count > 0)
                {
                    packet = _reservePacket;
                    _reservePacket = default;
                }
                else if (_sendQueue.Count > 0) packet = _sendQueue.Dequeue();
                else break;

                int packetSize = packet.Count;
                int availableSize = MAX_SEND_SIZE - totalPacketSize;

                if (packetSize > availableSize)
                {
                    _pendingList.Add(new ArraySegment<byte>(packet.Array, packet.Offset, availableSize));
                    _reservePacket = new ArraySegment<byte>(packet.Array, packet.Offset + availableSize, packetSize - availableSize);
                    totalPacketSize += availableSize;
                    break;
                }
                else
                {
                    _pendingList.Add(packet);
                    totalPacketSize += packetSize;
                }
            }

            if(_pendingList.Count == 0)
            {
                _isPendingSend = false;
                _sendArgs.BufferList = null;
                return;
            }

            _sendArgs.BufferList = _pendingList;
        }

        try
        {
            bool pending = _socket.SendAsync(_sendArgs);
            if (!pending)
            {
                ProcessSend(_sendArgs);
            }
        }
        catch (Exception ex)
        {
            lock (_lock) { _isPendingSend = false; }
            _ = DisConnect();
        }
    }
    /*private void RegisterSend()
    {
        int totalPacketSize = 0;

        while (totalPacketSize < MAX_SEND_SIZE)
        {
            ArraySegment<byte> packet;

            if(_reservePacket.Count > 0)
            {
                packet = _reservePacket;
                _reservePacket = default;
            }
            else if (_sendQueue.Count > 0) packet = _sendQueue.Dequeue();
            else break;

            int packetSize = packet.Count;
            int availableSize = MAX_SEND_SIZE - totalPacketSize;

            if (packetSize > availableSize)
            {
                _pendingList.Add(new ArraySegment<byte>(packet.Array, packet.Offset, availableSize));

                _reservePacket = new ArraySegment<byte>(packet.Array, packet.Offset + availableSize, packetSize - availableSize);

                totalPacketSize += availableSize;
                break;
            }
            else
            {
                _pendingList.Add(packet);
                totalPacketSize += packetSize;
            }
        }

        _sendArgs.BufferList = _pendingList;
        //Console.WriteLine($"[Debug] RegisterSend: Sending {_pendingList.Count} packets combined."); // 로그 추가

        bool pending = _socket.SendAsync(_sendArgs);
        if (!pending)
        {
            ProcessSend(_sendArgs);
        }
    }*/
    /*private void RegisterSend()
    {
        int currentTotalSize = 0;

        //while (_sendQueue.Count > 0)
        while (currentTotalSize < MAX_SEND_SIZE)
        {
            ArraySegment<byte> packet = _sendQueue.Peek();
            int remainPacket = packet.Count;

            if(remainPacket > MAX_SEND_SIZE - currentTotalSize)
            {
                if (currentTotalSize > 0) break;

                int sendSize = MAX_SEND_SIZE;

                _pendingList.Add(new ArraySegment<byte>(packet.Array, packet.Offset, sendSize));
                _sendQueue.Dequeue();
                _sendQueue.Enqueue(new ArraySegment<byte>(packet.Array, packet.Offset + sendSize, remainPacket - sendSize));
                break;
            }

            _pendingList.Add(_sendQueue.Dequeue());
            currentTotalSize += packet.Count;
        }
        _sendArgs.BufferList = _pendingList;
        //Console.WriteLine($"[Debug] RegisterSend: Sending {_pendingList.Count} packets combined."); // 로그 추가

        bool pending = _socket.SendAsync(_sendArgs);
        if (!pending)
        {
            ProcessSend(_sendArgs);
        }
    }*/

    private void ProcessSend(SocketAsyncEventArgs args)
    {
        if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
        {
            RegisterSend();
        }
        else
        {
            _ = DisConnect();
        }
    }
    /*private void ProcessSend(SocketAsyncEventArgs args)
    { 
        lock (_lock)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                //Console.WriteLine($"[Debug] ProcessSend: Sent {args.BytesTransferred} bytes successfully.");
                _sendArgs.BufferList = null;
                _pendingList.Clear();
                if (_sendQueue.Count > 0)
                {
                    RegisterSend();
                }
            }
            else
            {
                //Console.WriteLine($"[Error] ProcessSend Failed: Error={args.SocketError}"); // 에러 확인
                _= DisConnect();
            }
        }
    }*/

    public async Task<bool> DisConnect()
    {
        if (Interlocked.Exchange(ref _isDisconnected, 1) == 1) { return false; }

        bool isLeaveSuccess = true;
        if (MyPlayer != null)
        {
            int playerId = MyPlayer.CharacterId;

            var tcs = new TaskCompletionSource<bool>();

            _gameLogicThread.Enqueue(async () =>
            {
                try
                {
                    bool result = await PlayerManager.Instance.Leave(playerId);
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    Log.Error($"[UserSession] Leave 처리 중 에러: {ex.Message}");
                    tcs.SetResult(false);
                }
            });
            isLeaveSuccess = await tcs.Task;
        }

        //데이터 저장이 제대로 안 됐을 때를 대비해 일단 남겨둠
        //해당 플레이어 데이터를 열람해서 확인 할 수 있는 기능을 만들어
        //확인 후 문제 없다면 수동 업데이트 후 강제 로그아웃
        if (isLeaveSuccess)
        {
            MyPlayer = null;
            LoginManager.Instance.OnLogout(AccountId);
            SessionManager.Instance.Remove(this.SessionId);
        }
        else
        {
            Log.Fatal($"[UserSession] 로그아웃 중 치명적인 문제 발생 characterId {MyPlayer.CharacterId}");
        }

        try
        {
            if (_socket != null)
            {
                //Console.WriteLine("shutdown!");
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
            }
        }
        catch { }

        var arrayToReturn = _recvBuffer.UnderlyingArray;
        if (arrayToReturn != null)
        {
            await Task.Delay(5000).ContinueWith(_ => ArrayPool<byte>.Shared.Return(arrayToReturn));
        }

        return isLeaveSuccess;
    }
}