// Handlers/PrimitiveTypeHandler.cs
#nullable enable
using Newtonsoft.Json.Linq;
using System;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// 基本类型处理器
    /// </summary>
    public class PrimitiveTypeHandler : ITypeSchemaHandler
    {
        public int Priority => 100;

        public bool CanHandle(Type type)
        {
            return TypeHelper.IsPrimitiveType(type);
        }

        public JObject GenerateSchema(Type type, SchemaContext context)
        {
            return SchemaTemplates.GetPrimitiveSchema(type);
        }
    }
}