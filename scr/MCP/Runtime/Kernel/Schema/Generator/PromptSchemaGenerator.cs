#nullable enable
using MCP.Kernel.Cache;
using MCP.Kernel.Server;
using MCP.Protocol;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MCP.Kernel.Schema
{
    public sealed class PromptSchemaGenerator : McpSchemaGeneratorBase<Prompt, McpServerPromptAttribute>
    {
        protected override Prompt? GenerateCore(MethodInfo method)
        {
            var attr = GetAttribute(method);
            if (attr == null) return null;

            var prompt = new Prompt
            {
                Name = attr.Name ?? GetName(method),
                Title = attr.Title,
                Description = GetDescription(method),
                Arguments = BuildArguments(method),
                Meta = new JObject { ["method"] = method.Name }
            };

            if (!string.IsNullOrEmpty(attr.IconSource))
                prompt.Icons = new List<Icon> { new Icon { Source = attr.IconSource } };

            return prompt;
        }

        private IList<PromptArgument>? BuildArguments(MethodInfo method)
        {
            var args = method.GetParameters()
                .Where(p => !IsSpecialParameter(p))
                .Select(p => new PromptArgument
                {
                    Name = NameHelper.GetParameterName(p),
                    Description = GetParameterDescription(p),
                    Required = IsParameterRequired(p)
                })
                .ToList();

            return args.Count == 0 ? null : args;
        }
    }
}
