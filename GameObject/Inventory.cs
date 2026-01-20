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

    public Dictionary<int, Item> ItemList { get; set; } = new Dictionary<int, Item>();

    private object _lock = new object();

    public void InitInventory(List<ItemInfo> items)
    {
        lock (_lock)
        {
            ItemList = items.Select(item => new Item
            {
                InventoryId = item.InventoryId,
                OwnerId = item.OwnerId,
                ItemId = item.ItemId,
                Count = item.Count,
                IsEquipped = item.IsEquipped,
                Enhancement = item.Enhancement
            }).ToDictionary(item => item.InventoryId);
        }
    }

    public void AddItem(Item newItem)
    {
        lock (_lock)
        {
            ItemList.Add(newItem.InventoryId, newItem);
        }
    }

    public void RemoveItem(int inventoryId)
    {
        lock (_lock)
        {
            ItemList.Remove(inventoryId);
        }
    }

    public async void UseItem(UserSession session, int inventoryId)
    {
        Item item = null;
        lock (_lock)
        {
            if (!ItemList.TryGetValue(inventoryId, out item) || item.Count <= 0)
            {
                var sendBuff = PacketMaker.Instance.MakeUseItemBuffer(inventoryId, false);
                _mySession.Send(sendBuff);
                return;
            }

            item.Count--;
            if(item.Count == 0) ItemList.Remove(inventoryId);
        }

        try
        {
            //db 갱신은 어느 타이밍에 할 것인지 고민 필요. 모아서 하면 좋을 듯 한데 지금은 바로 업데이트
            int result = await DbManager.DecreaseIntemCount(session.MyPlayer.CharacterId, inventoryId, 1);
            if (result <= 0) throw new Exception("DB Update Failed");

            var sendBuff = PacketMaker.Instance.MakeUseItemBuffer(inventoryId, true);
            _mySession.Send(sendBuff);
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                if(ItemList.ContainsKey(inventoryId))
                    ItemList[inventoryId].Count++;
                else
                    ItemList.Add(inventoryId, item);
            }
            var sendBuff = PacketMaker.Instance.MakeUseItemBuffer(inventoryId, false);
            _mySession.Send(sendBuff);
            Console.WriteLine($"UseItem Error: {ex.Message}");
        }
    }

    public async void DropItem(int inventoryId, int mapId)
    {
        Item originalItem = null;
        lock (_lock)
        {
            if (!ItemList.TryGetValue(inventoryId, out originalItem) || originalItem.Count <= 0)
            {
                var sendBuff = PacketMaker.Instance.MakeDropItemBuffer(null, mapId, false);
                _mySession.Send(sendBuff);
                return;
            }

            originalItem.Count--;
            if (originalItem.Count == 0) ItemList.Remove(inventoryId);
        }

        Item copyItem = new Item
        {
            OwnerId = -1,
            ItemId = ItemList[inventoryId].ItemId,
            Count = 1,
            IsEquipped = false,
            Enhancement = ItemList[inventoryId].Enhancement
        };

        try
        {
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

            Map map = WorldManager.Instance.GetMap(mapId);
            copyItem.InventoryId = newInventoryId;
            //map.AddDropItem(copyItem);

            ArraySegment<byte> sendBuff = PacketMaker.Instance.MakeDropItemBuffer(copyItem, map.MapId, true);
            _mySession.Send(sendBuff);

            map.BroadcastSpwanItem(copyItem);
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                //롤백
                if (ItemList.ContainsKey(inventoryId))
                    ItemList[inventoryId].Count++;
                else
                    ItemList.Add(inventoryId, originalItem);
            }
            var sendBuff = PacketMaker.Instance.MakeDropItemBuffer(null, mapId, false);
            _mySession.Send(sendBuff);
            Console.WriteLine($"DropItem Error: {ex.Message}");
        }
    }

    //추가 개선점
    //0. 예외 상황 처리
    //1. 인벤토리 접근 락
    //2. 수량을 먼저 줄이고 db처리(아이템 중복 사용 위험) => 사용 실패시 수량 복구
    /*public async Task DropItem(int inventoryId, int mapId)
    {
        //플레이어 인벤토리에서 아이템 1개 복제 후 제거
        //1. 인벤토리에서 아이템 복제
        Item copyItem = new Item
        {
            //InventoryId = -9999, //db에서 할당 받아서 만들어야하는데 어떻게 하는게 좋을까
            OwnerId = -1,
            ItemId = ItemList[inventoryId].ItemId,
            Count = 1,
            IsEquipped = false,
            Enhancement = ItemList[inventoryId].Enhancement
        };

        //2. DB Insert용 Dto 생성 후 DB에 아이템 추가
        InventoryDto inventoryDto = new InventoryDto
        {
            owner_id = copyItem.OwnerId,
            item_id = copyItem.ItemId,
            count = copyItem.Count,
            is_equipped = copyItem.IsEquipped ? 1 : 0,
            enhancement = copyItem.Enhancement
        };
        int newInventoryId = await DbManager.InsertItem(inventoryDto);

        if (newInventoryId > 0)
        {
            //3. 해당 세션 인벤토리에서 아이템 개수 감소 및 제거
            ItemList[inventoryId].Count--;
            if (ItemList[inventoryId].Count == 0)
            {
                RemoveItem(inventoryId);
            }

            //4. newInventoryId 설정 후 맵에 copyItem 추가(드랍템)
            Map map = WorldManager.Instance.GetMap(mapId);
            copyItem.InventoryId = newInventoryId;
            map.AddDropItem(copyItem);

            //5. 클라이언트에 드랍 응답
            //send. session이 필요
            //Network.Send(useItemResponse); 클라이언트와 다르게 network 하나만 있지 않기 때문에 session으로 처리해야 할 듯 함
            //5-1. 아이템을 버린 세션유저에게는 성공 응답(세션정보, 아이템정보, 성공여부, 맵아이디)
            //PacketSender.Instance.DropItemResponse(_mySession, copyItem, map.MapId, true); 폐기
            ArraySegment<byte> sendBuff = PacketMaker.Instance.MakeDropItemBuffer(copyItem, map.MapId, true);
            _mySession.Send(sendBuff);
            //5-2. 같은 맵 플레이어에게 브로드 캐스트
            map.BroadcastSpwanItem(copyItem);
        }
    }*/
}

public class Item //인벤토리 내 아이템 정보
{
    public int InventoryId { get; set; }
    public int OwnerId { get; set; }
    public int ItemId { get; set; }
    public int Count { get; set; }
    public bool IsEquipped { get; set; }
    public int Enhancement { get; set; }

    //public ItemData Data => DataManager.Instance.GetItemData(ItemId);
}