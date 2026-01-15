using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

public class Player
{
    public int CharacterId { get; set; }
    public int UserId { get; set; }
    public string Nickname { get; set; }
    public int ClassId { get; set; }
    public int Level { get; set; }
    public int Exp { get; set; }
    public int Map { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
    public UserSession Session { get; set; }

    public Map CurrentMap { get; set; }

    //public PlayerState { get; set; }

    public Player(UserSession session, CharacterDto character)
    {
        CharacterId = character.id;
        UserId = character.user_id;
        Nickname = character.nickname;
        ClassId = character.class_id;
        Level = character.level;
        Exp = character.exp;
        Map = character.map;
        PosX = character.pos_x;
        PosY = character.pos_y;
        PosZ = character.pos_z;
        Session = session;
    }

    public void Send<T>(T packet) where T : IPacket => Session.Send(packet);
}