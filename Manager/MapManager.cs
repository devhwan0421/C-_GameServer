using System;
using System.Collections.Generic;

public class MapManager
{
    public static MapManager Instance { get; } = new MapManager();

    private Dictionary<int, Map> _maps = new Dictionary<int, Map>();

    private bool isInit = false;

    public void Init()
    {
        //맵 목록 추가
        //_maps.Add(1, new Map(1));
        //_maps.Add(2, new Map(2));
        /*_maps.Add(1, new Map { MapId = 1 });
        _maps.Add(2, new Map { MapId = 2 });*/

        int mapCount = DataManager.Instance.Map.Count();

        Map map = null;
        for (int i = 1; i <= mapCount; i++)
        {
            map = DataManager.Instance.Map.GetMapData(i);
            _maps.Add(i, map);
            map.Init();
            Console.WriteLine($"Map Add: {map.MapId}");
        }

        isInit = true;
        Console.WriteLine("맵 생성 완료");
    }

    public Map GetMap(int mapId)
    {
        _maps.TryGetValue(mapId, out var map);
        return map;
    }

    public void Update(float deltaTime, long nextTickTime)
    {
        if (!isInit) return;

        foreach (var map in _maps.Values)
        {
            map.Update(deltaTime, nextTickTime);
        }
    }
}