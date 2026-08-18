namespace MCP.Result
{
    public sealed class OkResult : IResult
    {
        public bool Result => true;
    }

    public sealed class OkResult<TValue> : IResult<TValue>
    {
        public bool Result => true;

        public TValue Value { get; }

        public OkResult(TValue value)
        {
            Value = value;
        }
    }
}
