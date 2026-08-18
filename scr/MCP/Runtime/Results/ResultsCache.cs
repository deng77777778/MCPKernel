namespace MCP.Result
{
    public static class ResultsCache
    {
        public static OkResult Ok { get; } = new();
        public static BadResult Failed { get; } = new();
    }
}

