namespace MCP.Result
{
    public interface IResult
    {
        bool Result { get; }
    }

    public interface IResult<out T> : IResult
    {
        T Value { get; }
    }
}

