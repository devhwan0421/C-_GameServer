using Google.Protobuf;
using Protocol;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;

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

    /*public async Task OnRecvPacket(UserSession session, PacketID id, string json)
    {
        //int characterId = session.MyPlayer

        using (LogContext.PushProperty("SessionId", session.SessionId))
        using (LogContext.PushProperty("PacketID", id))
        {
            if (_handlers.TryGetValue(id, out var handler))
            {
                await handler.Invoke(session, json);
            }
            else
            {
                Log.Warning("[PacketHandler] 정의되지 않은 패킷이 수신되었습니다.");
            }
        }
    }*/
    public Task OnRecvPacket(UserSession session, PacketID id, string json)
    {
        if (_handlers.TryGetValue(id, out var handler))
        {
            return handler.Invoke(session, json);
        }

        Log.Warning("[PacketHandler] 정의되지 않은 패킷이 수신되었습니다.");
        return Task.CompletedTask;
    }

    public Task OnRecvPacketProto(UserSession session, _PacketID id, IMessage packetData)
    {
        switch (id)
        {
            case _PacketID.PlayerMoveRequestId:
                HandlePlayerMoveRequest(session, (PlayerMoveRequestProto)packetData);
                break;
            case _PacketID.TimeSyncRequestId:
                HandleTimeSyncProto(session, (TimeSyncRequestProto)packetData);
                break;
        }
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

        bool result = await LoginManager.Instance.TryLogin(loginDto.id, session);
        if (!result)
        {
            Log.Fatal("[PacketHandler] 로그인 실패(TryLogin)");

            return;
        }

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
        int timeoutMs = 5000; // 5초 타임아웃 설정

        try
        {
            // 1. 캐릭터 정보 조회 타임아웃 처리
            var charTask = DbManager.GetCharacterByCharacterId(req.CharacterId);
            if (await Task.WhenAny(charTask, Task.Delay(timeoutMs)) != charTask)
            {
                throw new TimeoutException("캐릭터 정보 조회 타임아웃");
            }
            CharacterDto character = await charTask;
            if (character == null) return;

            Log.Information($"[character] {req.CharacterId}");

            // 2. 나머지 데이터들 (인벤, 퀘스트 등) 병렬 조회 및 타임아웃 처리
            Task<List<InventoryDto>> inventoryTask = DbManager.GetInventoryByOwnerId(character.id);
            Task<List<QuestDto>> questTask = DbManager.GetQuestByCharacterId(character.id);
            Task<List<QuestProgressDto>> questProgressTask = DbManager.GetQuestProgressByCharacterId(character.id);

            Log.Information($"[questProgressTask] {req.CharacterId}");
            // 모든 작업이 5초 안에 끝나는지 감시
            var allTasks = Task.WhenAll(inventoryTask, questTask, questProgressTask);
            if (await Task.WhenAny(allTasks, Task.Delay(timeoutMs)) != allTasks)
            {
                throw new TimeoutException("인벤토리/퀘스트 데이터 로드 타임아웃");
            }
            Log.Information($"[WhenAll] {req.CharacterId}");
            var inventory = await inventoryTask;
            var quest = await questTask;
            var questProgress = await questProgressTask;

            // --- 이후 로직 동일 ---
            Player player = new Player(session, character);
            bool result = await PlayerManager.Instance.Enter(player);
            if (!result) return;

            session.MyPlayer = player;
            if (inventory != null) player.Inventory.InitInventory(inventory);
            if (quest != null) player.QuestComponent.LoadDbQuestTable(quest, questProgress);

            Map targetMap = MapManager.Instance.GetMap(player.Map);
            targetMap.Enter(player);

            var enterWorldResponseBuff = PacketMaker.Instance.EnterWorldResponse(true, character, inventory, targetMap.GetPlayers(), targetMap.GetDropItems(), targetMap.GetMonsters());
            session.Send(enterWorldResponseBuff);

            Log.Information($"[Enter World Success] {player.Nickname}");
        }
        catch (TimeoutException ex)
        {
            Log.Error($"[Enter World Timeout] {req.CharacterId} : {ex.Message}");
            // 클라이언트에게 실패 패킷 전송 로직이 있다면 추가
        }
        catch (Exception ex)
        {
            Log.Error($"[Enter World Error] {ex.Message}");
        }
    }

    private Task HandlePlayerMoveRequest(UserSession session, string json)
   {
        PlayerMoveRequest req = JsonSerializer.Deserialize<PlayerMoveRequest>(json);
        session.MyPlayer.UpdatePlayerPos(req);

        return Task.CompletedTask;
    }

    private Task HandlePlayerMoveRequest(UserSession session, PlayerMoveRequestProto req){
        //서버에서 이동 패킷을 받으면 처리하는 함수
        session.MyPlayer.UpdatePlayerPos(req); //플레이어 좌표받으면 해당 객체에 업데이트만 하고 종료
        return Task.CompletedTask;
    }

    private Task HandleTimeSyncProto(UserSession session, TimeSyncRequestProto req)
    {
        var res = new TimeSyncResponseProto
        {
            ClientSendTime = req.ClientSendTime,
            ServerTime = DateTime.UtcNow.Ticks
        };
        ArraySegment<byte> packet = PacketSerializer.SerializeProto((ushort)res.PacketId, res);
        session.Send(packet);

        return Task.CompletedTask;
    }

    private Task HandleUseItemRequest(UserSession session, string json)
    {
        UseItemRequest req = JsonSerializer.Deserialize<UseItemRequest>(json);

        //session.MyPlayer.Inventory.UseItem(session, req.InventoryId);
        session.MyPlayer.UseItem(req.InventoryId);

        return Task.CompletedTask;
    }

    private Task HandleDropItemRequest(UserSession session, string json)
    {
        DropItemRequest req = JsonSerializer.Deserialize<DropItemRequest>(json);

        //session.MyPlayer.Inventory.DropItem(session, req.InventoryId, req.MapId, req.PosX, req.PosY, req.PosZ);
        session.MyPlayer.DropItem(req);

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