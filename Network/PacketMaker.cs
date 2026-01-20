using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class PacketMaker
{
    public static PacketMaker Instance { get; } = new PacketMaker();

    public ArraySegment<byte> MakeUseItemBuffer(int inventoryId, bool success)
    {
        var packet = new UseItemResponse
        {
            InventoryId = inventoryId,
            Success = success
        };
        string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }

    public ArraySegment<byte>MakeDropItemBuffer(Item copyItem, int mapId, bool success)
    {
        var packet = new DropItemResponse
        {
            Item = new ItemInfo
            {
                InventoryId = copyItem.InventoryId,
                OwnerId = copyItem.OwnerId,
                ItemId = copyItem.ItemId,
                Count = copyItem.Count,
                IsEquipped = copyItem.IsEquipped,
                Enhancement = copyItem.Enhancement
            },
            MapId = mapId,
            Success = success
        };
        string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }

    public ArraySegment<byte> MakeSpawnItemBuffer(Item copyItem, int mapId)
    {
        var packet = new SpawnItemResponse
        {
            Item = new ItemInfo
            {
                InventoryId = copyItem.InventoryId,
                OwnerId = copyItem.OwnerId,
                ItemId = copyItem.ItemId,
                Count = copyItem.Count,
                IsEquipped = copyItem.IsEquipped,
                Enhancement = copyItem.Enhancement
            },
            MapId = mapId
        };

        string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }
}