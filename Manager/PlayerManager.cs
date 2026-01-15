using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class PlayerManager
{
    public static PlayerManager Instance { get; } = new PlayerManager();
    private readonly Dictionary<int, Player> _players = new Dictionary<int, Player>();

    public void Enter(Player player)
    {
        if (_players.ContainsKey(player.CharacterId)) //이미 접속한 플레이어인지 확인
        {
            _players.TryGetValue(player.CharacterId, out var oldPlayer);
            oldPlayer.Send(new ServerMessageResponse { Message = "다른 곳에서 접속하여 강제 로그아웃되었습니다." });
            _players.Remove(player.CharacterId);
        }

        _players.Add(player.CharacterId, player);
        Console.WriteLine(player.Nickname + $"님이 접속했습니다. (현재 동접: {_players.Count})");
    }

    public void Leave(int playerId)
    {
        if (_players.TryGetValue(playerId, out var player))
        {
            _players.Remove(playerId);
            Map targetMap = WorldManager.Instance.GetMap(player.Map);
            targetMap.Leave(playerId);
            Console.WriteLine(player.Nickname + $"님이 떠났습니다. (현재 동접: {_players.Count})");
        }
    }

    public void Broadcast<T>(T packet) where T : IPacket
    {
        foreach (Player player in _players.Values)
        {
            player.Send(packet);
        }
    }
}