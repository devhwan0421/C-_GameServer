using Google.Protobuf;
using Protocol;
using System;
using System.Buffers.Binary;
using System.Text;
using System.Threading;

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

        if (id != 12 && id != 28 && id != 46)
            Console.WriteLine($"[Debug] Serialized Packet: ID={id}, Size={totalSize}, JSON={json}"); // 로그 추가

        return new ArraySegment<byte>(packetData);
    }

    public static ArraySegment<byte> SerializeProto(ushort id, IMessage packet)
    {
        byte[] buffer = SendBuffer.Value;
        int bodySize = packet.CalculateSize();
        packet.WriteTo(new Span<byte>(buffer, 4, bodySize));

        ushort totalSize = (ushort)(bodySize + 4);

        BinaryPrimitives.WriteUInt16LittleEndian(new Span<byte>(buffer, 0, 2), totalSize);
        BinaryPrimitives.WriteUInt16LittleEndian(new Span<byte>(buffer, 2, 2), id);

        byte[] packetData = new byte[totalSize];
        Buffer.BlockCopy(buffer, 0, packetData, 0, totalSize);

        /*if (id != 12 && id != 28 && id != 46 && id != 901)
            Console.WriteLine($"[Debug] Serialized Packet: ID={id}, Size={totalSize}, Data={packet.ToString()}"); // 로그 추가

        if (id == 46)
            Console.WriteLine($"[MovePacketSend] {packet.ToString()} **ServerTime:{DateTime.UtcNow.Ticks}");*/

        return new ArraySegment<byte>(packetData);
    }
}