using MCP.Protocol;

namespace MCP
{
    /// <summary>
    /// ToolAnnotations扩展方法
    /// </summary>
    public static class ToolAnnotationsExtensions
    {
        /// <summary>
        /// 检查是否有显式设置的注解
        /// </summary>
        public static bool HasAnyExplicit(this ToolAnnotations annotations)
        {
            if (annotations == null)
                return false;

            return annotations.DestructiveHint.HasValue ||
                   annotations.IdempotentHint.HasValue ||
                   annotations.OpenWorldHint.HasValue ||
                   annotations.ReadOnlyHint.HasValue;
        }
    }
}
