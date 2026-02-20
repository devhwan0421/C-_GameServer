using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class PlayerManager
{
    public static PlayerManager Instance { get; } = new PlayerManager();
    private readonly Dictionary<int, Player> _players = new Dictionary<int, Player>();

    private readonly Random _rand = new Random();
    private const float MIN_SAVE_INTERVAL = 180f;
    private const float MAX_SAVE_INTERVAL = 300f;

    //private const float MIN_SAVE_INTERVAL = 10f;
    //private const float MAX_SAVE_INTERVAL = 10f;

    public void Update(float deltaTime)
    {
        foreach (var player in _players.Values)
        {
            player.Update(deltaTime);
        }
    }

    private float GetRandomInterval()
    {
        return (float)(_rand.NextDouble() * (MAX_SAVE_INTERVAL - MIN_SAVE_INTERVAL) + MIN_SAVE_INTERVAL);
    }

    public async Task<bool> Enter(Player player)
    {
        if (_players.TryGetValue(player.CharacterId, out var oldPlayer))
        {
            Log.Warning($"[PlayerManager] 중복 캐릭터 입장 시도 {player.Nickname}({player.CharacterId})");

            oldPlayer.Send(new ServerMessageResponse { Message = "다른 곳에서 접속하여 강제 로그아웃되었습니다." });

            if (oldPlayer._mySession != null)
            {
                bool result = await oldPlayer._mySession.DisConnect();
                if (!result) return false;
            }
        }

        //db update 주기 설정
        player.SetDbUpdateTimer(GetRandomInterval());
        _players.Add(player.CharacterId, player);

        Log.Information($"{player.Nickname}({player.CharacterId}) + 님이 접속했습니다. (현재 동접: {_players.Count})");
        return true;
    }
    /*public async Task Enter(Player player)
    {
        Log.Warning($"[PlayerManager] 중복 캐릭터 입장 시도 {player.Nickname}({player.CharacterId})");
        if (_players.ContainsKey(player.CharacterId)) //이미 접속한 플레이어인지 확인
        {
            _players.TryGetValue(player.CharacterId, out var oldPlayer);
            oldPlayer.Send(new ServerMessageResponse { Message = "다른 곳에서 접속하여 강제 로그아웃되었습니다." });
            _players.Remove(player.CharacterId);
        }

        //db update 주기 설정
        player.SetDbUpdateTimer(GetRandomInterval());

        _players.Add(player.CharacterId, player);
        Console.WriteLine(player.Nickname + $"님이 접속했습니다. (현재 동접: {_players.Count})");
    }*/

    public async Task<bool> Leave(int playerId)
    {
        if (_players.TryGetValue(playerId, out var player))
        {
            bool result = await player.ImmediateDbUpdate();
            if (!result) return false;

            Map targetMap = MapManager.Instance.GetMap(player.Map);
            targetMap.Leave(playerId);

            //실패시 맵에서는 내보내고 플레이어데이터만 남기는 게 나을지도
            _players.Remove(playerId);

            Log.Information($"{player.Nickname}({playerId})님이 떠났습니다. (현재 동접: {_players.Count})");
        }
        return true;
    }

    public void Broadcast<T>(T packet) where T : IPacket
    {
        foreach (Player player in _players.Values)
        {
            player.Send(packet);
        }
    }
}