#nullable enable
using System;
using System.Text;

namespace MCP.AI
{
    /// <summary>
    /// 带自动归还的 StringBuilder 租用器
    /// </summary>
    public struct StringBuilderRental : IDisposable
    {
        private StringBuilder? _sb;

        public StringBuilderRental(int minimumCapacity = 256)
        {
            _sb = StringBuilderPool.Rent(minimumCapacity);
        }

        public readonly StringBuilder StringBuilder => _sb ?? new StringBuilder();
        public readonly int Length => _sb?.Length ?? 0;

        public void Dispose()
        {
            if (_sb != null)
            {
                StringBuilderPool.Return(_sb);
                _sb = null;
            }
        }

        public override string ToString() => _sb?.ToString() ?? string.Empty;

        public string ToStringAndDispose()
        {
            var result = ToString();
            Dispose();
            return result;
        }

        public readonly void Append(string value) => _sb?.Append(value);
        public readonly void Append(char value) => _sb?.Append(value);
        public readonly void AppendLine(string value) => _sb?.AppendLine(value);
        public readonly void AppendFormat(string format, object? arg0) => _sb?.AppendFormat(format, arg0);
        public readonly void AppendFormat(string format, object? arg0, object? arg1) => _sb?.AppendFormat(format, arg0, arg1);
    }
}