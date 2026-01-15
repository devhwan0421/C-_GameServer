using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class SessionManager
{
    public static SessionManager Instance { get; } = new SessionManager();

    private object _lock = new object();
    private Dictionary<int, UserSession> _sessions = new Dictionary<int, UserSession>();
    private int _sessionIdCounter = 0; //개선 필요해 보임

    public UserSession Generate(Socket socket, GameLogicThread scheduler, PacketHandler handler)
    {
        lock (_lock)
        {
            int sessionId = ++_sessionIdCounter;
            UserSession session = new UserSession(socket, scheduler, handler, sessionId);

            _sessions.Add(sessionId, session);

            return session;
        }
    }

    public void Remove(int sessionId)
    {
        lock (_lock)
        {
            _sessions.Remove(sessionId);
        }
    }

    public void Broadcast(ushort id, string json)
    {
        List<UserSession> snapshot;
        lock (_lock)
        {
            snapshot = new List<UserSession>(_sessions.Values);
        }

        foreach (UserSession session in snapshot)
        {
            session.Send(id, json);
        }
    }
}