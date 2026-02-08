using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class MonsterTemplate
{
    public int MonsterId { get; set; }
    public string Nickname { get; set; }
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int Damage { get; set; }
    public int Exp { get; set; }
    public List<int> DropItemIdList { get; set; }
}