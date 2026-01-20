using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Map
{
    public int MapId { get; set; }
    private Dictionary<int, Player> _players = new Dictionary<int, Player>();
    private Dictionary<int, Item> _dropItems = new Dictionary<int, Item>(); 

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
        Console.WriteLine($"플레이어 id: {newPlayer.CharacterId} 를 Map: {MapId}에 추가");

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

    public void AddDropItem(Item item)
    {
        lock (_lock)
        {
            _dropItems[item.InventoryId] = item; //_dropItems.Add(item.InventoryId, item);로 안하고 저렇게 하면 같은 키를 덟어 씀. 중복처리.
        }
    }

    public void RemoveDropItem(int inventoryId)
    {
        lock (_lock)
        {
            _dropItems.Remove(inventoryId);
        }
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

    public void BroadcastSpwanItem(Item copyItem)
    {
        ArraySegment<byte> sendBuff = PacketMaker.Instance.MakeSpawnItemBuffer(copyItem, MapId);

        List<Player> snapshot;
        lock (_lock)
        {
            snapshot = new List<Player>(_players.Values);
        }

        foreach (var player in snapshot)
        {
            player._mySession.Send(sendBuff);
        }
    }

    /*public void BroadcastSpwanItem(Item copyItem)
    {
        List<Player> snapshot;
        lock (_lock)
        {
            //플레이어 목록 스냅샷
            snapshot = new List<Player>(_players.Values);
        }
        foreach (var player in snapshot)
        {
            //이러면 모든 유저에게 아이템을 새로 만들어서 보내는데
            //세션만 달라지고 보내는 내용은 같은데 개선 필요
            //100명이면 직렬화도 100번 해야 됨, ItemInfo new 연산 100번
            PacketSender.Instance.SpawnItemResponse(player._mySession, copyItem);
            //이러면 센더를 쓰는게 아니라 완성 패킷 바이트를 만들어서 보내야하는데 센더가 애매해짐
        }
    }*/
}