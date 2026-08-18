#nullable enable
using MCP.AI;
using MCP.Kernel.Extensions;
using MCP.Kernel.Schema;
using MCP.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MCP.Kernel.Server
{

    /// <summary>
    /// 提供通过 <see cref="AIFunction"/> 实现的 <see cref="McpServerTool"/>。
    /// </summary>
    internal sealed partial class AIFunctionMcpServerTool : McpServerTool
    {
        private readonly IReadOnlyList<object> _metadata;
        /// <summary>
        /// Creates an <see cref="McpServerTool"/> instance for a method, specified via a <see cref="Delegate"/> instance.
        /// </summary>
        public static new AIFunctionMcpServerTool Create(
            Delegate method,
            McpServerToolCreateOptions? options)
        {
            Throw.IfNull(method);

            options = DeriveOptions(method.Method, options);

            return Create(method.Method, method.Target, options);
        }

        /// <summary>
        /// Creates an <see cref="McpServerTool"/> instance for a method, specified via a <see cref="MethodInfo"/> instance.
        /// </summary>
        public static new AIFunctionMcpServerTool Create(
            MethodInfo method,
            object? target,
            McpServerToolCreateOptions? options)
        {
            Throw.IfNull(method);
            options = DeriveOptions(method, options);

            return Create(
                AIFunctionFactory.Create(method, target, CreateAIFunctionFactoryOptions(method, options)),
                options);
        }

        ///// <summary>
        ///// Creates an <see cref="McpServerTool"/> instance for a method, specified via a <see cref="MethodInfo"/> instance.
        ///// </summary>
        //public static new AIFunctionMcpServerTool Create(
        //    MethodInfo method,
        //    Func<RequestContext<CallToolRequestParams>, object> createTargetFunc,
        //    McpServerToolCreateOptions? options)
        //{
        //    Throw.IfNull(method);
        //    Throw.IfNull(createTargetFunc);

        //    options = DeriveOptions(method, options);

        //    return Create(
        //        AIFunctionFactory.Create(method, args =>
        //        {
        //            Debug.Assert(args.Services is RequestServiceProvider<CallToolRequestParams>, $"The service provider should be a {nameof(RequestServiceProvider<>)} for this method to work correctly.");
        //            return createTargetFunc(((RequestServiceProvider<CallToolRequestParams>)args.Services!).Request);
        //        }, CreateAIFunctionFactoryOptions(method, options)),
        //        options);
        //}
        private static AIFunctionFactoryOptions CreateAIFunctionFactoryOptions(
            MethodInfo method, McpServerToolCreateOptions? options) =>
            new()
            {
                Name = options?.Name ?? method.GetCustomAttribute<McpServerToolAttribute>()?.Name ?? DeriveName(method),
                Description = options?.Description,
                MarshalResult = static (result, _, cancellationToken) => new ValueTask<object?>(result),
                SerializerOptions = options?.SerializerSettings ?? McpJsonUtilities.DefaultSettings,
                JsonSchemaCreateOptions = options?.SchemaCreateOptions,
                ConfigureParameterBinding = pi =>
                {
                    if (pi.ParameterType.IsAugmentedWith<CallToolRequestParams>())
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

        /// <summary>
        /// 创建包装指定 <see cref="AIFunction"/> 的 <see cref="McpServerTool"/>。
        /// </summary>
        public static new AIFunctionMcpServerTool Create(AIFunction function, McpServerToolCreateOptions? options)
        {
            Throw.IfNull(function);

            Tool tool = new()
            {
                Name = NameHelper.ToSnakeCase(options?.Name ?? function.Name),
                Description = GetToolDescription(function, options),
                InputSchema = function.JsonSchema,
                OutputSchema = CreateOutputSchema(function, options),
                Icons = options?.Icons,
            };
            if (options is not null)
            {
                if (options.Title is not null ||
                    options.Idempotent is not null ||
                    options.Destructive is not null ||
                    options.OpenWorld is not null ||
                    options.ReadOnly is not null)
                {
                    tool.Title = options.Title;

                    tool.Annotations = new()
                    {
                        Title = options.Title,
                        IdempotentHint = options.Idempotent,
                        DestructiveHint = options.Destructive,
                        OpenWorldHint = options.OpenWorld,
                        ReadOnlyHint = options.ReadOnly,
                    };
                }

                tool.Meta = function.UnderlyingMethod is not null ?
                    CreateMetaFromAttributes(function.UnderlyingMethod, options.Meta) :
                    options.Meta;
            }

            return new AIFunctionMcpServerTool(function, tool, options?.Services, options?.Metadata ?? new List<object>());
        }

        private static McpServerToolCreateOptions DeriveOptions(MethodInfo method, McpServerToolCreateOptions? options)
        {
            McpServerToolCreateOptions newOptions = options?.Clone() ?? new();

            if (method.GetCustomAttribute<McpServerToolAttribute>() is { } toolAttr)
            {
                newOptions.Name ??= DeriveName(method);
                newOptions.Title ??= toolAttr.Title;

                if (toolAttr._destructive is bool destructive)
                {
                    newOptions.Destructive ??= destructive;
                }

                if (toolAttr._idempotent is bool idempotent)
                {
                    newOptions.Idempotent ??= idempotent;
                }

                if (toolAttr._openWorld is bool openWorld)
                {
                    newOptions.OpenWorld ??= openWorld;
                }

                if (toolAttr._readOnly is bool readOnly)
                {
                    newOptions.ReadOnly ??= readOnly;
                }

                if (newOptions.Icons is null && toolAttr.IconSource is { Length: > 0 } iconSource)
                {
                    newOptions.Icons = new List<Icon>() { new Icon { Source = iconSource } };
                }

                newOptions.UseStructuredContent = toolAttr.UseStructuredContent;

                if (toolAttr.OutputSchemaType is Type outputSchemaType)
                {
                    newOptions.OutputSchema ??= AIJsonUtilities.CreateJsonSchema(outputSchemaType,
                        settings: newOptions.SerializerSettings ?? McpJsonUtilities.DefaultSettings,
                        options: newOptions.SchemaCreateOptions);
                }
            }

            if (method.GetCustomAttribute<DescriptionAttribute>() is { } descAttr)
            {
                newOptions.Description ??= descAttr.Description;
            }

            // 如果尚未提供元数据则设置
            newOptions.Metadata ??= CreateMetadata(method);

            return newOptions;
        }

        /// <summary>获取此工具包装的 <see cref="AIFunction"/>。</summary>
        internal AIFunction AIFunction { get; }

        /// <summary>初始化 <see cref="McpServerTool"/> 类的新实例。</summary>
        private AIFunctionMcpServerTool(AIFunction function, Tool tool, MCP.DependencyInjection.IServiceProvider? serviceProvider, IReadOnlyList<object> metadata)
        {
            ValidateToolName(tool.Name);

            AIFunction = function;
            ProtocolTool = tool;

            _metadata = metadata;
        }

        /// <inheritdoc />
        public override Tool ProtocolTool { get; }

        /// <inheritdoc />
        public override IReadOnlyList<object> Metadata => _metadata;

        /// <summary>
        /// 返回一个 <see cref="Tool"/> 克隆，其 <see cref="Tool.OutputSchema"/> 被重写为
        /// 协议版本早于 <c>"2026-07-28"</c> 的客户端所需的线路形状。
        /// </summary>
        internal Tool BuildLegacyWireProtocolTool()
        {
            if (ProtocolTool.OutputSchema is not { } natural)
            {
                return ProtocolTool;
            }

            JToken legacyOutputSchema = TransformOutputSchemaForLegacyWire(natural);

            return new Tool
            {
                Name = ProtocolTool.Name,
                Title = ProtocolTool.Title,
                Description = ProtocolTool.Description,
                InputSchema = ProtocolTool.InputSchema,
                OutputSchema = legacyOutputSchema,
                Annotations = ProtocolTool.Annotations,
                Icons = ProtocolTool.Icons,
                Meta = ProtocolTool.Meta,
            };
        }

        /// <inheritdoc />
        public override async ValueTask<CallToolResult> InvokeAsync(
            RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken = default)
        {
            Throw.IfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            request.Services = MCP.DependencyInjection.ServiceContainer.Provider;
            AIFunctionArguments arguments = new() { Services = request.Services };

            if (request.Params?.Arguments is { } argDict)
            {
                foreach (var kvp in argDict)
                {
                    arguments[kvp.Key] = kvp.Value;
                }
            }

            object? result = await AIFunction.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);

            JToken? structuredContent = CreateStructuredResponse(result);
            return result switch
            {
                AIContent aiContent => new CallToolResult
                {
                    Content = new List<ContentBlock>() { aiContent.ToContentBlock() },
                    StructuredContent = structuredContent,
                    IsError = aiContent is ErrorContent
                },

                null => new CallToolResult
                {
                    Content = new List<ContentBlock>() { },
                    StructuredContent = structuredContent,
                },

                string text => new CallToolResult
                {
                    Content = new List<ContentBlock>() { new TextContentBlock { Text = text } },
                    StructuredContent = structuredContent,
                },

                ContentBlock content => new CallToolResult
                {
                    Content = new List<ContentBlock>() { content },
                    StructuredContent = structuredContent,
                },

                IEnumerable<AIContent> contentItems => ConvertAIContentEnumerableToCallToolResult(contentItems, structuredContent),

                IEnumerable<ContentBlock> contents => new CallToolResult
                {
                    Content = contents.ToList(),
                    StructuredContent = structuredContent,
                },

                CallToolResult callToolResponse => callToolResponse,

                _ => new CallToolResult
                {
                    Content = new List<ContentBlock>() { new TextContentBlock { Text = JsonConvert.SerializeObject(result, AIFunction.JsonSerializerSettings) } },
                    StructuredContent = structuredContent,
                },
            };
        }

        /// <summary>基于提供的方法和命名策略创建要使用的名称。</summary>
        internal static string DeriveName(MethodInfo method)
        {
            string name = method.Name;

            // 如果方法是异步方法且方法名不仅仅是 "Async"，则移除 "Async" 后缀
            const string AsyncSuffix = "Async";
            if (IsAsyncMethod(method) &&
                name.EndsWith(AsyncSuffix, StringComparison.Ordinal) &&
                name.Length > AsyncSuffix.Length)
            {
                name = name.Substring(0, name.Length - AsyncSuffix.Length);
            }

            // 将除 ASCII 字母或数字之外的任何字符替换为下划线，去除首尾下划线
            name = NonAsciiLetterDigitsRegex().Replace(name, "_").Trim('_');

            // 如果经过所有转换后名称为空，则使用原始方法名称
            if (name.Length == 0)
            {
                name = method.Name;
            }

            // 基于提供的命名策略转换名称大小写
            return NameHelper.ToSnakeCase(name);

            static bool IsAsyncMethod(MethodInfo method)
            {
                Type t = method.ReturnType;

                if (t == typeof(Task) || t == typeof(ValueTask))
                {
                    return true;
                }

                if (t.IsGenericType)
                {
                    t = t.GetGenericTypeDefinition();
                    if (t == typeof(Task<>) || t == typeof(ValueTask<>) || t == typeof(IAsyncEnumerable<>))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// 从指定方法及其声明类上的属性创建元数据，MethodInfo 作为第一个项。
        /// </summary>
        internal static IReadOnlyList<object> CreateMetadata(MethodInfo method)
        {
            // 将 MethodInfo 添加到元数据的开头，类似于 RouteEndpointDataSource 对最小端点所做的
            List<object> metadata = new() { method };

            // 首先添加类级别属性，因为这些不太具体
            if (method.DeclaringType is not null)
            {
                metadata.AddRange(method.DeclaringType.GetCustomAttributes());
            }

            // 其次添加方法级别属性，因为这些更具体
            metadata.AddRange(method.GetCustomAttributes());

            return metadata.AsReadOnly();
        }

        private static readonly Regex _nonAsciiLetterDigits = new("[^0-9A-Za-z]+", RegexOptions.Compiled);
        private static readonly Regex _validateToolName = new(@"^[A-Za-z0-9_.-]{1,128}\z", RegexOptions.Compiled);

        private static Regex NonAsciiLetterDigitsRegex() => _nonAsciiLetterDigits;
        private static Regex ValidateToolNameRegex() => _validateToolName;

        private static void ValidateToolName(string? name)
        {
            if (name is null)
            {
                throw new ArgumentException("Tool name cannot be null.");
            }

            if (!ValidateToolNameRegex().IsMatch(name))
            {
                throw new ArgumentException($"The tool name '{name}' is invalid. Tool names must match the regular expression '{ValidateToolNameRegex()}'");
            }
        }

        /// <summary>
        /// 获取工具描述，在适当时综合函数描述和返回描述。
        /// </summary>
        private static string? GetToolDescription(AIFunction function, McpServerToolCreateOptions? options)
        {
            string? description = options?.Description ?? function.Description;

            // 如果启用了结构化内容，返回描述将在输出模式中
            if (options?.UseStructuredContent is true)
            {
                return description;
            }

            // 当禁用结构化内容时，尝试从 ReturnJsonSchema 提取返回描述
            string? returnDescription = GetReturnDescription(function.ReturnJsonSchema);
            if (string.IsNullOrWhiteSpace(returnDescription))
            {
                return description;
            }

            // 合成组合描述
            if (string.IsNullOrWhiteSpace(description))
            {
                return $"Returns: {returnDescription}";
            }

            return $"{description}\nReturns: {returnDescription}";
        }

        /// <summary>
        /// 如果存在，从 ReturnJsonSchema 提取描述属性。
        /// </summary>
        private static string? GetReturnDescription(JToken? returnJsonSchema)
        {
            if (returnJsonSchema is not JObject schema ||
                schema["description"] is not JValue descriptionValue ||
                descriptionValue.Type != JTokenType.String)
            {
                return null;
            }

            return descriptionValue.ToString();
        }

        private static JToken? CreateOutputSchema(AIFunction function, McpServerToolCreateOptions? toolCreateOptions)
        {
            if (toolCreateOptions?.UseStructuredContent is not true)
            {
                return null;
            }

            // 显式 OutputSchema 优先于 AIFunction 的返回模式
            if (toolCreateOptions.OutputSchema is { } explicitSchema)
            {
                return explicitSchema;
            }

            if (function.ReturnJsonSchema is { } returnSchema)
            {
                return returnSchema;
            }

            return null;
        }

        /// <summary>
        /// 将 <paramref name="naturalSchema"/> 转换为协议版本早于 <c>"2026-07-28"</c> 的客户端所需的线路形状。
        /// </summary>
        internal static JToken TransformOutputSchemaForLegacyWire(JToken naturalSchema)
        {
            if (naturalSchema is JObject objSchema &&
                objSchema["type"] is JValue typeValue &&
                typeValue.Type == JTokenType.String &&
                typeValue.ToString() == "object")
            {
                return naturalSchema;
            }

            // 深拷贝原始模式以便修改
            JToken schemaClone = naturalSchema.DeepClone();

            if (schemaClone is JObject schemaObj)
            {
                if (schemaObj["type"] is JArray typeArray && typeArray.Count == 2)
                {
                    var types = typeArray.Select(t => t.ToString()).ToList();
                    if (types.Contains("object") && types.Contains("null"))
                    {
                        // type:["object","null"] → 规范化为纯 "object"。无信封。
                        schemaObj["type"] = "object";
                        return schemaObj;
                    }
                }

                // 其他任何内容（字符串、整数、数组、布尔模式、缺少类型、组合）。
                // 包装在 {"result": <schema>} 信封中。
                var wrappedSchema = new JObject
                {
                    ["type"] = "object",
                    ["properties"] = new JObject
                    {
                        ["result"] = schemaClone
                    },
                    ["required"] = new JArray { "result" }
                };

                // 包装后，重写 $ref 指针以考虑新位置
                RewriteRefPointers(wrappedSchema["properties"]!["result"]);

                return wrappedSchema;
            }

            // 如果模式不是对象（例如原始类型），直接包装
            var result = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["result"] = schemaClone
                },
                ["required"] = new JArray { "result" }
            };

            return result;
        }

        /// <summary>
        /// 递归重写给定节点中的所有 <c>$ref</c> JSON 指针值，
        /// 以考虑模式已被包装在 <c>properties.result</c> 下。
        /// </summary>
        private static void RewriteRefPointers(JToken? node)
        {
            if (node is JObject obj)
            {
                if (obj["$ref"] is JValue refValue && refValue.Type == JTokenType.String)
                {
                    string refString = refValue.ToString();
                    if (refString == "#")
                    {
                        obj["$ref"] = "#/properties/result";
                    }
                    else if (refString.StartsWith("#/", StringComparison.Ordinal))
                    {
                        obj["$ref"] = "#/properties/result" + refString.Substring(1);
                    }
                }

                foreach (var property in obj.Properties().ToList())
                {
                    RewriteRefPointers(property.Value);
                }
            }
            else if (node is JArray arr)
            {
                foreach (var item in arr)
                {
                    RewriteRefPointers(item);
                }
            }
        }

        private JToken? CreateStructuredResponse(object? aiFunctionResult)
        {
            if (ProtocolTool.OutputSchema is null)
            {
                return null;
            }

            JToken? elementResult = aiFunctionResult switch
            {
                JToken token => token,
                null => null,
                _ => JToken.FromObject(aiFunctionResult, JsonSerializer.Create(AIFunction.JsonSerializerSettings)),
                //JToken.FromObject(aiFunctionResult, JsonSerializer.Create(AIFunction.JsonSerializerSettings)),
            };

            return elementResult;
        }

        private static CallToolResult ConvertAIContentEnumerableToCallToolResult(
            IEnumerable<AIContent> contentItems, JToken? structuredContent)
        {
            List<ContentBlock> contentList = new();
            bool allErrorContent = true;
            bool hasAny = false;

            foreach (var item in contentItems)
            {
                contentList.Add(item.ToContentBlock());
                hasAny = true;

                if (allErrorContent && item is not ErrorContent)
                {
                    allErrorContent = false;
                }
            }

            return new CallToolResult
            {
                Content = contentList,
                StructuredContent = structuredContent,
                IsError = allErrorContent && hasAny
            };
        }

        /// <summary>Creates a Meta <see cref="JsonObject"/> from <see cref="McpMetaAttribute"/> instances on the specified method.</summary>
        /// <param name="method">The method to extract <see cref="McpMetaAttribute"/> instances from.</param>
        /// <param name="meta">Optional <see cref="JsonObject"/> to seed the Meta with. Properties from this object take precedence over attributes.</param>
        /// <returns>A <see cref="JsonObject"/> with metadata, or null if no metadata is present.</returns>
        internal static JObject? CreateMetaFromAttributes(MethodInfo method, JObject? meta = null)
        {
            // Transfer all McpMetaAttribute instances to the Meta JsonObject, ignoring any that would overwrite existing properties.
            foreach (var attr in method.GetCustomAttributes<McpMetaAttribute>())
            {
                if (meta?.ContainsKey(attr.Name) is not true)
                {
                    var parsedValue = JToken.Parse(attr.JsonValue);
                    (meta ??= new JObject())[attr.Name] = parsedValue;
                }
            }

            return meta;
        }


        private static bool IsPrimitiveHeaderType(Type type)
        {
            return type == typeof(string) ||
                   type == typeof(bool) ||
                   type == typeof(byte) ||
                   type == typeof(sbyte) ||
                   type == typeof(short) ||
                   type == typeof(ushort) ||
                   type == typeof(int) ||
                   type == typeof(uint) ||
                   type == typeof(long);
        }
    }
}
