using Serilog;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;

public class MonsterPatrolInfo
{
    public int MonsterId { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float MinPosX { get; set; }
    public float MaxPosX { get; set; }
}

public class Map
{
    public int MapId { get; set; }
    private List<MonsterPatrolInfo> _monsterPatrolInfo {  get; set; } = new List<MonsterPatrolInfo>();

    private Dictionary<int, Player> _players = new Dictionary<int, Player>();
    private Dictionary<int, Item> _dropItems = new Dictionary<int, Item>(); 
    private Dictionary<int, Monster> _monsters = new Dictionary<int, Monster>();

    private float _packetSendTimer = 0;
    private const float _packetSendInterval = 0.1f;
    private float _waitDeactivateTimer = 0;
    private const float _waitActivateSec = 10.0f;

    private bool _isActive = false;
    private bool _isPlayerZero = true;

    private static readonly Random _rand = new Random();

    public Map(MapTemplate mapTemplate)
    {
        MapId = mapTemplate.MapId;
        _monsterPatrolInfo = mapTemplate.MonsterPatrolInfo;
    }

    public void Init()
    {
        SpawnMonster();
        Console.WriteLine($"MapId: {MapId} Init Complete.");
    }

    public void Update(float deltaTime)
    {
        if (!_isActive) return;

        if (_isPlayerZero)
        {
            _waitDeactivateTimer += deltaTime;
            if (_waitDeactivateTimer >= _waitActivateSec)
            {
                _isActive = false;
                _waitDeactivateTimer = 0;
            }
        }

        foreach (var monster in _monsters.Values)
        {
            //monster.Update(deltaTime, this);
            monster.Update(deltaTime);
            //Console.WriteLine($"{monster.Nickname} Update. {monster.PosX}, {monster.PosY}");
        }

        _packetSendTimer += deltaTime;
        if (_packetSendTimer >= _packetSendInterval)
        {
            _packetSendTimer = 0;
            BroadcastMonsterUpdates();
        }
    }

    //몬스터 리스트 반환
    public List<MonsterInfo> GetMonsters()
    {
        List<MonsterInfo> monsterInfos = new List<MonsterInfo>();
        foreach (var monster in _monsters.Values)
        {
            monsterInfos.Add(new MonsterInfo
            {
                SpawnId = monster.SpawnId,
                MonsterId = monster.MonsterId,
                Nickname = monster.Nickname,
                Hp = monster.Hp,
                MaxHp = monster.MaxHp,
                PosX = monster.PosX,
                PosY = monster.PosY,
                PosZ = monster.PosZ,
                State = monster.State,
                Dir = monster.Dir,
                Damage = monster.Damage
            });
        }
        return monsterInfos;
    }

    //기존 플레이어 리스트 반환
    public List<PlayerInfo> GetPlayers()
    {
        List<PlayerInfo> players = new List<PlayerInfo>();
        foreach (Player player in _players.Values)
        {
            players.Add(new PlayerInfo
            {
                CharacterId = player.CharacterId,
                Nickname = player.Nickname,
                Level = player.Level,
                ClassId = player.ClassId,
                PosX = player.PosX,
                PosY = player.PosY,
                PosZ = player.PosZ,
                Hp = player.Hp,
                MaxHp = player.MaxHp,
                State = player.State,
                Damage = player.Damage
            });
        }
        return players;
    }

    public void UpdatePlayer(int characterId, float posX, float posY, float posZ)
    {
        if (_players.TryGetValue(characterId, out Player player))
        {
            player.PosX = posX;
            player.PosY = posY;
            player.PosZ = posZ;
        }
    }

    public void AddPlayer(Player player)
    {
        _players[player.CharacterId] = player;
        player.CurrentMap = this;
    }

