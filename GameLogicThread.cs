using Serilog;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

public class GameLogicThread : SynchronizationContext //https://blog.naver.com/vactorman/220371851151
                                                      //https://blog.naver.com/vactorman/220340600110 차근차근 더 자세히 읽어볼 것
{
    public static GameLogicThread Instance { get; } = new GameLogicThread();

    private readonly BlockingCollection<Action> _jobQueue = new BlockingCollection<Action>();
    public Thread LogicThread { get; private set; }

    public void Start()
    {
        LogicThread = new Thread(RunLoop) { Name = "LogicThread" };
        LogicThread.Start();
    }

    private long frameCount = 0;
    private long totalProcessTime = 0;
    private long lastReportTime = 0;

    private void UpdateStatistics(long startTickTime, long endTickTime)
    {
        long processTime = endTickTime - startTickTime;

        frameCount++;
        totalProcessTime += processTime;

        if (endTickTime - lastReportTime > 1000)
        {
            double avgProcessTime = (double)totalProcessTime / frameCount;

            Console.WriteLine($"[Logic] Avg: {avgProcessTime:F2}ms, FPS: {frameCount}, PendingJobs: {_jobQueue.Count}");

            frameCount = 0;
            totalProcessTime = 0;
            lastReportTime = endTickTime;
        }
    }

    private void RunLoop()
    {
        SynchronizationContext.SetSynchronizationContext(this);
        Stopwatch sw = Stopwatch.StartNew();

        int TICK_RATE = 30;
        long MS_PER_TICK = 1000 / TICK_RATE;
        long JOB_BUDGET_MS = 10;

        double nextTickTime = sw.Elapsed.TotalMilliseconds;
        //long lastTickTimeFrame = (long)nextFrameTargetMs;

        while (true)
        {
            double currentTime = sw.Elapsed.TotalMilliseconds;

            if (currentTime >= nextTickTime)
            {
                float deltaTime = (float)(MS_PER_TICK / 1000.0);

                Stopwatch jobTimer = Stopwatch.StartNew();
                while (_jobQueue.TryTake(out Action job))
                {
                    try { job.Invoke(); }
                    catch (Exception ex) { Console.WriteLine(ex); }

                    if (jobTimer.ElapsedMilliseconds > JOB_BUDGET_MS) break;
                }

                MapManager.Instance.Update(deltaTime);
                PlayerManager.Instance.Update(deltaTime);

                UpdateStatistics((long)currentTime, (long)sw.Elapsed.TotalMilliseconds);

                nextTickTime += MS_PER_TICK;

                if (sw.ElapsedMilliseconds > nextTickTime + MS_PER_TICK)
                {
                    Log.Warning("[GameLogicThread] 서버 렉 발생으로 프레임 스킵됨");
                    nextTickTime = sw.ElapsedMilliseconds;
                }
            }
            else
            {
                double remainingTime = nextTickTime - currentTime;
                if (remainingTime > 2.0)
                    Thread.Sleep(1);
                else
                    Thread.Yield();
            }
        }
    }

    public override void Post(SendOrPostCallback d, object state)
    {
        _jobQueue.Add(() => d(state));
    }

    public void Enqueue(Action action) => _jobQueue.Add(action);
}