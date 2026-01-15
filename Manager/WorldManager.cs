using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class WorldManager
{
    public static WorldManager Instance { get; } = new WorldManager();

    private Dictionary<int, Map> _maps = new Dictionary<int, Map>();

    public void Init()
    {
        //맵 목록 추가
        _maps.Add(1, new Map { MapId = 1 });
        _maps.Add(2, new Map { MapId = 2 });

        Console.WriteLine("WorldManager 초기화 완료");
    }

    public Map GetMap(int mapId)
    {
        _maps.TryGetValue(mapId, out var map);
        return map;
    }
}