using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTestProject1
{
    [TestClass]
    public class IntegrationTest
    {
        [TestMethod]
        public async Task IntegrationTestFunc()
        {
            // 1. 접속
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
}