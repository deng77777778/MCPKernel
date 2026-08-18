#nullable enable
using MCP.Kernel.Server;
using MCP.Protocol;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace MCP.Kernel.Schema
{
    public sealed class ResourceSchemaGenerator : McpSchemaGeneratorBase<Resource, McpServerResourceAttribute>
    {
        protected override Resource? GenerateCore(MethodInfo method)
        {
            var attr = GetAttribute(method);
            if (attr == null) return null;

            return new Resource
            {
                Name = attr.Name ?? GetName(method),
                Title = attr.Title,
                Uri = attr.UriTemplate ?? $"resource://{GetName(method)}",
                Description = GetDescription(method),
                MimeType = attr.MimeType,
                Meta = new JObject { ["method"] = method.Name, ["isTemplate"] = IsTemplate(attr) }
            };
        }

        private static bool IsTemplate(McpServerResourceAttribute attr)
            => !string.IsNullOrEmpty(attr.UriTemplate) && attr.UriTemplate.Contains("{");
    }
}
