using Dapper;
using MySqlConnector;
using System;
using System.Collections.Generic;
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
    }

    public Task OnRecvPacket(UserSession session, PacketID id, string json)
    {
        if(_handlers.TryGetValue(id, out var handler))
        {
            return handler.Invoke(session, json);
        }

        return Task.CompletedTask;
    }

    private async Task HandleLoginRequest(UserSession session, string json)
    {
        var req = JsonSerializer.Deserialize<LoginRequest>(json);

        LoginDto loginDto = await DbTransactionWorker.Instance.PushQuery(db =>
        {
            return DbManager.LoginRequest(db, req.Username, req.Password);
        });

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

        List<CharacterDto> characters = await DbTransactionWorker.Instance.PushQuery(db =>
        {
            return DbManager.GetCharacterListByUserId(db, loginDto.id);
        });

        if (characters == null)
        {
            Console.WriteLine("보유한 캐릭터가 없습니다.");
            return;
        }

        Console.WriteLine($"보유한 캐릭터 수: {characters.Count}");

        GetCharacterListResponse res2 = new GetCharacterListResponse(); //월드 들어가기 전까지 임시로 가지고 있다가 활용하는 게 좋을 듯
                                                                        //월드 접속 시 DB 조회 안 해도 됨

        foreach (var dbData in characters)
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

        //Console.WriteLine($"[유저] ({session.AccountId}) 패킷 처리 시작");
        //await Task.Delay(1000);

        CharacterDto character = await DbTransactionWorker.Instance.PushQuery(db =>
        {
            return DbManager.GetCharacterByCharacterId(db, req.CharacterId);
        });

        if (character == null)
        {
            Console.WriteLine("[에러] 해당 캐릭터를 찾을 수 없습니다.");
            return;
        }

        Player player = new Player(session, character);

        Map targetMap = WorldManager.Instance.GetMap(player.Map);

        session.MyPlayer = player;
        PlayerManager.Instance.Enter(player);

        targetMap.Enter(player);

        EnterWorldResponse res = new EnterWorldResponse
        {
            Character = new CharacterInfo
            {
                CharacterId = character.id,
                UserId = character.user_id,
                Nickname = character.nickname,
                ClassId = character.class_id,
                Level = character.level,
                Exp = character.exp,
                Map = character.map,
                Pos_x = character.pos_x,
                Pos_y = character.pos_y,
                Pos_z = character.pos_z
            }
        };
        session.Send(res);
        //Console.WriteLine($"[Send] {JsonSerializer.Serialize(res)}");

        Console.WriteLine($"[Enter World] 캐릭터 ID: {player.CharacterId}, 이름: {player.Nickname}, 위치: ({player.PosX}, {player.PosY}, {player.PosZ})");
    }

    private Task HandleMoveRequest(UserSession session, string json)
    {
        return Task.CompletedTask;
    }
}