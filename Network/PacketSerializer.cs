using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public static class PacketSerializer
{
    //스레드별 개별 작업대 (Lock 최소화)
    public static ThreadLocal<byte[]> SendBuffer = new ThreadLocal<byte[]>(() => new byte[65536]);

    public static ArraySegment<byte> Serialize(ushort id, string json)
    {
        byte[] body = Encoding.UTF8.GetBytes(json);
        ushort size = (ushort)(body.Length + 4);
        byte[] buffer = SendBuffer.Value;

        //헤더 작성
        //buffer[0] = (byte)size; buffer[1] = (byte)(size >> 8);
        //BitConverter.TryWriteBytes(new Span<byte>(buffer, 0, 2), size);
        BinaryPrimitives.WriteUInt16LittleEndian(new Span<byte>(buffer, 0, 2), size);
        //BitConverter.TryWriteBytes(new Span<byte>(buffer, 2, 2), id);
        BinaryPrimitives.WriteUInt16LittleEndian(new Span<byte>(buffer, 2, 2), id);

        //바디 복사
        Array.Copy(body, 0, buffer, 4, body.Length); //4는 헤더 크기. 4바이트 뒤에 바디 복사

        //실제 데이터가 담긴 영역만 잘라서 반환(참조 전달)
        byte[] packetData = new byte[size];
        Array.Copy(buffer, 0, packetData, 0, size);
        return new ArraySegment<byte>(packetData);
    }
}