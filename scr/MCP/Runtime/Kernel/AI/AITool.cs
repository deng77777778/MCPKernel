#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MCP.AI
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public abstract class AITool
    {
        protected AITool() { }

        public virtual string Name => GetType().Name;
        public virtual string Description => string.Empty;
        public virtual IReadOnlyDictionary<string, object?> AdditionalProperties =>
            EmptyReadOnlyDictionary<string, object?>.Instance;

        public override string ToString() => Name;

        public virtual object? GetService(Type serviceType, object? serviceKey = null)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            return serviceKey == null && serviceType.IsInstanceOfType(this) ? this : null;
        }

        public TService? GetService<TService>(object? serviceKey = null) =>
            GetService(typeof(TService), serviceKey) is TService service ? service : default;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get
            {
                var sb = new StringBuilder(Name);
                if (!string.IsNullOrEmpty(Description))
                {
                    sb.Append(" (").Append(Description).Append(')');
                }
                foreach (var entry in AdditionalProperties)
                {
                    sb.Append(", ").Append(entry.Key).Append(" = ").Append(entry.Value);
                }
                return sb.ToString();
            }
        }
    }
}