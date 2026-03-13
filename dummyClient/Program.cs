using Google.Protobuf.WellKnownTypes;
using Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StressTester
{
    class Program
    {
        // 기본 설정 (인자가 없을 경우 사용)
        private static string SERVER_IP = "127.0.0.1";
        private static int SERVER_PORT = 7777;
        private static int REQUEST_PER_SECOND = 10;
        private static int TEST_DURATION_SEC = 420;

        // 카운터 변수
        private static int _connectedCount = 0;
        private static int _loginCount = 0;
        private static int _worldEntryCount = 0;
        private static int _errorCount = 0;
        private static int _activeMovingCount = 0;

        static async Task Main(string[] args)
        {
            // 실행 인자 처리: StressTester.exe [시작인덱스] [유저수] [서버IP]
            int startIdx = args.Length > 0 ? int.Parse(args[0]) : 5; // db에 user4 안 만듬
            int clientCount = args.Length > 1 ? int.Parse(args[1]) : 50;
            if (args.Length > 2) SERVER_IP = args[2];

            int targetCount = clientCount;
            int endIdx = startIdx + clientCount - 1;

            Console.WriteLine("==================================================");
            Console.WriteLine($"부하 테스트 에이전트 시작 (초당 200회 - 420초 유지)");
            Console.WriteLine($"서버: {SERVER_IP}:{SERVER_PORT}");
            Console.WriteLine($"범위: user{startIdx} ~ user{endIdx} (총 {clientCount}명)");
            Console.WriteLine("==================================================");

            var behaviorTasks = new List<Task>();

            // 1. 클라이언트 생성 및 접속 루프
            for (int i = startIdx; i <= endIdx; i++)
            {
                int userIndex = i;
                behaviorTasks.Add(Task.Run(async () =>
                {
                    DummyClient client = new DummyClient();
                    bool enteredMovingLoop = false;

                    try
                    {
                        // 접속 시도
                        if (await client.ConnectAsync(SERVER_IP, SERVER_PORT))
                        {
                            Interlocked.Increment(ref _connectedCount);

                            // 로그인
                            var loginReq = new LoginRequest { Username = $"user{userIndex}", Password = $"user{userIndex}" };
                            client.Send(PacketSerializer.Serialize((ushort)PacketID.LoginRequest, JsonSerializer.Serialize(loginReq)));
                            await client.WaitForPacket((ushort)PacketID.LoginResponse);
                            Interlocked.Increment(ref _loginCount);

                            // 캐릭터 목록 조회
                            string charListJson = await client.WaitForPacket((ushort)PacketID.GetCharacterListResponse);
                            var charListRes = JsonSerializer.Deserialize<GetCharacterListResponse>(charListJson);

                            if (charListRes?.Characters != null && charListRes.Characters.Count > 0)
                            {
                                // 월드 입장 (첫 번째 캐릭터)
                                var enterReq = new EnterWorldRequest { CharacterId = charListRes.Characters[0].CharacterId };
                                client.Send(PacketSerializer.Serialize((ushort)PacketID.EnterWorldRequest, JsonSerializer.Serialize(enterReq)));
                                await client.WaitForPacket((ushort)PacketID.EnterWorldResponse);
                                Interlocked.Increment(ref _worldEntryCount);

                                // --- 이동 시뮬레이션 설정 ---
                                Interlocked.Increment(ref _activeMovingCount);
                                enteredMovingLoop = true;

                                int delay = 1000 / REQUEST_PER_SECOND;
                                DateTime endTime = DateTime.Now.AddSeconds(TEST_DURATION_SEC);

                                // [최적화] 패킷 미리 구워두기
                                int myId = charListRes.Characters[0].CharacterId;
                                //string fastJson = $"{{\"CharacterId\":{myId},\"PosX\":11.11,\"PosY\":0.0,\"PosZ\":0.0,\"Dir\":1,\"State\":1}}";
                                //ArraySegment<byte> finalPacket = PacketSerializer.Serialize((ushort)PacketID.PlayerMoveRequest, fastJson);

                                PlayerMoveRequestProto packet = new PlayerMoveRequestProto
                                {
                                    CharacterId = myId,
                                    PosX = 11.11f,
                                    PosY = 0.0f,
                                    PosZ = 0.0f,
                                    Dir = 1,
                                    State = 1,
                                    Vx = 2.2552f,
                                    Vy = 3.2523f,
                                    TimeStamp = 1231231231232323232
                                };

                                ArraySegment<byte> finalPacket = PacketSerializer.SerializeProto((ushort)packet.PacketId, packet);


                                while (DateTime.Now < endTime)
                                {
                                    // 비동기 전송으로 스레드 차단 방지
                                    await client.SendAsync(finalPacket);
                                    await Task.Delay(delay);
                                }
                            }
                        }
                        else
                        {
                            Interlocked.Increment(ref _errorCount);
                        }
                    }
                    catch (Exception)
                    {
                        Interlocked.Increment(ref _errorCount);
                    }
                    finally
                    {
                        if (enteredMovingLoop) Interlocked.Decrement(ref _activeMovingCount);
                        client.Disconnect();
                    }
                }));

                // 접속 폭주로 인한 포트 고갈 및 서버 Accept 병목 방지
                await Task.Delay(30);

                if (i % 50 == 0) Console.WriteLine($"접속 시도 중... ({i - startIdx + 1}/{clientCount})");
            }

            // 2. 모니터링 루프
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    string status = $"[STATUS] 접속:{_connectedCount} | 로그인:{_loginCount} | 월드입장:{_worldEntryCount} | 이동중:{_activeMovingCount} | 에러:{_errorCount}";
                    Console.WriteLine(status);

                    if (_connectedCount > 0 && (_worldEntryCount + _errorCount >= targetCount) && _activeMovingCount == 0)
                    {
                        Console.WriteLine(">> 모든 유저 시나리오 종료.");
                        break;
                    }
                    await Task.Delay(1000);
                }
            });

            await Task.WhenAll(behaviorTasks);
            Console.WriteLine("부하 테스트 에이전트가 최종 종료되었습니다. 아무 키나 누르세요.");
            Console.ReadKey();
        }
    }
}