using System.Collections.Generic;

public class MapTemplate
{
    public int MapId { get; set; }
    public List<MonsterPatrolInfo> MonsterPatrolInfo { get; set; } = new List<MonsterPatrolInfo>();
}