using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
// 서버 프로젝트의 네임스페이스를 참조하세요 (예: Server)

namespace UnitTestProject1
{
    public class DummyClient
    {
        private TcpClient _client;
        private NetworkStream _stream;

        // 특정 패킷이 올 때까지 기다리기 위한 장부
        private Dictionary<ushort, TaskCompletionSource<string>> _pendingPackets = new Dictionary<ushort, TaskCompletionSource<string>>();

        public async Task<bool> ConnectAsync(string ip, int port)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ip, port);
                _stream = _client.GetStream();

                // 수신 루프 시작
                _ = Task.Run(ReceiveLoop);
                return true;
            }
            catch { return false; }
        }

        // 서버에 패킷을 보낼 때 서버의 PacketSerializer를 사용합니다.
        public void Send(ArraySegment<byte> buffer)
        {
            _stream?.Write(buffer.Array, buffer.Offset, buffer.Count);
        }

        public async Task SendAsync(ArraySegment<byte> buffer)
        {
            if (_stream == null || !_stream.CanWrite) return;

            try
            {
                // WriteAsync를 사용하면 데이터 전송을 OS에 맡기고 
                // 스레드는 즉시 다음 작업을 하러 갈 수 있습니다.
                await _stream.WriteAsync(buffer.Array, buffer.Offset, buffer.Count);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Send Error: {ex.Message}");
            }
        }

        // 특정 패킷 ID가 올 때까지 비동기로 대기
        public Task<string> WaitForPacket(ushort packetId)
        {
            var tcs = new TaskCompletionSource<string>();
            _pendingPackets[packetId] = tcs;
            return tcs.Task;
        }

        public void Disconnect()
        {
            try
            {
                // 스트림을 먼저 닫아서 ReceiveLoop의 ReadAsync를 종료시킴
                _stream?.Close();
                _client?.Close();
            }
            catch { /* 종료 시 발생하는 예외는 무시 */ }
            finally
            {
                _stream = null;
                _client = null;
            }
        }

        private async Task ReceiveLoop()
        {
            try
            {
                // _client null 체크를 통해 안전하게 시작
                while (_client != null && _client.Connected)
                {
                    byte[] headerBuffer = new byte[4];
                    int read = 0;

                    // 1. 헤더(4바이트) 읽기
                    while (read < 4)
                    {
                        // 스트림이 닫혔는지 수시로 확인
                        if (_stream == null) return;

                        int n = await _stream.ReadAsync(headerBuffer, read, 4 - read);
                        if (n <= 0) return; // 서버가 정상적으로 연결을 끊음
                        read += n;
                    }

                    ushort size = BitConverter.ToUInt16(headerBuffer, 0);
                    ushort id = BitConverter.ToUInt16(headerBuffer, 2);

                    // 헤더에 적힌 size가 4보다 작으면 잘못된 패킷이므로 방어
                    if (size < 4) continue;

                    // 2. 바디(size - 4) 읽기
                    byte[] bodyBuffer = new byte[size - 4];
                    read = 0;
                    while (read < bodyBuffer.Length)
                    {
                        if (_stream == null) return;

                        int n = await _stream.ReadAsync(bodyBuffer, read, bodyBuffer.Length - read);
                        if (n <= 0) return;
                        read += n;
                    }

                    string json = Encoding.UTF8.GetString(bodyBuffer);

                    if (_pendingPackets.TryGetValue(id, out var tcs))
                    {
                        // tcs가 이미 완료되었을 수도 있으므로 TrySetResult 사용
                        tcs.TrySetResult(json);
                        _pendingPackets.Remove(id);
                    }
                }
            }
            // Disconnect() 호출 시 stream이나 client가 null/disposed 되어 발생하는 예외 무시
            catch (Exception ex) when (ex is ObjectDisposedException || ex is NullReferenceException || ex is System.IO.IOException)
            {
                Debug.WriteLine("연결이 종료되어 수신 루프를 마칩니다.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"수신 루프 예상치 못한 에러: {ex.Message}");
            }
        }
    }
}