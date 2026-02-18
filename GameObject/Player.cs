using Serilog;
using Serilog.Context;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;
using Google.Protobuf;
using Protocol;

public class Player
{
    public readonly UserSession _mySession;

    //public CharacterInfo

    //DB
    //public bool IsDirty = false; //플레이어 데이터에 변화가 있었는 지 체크
    public bool PlayerInfoIsDirty = true;
    private float _saveInterval = 0;
    private float _saveTimer = 0;

    //플레이어 변경사항이 있을 경우 호출
    //public void SetDirty() => IsDirty = true;

    public void SetDbUpdateTimer(float randTime)
    {
        //PlayerManager에서 랜덤값을 받아(매번 new 연산 x) 설정
        _saveInterval = randTime;
        _saveTimer = 0;
    }

    public void Update(float deltaTime)
    {
        _saveTimer += deltaTime;

        if (_saveTimer >= _saveInterval)
        {
            _= DbUpdate();

            _saveTimer = 0;
        }
    }

    public async Task<bool> ImmediateDbUpdate()
    {
        bool result = await DbUpdate();
        if (!result)
        {
            //DB저장 재시도
            result = await DbUpdate();
            if (!result)
            {
                return false;
            }
        }
        return true;
    }

    private async Task<bool> DbUpdate()
    {
        Log.Information("[Player] 플레이어 DB 업데이트 수행");
        //인벤토리, 퀘스트 ,플레이어 정보 업데이트.
        //셋 중 변경사항이 없었던 정보는 업데이트하지 않음
        CharacterDto characterDto = null;
        List<InventoryDto> inventoryDtos = null;
        QuestDtoSet questDto = null;

        //PlayerInfoIsDirty = true; //임시 활성화
        //이제 이동시 플래그 활성화 됨

        if (PlayerInfoIsDirty)
        {
            characterDto = CharacterInfoToDto();
            PlayerInfoClearDirty();
        }
        
        //아이템 수량이 0일 경우 DB에서 삭제하는 로직 추가할 것
        if (Inventory.IsDirty)
        {
            inventoryDtos = Inventory.GetDirtyInventory();
            Inventory.ClearDirty();
        }

        if (QuestComponent.IsDirty)
        {
            questDto = QuestComponent.GetDirtyQuests();
            QuestComponent.ClearDirty();
        }

        if (characterDto == null && inventoryDtos == null && questDto == null) return true;

        bool result = await DbManager.SavePlayerData(characterDto, inventoryDtos, questDto);
        if (!result)
        {
            PlayerInfoIsDirty = true;
            Inventory.RestoreDirty(inventoryDtos);
            QuestComponent.RestoreDirty(questDto);
            
            Log.Fatal("[Player] 플레이어 데이터 업데이트 실패");

            return false;
        }
        Log.Information("[Player] 플레이어 DB 업데이트 수행 완료");
        return true;
    }

    public Player(UserSession session, CharacterDto character)
    {
        CharacterId = character.id;
        UserId = character.user_id;
        Nickname = character.nickname;
        ClassId = character.class_id;
        Level = character.level;
        Exp = character.exp;
        Map = character.map;
        PosX = character.pos_x;
        PosY = character.pos_y;
        PosZ = character.pos_z;
        Hp = character.hp;
        MaxHp = character.max_hp;
        Damage = character.damage;

        _mySession = session;
        //Inventory = new Inventory(session);
        Inventory = new Inventory(this);
        //Quest = new Quest(this);
        QuestComponent = new QuestComponent(this);
    }

    /*public class CharacterDto
    {
        public int id { get; set; }
        public int user_id { get; set; }
        public string nickname { get; set; }
        public int level { get; set; }
        public int class_id { get; set; }
        public int exp { get; set; }
        public int map { get; set; }
        public float pos_x { get; set; }
        public float pos_y { get; set; }
        public float pos_z { get; set; }
        public int hp { get; set; }
        public int max_hp { get; set; }
        public int damage { get; set; }
    }*/

    public CharacterDto CharacterInfoToDto()
    {
        CharacterDto characterDto = new CharacterDto();
        characterDto.id = CharacterId;
        characterDto.user_id = UserId;
        characterDto.nickname = Nickname;
        characterDto.level = Level;
        characterDto.class_id = ClassId;
        characterDto.exp = Exp;
        characterDto.map = Map;
        characterDto.pos_x = PosX;
        characterDto.pos_y = PosY;
        characterDto.pos_z = PosZ;
        characterDto.hp = Hp;
        characterDto.max_hp = MaxHp;
        characterDto.damage = Damage;

        return characterDto;
    }

    public void PlayerInfoClearDirty()
    {
        PlayerInfoIsDirty = false;
    }

    //설계 초기에 객체로 분리하지 않았던게 큰 걸림돌이 되는 중
    public int CharacterId { get; set; }
    public int UserId { get; set; }
    public string Nickname { get; set; }
    public int ClassId { get; set; }
    public int Level { get; set; }
    public int Exp { get; set; }
    public int Map { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }

