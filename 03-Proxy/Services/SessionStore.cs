using System.Collections.Concurrent;
using OpenAI.Chat;

namespace _03_Proxy.Services;

public class SessionStore
{
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _sessions = new();

    public List<ChatMessage> GetOrCreate(string sessionId) =>
        _sessions.GetOrAdd(sessionId, _ => []);
}
