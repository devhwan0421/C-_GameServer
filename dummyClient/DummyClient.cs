using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class DummyClient
{
    private TcpClient _client;
    private NetworkStream _stream;

    private Dictionary<ushort, TaskCompletionSource<string>> _pendingPackets = new Dictionary<ushort, TaskCompletionSource<string>>();

    public async Task<bool> ConnectAsync(string ip, int port)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(ip, port);
            _stream = _client.GetStream();

            _ = Task.Run(ReceiveLoop);
            return true;
        }
        catch { return false; }
    }

    // 서버에 패킷을 보낼 때 서버의 PacketSerializer를 사용
    public void Send(ArraySegment<byte> buffer)
    {
        _stream?.Write(buffer.Array, buffer.Offset, buffer.Count);
    }

    public async Task SendAsync(ArraySegment<byte> buffer)
    {
        if (_stream == null || !_stream.CanWrite) return;

        try
        {
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
            _stream?.Close();
            _client?.Close();
        }
        catch { }
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
            while (_client != null && _client.Connected)
            {
                byte[] headerBuffer = new byte[4];
                int read = 0;

                while (read < 4)
                {
                    if (_stream == null) return;

                    int n = await _stream.ReadAsync(headerBuffer, read, 4 - read);
                    if (n <= 0) return; // 서버가 정상적으로 연결을 끊음
                    read += n;
                }

                ushort size = BitConverter.ToUInt16(headerBuffer, 0);
                ushort id = BitConverter.ToUInt16(headerBuffer, 2);

                if (size < 4) continue;

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
