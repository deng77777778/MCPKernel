// Generators/TypeSchemaGenerator.cs
#nullable enable
using Newtonsoft.Json.Linq;
using System;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// 类型 Schema 生成器
    /// </summary>
    public class TypeSchemaGenerator : SchemaGeneratorBase<Type, JObject>
    {
        protected override string GeneratorName => "TypeSchema";

        protected override JObject GenerateCore(Type type, SchemaContext context)
        {
            var handler = TypeHandlerRegistry.GetHandler(type);
            return handler.GenerateSchema(type, context);
        }
    }
}