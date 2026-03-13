using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace UnitTestProject1
{
    [TestClass]
    public class IntegrationTest
    {
        [TestMethod]
        public async Task Test_Full_Flow_To_WorldEntry()
        {
            // 1. 준비 및 접속
            DummyClient client = new DummyClient();
            await client.ConnectAsync("127.0.0.1", 7777);

            // 2. 로그인 요청
            var loginReq = new LoginRequest { Username = "user3", Password = "user3" };
            client.Send(PacketSerializer.Serialize((ushort)PacketID.LoginRequest, JsonSerializer.Serialize(loginReq)));

            // 3. 로그인 응답 및 캐릭터 목록 수신 대기
            await client.WaitForPacket((ushort)PacketID.LoginResponse);
            string charListJson = await client.WaitForPacket((ushort)PacketID.GetCharacterListResponse);
            var charListRes = JsonSerializer.Deserialize<GetCharacterListResponse>(charListJson);

            // 캐릭터가 하나도 없으면 테스트 실패
            Assert.IsNotNull(charListRes.Characters, "캐릭터 목록이 비어있습니다.");
            Assert.IsTrue(charListRes.Characters.Count > 0, "보유한 캐릭터가 없습니다.");

            // 4. 첫 번째 캐릭터 선택 및 월드 입장 요청
            int targetCharId = charListRes.Characters[0].CharacterId;
            var enterReq = new EnterWorldRequest { CharacterId = targetCharId };

            // EnterWorldRequest 전송
            client.Send(PacketSerializer.Serialize((ushort)PacketID.EnterWorldRequest, JsonSerializer.Serialize(enterReq)));

            // 5. 월드 입장 응답 대기
            string enterResJson = await client.WaitForPacket((ushort)PacketID.EnterWorldResponse);
            var enterRes = JsonSerializer.Deserialize<EnterWorldResponse>(enterResJson);

            // 6. 최종 검증
            Assert.IsTrue(enterRes.Success, "월드 입장에 실패했습니다.");
            Assert.IsNotNull(enterRes.Inventory, "인벤토리 정보가 로드되지 않았습니다.");

            Console.WriteLine($"[성공] 캐릭터 {enterRes.Character.Nickname}으로 월드 입장 완료!");
            Console.WriteLine($"[데이터] 인벤토리 아이템 수: {enterRes.Inventory.Count}");
        }
    }

    /*[TestClass]
    public class StressTest
    {
        private const int TOTAL_CLIENT_COUNT = 1; // 동시 접속자 수
        private const int REQUEST_PER_SECOND = 1; // 클라이언트당 초당 요청 횟수

        [TestMethod]
        public async Task MassiveConnectionStressTest()
        {
            List<DummyClient> clients = new List<DummyClient>();
            List<Task> connectionTasks = new List<Task>();

            Console.WriteLine($"{TOTAL_CLIENT_COUNT}개 세션 접속 시작...");

            // 1. 동시 접속 시도 (Burst Connect)
            for (int i = 0; i < TOTAL_CLIENT_COUNT; i++)
            {
                var client = new DummyClient();
                clients.Add(client);
                connectionTasks.Add(client.ConnectAsync("127.0.0.1", 7777));
            }

            // 모든 접속이 완료될 때까지 대기
            await Task.WhenAll(connectionTasks);
            Console.WriteLine("모든 클라이언트 접속 완료.");

            // 2. 초당 요청 횟수(TPS) 테스트 루프
            // 각 클라이언트가 독립적으로 패킷을 쏘는 Task 생성
            List<Task> spamTasks = new List<Task>();
            bool isRunning = true;

            foreach (var client in clients)
            {
                spamTasks.Add(Task.Run(async () =>
                {
                    int delay = 1000 / REQUEST_PER_SECOND;
                    while (isRunning)
                    {
                        // 이동 패킷이나 심박수(Heartbeat) 패킷 등을 시뮬레이션
                        var moveReq = new PlayerMoveRequest { CharacterId = 1, PosX = 10, PosY = 0, PosZ = 10 };
                        client.Send(PacketSerializer.Serialize((ushort)PacketID.PlayerMoveRequest, JsonSerializer.Serialize(moveReq)));

                        await Task.Delay(delay);
                    }
                }));
            }

            // 30초 동안 부하 유지 후 종료
            await Task.Delay(30000);
            isRunning = false;

            await Task.WhenAll(spamTasks);
            Console.WriteLine("부하 테스트 종료.");
        }
    }*/
}