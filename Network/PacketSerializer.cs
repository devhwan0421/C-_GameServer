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
    public static ThreadLocal<byte[]> SendBuffer = new ThreadLocal<byte[]>(() => new byte[128 * 1024]);

    public static ArraySegment<byte> Serialize(ushort id, string json)
    {
        byte[] buffer = SendBuffer.Value;
        int bodySize = Encoding.UTF8.GetBytes(json, 0, json.Length, buffer, 4);
        ushort totalSize = (ushort)(bodySize + 4);

        BinaryPrimitives.WriteUInt16LittleEndian(new Span<byte>(buffer, 0, 2), totalSize);
        BinaryPrimitives.WriteUInt16LittleEndian(new Span<byte>(buffer, 2, 2), id);

        byte[] packetData = new byte[totalSize];
        Buffer.BlockCopy(buffer, 0, packetData, 0, totalSize);
        
        if(id != 12 && id != 28 && id != 46)
            Console.WriteLine($"[Debug] Serialized Packet: ID={id}, Size={totalSize}, JSON={json}"); // 로그 추가

        return new ArraySegment<byte>(packetData);
    }
}