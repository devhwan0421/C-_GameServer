using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public class MonsterData
{
    private Dictionary<int, MonsterTemplate> _monsterDatas = new Dictionary<int, MonsterTemplate>();

    public int Count() => _monsterDatas.Count();

    public void LoadJson()
    {
        string folderPath = "./GameData/Monster";

        string[] files = Directory.GetFiles(folderPath, "monster*.json");

        foreach (string filePath in files)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath);

                MonsterTemplate monster = JsonSerializer.Deserialize<MonsterTemplate>(jsonString);

                if (_monsterDatas != null) _monsterDatas.Add(monster.MonsterId, monster);

                Console.WriteLine($"[데이터 로드] Id: {monster.MonsterId}, Hp: {monster.MaxHp}, Damage: {monster.Damage}, List: {monster.DropItemIdList.Count}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }

    public MonsterTemplate GetMonsterData(int monsterId)
    {
        if (_monsterDatas.TryGetValue(monsterId, out var data))
            return data;
        return null;
    }
}