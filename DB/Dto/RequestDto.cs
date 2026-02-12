using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class LoginDto
{
    public int id { get; set; }
    public string username { get; set; }
    public string password { get; set; }
}

public class CharacterDto
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
}

public class InventoryDto
{
    public int id { get; set; }
    public int owner_id { get; set; }
    public int item_id { get; set; }
    public int count { get; set; }
    public int is_equipped { get; set; }
    public int enhancement { get; set; }

    public InventoryDto(Item item)
    {
        owner_id = item.OwnerId;
        item_id = item.ItemId;
        count = item.Count;
        is_equipped = item.IsEquipped ? 1 : 0;
        enhancement = item.Enhancement;
    }

    public InventoryDto() { }
}

public class QuestDto
{
    public int quest_id { get; set; }
    public int character_id { get; set; }
    public int state { get; set; }
}

public class QuestProgressDto
{
    public int questProgress_id { get; set; }
    public int character_id { get; set; }
    public int quest_id { get; set; }
    public int monster_id { get; set; }
    public int current_count { get; set; }
}

public class QuestDtoSet
{
    public List<QuestDto> Quests = new List<QuestDto>();
    public List<QuestProgressDto> QuestProgresses = new List<QuestProgressDto>();
}