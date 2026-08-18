namespace MCP.Result
{
    internal static class TypedResults
    {
        public static OkResult Ok() => ResultsCache.Ok;
        public static OkResult<TValue> Ok<TValue>(TValue value) => new(value);


        public static BadResult Bad() => ResultsCache.Failed;
        public static BadResult<TValue> Bad<TValue>(TValue value) => new(value);

    }
}

