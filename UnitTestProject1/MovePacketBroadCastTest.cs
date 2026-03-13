using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UnitTestProject1
{
    [TestClass]
    public class MovePacketBroadCastStressTest
    {
        List<Map> _maps;
        List<Player> _players;

        int USERCOUNT = 10000;
        int MAX_MAP_USERCOUNT = 500;

        public MovePacketBroadCastStressTest() {
            _maps = new List<Map>();
            _players = new List<Player>();
        }

        [TestMethod]
        public void TestMovePacketBroadCast() {
            InitMap();
            GenerateSessionAndPlayer();

            for (int i = 0; i < 3; i++)
            {
                foreach (Map map in _maps)
                {
                    map.BroadcastPlayerPosUpdates(0);
                }

                foreach (var player in _players)
                {
                    player.NeedMoveSync = true;
                }
            }

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            foreach (Map map in _maps)
            {
                map.BroadcastPlayerPosUpdates(0); //1만명 브로드캐스트 수행
            }

            sw.Stop();

            Console.WriteLine($"[테스트 조건] 유저수: {USERCOUNT}명, 맵 개수: {USERCOUNT / MAX_MAP_USERCOUNT}개, 맵당 유저수: {MAX_MAP_USERCOUNT}명");
            Console.WriteLine($"[성능 결과] 브로드캐스트 총 수행 시간: {sw.Elapsed.TotalMilliseconds} ms");
        }

        public void InitMap()
        {
            for (int mapId = 0; mapId < (USERCOUNT / MAX_MAP_USERCOUNT); mapId++)
            {
                MapTemplate mapTemplate = new MapTemplate
                {
                    MapId = mapId + 1
                };

                Map map = new Map(mapTemplate);
                _maps.Add(map);
            }
        }

        public void GenerateSessionAndPlayer() {
            int mapId = 0;
            for (int i = 0; i < USERCOUNT; i++)
            {
                if (i != 0 && i % MAX_MAP_USERCOUNT == 0)
                {
                    mapId++;
                }

                TestUserSession session = new TestUserSession();
                Player player = new Player(session, new CharacterDto
                {
                    id = i + 1,
                    pos_x = 10,
                    pos_y = 20,
                    pos_z = 30,
                });
                player.Dir = 1;
                player.State = 1;
                player.Vx = 2.2552f;
                player.Vy = 3.2523f;
                player.Timestamp = 1231231231232323232;
                player.NeedMoveSync = true;

                _players.Add(player);
                session.MyPlayer = player;
                _maps[mapId].AddPlayer(player);
            }
        }
    }

    public class TestUserSession : UserSession
    {
        public TestUserSession() : base(null, null, null, 0) { }

        internal override void RegisterSend() { }
    }
}