using System;
using System.Data;

public class DbJob
{
    public Action<IDbConnection> DbTask;

    public DbJob(Action<IDbConnection> dbTask)
    {
        DbTask = dbTask;
    }
}
