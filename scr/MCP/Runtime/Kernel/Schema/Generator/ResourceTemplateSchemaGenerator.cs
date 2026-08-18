#nullable enable
using MCP.Kernel.Server;
using MCP.Protocol;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace MCP.Kernel.Schema
{
    public sealed class ResourceTemplateSchemaGenerator : McpSchemaGeneratorBase<ResourceTemplate, McpServerResourceAttribute>
    {
        protected override ResourceTemplate? GenerateCore(MethodInfo method)
        {
            var attr = GetAttribute(method);
            if (attr == null) return null;

            if (string.IsNullOrEmpty(attr.UriTemplate) || !attr.UriTemplate.Contains("{"))
                return null;

            return new ResourceTemplate
            {
                Name = attr.Name ?? GetName(method),
                Title = attr.Title,
                UriTemplate = attr.UriTemplate,
                Description = GetDescription(method),
                MimeType = attr.MimeType,
                Meta = new JObject { ["method"] = method.Name }
            };
        }
    }
}
