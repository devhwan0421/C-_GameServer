using System;

public class DataManager
{
    public static DataManager Instance { get; } = new DataManager();

    public MonsterData Monster { get; set; } = new MonsterData();
    public MapData Map { get; set; } = new MapData();
    public ItemData Item { get; set; } = new ItemData();
    public NpcData Npc { get; set; } = new NpcData();
    public QuestData Quest { get; set; } = new QuestData();

    private DataManager()
    {
        //메인에서 init 호출해서 순서대로 초기화 할 것
        Monster.LoadJson();
        Console.WriteLine("몬스터 데이터 로드 완료");
        Map.LoadJson();
        Console.WriteLine("맵 데이터 로드 완료");
        Item.LoadJson();
        Console.WriteLine("아이템 데이터 로드 완료");
        Npc.LoadJson();
        Console.WriteLine("엔피시 데이터 로드 완료");
        Quest.LoadJson();
        Console.WriteLine("Quest 데이터 로드 완료");
    }
}