using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MCP.Kernel.Server
{
    /// <summary>Provides basic support for parsing and formatting URI templates.</summary>
    /// <remarks>
    /// This implementation should correctly handle valid URI templates, but it has undefined output for invalid templates,
    /// e.g. it may treat portions of invalid templates as literals rather than throwing.
    /// </remarks>
    /// <summary>Provides basic support for parsing and formatting URI templates.</summary>
    /// <remarks>
    /// This implementation should correctly handle valid URI templates, but it has undefined output for invalid templates,
    /// e.g. it may treat portions of invalid templates as literals rather than throwing.
    /// </remarks>
    internal static partial class UriTemplate
    {
        /// <summary>Regex pattern for finding URI template expressions and parsing out the operator and varname.</summary>
        private const string UriTemplateExpressionPattern = @"
        {                                                       # opening brace
            (?<operator>[+#./;?&]?)                             # optional operator
            (?<varname>
                (?:[A-Za-z0-9_]|%[0-9A-Fa-f]{2})                # varchar: letter, digit, underscore, or pct-encoded
                (?:\.?(?:[A-Za-z0-9_]|%[0-9A-Fa-f]{2}))*        # optionally dot-separated subsequent varchars
            )
            (?: :[1-9][0-9]{0,3} )?                             # optional prefix modifier (1–4 digits)
            \*?                                                 # optional explode
            (?:,                                                # comma separator, followed by the same as above
                (?<varname>
                    (?:[A-Za-z0-9_]|%[0-9A-Fa-f]{2})
                    (?:\.?(?:[A-Za-z0-9_]|%[0-9A-Fa-f]{2}))*
                )
                (?: :[1-9][0-9]{0,3} )?
                \*?
            )*                                                  # zero or more additional vars
        }                                                       # closing brace
        ";

        private static readonly Regex _uriTemplateExpression = new Regex(
            UriTemplateExpressionPattern,
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled
        );

        /// <summary>Gets a regex for finding URI template expressions and parsing out the operator and varname.</summary>
        /// <remarks>
        /// This regex is for parsing a static URI template.
        /// It is not for parsing a URI according to a template.
        /// </remarks>
        private static Regex UriTemplateExpression() => _uriTemplateExpression;

        /// <summary>
        /// Create a <see cref="Regex"/> for matching a URI against a URI template.
        /// </summary>
        /// <param name="uriTemplate">The template against which to match.</param>
        /// <returns>A regex pattern that can be used to match the specified URI template.</returns>
        public static Regex CreateParser(string uriTemplate)
        {
            var pattern = new StringBuilder(256);
            pattern.Append('^');

            int lastIndex = 0;
            for (Match m = UriTemplateExpression().Match(uriTemplate); m.Success; m = m.NextMatch())
            {
                pattern.Append(Regex.Escape(uriTemplate.Substring(lastIndex, m.Index - lastIndex)));
                lastIndex = m.Index + m.Length;

                var captures = m.Groups["varname"].Captures;
                List<string> paramNames = new List<string>(captures.Count);
                foreach (Capture c in captures)
                {
                    paramNames.Add(c.Value);
                }

                switch (m.Groups["operator"].Value)
                {
                    case "+": AppendExpression(pattern, paramNames, null, "[^?&#]*"); break;
                    case "#": AppendExpression(pattern, paramNames, '#', ".*"); break;
                    case ".": AppendExpression(pattern, paramNames, '.', "[^/?#]*"); break;
                    case "/": AppendExpression(pattern, paramNames, '/', "[^/?#]*"); break;
                    default: AppendExpression(pattern, paramNames, null, "[^/?&#]*"); break;

                    case "?": AppendQueryExpression(pattern, paramNames, '?'); break;
                    case "&": AppendQueryExpression(pattern, paramNames, '&'); break;
                    case ";": AppendPathParameterExpression(pattern, paramNames); break;
                }
            }

            pattern.Append(Regex.Escape(uriTemplate.Substring(lastIndex)));
            pattern.Append('$');

            return new Regex(
                pattern.ToString(),
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        }

        /// <summary>
        /// Expand a URI template using the given variable values.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="uriTemplate"/> is <see langword="null"/>.</exception>
        public static string FormatUri(string uriTemplate, IReadOnlyDictionary<string, object> arguments)
        {
            if (uriTemplate == null)
                throw new ArgumentNullException(nameof(uriTemplate));

            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));

            var builder = new StringBuilder(256);
            int currentPos = 0;

            while (currentPos < uriTemplate.Length)
            {
                // Find the next expression.
                int openBracePos = uriTemplate.IndexOf('{', currentPos);
                if (openBracePos < 0)
                {
                    builder.Append(uriTemplate.Substring(currentPos));
                    break;
                }

                // Append as a literal everything before the next expression.
                builder.Append(uriTemplate.Substring(currentPos, openBracePos - currentPos));
                currentPos = openBracePos + 1;

                int closeBracePos = uriTemplate.IndexOf('}', currentPos);
                if (closeBracePos < 0)
                {
                    throw new FormatException($"Unmatched '{{' in URI template '{uriTemplate}'");
                }

                string expression = uriTemplate.Substring(currentPos, closeBracePos - currentPos);
                currentPos = closeBracePos + 1;

                if (string.IsNullOrEmpty(expression))
                {
                    continue;
                }

                // The start of the expression may be a modifier; if it is, slice it off the expression.
                char modifier = expression[0];
                var modifierBehavior = GetModifierBehavior(modifier);
                string expressionWithoutModifier = expression.Substring(modifierBehavior.ExpressionSlice);

                List<string> expansions = new List<string>();

                // Process each varspec in the comma-delimited list in the expression
                int startIndex = 0;
                while (startIndex < expressionWithoutModifier.Length)
                {
                    int commaPos = expressionWithoutModifier.IndexOf(',', startIndex);
                    string name;
                    if (commaPos < 0)
                    {
                        name = expressionWithoutModifier.Substring(startIndex);
                        startIndex = expressionWithoutModifier.Length;
                    }
                    else
                    {
                        name = expressionWithoutModifier.Substring(startIndex, commaPos - startIndex);
                        startIndex = commaPos + 1;
                    }

                    bool explode = false;
                    int prefixLength = -1;

                    // If the name ends with a *, it means we should explode the value into separate
                    // name=value pairs. If it has a colon, it means we should only take the first N characters
                    // of the value. If it has both, the * takes precedence and we ignore the colon.
                    if (!string.IsNullOrEmpty(name) && name[name.Length - 1] == '*')
                    {
                        explode = true;
                        name = name.Substring(0, name.Length - 1);
                    }
                    else
                    {
                        int colonPos = name.IndexOf(':');
                        if (colonPos >= 0)
                        {
                            if (!int.TryParse(name.Substring(colonPos + 1), out prefixLength))
                            {
                                throw new FormatException($"Invalid prefix length in varspec '{name}'");
                            }
                            name = name.Substring(0, colonPos);
                        }
                    }

                    // Look up the value for this name. If it doesn't exist, skip it.
                    if (!arguments.TryGetValue(name, out object value) || value == null)
                    {
                        continue;
                    }

                    if (value is IEnumerable<string> list)
                    {
                        var items = list.Select(i => Encode(i, modifierBehavior.AllowReserved));
                        if (explode)
                        {
                            if (modifierBehavior.Named)
                            {
                                foreach (var item in items)
                                {
                                    expansions.Add($"{name}={item}");
                                }
                            }
                            else
                            {
                                foreach (var item in items)
                                {
                                    expansions.Add(item);
                                }
                            }
                        }
                        else
                        {
                            string joined = string.Join(",", items);
                            expansions.Add(joined.Length > 0 && modifierBehavior.Named ?
                                $"{name}={joined}" :
                                joined);
                        }
                    }
                    else if (value is IReadOnlyDictionary<string, string> assoc)
                    {
                        var pairs = assoc.Select(kvp => (
                            Encode(kvp.Key, modifierBehavior.AllowReserved),
                            Encode(kvp.Value, modifierBehavior.AllowReserved)
                        ));

                        if (explode)
                        {
                            foreach (var (k, v) in pairs)
                            {
                                expansions.Add($"{k}={v}");
                            }
                        }
                        else
                        {
                            string joined = string.Join(",", pairs.Select(p => $"{p.Item1},{p.Item2}"));
                            if (joined.Length > 0)
                            {
                                expansions.Add(modifierBehavior.Named ? $"{name}={joined}" : joined);
                            }
                        }
                    }
                    else
                    {
                        string s = value as string ??
                            (value is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : value.ToString()) ??
                            string.Empty;

                        if (prefixLength >= 0 && prefixLength < s.Length)
                        {
                            s = s.Substring(0, prefixLength);
                        }
                        s = Encode(s, modifierBehavior.AllowReserved);

                        if (!modifierBehavior.Named)
                        {
                            expansions.Add(s);
                        }
                        else if (s.Length != 0 || modifierBehavior.IncludeNameIfEmpty)
                        {
                            expansions.Add(
                                s.Length != 0 ? $"{name}={s}" :
                                modifierBehavior.IncludeSeparatorIfEmpty ? $"{name}=" :
                                name);
                        }
                    }
                }

                if (expansions.Count > 0 &&
                    (modifierBehavior.PrefixEmptyExpansions || !expansions.TrueForAll(string.IsNullOrEmpty)))
                {
                    builder.Append(modifierBehavior.Prefix);
                    AppendJoin(builder, modifierBehavior.Separator, expansions);
                }
            }

            return builder.ToString();
        }

        private static (string Prefix, string Separator, bool Named, bool IncludeNameIfEmpty,
            bool IncludeSeparatorIfEmpty, bool AllowReserved, bool PrefixEmptyExpansions, int ExpressionSlice)
            GetModifierBehavior(char modifier)
        {
            switch (modifier)
            {
                case '+': return (string.Empty, ",", false, false, true, true, false, 1);
                case '#': return ("#", ",", false, false, true, true, true, 1);
                case '.': return (".", ".", false, false, true, false, true, 1);
                case '/': return ("/", "/", false, false, true, false, false, 1);
                case ';': return (";", ";", true, true, false, false, false, 1);
                case '?': return ("?", "&", true, true, true, false, false, 1);
                case '&': return ("&", "&", true, true, true, false, false, 1);
                default: return (string.Empty, ",", false, false, true, false, false, 0);
            }
        }

        private static void AppendJoin(StringBuilder builder, string separator, List<string> values)
        {
            int count = values.Count;
            if (count > 0)
            {
                builder.Append(values[0]);
                for (int i = 1; i < count; i++)
                {
                    builder.Append(separator);
                    builder.Append(values[i]);
                }
            }
        }

        private static string Encode(string value, bool allowReserved)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (!allowReserved)
            {
                return Uri.EscapeDataString(value);
            }

            var builder = new StringBuilder(value.Length * 2);
            int i = 0;

            for (; i < value.Length; ++i)
            {
                char c = value[i];
                if (IsUnreservedOrReserved(c))
                {
                    builder.Append(c);
                }
                else if (c == '%' && i < value.Length - 2 && Uri.IsHexDigit(value[i + 1]) && Uri.IsHexDigit(value[i + 2]))
                {
                    builder.Append(value.Substring(i, 3));
                    i += 2;
                }
                else
                {
                    AppendHex(builder, c);
                }
            }

            return builder.ToString();
        }

        private static bool IsUnreservedOrReserved(char c)
        {
            // ASCII letters (a-z, A-Z)
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                return true;

            // ASCII digits (0-9)
            if (c >= '0' && c <= '9')
                return true;

            // Unreserved: - . _ ~
            // Reserved: : / ? # [ ] @ ! $ & ' ( ) * + , ; =
            return "-._~:/?#[]@!$&'()*+,;=".IndexOf(c) >= 0;
        }

        private static void AppendHex(StringBuilder builder, char c)
        {
            string hexDigits = "0123456789ABCDEF";

            if (c <= 0x7F)
            {
                builder.Append('%');
                builder.Append(hexDigits[c >> 4]);
                builder.Append(hexDigits[c & 0xF]);
            }
            else
            {
                byte[] utf8Bytes = Encoding.UTF8.GetBytes(new char[] { c });
                foreach (byte b in utf8Bytes)
                {
                    builder.Append('%');
                    builder.Append(hexDigits[b >> 4]);
                    builder.Append(hexDigits[b & 0xF]);
                }
            }
        }

        // ============ Parser Helper Methods ============

        private static void AppendQueryExpression(StringBuilder pattern, List<string> paramNames, char prefix)
        {
            Debug.Assert(prefix == '?' || prefix == '&');

            pattern.Append("(?:\\");
            pattern.Append(prefix);

            if (paramNames.Count > 0)
            {
                AppendParameter(pattern, paramNames[0]);
                for (int i = 1; i < paramNames.Count; i++)
                {
                    pattern.Append("\\&?");
                    AppendParameter(pattern, paramNames[i]);
                }
            }

            pattern.Append(")?");
        }

        private static void AppendParameter(StringBuilder pattern, string paramName)
        {
            string escapedName = Regex.Escape(paramName);
            pattern.Append("(?:");
            pattern.Append(escapedName);
            pattern.Append("=(?<");
            pattern.Append(escapedName);
            pattern.Append(">[^/?&]*))?");
        }

        private static void AppendExpression(StringBuilder pattern, List<string> paramNames, char? prefix, string valueChars)
        {
            Debug.Assert(prefix == null || prefix == '#' || prefix == '/' || prefix == '.');

            if (paramNames.Count > 0)
            {
                if (prefix.HasValue)
                {
                    pattern.Append('\\');
                    pattern.Append(prefix.Value);
                    pattern.Append('?');
                }

                AppendExpressionParameter(pattern, paramNames[0], valueChars);

                string separator = prefix switch
                {
                    '.' => "\\.",
                    '/' => "\\/",
                    _ => "\\,"
                };

                for (int i = 1; i < paramNames.Count; i++)
                {
                    pattern.Append(separator);
                    pattern.Append('?');
                    AppendExpressionParameter(pattern, paramNames[i], valueChars);
                }
            }
        }

        private static void AppendExpressionParameter(StringBuilder pattern, string paramName, string valueChars)
        {
            string escapedName = Regex.Escape(paramName);
            pattern.Append("(?<");
            pattern.Append(escapedName);
            pattern.Append('>');
            pattern.Append(valueChars);
            pattern.Append(")?");
        }

        private static void AppendPathParameterExpression(StringBuilder pattern, List<string> paramNames)
        {
            if (paramNames.Count > 0)
            {
                AppendPathParameter(pattern, paramNames[0]);
                for (int i = 1; i < paramNames.Count; i++)
                {
                    AppendPathParameter(pattern, paramNames[i]);
                }
            }
        }

        private static void AppendPathParameter(StringBuilder pattern, string paramName)
        {
            string escapedName = Regex.Escape(paramName);
            pattern.Append("(?:;");
            pattern.Append(escapedName);
            pattern.Append("(?:=(?<");
            pattern.Append(escapedName);
            pattern.Append(">[^;/?&]*))?)?");
        }

        // ============ UriTemplateComparer ============

        /// <summary>
        /// Defines an equality comparer for Uri templates as follows:
        /// 1. Non-templated Uris use regular System.Uri equality comparison (host name is case insensitive).
        /// 2. Templated Uris use regular string equality.
        /// </summary>
        internal sealed class UriTemplateComparer : IEqualityComparer<string>
        {
            public static IEqualityComparer<string> Instance { get; } = new UriTemplateComparer();

            public bool Equals(string uriTemplate1, string uriTemplate2)
            {
                if (TryParseAsNonTemplatedUri(uriTemplate1, out Uri uri1) &&
                    TryParseAsNonTemplatedUri(uriTemplate2, out Uri uri2))
                {
                    return uri1 == uri2;
                }

                return string.Equals(uriTemplate1, uriTemplate2, StringComparison.Ordinal);
            }

            public int GetHashCode(string uriTemplate)
            {
                if (TryParseAsNonTemplatedUri(uriTemplate, out Uri uri))
                {
                    return uri.GetHashCode();
                }
                else
                {
                    return StringComparer.Ordinal.GetHashCode(uriTemplate);
                }
            }

            private static bool TryParseAsNonTemplatedUri(string uriTemplate, out Uri uri)
            {
                if (string.IsNullOrEmpty(uriTemplate) || uriTemplate.Contains('{'))
                {
                    uri = null;
                    return false;
                }

                return Uri.TryCreate(uriTemplate, UriKind.Absolute, out uri);
            }
        }
    }
}
