using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

public enum PacketID : ushort
{
    LoginRequest = 1,
    LoginResponse = 2,
    GetCharacterListRequest = 3,
    GetCharacterListResponse = 4,
    GetCharacterRequest = 5,
    GetCharacterResponse = 6,
    EnterWorldRequest = 7,
    EnterWorldResponse = 8,
    ExistingPlayerListResponse = 9,
    SpawnPlayerResponse = 10,
    ServerMessageResponse = 800
    //MoveRequest = 3,
    //MoveResponse = 4
}

public interface IPacket
{
    PacketID PacketId { get; }
}

[Serializable]
public class LoginRequest : IPacket
{
    public PacketID PacketId => PacketID.LoginRequest; // public PacketID Id { get { return PacketID.LoginRequest; } }
    public string Username { get; set; }
    public string Password { get; set; }
}

[Serializable]
public class LoginResponse : IPacket
{
    public PacketID PacketId => PacketID.LoginResponse;
    public string Username { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
    //public long UserGuid;
}

[Serializable]
public class GetCharacterListRequest : IPacket
{
    public PacketID PacketId => PacketID.GetCharacterListRequest;
    public int UserId { get; set; }
}

[Serializable]
public class GetCharacterListResponse : IPacket
{
    public PacketID PacketId => PacketID.GetCharacterListResponse;
    
    //보유 캐릭터 목록 반환
    public List<CharacterInfo> Characters { get; set; } = new List<CharacterInfo>();
}

[Serializable]
public class GetCharacterRequest : IPacket
{
    public PacketID PacketId => PacketID.GetCharacterRequest;
    public int CharacterId { get; set; }
}

[Serializable]
public class GetCharacterResponse : IPacket
{
    public PacketID PacketId => PacketID.GetCharacterResponse;

    public CharacterInfo Character { get; set; }
}

[Serializable]
public class CharacterInfo
{
    public int CharacterId { get; set; }
    public int UserId { get; set; }
    public string Nickname { get; set; }
    public int ClassId { get; set; }
    public int Level { get; set; }
    public int Exp { get; set; }
    public int Map { get; set; }
    public float Pos_x { get; set; }
    public float Pos_y { get; set; }
    public float Pos_z { get; set; }
}

[Serializable]
public class EnterWorldRequest : IPacket
{
    public PacketID PacketId => PacketID.EnterWorldRequest;
    public int CharacterId { get; set; }
}

[Serializable]
public class EnterWorldResponse : IPacket
{
    public PacketID PacketId => PacketID.EnterWorldResponse;
    public CharacterInfo Character { get; set; }
}

[Serializable]
public class PlayerInfo
{
    public int CharacterId { get; set; }
    public string Nickname { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
}

[Serializable]
public class ExistingPlayerListResponse : IPacket
{
    public PacketID PacketId => PacketID.ExistingPlayerListResponse;
    public List<PlayerInfo> players { get; set; } = new List<PlayerInfo>();
}

[Serializable]
public class SpawnPlayerResponse : IPacket
{
    public PacketID PacketId => PacketID.SpawnPlayerResponse;
    public PlayerInfo player { get; set; }
}

[Serializable]
public class ServerMessageResponse : IPacket
{
    public PacketID PacketId => PacketID.ServerMessageResponse;
    public string Message { get; set; }
}