using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class MapTemplate
{
    public int MapId { get; set; }
    public List<MonsterPatrolInfo> MonsterPatrolInfo { get; set; } = new List<MonsterPatrolInfo>();
}