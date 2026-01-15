using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;

class Program
{
    //public static JobQueue MainJobQueue = new JobQueue();
    static void Main(string[] args)
    {
        int processorCount = Environment.ProcessorCount;
        ThreadPool.SetMinThreads(processorCount, processorCount);
        ThreadPool.SetMaxThreads(processorCount * 2, processorCount * 2); //소켓 IO 스레드는 Task로 처리하고 있음. 최대 cpu 코어 * 2
                                                                          //게임 로직 스레드는 Thread 한개
                                                                          //DB 워커 스레드는 Thread 4개

        WorldManager.Instance.Init();

        PacketHandler handler = new PacketHandler();
        GameLogicThread gameLogicThread = new GameLogicThread();
        gameLogicThread.Start();

        DbTransactionWorker.Instance.Start(4, gameLogicThread);

        Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listenSocket.Bind(new IPEndPoint(IPAddress.Any, 7777));
        listenSocket.Listen(10); //listen: 최대 10개의 대기 큐

        Console.WriteLine("실버바인 엔진 서버 시작됨 ( 서버 대기 중...)");

        while (true)
        {
            Socket clientSocket = listenSocket.Accept();

            UserSession session = SessionManager.Instance.Generate(clientSocket, gameLogicThread, handler);
            if (session != null)
            {
                session.Start();
                Console.WriteLine($"[Accept] SessionID: {session.SessionId} IP: {clientSocket.RemoteEndPoint}");
            }
            else
            {
                Console.WriteLine("세션 생성 실패");
                clientSocket.Close();
            }
        }
    }
}