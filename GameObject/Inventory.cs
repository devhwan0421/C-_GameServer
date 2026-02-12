using Serilog;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

public class Inventory
{
    private Player _owner;
    //private readonly UserSession _mySession;

    //public Inventory(UserSession mySession) => _mySession = mySession;
    public Inventory(Player owner)
    {
        _owner = owner;
    }

    public bool IsDirty { get; private set; }
    private HashSet<int> _dirtyInventoryIds = new HashSet<int>();

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

    private void SetDirty(int inventoryId)
    {
        IsDirty = true;
        _dirtyInventoryIds.Add(inventoryId);
    }

    //수량이 0인 것은 디비 제거로직 추가할 것
    public List<InventoryDto> GetDirtyInventory()
    {
        var dirtyList = new List<InventoryDto>();
        foreach (var inventoryId in _dirtyInventoryIds)
        {
            if(ItemList.TryGetValue(inventoryId, out var item))
            {
                dirtyList.Add(new InventoryDto
                {
                    id = inventoryId,
                    owner_id = item.OwnerId,
                    item_id = item.ItemId,
                    count = item.Count,
                    is_equipped = item.IsEquipped ? 1 : 0,
                    enhancement = item.Enhancement
                });
            }
        }
        return dirtyList;
    }

    public void ClearDirty()
    {
        IsDirty = false;
        _dirtyInventoryIds.Clear();
    }

    public void RestoreDirty(List<InventoryDto> inventoryDtos)
    {
        if (inventoryDtos == null) return;

        foreach (var inventoryDto in inventoryDtos)
        {
            if (!_dirtyInventoryIds.Contains(inventoryDto.id))
            {
                _dirtyInventoryIds.Add(inventoryDto.id);
            }
        }
        IsDirty = true;
    }

    public void AddItem(Item newItem)
    {
        //Console.WriteLine($"인벤토리 목록에 아이템 추가됨 id: {newItem.InventoryId}");
        ItemList.Add(newItem.InventoryId, newItem);
        SetDirty(newItem.InventoryId);
    }

    /*public void RemoveItem(int inventoryId)
    {
        ItemList.Remove(inventoryId);
    }*/

    public int UseItem(int inventoryId)
    {
        using (LogContext.PushProperty("Event", "UseItem"))
        using (LogContext.PushProperty("InventoryId", inventoryId))
        {
            Log.Information("[UseItem] 아이템 사용");

            //1. 해당 아이템이 내 인벤토리에 존재하는지 체크
            if (!ItemList.TryGetValue(inventoryId, out var item) || item.Count <= 0)
            {
                Log.Error("[UseItem] 존재하지 않는 아이템 사용");
                return -1;
            }

            //2. 아이템 수량 감소
            item.Count--;
            Log.Information("[UseItem] 아이템 수량 감소 ItemId: {ItemId}, 남은수량: {count}", item.ItemId, item.Count);

            //3. DB 업데이트 예약
            SetDirty(inventoryId);
            Log.Information("[UseItem] InventoryId: {InventoryId} DB 업데이트 예약", inventoryId);

            //4. 아이템 사용효과 적용
            ItemTemplate itemTemplate = DataManager.Instance.Item.GetItemData(item.ItemId);
            if (itemTemplate != null && itemTemplate.Type == 0)
            {
                _owner.HealHp(itemTemplate.HealAmount);
                Log.Information("[UseItem] 아이템 사용 효과 적용 ItemId: {ItemId}, ItemType: {ItemType}", itemTemplate.ItemId, itemTemplate.Type);
            }

            //5. UseItem 요청 유저에게 응답(클라이언트 인벤토리 업데이트)
            var sendBuff = PacketMaker.Instance.MakeUseItemBuffer(inventoryId, true, itemTemplate.Type, itemTemplate.HealAmount);
            _owner._mySession.Send(sendBuff);
            Log.Information("[UseItem] 요청 세션에 응답 전송");

            return item.ItemId;
        }
    }
    /*public async void UseItem(UserSession session, int inventoryId)
    {
        Console.WriteLine($"아이템 사용 요청 들어옴 id: {inventoryId}");
        Item item = null;
        if (!ItemList.TryGetValue(inventoryId, out item) || item.Count <= 0)
        {
            var sendBuff = PacketMaker.Instance.MakeUseItemBuffer(inventoryId, false, -1, -1);
            _mySession.Send(sendBuff);
            Log.Error($"[Inventory] 존재하지 않는 아이템을 사용했습니다 characterId: {session.MyPlayer.CharacterId}, inventoryId: {inventoryId}");
            return;
        }

        item.Count--;
        //SetDirty(inventoryId);
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
    }*/

