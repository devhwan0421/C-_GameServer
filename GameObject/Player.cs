using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

public class Player
{
    public readonly UserSession _mySession;

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
        Inventory = new Inventory(session);
        //Quest = new Quest(this);
        QuestComponent = new QuestComponent(this);
    }

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
                Level++;
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

    public async void AddItem(List<int> itemIdList)
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
                IsEquipped = itemTemplate.Type == 1 ? true : false,
                Enhancement = 0
            };

            //인벤토리에 추가하고 퀘스트 상태에 반영
            Inventory.AddItem(item);
            QuestComponent.OnNotifyEvent(2, item.ItemId, item.Count);
        }

        //클라이언트 플레이어의 인벤토리 정보 업데이트 패킷 필요
    }

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
}