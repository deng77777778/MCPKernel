using Newtonsoft.Json.Linq;
using System;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// 类型 Schema 处理器接口
    /// </summary>
    public interface ITypeSchemaHandler
    {
        /// <summary>
        /// 优先级（数字越小越先执行）
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 判断是否能处理该类型
        /// </summary>
        bool CanHandle(Type type);

        /// <summary>
        /// 生成 Schema
        /// </summary>
        JObject GenerateSchema(Type type, SchemaContext context);
    }
}
