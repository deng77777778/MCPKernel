namespace MCP.Result
{
    public sealed class BadResult : IResult
    {
        public bool Result => false;

    }

    public sealed class BadResult<TValue> : IResult<TValue>
    {
        public bool Result => false;

        public TValue Value { get; }

        public BadResult(TValue value)
        {
            Value = value;
        }
    }
}

