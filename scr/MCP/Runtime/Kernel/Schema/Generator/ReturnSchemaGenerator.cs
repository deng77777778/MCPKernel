// Generators/ReturnSchemaGenerator.cs
#nullable enable
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// 返回类型 Schema 生成器
    /// </summary>
    public class ReturnSchemaGenerator : SchemaGeneratorBase<MethodInfo, JObject>
    {
        private readonly TypeSchemaGenerator _typeGenerator;

        public ReturnSchemaGenerator()
        {
            _typeGenerator = new TypeSchemaGenerator();
        }

        protected override string GeneratorName => "ReturnSchema";
        protected override bool EnableCaching => true;

        protected override JObject? GenerateCore(MethodInfo method, SchemaContext context)
        {
            var returnType = method.ReturnType;

            // Void 或 Task
            if (returnType == typeof(void) || returnType == typeof(Task) || returnType == typeof(ValueTask))
                return null;

            // 解包异步返回类型
            var unwrappedType = TypeHelper.GetUnwrappedReturnType(returnType);
            if (unwrappedType == null)
                return null;

            return GetTypeSchema(unwrappedType, context);
        }

        protected override JObject? PostProcess(JObject? result, SchemaContext context)
        {
            if (result == null) return null;

            // 如果结果已经是 AIContent 相关，保持原样
            if (context.CurrentType != null && TypeHelper.IsAIContentRelated(context.CurrentType))
                return result;

            return result;
        }

        /// <summary>
        /// 获取类型 Schema
        /// </summary>
        private JObject? GetTypeSchema(Type type, SchemaContext context)
        {
            return _typeGenerator.Generate(type, context);
        }
    }
}