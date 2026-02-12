using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class GameLogicThread : SynchronizationContext //https://blog.naver.com/vactorman/220371851151
                                                     //https://blog.naver.com/vactorman/220340600110 차근차근 더 자세히 읽어볼 것
{
    public static GameLogicThread Instance { get; } = new GameLogicThread();

    private readonly BlockingCollection<Action> _jobQueue = new BlockingCollection<Action>();
    public Thread LogicThread { get; private set; }

    /*public GameLogicThread()
    {
        LogicThread = new Thread(RunLoop) { Name = "LogicThread" };
    }*/

    public void Start()
    {
        LogicThread = new Thread(RunLoop) { Name = "LogicThread" };
        LogicThread.Start();
    }

    private void RunLoop()
    {
        SynchronizationContext.SetSynchronizationContext(this); //스레드에 SynchronizationContext 할당.
                                                                //이 스레드에서 실행되는 모든 비동기 작업은 이 컨텍스트를 사용
        Console.WriteLine($"[System] 로직 스레드 시작 (ID: {Thread.CurrentThread.ManagedThreadId})");

        Stopwatch sw = new Stopwatch();
        sw.Start();
        long lastTick = sw.ElapsedMilliseconds;

        Stopwatch jobTimer = new Stopwatch();

        while (true)
        {
            long currentTick = sw.ElapsedMilliseconds;
            float deltaTime = (currentTick - lastTick) / 1000.0f;
            lastTick = currentTick;

            jobTimer.Restart();

            while (_jobQueue.TryTake(out Action job))
            {
                try
                {
                    job.Invoke();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                if (jobTimer.ElapsedMilliseconds > 8) break;
            }

            MapManager.Instance.Update(deltaTime);

            PlayerManager.Instance.Update(deltaTime);

            if(_jobQueue.Count == 0)
            {
                Thread.Sleep(1);
            }
        }
    }
    /*private void RunLoop()
    {
        SynchronizationContext.SetSynchronizationContext(this); //스레드에 SynchronizationContext 할당.
                                                                //이 스레드에서 실행되는 모든 비동기 작업은 이 컨텍스트를 사용
        Console.WriteLine($"[System] 로직 스레드 시작 (ID: {Thread.CurrentThread.ManagedThreadId})");
        
        foreach (var job in _jobQueue.GetConsumingEnumerable()) //데이터가 들어올 때까지 스레드를 일시 정지
        {
            job.Invoke();
        }
    }*/

    public override void Post(SendOrPostCallback d, object state)
    {
        _jobQueue.Add(() => d(state)); 
    }

    public void Enqueue(Action action) => _jobQueue.Add(action);
}