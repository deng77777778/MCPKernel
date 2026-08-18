#nullable enable
using MCP.AI;
using MCP.DependencyInjection;
using MCP.Kernel.Extensions;
using MCP.Protocol;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MCP.Kernel.Server
{
    /// <summary>Provides an <see cref="McpServerPrompt"/> that's implemented via an <see cref="AIFunction"/>.</summary>
    internal sealed class AIFunctionMcpServerPrompt : McpServerPrompt
    {
        private readonly IReadOnlyList<object> _metadata;

        /// <summary>
        /// Creates an <see cref="McpServerPrompt"/> instance for a method, specified via a <see cref="Delegate"/> instance.
        /// </summary>
        public static new AIFunctionMcpServerPrompt Create(
            Delegate method,
            McpServerPromptCreateOptions? options)
        {
            Throw.IfNull(method);

            options = DeriveOptions(method.Method, options);

            return Create(method.Method, method.Target, options);
        }

        /// <summary>
        /// Creates an <see cref="McpServerPrompt"/> instance for a method, specified via a <see cref="MethodInfo"/> instance.
        /// </summary>
        public static new AIFunctionMcpServerPrompt Create(
            MethodInfo method,
            object? target,
            McpServerPromptCreateOptions? options)
        {
            Throw.IfNull(method);

            options = DeriveOptions(method, options);
            return Create(
                AIFunctionFactory.Create(method, target, CreateAIFunctionFactoryOptions(method, options)),
                options);
        }

        /// <summary>Creates an <see cref="McpServerPrompt"/> that wraps the specified <see cref="AIFunction"/>.</summary>
        public static new AIFunctionMcpServerPrompt Create(AIFunction function, McpServerPromptCreateOptions? options)
        {
            Throw.IfNull(function);

            List<PromptArgument> args = new();

            // 使用 JObject 替代 JsonElement
            JObject schema = JObject.Parse(function.JsonSchema.ToString());
            HashSet<string>? requiredProps = null;

            // 获取 required 属性
            if (schema["required"] is JArray requiredArray)
            {
                requiredProps = new HashSet<string>(
                    requiredArray.Select(p => p.Value<string>()!),
                    StringComparer.Ordinal);
            }

            // 获取 properties 属性
            if (schema["properties"] is JObject properties)
            {
                foreach (var param in properties.Properties())
                {
                    args.Add(new()
                    {
                        Name = param.Name,
                        Description = param.Value["description"]?.Value<string>(),
                        Required = requiredProps?.Contains(param.Name) ?? false,
                    });
                }
            }

            Prompt prompt = new()
            {
                Name = options?.Name ?? function.Name,
                Title = options?.Title,
                Description = options?.Description ?? function.Description,
                Arguments = args,
                Icons = options?.Icons,

                // Populate Meta from options and/or McpMetaAttribute instances if a MethodInfo is available
                Meta = function.UnderlyingMethod is not null ?
                    AIFunctionMcpServerTool.CreateMetaFromAttributes(function.UnderlyingMethod, options?.Meta) :
                    options?.Meta
            };

            return new AIFunctionMcpServerPrompt(function, prompt, options?.Metadata ?? new List<object>());
        }

        private static AIFunctionFactoryOptions CreateAIFunctionFactoryOptions(
    MethodInfo method, McpServerPromptCreateOptions? options) =>
    new()
    {
        Name = options?.Name ?? method.GetCustomAttribute<McpServerPromptAttribute>()?.Name ?? AIFunctionMcpServerTool.DeriveName(method),
        Description = options?.Description,
        MarshalResult = static (result, _, cancellationToken) => new ValueTask<object?>(result),
        SerializerOptions = options?.SerializerSettings ?? McpJsonUtilities.DefaultSettings,
        JsonSchemaCreateOptions = options?.SchemaCreateOptions,
        ConfigureParameterBinding = pi =>
        {
            if (pi.ParameterType.IsAugmentedWith<GetPromptRequestParams>())
            {
                return new()
                {
                    ExcludeFromSchema = true,
                    BindParameter = (pi, args) =>
                        args.Services?.GetService(pi.ParameterType) ??
                        (pi.HasDefaultValue ? null :
                         throw new ArgumentException("No service of the requested type was found.")),
                };
            }

            return default;
        },
    };

        private static McpServerPromptCreateOptions DeriveOptions(MethodInfo method, McpServerPromptCreateOptions? options)
        {
            McpServerPromptCreateOptions newOptions = options?.Clone() ?? new();

            if (method.GetCustomAttribute<McpServerPromptAttribute>() is { } promptAttr)
            {
                newOptions.Name ??= promptAttr.Name;
                newOptions.Title ??= promptAttr.Title;

                // Handle icon from attribute if not already specified in options
                if (newOptions.Icons is null && promptAttr.IconSource is { Length: > 0 } iconSource)
                {
                    newOptions.Icons = new List<Icon> { new() { Source = iconSource } };
                }
            }

            if (method.GetCustomAttribute<DescriptionAttribute>() is { } descAttr)
            {
                newOptions.Description ??= descAttr.Description;
            }

            // Set metadata if not already provided
            newOptions.Metadata ??= AIFunctionMcpServerTool.CreateMetadata(method);

            return newOptions;
        }

        /// <summary>Gets the <see cref="AIFunction"/> wrapped by this prompt.</summary>
        internal AIFunction AIFunction { get; }

        /// <summary>Initializes a new instance of the <see cref="McpServerPrompt"/> class.</summary>
        private AIFunctionMcpServerPrompt(AIFunction function, Prompt prompt, IReadOnlyList<object> metadata)
        {
            AIFunction = function;
            ProtocolPrompt = prompt;
            _metadata = metadata;
        }

        /// <inheritdoc />
        public override Prompt ProtocolPrompt { get; }

        /// <inheritdoc />
        public override IReadOnlyList<object> Metadata => _metadata;

        /// <inheritdoc />
        public override async ValueTask<GetPromptResult> GetAsync(
            RequestContext<GetPromptRequestParams> request, CancellationToken cancellationToken = default)
        {
            Throw.IfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            request.Services = ServiceContainer.Provider;
            AIFunctionArguments arguments = new() { Services = request.Services };

            if (request.Params?.Arguments is { } argDict)
            {
                foreach (var kvp in argDict)
                {
                    arguments[kvp.Key] = kvp.Value;
                }
            }

            object? result = await AIFunction.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);

            return result switch
            {
                GetPromptResult getPromptResult => getPromptResult,

                string text => new()
                {
                    Description = ProtocolPrompt.Description,
                    Messages = new List<PromptMessage> { new() { Role = Role.User, Content = new TextContentBlock { Text = text } } },
                },

                PromptMessage promptMessage => new()
                {
                    Description = ProtocolPrompt.Description,
                    Messages = new List<PromptMessage> { promptMessage }
                },

                IEnumerable<PromptMessage> promptMessages => new()
                {
                    Description = ProtocolPrompt.Description,
                    Messages = promptMessages.ToList(),
                },

                null => throw new InvalidOperationException("Null result returned from prompt function."),

                _ => throw new InvalidOperationException($"Unknown result type '{result.GetType()}' returned from prompt function."),
            };
        }
    }
}
