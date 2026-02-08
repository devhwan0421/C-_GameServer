using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

public class PacketMaker
{
    public static PacketMaker Instance { get; } = new PacketMaker();

    public ArraySegment<byte> MakeUseItemBuffer(int inventoryId, bool success, int type, int healAmount)
    {
        var packet = new UseItemResponse
        {
            InventoryId = inventoryId,
            Success = success,
            Type = type,
            HealAmount = healAmount //임시. 추후 다양한 타입과 효과를 넘길 수 있는 객체로 관리
        };
        string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }

    public ArraySegment<byte>MakeDropItemBuffer(Item copyItem, int mapId, bool success, int oldInventoryId, float posX, float posY, float posZ)
    {
        Console.WriteLine($"새로운 아이템 인벤토리 id : {copyItem.InventoryId}");

        var packet = new DropItemResponse
        {
            MapId = mapId,
            Success = success,
            OldInventoryId = oldInventoryId,
            PosX = posX,
            PosY = posY,
            PosZ = posZ
        };

        if (copyItem != null) {
            packet.Item = new ItemInfo
            {
                InventoryId = copyItem.InventoryId,
                OwnerId = copyItem.OwnerId,
                ItemId = copyItem.ItemId,
                Count = copyItem.Count,
                IsEquipped = copyItem.IsEquipped,
                Enhancement = copyItem.Enhancement
            };
        }
        else
        {
            packet.Item = null;
        }

            string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }

