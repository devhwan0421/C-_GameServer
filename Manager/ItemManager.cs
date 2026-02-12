using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ItemManager
{
    public static ItemManager Instance { get; } = new ItemManager();

    public async Task<Item> CreateItemAndInsertDB(int itemId)
    {
        ItemTemplate itemTemplate = DataManager.Instance.Item.GetItemData(itemId);

        InventoryDto inventoryDto = new InventoryDto
        {
            owner_id = -2,
            item_id = itemId,
            count = 1,
            is_equipped = itemTemplate.Type == 1 ? 1 : 0,
            enhancement = 0
        };

        int newInventoryId = await DbManager.InsertItem(inventoryDto);
        if (newInventoryId <= 0) throw new Exception("DB Insert Failed");

        Item item = new Item()
        {
            InventoryId = newInventoryId,
            OwnerId = -1,
            ItemId = itemId,
            Count = 1,
            IsEquipped = itemTemplate.Type == 1 ? true : false,
            Enhancement = 0
        };

        return item;
    }

    public async Task<List<Item>> CreateItemAndInsertDB(List<int> itemIdList, int characterId)
    {
        List<InventoryDto> inventoryDtos = new List<InventoryDto>();
        foreach (int itemId in itemIdList)
        {
            ItemTemplate itemTemplate = DataManager.Instance.Item.GetItemData(itemId);

            inventoryDtos.Add(new InventoryDto
            {
                owner_id = characterId,
                item_id = itemTemplate.ItemId,
                count = 1,
                is_equipped = itemTemplate.Type == 1 ? 1 : 0,
                enhancement = 0
            });
        }

        List<Item> items = await DbManager.InsertItemList(inventoryDtos);

        return items;
    }
}