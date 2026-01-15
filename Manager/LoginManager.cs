using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class LoginManager
{
    public static LoginManager Instance { get; } = new LoginManager();
    private ConcurrentDictionary<int, UserSession> _loginAccounts = new ConcurrentDictionary<int, UserSession>();

    public bool TryLogin(int accountId, UserSession session)
    {
        if(_loginAccounts.TryGetValue(accountId, out UserSession oldSession))
        {
            oldSession.DisConnect();
            _loginAccounts.TryRemove(accountId, out _); //out _: 제거된 값을 무시. 
        }

        return _loginAccounts.TryAdd(accountId, session);
    }

    public void OnLogout(int accountId)
    {
        _loginAccounts.TryRemove(accountId, out _);
    }
}