using Dapper;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

public class PacketHandler
{
    private Dictionary<PacketID, Func<UserSession, string, Task>> _handlers = new Dictionary<PacketID, Func<UserSession, string, Task>>();

    public PacketHandler()
    {
        _handlers.Add(PacketID.LoginRequest, HandleLoginRequest);
        _handlers.Add(PacketID.EnterWorldRequest, HandleEnterWorldRequest);
        _handlers.Add(PacketID.PlayerMoveRequest, HandlePlayerMoveRequest);
        _handlers.Add(PacketID.UseItemRequest, HandleUseItemRequest);
        _handlers.Add(PacketID.DropItemRequest, HandleDropItemRequest);
        _handlers.Add(PacketID.MoveMapRequest, HandleMoveMapRequest);
        _handlers.Add(PacketID.DropItemDestroyResponse, HandleDropItemRequest);
        _handlers.Add(PacketID.PickUpItemRequest, HandlePickUpItemRequest);
        _handlers.Add(PacketID.PlayerTakeDamageRequest, HandlePlayerTakeDamageRequest);
        _handlers.Add(PacketID.PlayerHitRequest, HandlePlayerHitRequest);
        _handlers.Add(PacketID.NpcTalkRequest, HandleNpcTalkRequest);
        //_handlers.Add(PacketID.QuestAcceptRequest, HandleQuestRequest);
    }

    public Task OnRecvPacket(UserSession session, PacketID id, string json)
    {
        if(_handlers.TryGetValue(id, out var handler))
        {
            return handler.Invoke(session, json);
        }
        Console.WriteLine("정의되지 않은 패킷이 수신되었습니다.");
        return Task.CompletedTask;
    }

    private async Task HandleLoginRequest(UserSession session, string json)
    {
        var req = JsonSerializer.Deserialize<LoginRequest>(json);

        /*LoginDto loginDto = await DbTransactionWorker.Instance.PushQuery(db =>
        {
            return DbManager.LoginRequest(db, req.Username, req.Password);
        });*/
        LoginDto loginDto = await DbManager.LoginRequest(req.Username, req.Password);

        if (loginDto == null)
        {
            var resFailed = new LoginResponse { Username = req.Username, Success = false, Message = "로그인 실패" };
            session.Send(resFailed);    
            Console.WriteLine("DB에 일치하는 계정이 없습니다.");
            return;
        }

        LoginManager.Instance.TryLogin(loginDto.id, session);
        session.AccountId = loginDto.id;

        var res = new LoginResponse { Username = req.Username, Success = true, Message = "환영합니다. v0.1" };
        session.Send(res);
        Console.WriteLine($"[Login Success] {req.Username}");

        /*List<CharacterDto> characters = await DbTransactionWorker.Instance.PushQuery(db =>
        {
            return DbManager.GetCharacterListByUserId(db, loginDto.id);
        });*/
        List<CharacterDto> characters = await DbManager.GetCharacterListByUserId(loginDto.id);

        if (characters == null)
        {
            Console.WriteLine("보유한 캐릭터가 없습니다.");
            return;
        }

        Console.WriteLine($"보유한 캐릭터 수: {characters.Count}");

        GetCharacterListResponse res2 = new GetCharacterListResponse(); //월드 들어가기 전까지 임시로 가지고 있다가 활용하는 게 좋을 듯
                                                                        //월드 접속 시 DB 조회 안 해도 됨

        foreach (var dbData in characters) //Linq로 변환할 것
        {
            res2.Characters.Add(new CharacterInfo
            {
                CharacterId = dbData.id,
                UserId = dbData.user_id,
                Nickname = dbData.nickname,
                ClassId = dbData.class_id,
                Level = dbData.level,
                Exp = dbData.exp,
                Map = dbData.map,
                Pos_x = dbData.pos_x,
                Pos_y = dbData.pos_y,
                Pos_z = dbData.pos_z
            });
            //Console.WriteLine($"캐릭터 ID: {dbData.id}, 이름: {dbData.nickname}, 레벨: {dbData.level}");
            //조회한 id값이 제대로 안 되고 있음. 캐릭터id가 0, 1 이어야하는데 둘 다 0이고 userid도 1인데 둘다 0으로 나옴
            //level값은 잘 받고 있었는데 dto를 Lev_el로 바꾸니 못 받음. 이름이 같아야 인식하는 듯
        }

        session.Send(res2);
        //Console.WriteLine($"[Send] {JsonSerializer.Serialize(res2)}");
    }

