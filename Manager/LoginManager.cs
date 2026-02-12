using Serilog;
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

    public async Task<bool> TryLogin(int accountId, UserSession session)
    {
        if(_loginAccounts.TryGetValue(accountId, out UserSession oldSession))
        {
            //강제로 즉시 접속을 해제하지 않는다
            //oldSession.DisConnect(); 
            //_loginAccounts.TryRemove(accountId, out _); //out _: 제거된 값을 무시. 

            //클라이언트로 접속중 메시지를 보낸다.
            //1. 로그인 시도를 받으면 접속 중을 보내고
            //2. 이미 접속 중인 세션을 마무리 후 로그아웃
            //3. 로그인 허가

            Log.Information("[LoginManager] 중복 접속 시도. 이전 접속 종료중");

            await oldSession.DisConnect();
            _loginAccounts.TryRemove(accountId, out _);

            Log.Information("[LoginManager] 이전 접속 종료 완료. 새로운 접속 승인");
        }

        return _loginAccounts.TryAdd(accountId, session);
    }

    public void OnLogout(int accountId)
    {
        _loginAccounts.TryRemove(accountId, out _);
    }
}