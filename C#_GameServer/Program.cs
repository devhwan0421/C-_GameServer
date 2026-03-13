using Serilog;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        /*AWS t3.micro에서 테스트를 위해 비활성화
        int processorCount = Environment.ProcessorCount;
        ThreadPool.SetMinThreads(processorCount, processorCount);
        ThreadPool.SetMaxThreads(processorCount, processorCount);
        */

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/lo-.txt", rollingInterval: RollingInterval.Day)
            .WriteTo.Seq("http://localhost:5341/")
            .CreateLogger();
        
        Log.Information("[System] 로그 기록 시작...");
        //Serilog.Debugging.SelfLog.Enable(msg => Log.Information(msg));

        PacketHandler handler = new PacketHandler();
        GameLogicThread gameLogicThread = GameLogicThread.Instance;
        gameLogicThread.Start();

        MapManager.Instance.Init();

        DbTransactionWorker.Instance.Start(64, gameLogicThread);

        Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listenSocket.Bind(new IPEndPoint(IPAddress.Any, 7777));
        listenSocket.Listen(1000);

        Log.Information("[System] 실버바인 엔진 서버 시작됨 ( 서버 대기 중...)");

        while (true)
        {
            Socket clientSocket = listenSocket.Accept();
            clientSocket.NoDelay = true;

            UserSession session = SessionManager.Instance.Generate(clientSocket, gameLogicThread, handler);
            if (session != null)
            {
                session.Start();
                Log.Debug($"[Accept] SessionID: {session.SessionId} IP: {clientSocket.RemoteEndPoint}");
            }
            else
            {
                Log.Error("[Accept] 세션 생성 실패");
                clientSocket.Close();
            }
        }
    }
}