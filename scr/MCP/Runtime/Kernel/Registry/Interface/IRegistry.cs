using MCP.Result;

public interface IRegistry<in TKey, TValue>
{
    void Register(TKey key, TValue value);
    IResult<TValue> Resolve(TKey key);
    bool UnRegister(TKey key);
    void Clear();
}
