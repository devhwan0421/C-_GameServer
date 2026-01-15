using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class DbJob
{
    public Action<IDbConnection> DbTask;

    public DbJob(Action<IDbConnection> dbTask)
    {
        DbTask = dbTask;
    }
}
