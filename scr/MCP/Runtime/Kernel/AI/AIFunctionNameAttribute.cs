using System;

namespace MCP.AI
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class AIFunctionNameAttribute : Attribute
    {
        public string Name { get; }

        public AIFunctionNameAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));
            Name = name;
        }
    }
}
