using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public class MapData
{
    internal Dictionary<int, Map> _mapDatas = new Dictionary<int, Map>();

    public int Count() => _mapDatas.Count();

    public void LoadJson()
    {
        string folderPath = "./GameData/Map";

        string[] files = Directory.GetFiles(folderPath, "map*.json");

        foreach (string filePath in files)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath);

                MapTemplate mapTemplate = JsonSerializer.Deserialize<MapTemplate>(jsonString);

                if (mapTemplate != null)
                {
                    Map map = new Map(mapTemplate);
                    _mapDatas.Add(map.MapId, map);

                    Console.WriteLine($"[데이터 로드] Id: {mapTemplate.MapId}, MonsterCount: {mapTemplate.MonsterPatrolInfo.Count}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }

    public Map GetMapData(int mapId)
    {
        if (_mapDatas.TryGetValue(mapId, out var data))
            return data;
        return null;
    }
}