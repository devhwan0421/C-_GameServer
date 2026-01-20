using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//폐기. 역할이 애매해짐
public class PacketSender
{
    public static PacketSender Instance { get; } = new PacketSender();

    /*public void UseItemResponse(UserSession session, int inventoryId, bool success)
    {
        var useItemResponse = new UseItemResponse
        {
            InventoryId = inventoryId,
            Success = success
        };
        session.Send(useItemResponse);
    }

    public void DropItemResponse(UserSession session, Item copyItem, int mapId, bool success)
    {
        var dropItemResponse = new DropItemResponse
        {
            Item = new ItemInfo{
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
        session.Send(dropItemResponse);
    }*/

    /*public void SpawnItemResponse(UserSession session, Item copyItem)
    {
        var spawnItemResponse = new SpawnItemResponse
        {
            Item = new ItemInfo
            {
                InventoryId = copyItem.InventoryId,
                OwnerId = copyItem.OwnerId,
                ItemId = copyItem.ItemId,
                Count = copyItem.Count,
                IsEquipped = copyItem.IsEquipped,
                Enhancement = copyItem.Enhancement
            }
        };
        session.Send(spawnItemResponse);
    }*/
}