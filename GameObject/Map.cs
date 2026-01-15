using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Map
{
    public int MapId { get; set; }
    private Dictionary<int, Player> _players = new Dictionary<int, Player>();
    private object _lock = new object();

    public void Enter(Player newPlayer)
    {
        /*_players[player.CharacterId] = player;
        player.CurrentMap = this;
        player.Map = MapId;

        //Broadcast(new S_Spawn { Player = player.ToDto() });*/

        //기존 플레이어 정보 전송
        ExistingPlayerListResponse res = new ExistingPlayerListResponse();
        foreach (Player existingPlayer in _players.Values)
        {
            res.players.Add(new PlayerInfo
            {
                CharacterId = existingPlayer.CharacterId,
                Nickname = existingPlayer.Nickname,
                PosX = existingPlayer.PosX,
                PosY = existingPlayer.PosY,
                PosZ = existingPlayer.PosZ
            });
        }
        newPlayer.Send(res);

        //해당 플레이어 맵에 추가
        _players[newPlayer.CharacterId] = newPlayer;
        newPlayer.CurrentMap = this;

        //해당 플레이어 스폰 브로드캐스트
        SpawnPlayerResponse spawnSelf = new SpawnPlayerResponse
        {
            player = new PlayerInfo
            {
                CharacterId = newPlayer.CharacterId,
                Nickname = newPlayer.Nickname,
                PosX = newPlayer.PosX,
                PosY = newPlayer.PosY,
                PosZ = newPlayer.PosZ
            }
        };
        Broadcast(spawnSelf, newPlayer.CharacterId);
    }

    public void Leave(int characterId)
    {
        Player player = null;
        if (_players.TryGetValue(characterId, out player))
        {
            _players.Remove(characterId);
            player.CurrentMap = null;
            
            //Broadcast(new S_Despawn { CharacterId = characterId });
        }
    }

    public void ChangeMap(Player player, int newMapId)
    {
        //기존 맵 퇴장
        player.CurrentMap.Leave(player.CharacterId);

        //새 맵 입장
        Map newMap = WorldManager.Instance.GetMap(newMapId);
        newMap.Enter(player);
    }

    public void Broadcast<T>(T packet, int exceptPlayerId) where T : IPacket
    {
        foreach (var player in _players.Values)
        {
            if (player.CharacterId == exceptPlayerId)
                continue;
            player.Send(packet);
        }
    }
}