    public async Task<int> DropItem(DropItemRequest req)
    {
        Log.Information($"[Inventory] 아이템 드랍 characterId {_owner.CharacterId}, inventoryId {req.InventoryId}, mapId {req.MapId}");

        if (!ItemList.TryGetValue(req.InventoryId, out var originalItem) || originalItem.Count <= 0)
        {
            Log.Information($"[Inventory] 존재하지 않는 아이템을 드랍했습니다 characterId {_owner.CharacterId}, inventoryId {req.InventoryId}, mapId {req.MapId}");
            return -1;
        }

        //1. 드랍아이템으로 쓸 아이템 복제본 생성
        Item copyItem = new Item
        {
            OwnerId = -1,
            ItemId = ItemList[req.InventoryId].ItemId,
            Count = 1,
            IsEquipped = false,
            Enhancement = ItemList[req.InventoryId].Enhancement,
            PosX = req.PosX,
            PosY = req.PosY,
            PosZ = req.PosZ
        };

        //2. 원본 아이템 수량 감소
        originalItem.Count--;

        try
        {
            //3. DB에 드랍 아이템 추가
            int newInventoryId = await DbManager.InsertItem(new InventoryDto(copyItem));
            if (newInventoryId <= 0) 
                throw new Exception("드랍아이템 DB 생성 실패 {_owner.CharacterId}, inventoryId {_owner.Inventory}");
            
            Log.Information($"[Inventory] DB에 드랍아이템 추가 완료 inventoryId {newInventoryId}");

            //4. 맵의 드랍아이템 리스트에 아이템 추가
            Map map = MapManager.Instance.GetMap(req.MapId);
            copyItem.InventoryId = newInventoryId;
            map.AddDropItem(copyItem);
            Log.Information($"[Inventory] 맵에 드랍아이템 추가 완료 inventoryId {copyItem.InventoryId}");

            //5. DB 업데이트 예약
            SetDirty(req.InventoryId);

            //6. DropItem 요청 유저에게 응답(드랍 아이템 스폰 및 클라이언트 인벤토리 업데이트)
            ArraySegment<byte> sendBuff = PacketMaker.Instance.MakeDropItemBuffer(copyItem, map.MapId, true, req.InventoryId, req.PosX, req.PosY, req.PosZ);
            _owner._mySession.Send(sendBuff);

            //7. 본인 제외 해당 맵 전체 유저에게 아이템 스폰 브로드캐스트
            map.BroadcastSpwanItem(copyItem, _owner.CharacterId);

            return copyItem.ItemId;
        }
        catch (Exception ex)
        {
            Log.Error($"[Inventory] {ex.Message}");

            //원본 아이템 수량 롤백
            originalItem.Count++;

            return -1;
        }
    }
    /*public async void DropItem(UserSession session, int inventoryId, int mapId, float posX, float posY, float posZ)
    {
        Log.Information($"[Inventory] 아이템 드랍 characterId {session.MyPlayer.CharacterId}, inventoryId {inventoryId}, mapId {mapId}");

        Item originalItem = null;
        Item copyItem = null;
        if (!ItemList.TryGetValue(inventoryId, out originalItem) || originalItem.Count <= 0)
        {
            Log.Information($"[Inventory] 존재하지 않는 아이템을 드랍했습니다 characterId {session.MyPlayer.CharacterId}, inventoryId {inventoryId}, mapId {mapId}");
            *//*var sendBuff = PacketMaker.Instance.MakeDropItemBuffer(null, mapId, false, inventoryId, 0, 0, 0);
            _owner._mySession.Send(sendBuff);*//*
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
            _owner._mySession.Send(sendBuff);

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
            _owner._mySession.Send(sendBuff);
            Console.WriteLine($"DropItem Error: {ex.Message}");
        }
    }*/

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