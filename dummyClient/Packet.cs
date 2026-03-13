using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

public enum PacketID : ushort
{
    LoginRequest = 1,
    LoginResponse = 2,
    GetCharacterListRequest = 3,
    GetCharacterListResponse = 4,
    GetCharacterRequest = 5,
    GetCharacterResponse = 6,
    EnterWorldRequest = 7,
    EnterWorldResponse = 8,
    ExistingPlayerListResponse = 9,
    SpawnPlayerResponse = 10,
    PlayerMoveRequest = 11,
    PlayerMoveResponse = 12,
    InventoryResponse = 13,
    UseItemRequest = 14,
    UseItemResponse = 15,
    DropItemRequest = 16,
    DropItemResponse = 17,
    SpawnItemRequest = 18,
    SpawnItemResponse = 19,
    MapInfoRequest = 20,
    MapInfoResponse = 21,
    DespawnPlayerResponse = 22,
    MoveMapRequest = 23,
    MoveMapResponse = 24,
    PickUpItemRequest = 25,
    PickUpItemResponse = 26,
    DropItemDestroyResponse = 27,
    MonsterMoveResponse = 28,
    PlayerTakeDamageRequest = 29,
    PlayerTakeDamageResponse = 30,
    PlayerDeathResponse = 31,
    PlayerHitRequest = 32,
    PlayerHitMonsterResponse = 33,
    MonsterDeathResponse = 34,
    MonsterSpawnResponse = 35,
    NpcTalkRequest = 36,
    DialogueSimpleResponse = 37,
    DialogueOkResponse = 38,
    DialogueNextResponse = 39,
    DialogueAcceptDeclineResponse = 40,
    DialogueSelectionResponse = 41,
    QuestAcceptRequest = 42,
    QuestAcceptResponse = 43,
    PlayerLevelUpResponse = 44,
    QuestCompleteResponse = 45,
    ServerMessageResponse = 800,
}

public interface IPacket
{
    PacketID PacketId { get; }
}

[Serializable]
public class LoginRequest : IPacket
{
    public PacketID PacketId => PacketID.LoginRequest; // public PacketID Id { get { return PacketID.LoginRequest; } }
    public string Username { get; set; }
    public string Password { get; set; }
}

[Serializable]
public class LoginResponse : IPacket
{
    public PacketID PacketId => PacketID.LoginResponse;
    public string Username { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
    //public long UserGuid;
}

[Serializable]
public class GetCharacterListRequest : IPacket
{
    public PacketID PacketId => PacketID.GetCharacterListRequest;
    public int UserId { get; set; }
}

[Serializable]
public class GetCharacterListResponse : IPacket
{
    public PacketID PacketId => PacketID.GetCharacterListResponse;
    
    //보유 캐릭터 목록 반환
    public List<CharacterInfo> Characters { get; set; } = new List<CharacterInfo>();
}

[Serializable]
public class GetCharacterRequest : IPacket
{
    public PacketID PacketId => PacketID.GetCharacterRequest;
    public int CharacterId { get; set; }
}

[Serializable]
public class GetCharacterResponse : IPacket
{
    public PacketID PacketId => PacketID.GetCharacterResponse;
    public CharacterInfo Character { get; set; }
}

[Serializable]
public class CharacterInfo
{
    public int CharacterId { get; set; }
    public int UserId { get; set; }
    public string Nickname { get; set; }
    public int ClassId { get; set; }
    public int Level { get; set; }
    public int Exp { get; set; }
    public int Map { get; set; }
    public float Pos_x { get; set; }
    public float Pos_y { get; set; }
    public float Pos_z { get; set; }

    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int Damage { get; set; }

    //다른 플레이어를 나타낼 때는 필요 없는 정보들
    //public int Mp { get; set; }
    //public int Str { get; set; }
    //public int Dex { get; set; }
    //public int Int { get; set; }
    //public int Luk { get; set; }
    //public Skill Skill { get; set; }
    //public Quest Quest { get; set; }
    //public Inventory Inventory { get; set; }
}

[Serializable]
public class PlayerInfo
{
    public int CharacterId { get; set; }
    public string Nickname { get; set; }
    public int Level { get; set; }
    public int ClassId { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }

    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int State { get; set; }
    public int Damage { get; set; }

