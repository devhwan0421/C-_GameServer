using MySqlConnector;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

public class DbTransactionWorker
{
    public static DbTransactionWorker Instance { get; } = new DbTransactionWorker();
    private BlockingCollection<DbJob> _jobQueue = new BlockingCollection<DbJob>();

    public void Push(Action<IDbConnection> dbTask)
    {
        _jobQueue.Add(new DbJob(dbTask));
    }

    public Task<T> PushQuery<T>(Func<IDbConnection, T> dbTask)
    {
        TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

        _jobQueue.Add(new DbJob(
            dbTask: (db) =>
            {
                try
                {
                    var result = dbTask(db);
                    tcs.SetResult(result); //작업이 성공적으로 완료되었음을 나타내는 메서드. tcs.Task에서 결과를 가져올 수 있게 함
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            }
        ));

        return tcs.Task; //호출한 쪽에서 await로 결과를 기다릴 수 있게 함
    }

    public void Start(int threadCount, GameLogicThread scheduler)
    {
        for (int i = 0; i < threadCount; i++)
        {
            Thread t = new Thread(() =>
            {
                Console.WriteLine($"[DbTransactionWorker] DB 작업 스레드 시작 (ID: {Thread.CurrentThread.ManagedThreadId})");
                foreach (var job in _jobQueue.GetConsumingEnumerable())
                {
                    using (IDbConnection db = new MySqlConnection(DbManager._connectionString))
                    {
                        try
                        {
                            db.Open();
                            //Console.WriteLine(db.State);
                            job.DbTask.Invoke(db);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[DbTransactionWorker] DB 작업 중 예외 발생: {ex.Message}");
                        }
                    }
                }
            });

            t.Name = $"DB_Worker_{i}";
            t.IsBackground = true;
            t.Start();
        }
    }
}