    public void Enter(Player newPlayer)
    {
        _isActive = true;
        _isPlayerZero = false;

        //해당 플레이어 맵에 추가
        _players[newPlayer.CharacterId] = newPlayer;
        newPlayer.CurrentMap = this;
        newPlayer.Map = this.MapId;
        Console.WriteLine($"플레이어 id: {newPlayer.CharacterId} 를 Map: {MapId}에 추가");

        //해당 플레이어 스폰 브로드캐스트
        SpawnPlayerResponse spawnSelf = new SpawnPlayerResponse
        {
            player = new PlayerInfo
            {
                CharacterId = newPlayer.CharacterId,
                Nickname = newPlayer.Nickname,
                Level = newPlayer.Level,
                ClassId = newPlayer.ClassId,
                PosX = newPlayer.PosX,
                PosY = newPlayer.PosY,
                PosZ = newPlayer.PosZ,
                Hp = newPlayer.Hp,
                MaxHp = newPlayer.MaxHp
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

            DespawnPlayerResponse spawnSelf = new DespawnPlayerResponse
            {
                CharacterId = characterId
            };
            Broadcast(spawnSelf, characterId);
        }
        Console.WriteLine($"플레이어 id: {characterId} 가 Map: {MapId} 에서 퇴장");

        if (_players.Count == 0) _isPlayerZero = true;
    }

    public void ChangeMap(Player player, int newMapId, float targetPosX, float targetPosY)
    {
        Console.WriteLine($"newMapId: {newMapId}, x: {targetPosX}, y: {targetPosY}");
        //플레이어 스폰 위치 설정
        player.PosX = targetPosX;
        player.PosY = targetPosY;

        using (LogContext.PushProperty("MapId", MapId))
        using (LogContext.PushProperty("CharacterId", player.CharacterId))
        {
            Log.Information($"[캐릭터 정보] CharacterId: {player.CharacterId} Nickname: {player.Nickname} Level: {player.Level} HP: {player.Hp}");
        }

        //기존 맵 퇴장
        if(player.CurrentMap == null)
        {
            Console.WriteLine("플레이어의 현재맵이 null");
        }
        player.CurrentMap.Leave(player.CharacterId);

        //새로운 맵 정보 가져오기
        Map newMap = MapManager.Instance.GetMap(newMapId);
        if (newMap == null) //클라이언트 작업자의 실수 방지 (포탈 정보 오기입)
        {
            Console.WriteLine("해당 맵이 존재하지 않습니다.");
            return; 
        }

        //새 맵 입장
        newMap.Enter(player);

        var moveMapResponseBuff = PacketMaker.Instance.MoveMapResponse(true, newMapId, newMap.GetPlayers(), newMap.GetDropItems(), newMap.GetMonsters());
        player._mySession.Send(moveMapResponseBuff);
    }

    public List<ItemInfo> GetDropItems()
    {
        List<ItemInfo> items = new List<ItemInfo>();
        foreach (Item item in _dropItems.Values)
        {
            items.Add(new ItemInfo
            {
                InventoryId = item.InventoryId,
                OwnerId = item.OwnerId,
                ItemId = item.ItemId,
                Count = item.Count,
                IsEquipped = item.IsEquipped,
                Enhancement = item.Enhancement,
                PosX = item.PosX,
                PosY = item.PosY,
                PosZ = item.PosZ
            });
        }
        return items;
    }

    public void AddDropItem(Item item)
    {
        _dropItems[item.InventoryId] = item; //_dropItems.Add(item.InventoryId, item);로 안하고 저렇게 하면 같은 키를 덮어 씀. 중복처리.
    }

    public void RemoveDropItem(int inventoryId)
    {
        _dropItems.Remove(inventoryId);
    }

    //public async void PickUpItem(UserSession session, int inventoryId)
    public void PickUpItem(UserSession session, int inventoryId)
    {
        Item pickedItem = null;

        if (_dropItems.TryGetValue(inventoryId, out pickedItem))
        {
            _dropItems.Remove(inventoryId);
        }
        else
        {
            //먼저 주운 사람이 있음
            return;
        }

        //아이템 오너 변경
        pickedItem.OwnerId = session.MyPlayer.CharacterId;

        //아이템 소유권 업데이트 -> setdirty에서 하므로 즉시할 필요 x
        /*int result = await DbManager.ItemOwnerUpdate(inventoryId, pickedItem.OwnerId);
        if (result <= 0) throw new Exception("DB Update Failed");*/

        //본인 인벤토리에 추가
        //session.MyPlayer.Inventory.AddItem(pickedItem);
        session.MyPlayer.AddItem(pickedItem);
        //퀘스트 상태에 반영 -> 플렝이어.AddItem함수에서 처리함
        //session.MyPlayer.QuestComponent.OnNotifyEvent(2, pickedItem.ItemId, pickedItem.Count);

        

        //본인 세션에 결과 전송 -> 플레이어.AddItem에서 처리
        /*var pickUpItemResponseBuff = PacketMaker.Instance.PickUpItemResponse(pickedItem, true);
        session.Send(pickUpItemResponseBuff);*/

        //브로드캐스트
        var dropItemDestroy = PacketMaker.Instance.DropItemDestroy(MapId, inventoryId);

        int exceptPlayerId = session.MyPlayer.CharacterId;

        BroadcastArraySegment(dropItemDestroy, exceptPlayerId);

        /*foreach (var player in _players.Values)
        {
            if (player.CharacterId == exceptPlayerId)
                continue;
            player._mySession.Send(dropItemDestroy);
            Console.WriteLine($"player: {player.CharacterId} 에게 디스트로이 전송완료.");
        }*/
    }

    public void Broadcast<T>(T packet, int exceptPlayerId) where T : IPacket
    {
        foreach (var player in _players.Values)
        {
            if (player.CharacterId == exceptPlayerId)
                continue;
            player.Send(packet); //다름. 이후에 통합해야됨
        }
    }

    public void BroadcastArraySegment(ArraySegment<byte> packet, int exceptPlayerId = -1)
    {
        foreach (var player in _players.Values)
        {
            if (player.CharacterId == exceptPlayerId)
                continue;
            player._mySession.Send(packet);
        }
    }

    public void BroadcastSpwanItem(Item copyItem, int exceptPlayerId = -1)
    {
        ArraySegment<byte> sendBuff = PacketMaker.Instance.MakeSpawnItemBuffer(copyItem, MapId, true);

        BroadcastArraySegment(sendBuff, exceptPlayerId);

        /*foreach (var player in _players.Values)
        {
            if (player.CharacterId == exceptPlayerId)
                continue;
            player._mySession.Send(sendBuff);
        }*/
    }

    //public void SpawnMonster(int MonsterId, string nickname, float PosX, float PosY, float MinPosX, float MaxPosX, int damage)
    public void SpawnMonster()
    {
        int spawnId = 1;
        foreach(var moveData in _monsterPatrolInfo)
        {
            MonsterTemplate monsterData = DataManager.Instance.Monster.GetMonsterData(moveData.MonsterId);

            Monster monster = new Monster(spawnId, monsterData, moveData, this);

            Console.WriteLine($"spawnId: {monster.SpawnId}, monsterId: {monster.MonsterId} posX: {monster.PosX}, posY: {monster.PosY}, minPosX: {monster.MinPosX}, maxPosX: {monster.MaxPosX}");
            _monsters[monster.SpawnId] = monster;
            spawnId++;
        }
    }

    public void RespawnMonster(int spawnId, float spawnPosX, float spawnPosY)
    {
        //Console.WriteLine($"RespawnMonster() spawnId: {spawnId} _monsterPatrolInfo.count: {_monsterPatrolInfo.Count} _monsters.count: {_monsters.Count}");
        //몬스터가 삭제되지 않고 State 9로 업데이트만 하지 않았기 때문에 무언가를 새로 생성할 필요는 없음
        /*MonsterPatrolInfo monsterData = _monsterPatrolInfo[_monsters[spawnId].MonsterId];
        _monsters[spawnId].PosX = monsterData.PosX;
        _monsters[spawnId].PosY = monsterData.PosY;
        _monsters[spawnId].Hp = _monsters[spawnId].MaxHp;*/

        Console.WriteLine($"id: {spawnId}, spawnPosX: {spawnPosX}, spawnPosY: {spawnPosY}");

        var monsterSpawnResponseBuff = PacketMaker.Instance.MonsterSpawn(spawnId, spawnPosX, spawnPosY);
        BroadcastArraySegment(monsterSpawnResponseBuff);
    }

    public void TakeDamage(int characterId, int damage)
    {
        if(_players.TryGetValue(characterId, out var player))
        {
            player.Hp -= damage;
            if(player.Hp <= 0)
            {
                player.Hp = 0;
                player.State = 9; //9: 죽음

                var playerDeathResponseBuff = PacketMaker.Instance.PlayerDeath(characterId);
                BroadcastArraySegment(playerDeathResponseBuff);
            }
            else
            {
                var playerTakeDamageResponseBuff = PacketMaker.Instance.PlayerTakeDamage(characterId, player.Hp, damage);
                BroadcastArraySegment(playerTakeDamageResponseBuff);
            }
        }
    }

    public async void PlayerHitMonster(UserSession session, int characterId, int spawnId, int attackType, int damage, int knockbackDir)
    {
        if(_players.TryGetValue(characterId, out var player))
        {
            if(_monsters.TryGetValue(spawnId, out var monster))
            {
                int finalDamage = player.Damage; //방어력 없다고 가정
                monster.Hp -= finalDamage;

                if (monster.Hp <= 0)
                {
                    monster.Hp = 0;
                    monster.State = 9; //9: 죽음

                    //경험치 획득
                    //player.Exp += monster.Exp;
                    player.AddExp(monster.Exp);

                    //드랍 아이템 생성
                    int dropItemId = monster.GetDropItem();
                    Console.WriteLine("드랍아이템 번호 : " + dropItemId);
                    Item item = await CreateItemDB(dropItemId); //시간 남으면 일정 시간 사냥한 사람만 먹을 수 있도록 db랑 로직 수정할 것
                    item.PosX = monster.PosX;
                    item.PosY = monster.PosY;
                    //item.PosZ = monster.PosZ;
                    AddDropItem(item);

                    //몬스터 삭제
                    //리스폰할 것이기 때문에 삭제하지 않음
                    //_monsters.Remove(monsterId);

                    //퀘스트 상태에 반영
                    session.MyPlayer.QuestComponent.OnNotifyEvent(1, monster.MonsterId, 1);

                    //몬스터 데스 브로드캐스트
                    var monsterDeathResponseBuff = PacketMaker.Instance.MonsterDeath(spawnId);
                    BroadcastArraySegment(monsterDeathResponseBuff);

                    //아이템 스폰 브로드 캐스트
                    BroadcastSpwanItem(item);
                }
                else
                {
                    monster.OnKnockback(player, knockbackDir);

                    var playerHitResponseBuff = PacketMaker.Instance.PlayerHitMonster(spawnId, monster.Hp, finalDamage);
                    BroadcastArraySegment(playerHitResponseBuff);
                }
            }
        }
    }

    public async Task<Item> CreateItemDB(int itemId)
    {
        ItemTemplate itemTemplate = DataManager.Instance.Item.GetItemData(itemId);

        InventoryDto inventoryDto = new InventoryDto
        {
            owner_id = -2,
            item_id = itemId,
            count = 1,
            is_equipped = itemTemplate.Type == 1 ? 1 : 0,
            enhancement = 0
        };

        int newInventoryId = await DbManager.InsertItem(inventoryDto);
        if (newInventoryId <= 0) throw new Exception("DB Insert Failed");

        Item item = new Item()
        {
            InventoryId = newInventoryId,
            OwnerId = -1,
            ItemId = itemId,
            Count = 1,
            IsEquipped = itemTemplate.Type == 1 ? true : false,
            Enhancement = 0
        };
        
        return item;
    }

    private void BroadcastMonsterUpdates()
    {
        foreach (var monster in _monsters.Values)
        {
            if (monster.IsDirty)
            {
                var monsterMoveBuff = PacketMaker.Instance.MonsterMove(monster);

                foreach (var player in _players.Values)
                {
                    player._mySession.Send(monsterMoveBuff);
                }

                monster.IsDirty = false;
            }
        }
    }
}