    //public EquipmentInfo Equipment { get; set; }
}

[Serializable]
public class MonsterInfo
{
    public int SpawnId { get; set; }
    public int MonsterId { get; set; }
    public string Nickname { get; set; }
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int Damage { get; set; }
    public int State { get; set; }
    public int Dir { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
}

[Serializable]
public class MapInfo
{
    public int MapId { get; set; }
    public List<PlayerInfo> Players { get; set; }
    public List<MonsterInfo> Monsters { get; set; }
    public List<ItemInfo> DropItems { get; set; }
}

[Serializable]
public class EnterWorldRequest : IPacket
{
    public PacketID PacketId => PacketID.EnterWorldRequest;
    public int CharacterId { get; set; }
}

[Serializable]
public class EnterWorldResponse : IPacket
{
    public PacketID PacketId => PacketID.EnterWorldResponse;
    public bool Success { get; set; }

    //내 캐릭터 정보
    public CharacterInfo Character { get; set; }

    //내 캐릭터 인벤토리
    public List<ItemInfo> Inventory { get; set; }

    //맵 정보
    public MapInfo MapInfo { get; set; }
}

[Serializable]
public class ExistingPlayerListResponse : IPacket
{
    public PacketID PacketId => PacketID.ExistingPlayerListResponse;
    public List<PlayerInfo> players { get; set; } = new List<PlayerInfo>();
}

[Serializable]
public class SpawnPlayerResponse : IPacket
{
    public PacketID PacketId => PacketID.SpawnPlayerResponse;
    public PlayerInfo player { get; set; }
}

[Serializable]
public class ServerMessageResponse : IPacket
{
    public PacketID PacketId => PacketID.ServerMessageResponse;
    public string Message { get; set; }
}

[Serializable]
public class PlayerMoveRequest : IPacket
{
    public PacketID PacketId => PacketID.PlayerMoveRequest;
    public int CharacterId { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
    public int Dir { get; set; }
    public int State { get; set; }
}

[Serializable]
public class PlayerMoveResponse : IPacket
{
    public PacketID PacketId => PacketID.PlayerMoveResponse;
    public int CharacterId { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
    public int Dir { get; set; }
    public int State { get; set; }
}

[Serializable]
public class ItemInfo
{
    public int InventoryId { get; set; }
    public int OwnerId { get; set; }
    public int ItemId { get; set; }
    public int Count { get; set; }
    public bool IsEquipped { get; set; }
    public int Enhancement { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
}

[Serializable]
public class InventoryResponse : IPacket
{
    public PacketID PacketId => PacketID.InventoryResponse;
    public List<ItemInfo> Items { get; set; }
}

[Serializable]
public class UseItemRequest : IPacket
{
    public PacketID PacketId => PacketID.UseItemRequest;
    public int InventoryId { get; set; }
}

[Serializable]
public class UseItemResponse : IPacket
{
    public PacketID PacketId => PacketID.UseItemResponse;
    public int InventoryId { get; set; }
    public bool Success { get; set; }
    public int Type { get; set; }
    public int HealAmount { get; set; }
}

[Serializable]
public class DropItemRequest : IPacket
{
    public PacketID PacketId => PacketID.DropItemRequest;
    public int InventoryId { get; set; }
    public int MapId { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
}

[Serializable]
public class DropItemResponse : IPacket
{
    public PacketID PacketId => PacketID.DropItemResponse;
    public ItemInfo Item { get; set; }
    public int MapId { get; set; }
    public bool Success { get; set; }
    public int OldInventoryId { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
}

[Serializable]
public class SpawnItemResponse : IPacket
{
    public PacketID PacketId => PacketID.SpawnItemResponse;
    public ItemInfo Item { get; set; }
    public int MapId { get; set; }
    public bool Success { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
}

[Serializable]
public class DespawnPlayerResponse : IPacket
{
    public PacketID PacketId => PacketID.DespawnPlayerResponse;
    public int CharacterId { get; set; }
}

[Serializable]
public class MoveMapRequest : IPacket
{
    public PacketID PacketId => PacketID.MoveMapRequest;
    public int CharacterId { get; set; }
    public int TargetMapId { get; set; }
    public float TargetPosX { get; set; }
    public float TargetPosY { get; set; }
}

[Serializable]
public class MoveMapResponse : IPacket
{
    public PacketID PacketId => PacketID.MoveMapResponse;
    public bool Success { get; set; }
    public MapInfo MapInfo { get; set; } = new MapInfo();
}

[Serializable]
public class PickUpItemRequest : IPacket
{
    public PacketID PacketId => PacketID.PickUpItemRequest;
    public int MapId { get; set; }
    public int InventoryId { get; set; }
}

[Serializable]
public class PickUpItemResponse : IPacket
{
    public PacketID PacketId => PacketID.PickUpItemResponse;
    public ItemInfo Item { get; set; }
    public bool Success { get; set; }
}

[Serializable]
public class DropItemDestroyResponse : IPacket
{
    public PacketID PacketId => PacketID.DropItemDestroyResponse;
    public int MapId { get; set; }
    public int InventoryId { get; set; }
}

[Serializable]
public class MonsterMoveResponse : IPacket
{
    public PacketID PacketId => PacketID.MonsterMoveResponse;
    public int SpawnId { get; set; }
    public int State { get; set; }
    public int Dir { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
}

[Serializable]
public class PlayerTakeDamageRequest : IPacket
{
    public PacketID PacketId => PacketID.PlayerTakeDamageRequest;
    public int CharacterId { get; set; }
    public int Damage { get; set; }
    //public float KnockbackX { get; set; }
    //public float KnockbackY { get; set; }
}

[Serializable]
public class PlayerTakeDamageResponse : IPacket
{
    public PacketID PacketId => PacketID.PlayerTakeDamageResponse;
    public int CharacterId { get; set; }
    public int Hp { get; set; }
    public int Damage { get; set; }
}

[Serializable]
public class PlayerDeathResponse : IPacket
{
    public PacketID PacketId => PacketID.PlayerDeathResponse;
    public int CharacterId { get; set; }
}

[Serializable]
public class PlayerHitRequest : IPacket
{
    public PacketID PacketId => PacketID.PlayerHitRequest;
    public int CharacterId { get; set; }
    public int SpawnId { get; set; }
    public int AttackType { get; set; } //0은 일반 공격으로 넉백양을 고정
    public int Damage { get; set; } //사실 서버의 플레이어 데이터에 이미 있어서 받을 필요가 없어보임
    public int KnockbackDir { get; set; }
}

/*[Serializable]
public class PlayerHitResponse : IPacket
{
    public PacketID PacketId => PacketID.PlayerHitResponse;
    public int MonsterId { get; set; }
    public int Hp { get; set; }
    public int Damage { get; set; }
}*/

[Serializable]
public class PlayerHitMonsterResponse : IPacket
{
    public PacketID PacketId => PacketID.PlayerHitMonsterResponse;
    public int SpawnId { get; set; }
    public int Hp { get; set; }
    public int FinalDamage { get; set; }
}

[Serializable]
public class MonsterDeathResponse : IPacket
{
    public PacketID PacketId => PacketID.MonsterDeathResponse;
    public int SpawnId { get; set; }
}

[Serializable]
public class MonsterSpawnResponse : IPacket
{
    public PacketID PacketId => PacketID.MonsterSpawnResponse;
    public int SpawnId { get; set; }
    public float SpawnPosX { get; set; }
    public float SpawnPosY { get; set; }
}

[Serializable]
public class NpcTalkRequest : IPacket
{
    public PacketID PacketId => PacketID.NpcTalkRequest;
    public int Type { get; set; }
    public int NpcId { get; set; }
    public int DialogueId { get; set; }
    public int QuestId { get; set; }
}

[Serializable]
public class DialogueSimpleResponse : IPacket
{
    public PacketID PacketId => PacketID.DialogueSimpleResponse;
    public int NpcId { get; set; }
    public int Type { get; set; }
    public string Contents { get; set; }
}

[Serializable]
public class DialogueOkResponse : IPacket
{
    public PacketID PacketId => PacketID.DialogueOkResponse;
    public int NpcId { get; set; }
    public int Type { get; set; }
    public string Contents { get; set; }
}

[Serializable]
public class DialogueNextResponse : IPacket
{
    public PacketID PacketId => PacketID.DialogueNextResponse;
    public int NpcId { get; set; }
    public int Type { get; set; }
    public int NextDialogueId { get; set; }
    public string Contents { get; set; }
}

[Serializable]
public class DialogueAcceptDeclineResponse : IPacket
{
    public PacketID PacketId => PacketID.DialogueAcceptDeclineResponse;
    public int NpcId { get; set; }
    public int Type { get; set; }
    public string Contents { get; set; }
    public int QuestId { get; set; }
}

[Serializable]
public class DialogueSelectionResponse : IPacket
{
    public PacketID PacketId => PacketID.DialogueSelectionResponse;
    public int NpcId { get; set; }
    public int Type { get; set; }
    public string Contents { get; set; }
    public List<DialogueSelectionOption> Selections { get; set; }
}

/*[Serializable]
public class QuestAcceptRequest : IPacket
{
    public PacketID PacketId => PacketID.QuestAcceptRequest;
    public int NpcId { get; set; }
    public int QuestId { get; set; }
}

[Serializable]
public class QuestAcceptResponse : IPacket
{
    public PacketID PacketId => PacketID.QuestAcceptResponse;
    public int QuestId { get; set; }
    public string QuestName { get; set; }
    public bool success { get; set; }
}*/

[Serializable]
public class PlayerLevelUpResponse : IPacket
{
    public PacketID PacketId => PacketID.PlayerLevelUpResponse;
    public int Level { get; set; }
    public int Damage { get; set; }
}

[Serializable]
public class QuestCompleteResponse : IPacket
{
    public PacketID PacketId => PacketID.QuestCompleteResponse;
    public int QuestId;
    public string QuestName { get; set; }
}

/*[Serializable]
public class DialogueResponse
{
    public PacketID PacketId => PacketID.DialogueResponse;
    public int NpcId { get; set; }
    public int DialogueId { get; set; }
    public int Type { get; set; } //0:simple, 1:ok, 2:next, 3:accept/decline
    public int NextDialogueId { get; set; } //type이 2라면 next누르면 nextDialogueId 서버로 전송
    public int QuestId { get; set; } //type이 3이라면 accept누르면 questId 서버로 전송
    public string Contents { get; set; }
}*/











/*[Serializable]
public class DialogueText
{
    public int NpcId { get; set; }
    public int DialogueId { get; set; }
    public int Type { get; set; } //0:simple, 1:ok, 2:next, 3:accept/decline
    public int NextDialogueId { get; set; } //type이 2라면 next누르면 nextDialogueId 서버로 전송
    public int QuestId { get; set; } //type이 3이라면 accept누르면 questId 서버로 전송
    public string Contents { get; set; }
}*/
/*[Serializable]
public class DialogueText
{
    public int DialogueId { get; set; }
    public int Type { get; set; }
    public string Text { get; set; }
}*/


/*[Serializable]
public class NpcDialogueResponse : IPacket
{
    public PacketID PacketId => PacketID.NpcDialogueResponse;
    public int NpcId { get; set; }
    public int Type { get; set; }
    public List<DialogueText> TextList { get; set; } = new List<DialogueText>();

    public int QuestId { get; set; }
}*/



/*[Serializable]
public class MapInfoRequest : IPacket
{
    public PacketID PacketId => PacketID.MapInfoRequest;
    public int MapId { get; set; }
}

//enterworldresponse와 뭔가 애매하다. 고민해 볼 것. 실제로는 월드 진입 ok 받는건데 이름을 잘못 지은 것 같기도
//enterworld_ok를 받고 맵을 로딩한 후 맵 정보(MapInfoResponse)와 플레이어 정보(인벤토리 포함)가 도착하면 화면을 보여주는 게 맞는 것 같음
[Serializable]
public class MapInfoResponse : IPacket
{
    public PacketID PacketId => PacketID.MapInfoResponse;
    public int MapId { get; set; }
    public List<PlayerInfo> Players;
    public List<MonsterInfo> Monsters;
    public List<ItemInfo> DropItems;
}*/