    public int Dir { get; set; } //최근 추가함. 02-15

    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int State { get; set; }
    public int Damage { get; set; }

    //public EquipmentInfo Equipment { get; set; }
    //public int Mp { get; set; }
    //public int Str { get; set; }
    //public int Dex { get; set; }
    //public int Int { get; set; }
    //public int Luk { get; set; }
    //public Skill Skill { get; set; }
    //public Quest Quest { get; set; }

    public QuestComponent QuestComponent { get; set; }

    public Map CurrentMap { get; set; }

    public Inventory Inventory { get; set; }

    //public PlayerState { get; set; }

    public void Send<T>(T packet) where T : IPacket => _mySession.Send(packet);

    public void AddExp(int exp)
    {
        Console.WriteLine($"플레이어 경험치 획득! {exp}");
        while(true)
        {
            
            if (Exp + exp >= LevelData[Level])
            {
                int remain = (Exp + exp) - LevelData[Level];
                
                { //레벨업시 능력치, 체력 등 업데이트 함수 따로 필요 //임시
                    Level++;
                    HealHp(MaxHp); //임시. 현재 클라에 즉시반영 안됨
                }

                Console.WriteLine($"레벨업! {Level}");
                Damage += 5;
                //퀘스트 상태에 반영
                QuestComponent.OnNotifyEvent(3, 0, 1);

                Exp = remain;

                var playerLevelUpBuff = PacketMaker.Instance.PlayerLevelUp(Level, Damage);
                _mySession.Send(playerLevelUpBuff);
            }
            else
            {
                Exp += exp;
                break;
            }
        }
    }

    public void AddItem(Item item)
    {
        Inventory.AddItem(item);
        QuestComponent.OnNotifyEvent(2, item.ItemId, item.Count);

        //클라이언트 인벤토리 아이템 갱신(임시)
        //클라이언트 - 1.AddItem 2.InventoryRefresh
        var pickUpItemResponseBuff = PacketMaker.Instance.PickUpItemResponse(item, true);
        _mySession.Send(pickUpItemResponseBuff);
    }

    public void AddItem(List<Item> items)
    {
        foreach (var item in items)
        {
            AddItem(item);
        }
    }
    /*public async void AddItem(List<int> itemIdList)
    {
        foreach (var itemId in itemIdList)
        {
            //ItemManager도 하나 필요할 것 같음. 임시
            ItemTemplate itemTemplate = DataManager.Instance.Item.GetItemData(itemId);

            InventoryDto inventoryDto = new InventoryDto
            {
                owner_id = CharacterId,
                item_id = itemId,
                count = 1,
                is_equipped = itemTemplate.Type == 1 ? 1 : 0,
                enhancement = 0
            };

            //db병목이 예상됨
            int newInventoryId = await DbManager.InsertItem(inventoryDto);
            if (newInventoryId <= 0) throw new Exception("DB Insert Failed");

            Item item = new Item()
            {
                InventoryId = newInventoryId,
                OwnerId = CharacterId,
                ItemId = itemId,
                Count = 1,
                IsEquipped = itemTemplate.Type == 1,
                Enhancement = 0
            };

            //인벤토리에 추가하고 퀘스트 상태에 반영
            Inventory.AddItem(item);
            QuestComponent.OnNotifyEvent(2, item.ItemId, item.Count);
        }

        //클라이언트 플레이어의 인벤토리 정보 업데이트 패킷 필요
    }*/

    //다음 레벨까지 필요한 경험치량
    Dictionary<int, int> LevelData = new Dictionary<int, int>
    {
        { 1, 100 },
        { 2, 200 },
        { 3, 300 },
        { 4, 400 },
        { 5, 500 },
        { 6, 600 },
        { 7, 700 },
        { 8, 800 },
        { 9, 900 },
        { 10, 999999999 }
    };

    public void HealHp(int healAmount)
    {
        Hp = Math.Min(Hp + healAmount, MaxHp);
    }

    public void UseItem(int inventoryId)
    {
        using (LogContext.PushProperty("CharacterId", CharacterId))
        {
            int itemId = Inventory.UseItem(inventoryId);
            if (itemId != -1)
            {
                //퀘스트 진행현황에 반영
                QuestComponent.OnNotifyEvent(2, itemId, -1);
            }
        }
    }

    public async void DropItem(DropItemRequest req)
    {
        int itemId = await Inventory.DropItem(req);
        if (itemId != -1)
        {
            //퀘스트 진행현황에 반영
            QuestComponent.OnNotifyEvent(2, itemId, -1);
        }
    }

    public void UpdatePlayerPos(PlayerMoveRequest req)
    {
        PosX = req.PosX;
        PosY = req.PosY;
        PosZ = req.PosZ;
        Dir = req.Dir;
        State = req.State;
        PlayerInfoIsDirty = true;
    }
}