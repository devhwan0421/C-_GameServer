using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public class ItemData
{
    private Dictionary<int, ItemTemplate> _itemDatas = new Dictionary<int, ItemTemplate>();

    public int Count() => _itemDatas.Count();

    public void LoadJson()
    {
        string folderPath = "./GameData/Item";

        string[] files = Directory.GetFiles(folderPath, "item*.json");

        foreach (string filePath in files)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath);

                ItemTemplate item = JsonSerializer.Deserialize<ItemTemplate>(jsonString);

                if (_itemDatas != null) _itemDatas.Add(item.ItemId, item);

                Console.WriteLine($"[데이터 로드] Id: {item.ItemId}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }

    public ItemTemplate GetItemData(int itemId)
    {
        if (_itemDatas.TryGetValue(itemId, out var data))
            return data;
        return null;
    }
}