    private async Task HandleEnterWorldRequest(UserSession session, string json)
    {
        EnterWorldRequest req = JsonSerializer.Deserialize<EnterWorldRequest>(json);

        //DB에서 캐릭터 정보 조회 => 로그인 때 조회한 정보 활용하는 걸로 바꿀 것(db x)
        CharacterDto character = await DbManager.GetCharacterByCharacterId(req.CharacterId);
        if (character == null) return;

        //플레이어 객체 생성 및 플레이어 매니저 등록 + 세션에 플레이어 연결
        Player player = new Player(session, character);
        PlayerManager.Instance.Enter(player);
        session.MyPlayer = player;

        //맵 조회 및 입장
        Map targetMap = MapManager.Instance.GetMap(player.Map);
        targetMap.Enter(player);//클라이언트랑 순서 맞춰야 함. 현재 좀 애매해짐 => 해당 유저 빼고 모두 브로드 캐스트. 이대로 진행

        //인벤토리 조회
        List<InventoryDto> inventory = await DbManager.GetInventoryByOwnerId(player.CharacterId);
        if (inventory != null)
        {
            //세션의 플레이어 객체에 인벤토리 캐싱
            player.Inventory.InitInventory(inventory);

            //리스폰스 전송
            var enterWorldResponseBuff = PacketMaker.Instance.EnterWorldResponse(true, character, inventory, targetMap.GetPlayers(), targetMap.GetDropItems(), targetMap.GetMonsters());
            session.Send(enterWorldResponseBuff);
        }

        Console.WriteLine($"[Enter World] 캐릭터 ID: {player.CharacterId}, 이름: {player.Nickname}, 위치: ({player.PosX}, {player.PosY}, {player.PosZ})");
    }

    private Task HandlePlayerMoveRequest(UserSession session, string json)
    {
        //Console.WriteLine($"무브패킷 도착: {json}");
        PlayerMoveRequest req = JsonSerializer.Deserialize<PlayerMoveRequest>(json);

        //int playerId = session.MyPlayer.CharacterId;

        Map targetMap = MapManager.Instance.GetMap(session.MyPlayer.Map);
        targetMap.UpdatePlayer(req.CharacterId, req.PosX, req.PosY, req.PosZ);

        if (req.State == 3)
            Console.WriteLine("3333333333333");

        PlayerMoveResponse res = new PlayerMoveResponse
        {
            CharacterId = req.CharacterId,
            PosX = req.PosX,
            PosY = req.PosY,
            PosZ = req.PosZ,
            Dir = req.Dir,
            State = req.State
        };

        //Console.WriteLine("브로드캐스트");
        targetMap.Broadcast(res, session.MyPlayer.CharacterId);

        return Task.CompletedTask;
    }

    private Task HandleUseItemRequest(UserSession session, string json)
    {
        UseItemRequest req = JsonSerializer.Deserialize<UseItemRequest>(json);

        session.MyPlayer.Inventory.UseItem(session, req.InventoryId);

        return Task.CompletedTask;
    }

    private Task HandleDropItemRequest(UserSession session, string json)
    {
        DropItemRequest req = JsonSerializer.Deserialize<DropItemRequest>(json);

        session.MyPlayer.Inventory.DropItem(session, req.InventoryId, req.MapId, req.PosX, req.PosY, req.PosZ);

        return Task.CompletedTask;
    }

    private Task HandleMoveMapRequest(UserSession session, string json)
    {
        MoveMapRequest req = JsonSerializer.Deserialize<MoveMapRequest>(json);
        Map currentMap = MapManager.Instance.GetMap(session.MyPlayer.Map);
        currentMap.ChangeMap(session.MyPlayer, req.TargetMapId, req.TargetPosX, req.TargetPosY);
        return Task.CompletedTask;
    }

    private Task HandlePickUpItemRequest(UserSession session, string json)
    {
        PickUpItemRequest req = JsonSerializer.Deserialize<PickUpItemRequest>(json);
        Map currentMap = MapManager.Instance.GetMap(req.MapId);
        currentMap.PickUpItem(session, req.InventoryId);
        return Task.CompletedTask;
    }

    private Task HandlePlayerTakeDamageRequest(UserSession session, string json)
    {
        PlayerTakeDamageRequest req = JsonSerializer.Deserialize<PlayerTakeDamageRequest>(json);
        Map currentMap = MapManager.Instance.GetMap(session.MyPlayer.Map);
        currentMap.TakeDamage(req.CharacterId, req.Damage);
        return Task.CompletedTask;
    }

    private Task HandlePlayerHitRequest(UserSession session, string json)
    {
        PlayerHitRequest req = JsonSerializer.Deserialize<PlayerHitRequest>(json);
        Map currentMap = MapManager.Instance.GetMap(session.MyPlayer.Map);
        currentMap.PlayerHitMonster(session, req.CharacterId, req.SpawnId, req.AttackType, req.Damage, req.KnockbackDir);
        return Task.CompletedTask;
    }

    private Task HandleNpcTalkRequest(UserSession session, string json)
    {
        //Console.WriteLine("대화요청");
        NpcTalkRequest req = JsonSerializer.Deserialize<NpcTalkRequest>(json);
        NpcManager.Instance.OnNpcTalk(session, req);
        //NpcDialogueManager.Instance.GetDialouge(session, req);
        return Task.CompletedTask;
    }

    /*private Task HandleQuestRequest(UserSession session, string json)
    {
        QuestAcceptRequest req = JsonSerializer.Deserialize<QuestAcceptRequest>(json);
        QuestManager.Instance.AddQuest(session, req.NpcId, req.QuestId);
        return Task.CompletedTask;
    }*/
}