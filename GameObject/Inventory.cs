using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

public class Inventory
{
    private readonly UserSession _mySession;

    public Inventory(UserSession mySession) => _mySession = mySession;

    //Dictionary<inventoryId, Dictionary<itemId, Item>> 형식이 더 좋을 듯
    public Dictionary<int, Item> ItemList { get; set; } = new Dictionary<int, Item>();

    public void InitInventory(List<InventoryDto> inventory)
    {
        ItemList = inventory.Select(item => new Item
        {
            InventoryId = item.id,
            OwnerId = item.owner_id,
            ItemId = item.item_id,
            Count = item.count,
            IsEquipped = (item.is_equipped == 1),
            Enhancement = item.enhancement
        }).ToDictionary(item => item.InventoryId);
    }

    public void AddItem(Item newItem)
    {
        Console.WriteLine($"인벤토리 목록에 아이템 추가됨 id: {newItem.InventoryId}");
        ItemList.Add(newItem.InventoryId, newItem);
    }

    public void RemoveItem(int inventoryId)
    {
        ItemList.Remove(inventoryId);
    }

    public async void UseItem(UserSession session, int inventoryId)
    {
        Console.WriteLine($"아이템 사용 요청 들어옴 id: {inventoryId}");
        Item item = null;
        if (!ItemList.TryGetValue(inventoryId, out item) || item.Count <= 0)
        {
            var sendBuff = PacketMaker.Instance.MakeUseItemBuffer(inventoryId, false, -1, -1);
            _mySession.Send(sendBuff);
            Console.WriteLine("아이템이 없거나 수량이 없음");
            return;
        }

        item.Count--;
        if (item.Count == 0) ItemList.Remove(inventoryId);

        try
        {
            Console.WriteLine($"session.MyPlayer.CharacterId: {session.MyPlayer.CharacterId}, inventoryId: {inventoryId}, item.ItemId: {item.ItemId}");
            //db 갱신은 어느 타이밍에 할 것인지 고민 필요. 모아서 하면 좋을 듯 한데 지금은 바로 업데이트
            int result = await DbManager.DecreaseIntemCount(session.MyPlayer.CharacterId, inventoryId, 1);
            if (result <= 0) throw new Exception("DB Update Failed");

            Console.WriteLine("result: " + result);

            //ItemData itemData = DataManager.Instance.GetItemData(item.ItemId);
            ItemTemplate itemTemplate = DataManager.Instance.Item.GetItemData(item.ItemId);
            if (itemTemplate != null)
            {
                if (itemTemplate.Type == 0)
                {
                    Console.WriteLine("힐템 사용");

                    if (_mySession.MyPlayer.Hp + itemTemplate.HealAmount > _mySession.MyPlayer.MaxHp)
                    {
                        _mySession.MyPlayer.Hp += _mySession.MyPlayer.MaxHp;
                    }
                    else
                    {
                        _mySession.MyPlayer.Hp += itemTemplate.HealAmount;
                    }
                }
            }

            var sendBuff = PacketMaker.Instance.MakeUseItemBuffer(inventoryId, true, itemTemplate.Type, itemTemplate.HealAmount);
            _mySession.Send(sendBuff);
        }
        catch (Exception ex)
        {
            if (ItemList.ContainsKey(inventoryId))
                ItemList[inventoryId].Count++;
            else
                ItemList.Add(inventoryId, item);
            var sendBuff = PacketMaker.Instance.MakeUseItemBuffer(inventoryId, false, -1, -1);
            _mySession.Send(sendBuff);
            Console.WriteLine($"UseItem Error: {ex.Message}");
        }
    }

    public async void DropItem(UserSession session, int inventoryId, int mapId, float posX, float posY, float posZ)
    {
        Item originalItem = null;
        Item copyItem = null;
        if (!ItemList.TryGetValue(inventoryId, out originalItem) || originalItem.Count <= 0)
        {
            var sendBuff = PacketMaker.Instance.MakeDropItemBuffer(null, mapId, false, inventoryId, 0, 0, 0);
            _mySession.Send(sendBuff);
            return;
        }

        copyItem = new Item
        {
            OwnerId = -1,
            ItemId = ItemList[inventoryId].ItemId,
            Count = 1,
            IsEquipped = false,
            Enhancement = ItemList[inventoryId].Enhancement,
            PosX = posX,
            PosY = posY,
            PosZ = posZ
        };

        originalItem.Count--;
        //퀘스트 상태에 반영
        session.MyPlayer.QuestComponent.OnNotifyEvent(2, originalItem.ItemId, -1);
        if (originalItem.Count == 0) ItemList.Remove(inventoryId);

        try
        {
            Console.WriteLine($"charId: {session.MyPlayer.CharacterId}, inventoryId: {inventoryId}");
            //기존 아이템 수량 변경 쿼리
            int result = await DbManager.DecreaseIntemCount(session.MyPlayer.CharacterId, inventoryId, 1);
            if (result <= 0) throw new Exception("DB Update Failed");

            Console.WriteLine("result: " + result);

            InventoryDto inventoryDto = new InventoryDto
            {
                owner_id = copyItem.OwnerId,
                item_id = copyItem.ItemId,
                count = copyItem.Count,
                is_equipped = copyItem.IsEquipped ? 1 : 0,
                enhancement = copyItem.Enhancement
            };
            int newInventoryId = await DbManager.InsertItem(inventoryDto);
            if (newInventoryId <= 0) throw new Exception("DB Insert Failed");

            Console.WriteLine("newInventoryId: " + newInventoryId);

            Map map = MapManager.Instance.GetMap(mapId);
            copyItem.InventoryId = newInventoryId;
            map.AddDropItem(copyItem);

            Console.WriteLine("카피아이템 전송 true");
            ArraySegment<byte> sendBuff = PacketMaker.Instance.MakeDropItemBuffer(copyItem, map.MapId, true, inventoryId, posX, posY, posZ);
            _mySession.Send(sendBuff);

            map.BroadcastSpwanItem(copyItem, session.MyPlayer.CharacterId);
        }
        catch (Exception ex)
        {
            //롤백
            if (ItemList.ContainsKey(inventoryId))
                ItemList[inventoryId].Count++;
            else
                ItemList.Add(inventoryId, originalItem);
            var sendBuff = PacketMaker.Instance.MakeDropItemBuffer(null, mapId, false, inventoryId, 0, 0, 0);
            _mySession.Send(sendBuff);
            Console.WriteLine($"DropItem Error: {ex.Message}");
        }
    }

    public int getItemCount(int itemId)
    {
        int count = 0;
        foreach (var item in ItemList.Values)
        {
            if(item.ItemId == itemId)
            {
                count += item.Count;
            }
        }
        return count;
    }
}

public class Item //인벤토리 내 아이템 정보
{
    public int InventoryId { get; set; }
    public int OwnerId { get; set; }
    public int ItemId { get; set; }
    public int Count { get; set; }
    public bool IsEquipped { get; set; }
    public int Enhancement { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ {  get; set; }

    //public ItemData Data => DataManager.Instance.GetItemData(ItemId);
}