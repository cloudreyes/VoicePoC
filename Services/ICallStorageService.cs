using System.Collections.Concurrent;
using VoicePoC.Models;

namespace VoicePoC.Services;

public interface ICallStorageService
{
    void UpsertCall(string callSid, CallData data);
    IEnumerable<CallData> GetAllCalls();
}

public class CallStorageService : ICallStorageService
{
    private readonly ConcurrentDictionary<string, CallData> _callStore = new();

    public void UpsertCall(string callSid, CallData data)
    {
        _callStore[callSid] = data;
    }

    public IEnumerable<CallData> GetAllCalls()
    {
        return _callStore.Values.OrderByDescending(x => x.Timestamp);
    }
}
