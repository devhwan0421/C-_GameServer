using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTestProject1
{
    [TestClass]
    public class StressTest
    {
        private const int TOTAL_CLIENT_COUNT = 1000;
        private const int REQUEST_PER_SECOND = 10;
        private const int TEST_DURATION_SEC = 1200;

        private int _connectedCount = 0;
        private int _loginCount = 0;
        private int _charListCount = 0;
        private int _worldEntryCount = 0;
        private int _errorCount = 0;

        [TestMethod]
        public async Task MassiveConnectionStressTest()
        {
            // 1. 초기화
            _connectedCount = 0; _loginCount = 0; _charListCount = 0;
            _worldEntryCount = 0; _errorCount = 0;

            var behaviorTasks = new List<Task>();
            // 5번 유저부터 시작 (총 96명)
            int startIdx = 5;
            int targetCount = TOTAL_CLIENT_COUNT - startIdx + 1;

            Console.WriteLine($"{targetCount}명의 유저 시나리오 시작...");

            // 2. 클라이언트 생성 루프
            for (int i = startIdx; i <= TOTAL_CLIENT_COUNT; i++)
            {
                int userIndex = i;
                behaviorTasks.Add(Task.Run(async () =>
                {
                    DummyClient client = new DummyClient();
                    try
                    {
                        // 접속 시도
                        if (await client.ConnectAsync("127.0.0.1", 7777))
                        {
                            Interlocked.Increment(ref _connectedCount);

                            // 로그인
                            var loginReq = new LoginRequest { Username = $"user{userIndex}", Password = $"user{userIndex}" };
                            client.Send(PacketSerializer.Serialize((ushort)PacketID.LoginRequest, JsonSerializer.Serialize(loginReq)));
                            await client.WaitForPacket((ushort)PacketID.LoginResponse);
                            Interlocked.Increment(ref _loginCount);

                            // 캐릭터 목록
                            string charListJson = await client.WaitForPacket((ushort)PacketID.GetCharacterListResponse);
                            var charListRes = JsonSerializer.Deserialize<GetCharacterListResponse>(charListJson);
                            Interlocked.Increment(ref _charListCount);

                            if (charListRes?.Characters != null && charListRes.Characters.Count > 0)
                            {
                                // 월드 입장 (첫 번째 캐릭터)
                                var enterReq = new EnterWorldRequest { CharacterId = charListRes.Characters[0].CharacterId };
                                client.Send(PacketSerializer.Serialize((ushort)PacketID.EnterWorldRequest, JsonSerializer.Serialize(enterReq)));
                                await client.WaitForPacket((ushort)PacketID.EnterWorldResponse);
                                Interlocked.Increment(ref _worldEntryCount);

                                // --- 패트롤 시뮬레이션 설정 ---
                                Random rand = new Random();
                                int delay = 1000 / REQUEST_PER_SECOND;
                                DateTime endTime = DateTime.Now.AddSeconds(TEST_DURATION_SEC);

                                // 1. 초기 위치 설정 (DB에 저장된 마지막 위치가 있다면 그걸 써도 되지만, 여기서는 랜덤 시작)
                                float curX = (float)(rand.NextDouble() * 22 - 11);
                                //float curZ = (float)(rand.NextDouble() * 22 - 11);
                                float curY = (float)(rand.NextDouble() * 8);

                                // 2. 이동 속도 설정 (초당 3m 이동 가정)
                                float speed = 3.0f;
                                float moveDistance = speed / REQUEST_PER_SECOND;

                                int myId = charListRes.Characters[0].CharacterId;
                                string fastJson = $"{{\"CharacterId\":{myId},\"PosX\":11.11,\"PosY\":0.0,\"PosZ\":0.0,\"Dir\":1,\"State\":1}}";
                                ArraySegment<byte> finalPacket = PacketSerializer.Serialize((ushort)PacketID.PlayerMoveRequest, fastJson);
                                while (DateTime.Now < endTime)
                                {
                                    // [최적화 포인트 2] 
                                    // JsonSerializer 대신 보간 문자열($)을 사용하거나, 
                                    // 아예 고정된 값을 보냄으로써 CPU 연산을 아낍니다.
                                    // 서버 부하 테스트가 목적이라면 고정된 좌표 패킷을 반복 전송해도 무방합니다.


                                    // PacketSerializer 내부의 ThreadLocal 버퍼를 사용하여 전송
                                    //client.Send(PacketSerializer.Serialize((ushort)PacketID.PlayerMoveRequest, fastJson));
                                    //client.Send(finalPacket);
                                    await client.SendAsync(finalPacket);

                                    await Task.Delay(delay);
                                }

                                /*while (DateTime.Now < endTime)
                                {
                                    // 3. 현재 위치에서 아주 조금씩 이동 (방향성 유지)
                                    curX += (float)(rand.NextDouble() * 2 - 1) * moveDistance;
                                    //curZ += (float)(rand.NextDouble() * 2 - 1) * moveDistance;

                                    // 4. 이동 범위 제한 (-11 ~ 11 사이 유지)
                                    if (curX > 11.0f) curX = 11.0f;
                                    if (curX < -11.0f) curX = -11.0f;
                                    //if (curZ > 11.0f) curZ = 11.0f;
                                    //if (curZ < -11.0f) curZ = -11.0f;

                                    var moveReq = new PlayerMoveRequest
                                    {
                                        CharacterId = charListRes.Characters[0].CharacterId,
                                        PosX = (float)Math.Round(curX, 2),
                                        PosY = (float)Math.Round(curY, 2),
                                        //PosZ = (float)Math.Round(curZ, 2),
                                        Dir = rand.Next(0, 360),
                                        State = 1 // 1: 이동 중
                                    };

                                    string json = JsonSerializer.Serialize(moveReq);
                                    client.Send(PacketSerializer.Serialize((ushort)PacketID.PlayerMoveRequest, json));
                                    await Task.Delay(delay);
                                }*/
                            }
                        }
                        else
                        {
                            Interlocked.Increment(ref _errorCount);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[User{userIndex}] Error: {ex.Message}");
                        Interlocked.Increment(ref _errorCount);
                    }
                    finally
                    {
                        Debug.WriteLine($"[User{userIndex}] finally");
                        System.Diagnostics.Trace.WriteLine($"### [FINALLY] User{userIndex} - 종료됨 (현재시간: {DateTime.Now:HH:mm:ss}) ###");
                        client.Disconnect();
                    }
                }));
                //await Task.Delay(20);
            }

            // 3. 모니터링 루프 (메인 스레드에서 직접 수행)
            // 모든 클라이언트가 '월드입장' 했거나 '에러'가 날 때까지 대기
            while (_worldEntryCount + _errorCount < targetCount)
            {
                await Task.Delay(1000);
                string status = $"[STATUS] 접속:{_connectedCount} | 로그인:{_loginCount} | 월드입장:{_worldEntryCount} | 에러:{_errorCount}";
                Console.WriteLine(status);
                Debug.WriteLine(status);

                // 만약 10초 넘게 에러만 올라가거나 변화가 없다면 서버 상태 확인 필요
            }

            // 4. 모든 태스크의 최종 완료 대기
            await Task.WhenAll(behaviorTasks);
            Console.WriteLine("부하 테스트가 최종 종료되었습니다.");
        }
    }
}