    public ArraySegment<byte> MakeSpawnItemBuffer(Item copyItem, int mapId, bool success)
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
                Enhancement = copyItem.Enhancement,
                PosX = copyItem.PosX,
                PosY = copyItem.PosY,
                PosZ = copyItem.PosZ
            },
            MapId = mapId,
            Success = success,
            PosX= copyItem.PosX,
            PosY= copyItem.PosY,
            PosZ= copyItem.PosZ
        };

        string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }

    public ArraySegment<byte> EnterWorldResponse(bool success, CharacterDto character, List<InventoryDto> inventory, List<PlayerInfo> players, List<ItemInfo> dropItems, List<MonsterInfo> monsters)
    {
        var packet = new EnterWorldResponse
        {
            Success = true,
            Character = new CharacterInfo
            {
                CharacterId = character.id,
                UserId = character.user_id,
                Nickname = character.nickname,
                ClassId = character.class_id,
                Level = character.level,
                Exp = character.exp,
                Map = character.map,
                Pos_x = character.pos_x,
                Pos_y = character.pos_y,
                Pos_z = character.pos_z,
                Hp = character.hp,
                MaxHp = character.max_hp,
                Damage = character.damage,
            },
            Inventory = inventory.Select(dto => new ItemInfo
            {
                InventoryId = dto.id,
                OwnerId = dto.owner_id,
                ItemId = dto.item_id,
                Count = dto.count,
                IsEquipped = (dto.is_equipped == 1),
                Enhancement = dto.enhancement
            }).ToList(),
            MapInfo = new MapInfo
            {
                MapId = character.map,
                Players = players,
                DropItems = dropItems,
                Monsters = monsters
            }
        };

        string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }

    public ArraySegment<byte> MoveMapResponse(bool success, int mapId, List<PlayerInfo> players, List<ItemInfo> dropItems, List<MonsterInfo> monsters)
    {
        MoveMapResponse packet = new MoveMapResponse
        {
            Success = success,
            MapInfo = new MapInfo
            {
                MapId = mapId,
                Players = players,
                DropItems = dropItems,
                Monsters = monsters
            }
        };

        string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }

    public ArraySegment<byte> PickUpItemResponse(Item item, bool success)
    {
        PickUpItemResponse packet = new PickUpItemResponse
        {
            Item = new ItemInfo
            {
                InventoryId = item.InventoryId,
                OwnerId = item.OwnerId, //바로 갱신해줘야하나?
                ItemId = item.ItemId,
                Count = item.Count,
                IsEquipped = item.IsEquipped,
                Enhancement = item.Enhancement
            },
            Success = success
        };
        string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }

    public ArraySegment<byte> DropItemDestroy(int mapId, int inventoryId)
    {
        DropItemDestroyResponse packet = new DropItemDestroyResponse
        {
            MapId = mapId,
            InventoryId = inventoryId
        };
        string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }

    public ArraySegment<byte> MonsterMove(Monster monster)
    {
        MonsterMoveResponse packet = new MonsterMoveResponse
        {
            SpawnId = monster.SpawnId,
            State = monster.State,
            Dir = monster.Dir,
            PosX = monster.PosX
        };
        string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }

    public ArraySegment<byte> PlayerTakeDamage(int characterId, int hp, int damage) 
    {
        PlayerTakeDamageResponse packet = new PlayerTakeDamageResponse
        {
            CharacterId = characterId,
            Hp = hp,
            Damage = damage
        };
        string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }

    public ArraySegment<byte> PlayerDeath(int characterId)
    {
        PlayerDeathResponse packet = new PlayerDeathResponse
        {
            CharacterId = characterId
        };
        string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }
    
    public ArraySegment<byte> PlayerHitMonster(int spawnId, int hp, int finalDamage) //PlayerHitDamage(monsterId, monster.Hp, finalDamage);
    {
        PlayerHitMonsterResponse packet = new PlayerHitMonsterResponse
        {
            SpawnId = spawnId,
            Hp = hp,
            FinalDamage = finalDamage
        };
        string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }

    public ArraySegment<byte> MonsterDeath(int spawnId)
    {
        MonsterDeathResponse packet = new MonsterDeathResponse
        {
            SpawnId = spawnId
        };
        string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }

    public ArraySegment<byte> MonsterSpawn(int spawnId, float spawnPosX, float spawnPosY)
    {
        MonsterSpawnResponse packet = new MonsterSpawnResponse
        {
            SpawnId = spawnId,
            SpawnPosX = spawnPosX,
            SpawnPosY = spawnPosY
        };
        string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }

    public ArraySegment<byte> DialogueSimple(DialogueSimple dialogue)
    {
        DialogueSimpleResponse packet = new DialogueSimpleResponse
        {
            NpcId = dialogue.NpcId,
            Type = dialogue.Type,
            Contents = dialogue.Contents
        };
        string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }

    public ArraySegment<byte> DialogueOk(DialogueOk dialogue)
    {
        DialogueOkResponse packet = new DialogueOkResponse
        {
            NpcId = dialogue.NpcId,
            Type = dialogue.Type,
            Contents = dialogue.Contents
        };
        string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }

    public ArraySegment<byte> DialogueNext(DialogueNext dialogue)
    {
        DialogueNextResponse packet = new DialogueNextResponse
        {
            NpcId = dialogue.NpcId,
            Type = dialogue.Type,
            NextDialogueId = dialogue.NextDialogueId,
            Contents = dialogue.Contents
        };
        string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }

    public ArraySegment<byte> DialogueAcceptDecline(DialogueAcceptDecline dialogue)
    {
        DialogueAcceptDeclineResponse packet = new DialogueAcceptDeclineResponse
        {
            NpcId = dialogue.NpcId,
            Type = dialogue.Type,
            Contents = dialogue.Contents,
            QuestId = dialogue.QuestId,
        };
        string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }

    public ArraySegment<byte> DialogueSelection(DialogueSelection dialogue)
    {
        DialogueSelectionResponse packet = new DialogueSelectionResponse
        {
            NpcId = dialogue.NpcId,
            Type = dialogue.Type,
            Contents = dialogue.Contents,
            Selections = dialogue.Selections
        };
        string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }

    public ArraySegment<byte> PlayerLevelUp(int level, int damage)
    {
        PlayerLevelUpResponse packet = new PlayerLevelUpResponse
        {
            Level = level,
            Damage = damage
        };
        string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }

    public ArraySegment<byte> QuestComplete(int questId, string questName)
    {
        QuestCompleteResponse packet = new QuestCompleteResponse
        {
            QuestId = questId,
            QuestName = questName
        };
        string json = JsonSerializer.Serialize(packet);
        return PacketSerializer.Serialize((ushort)packet.PacketId, json);
    }
}