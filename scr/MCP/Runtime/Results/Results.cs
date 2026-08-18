namespace MCP.Result
{
    public static class Results
    {
        public static IResult Ok() => TypedResults.Ok();
        public static IResult<TValue> Ok<TValue>(TValue value) => TypedResults.Ok(value);


        public static IResult Bad() => TypedResults.Bad();
        public static IResult<TValue> Bad<TValue>(TValue value) => TypedResults.Bad(value);
        public static IResult<TValue> Bad<TValue>() => TypedResults.Bad<TValue>(default);
    }
}
