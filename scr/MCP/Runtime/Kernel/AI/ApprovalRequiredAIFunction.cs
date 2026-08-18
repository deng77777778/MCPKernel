namespace MCP.AI
{
    public sealed class ApprovalRequiredAIFunction : DelegatingAIFunction
    {
        public ApprovalRequiredAIFunction(AIFunction innerFunction) : base(innerFunction) { }
    }
}
