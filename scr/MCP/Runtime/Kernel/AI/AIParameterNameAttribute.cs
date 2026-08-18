using System;

namespace MCP.AI
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public sealed class AIParameterNameAttribute : Attribute
    {
        public string Name { get; }

        public AIParameterNameAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));
            Name = name;
        }
    }
}
