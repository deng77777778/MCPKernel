#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MCP.AI
{
    public static class Throw
    {
        /// <summary>
        /// Throws an <see cref="System.ArgumentNullException"/> if the specified argument is <see langword="null"/>.
        /// </summary>
        /// <typeparam name="T">Argument type to be checked for <see langword="null"/>.</typeparam>
        /// <param name="argument">Object to be checked for <see langword="null"/>.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        /// <returns>The original value of <paramref name="argument"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNull]
        public static T IfNull<T>([NotNull] T argument, string paramName = "")
        {
            if (argument is null)
            {
                ArgumentNullException(paramName);
            }

            return argument;
        }

        /// <summary>
        /// Throws an <see cref="System.ArgumentNullException"/> if the string is <see langword="null"/>,
        /// or <see cref="System.ArgumentException"/> if it is empty.
        /// </summary>
        /// <param name="argument">String to be checked for <see langword="null"/> or empty.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        /// <returns>The original value of <paramref name="argument"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNull]
        public static string IfNullOrEmpty([NotNull] string? argument, string paramName = "")
        {
#if !NETCOREAPP3_1_OR_GREATER
            if (argument == null)
            {
                ArgumentNullException(paramName);
            }
#endif

            if (string.IsNullOrEmpty(argument))
            {
                if (argument == null)
                {
                    ArgumentNullException(paramName);
                }
                else
                {
                    ArgumentException(paramName, "Argument is an empty string");
                }
            }

            return argument;
        }

        /// <summary>
        /// Throws an <see cref="System.ArgumentException"/>.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the exception.</param>
        /// <param name="message">A message that describes the error.</param>
#if !NET6_0_OR_GREATER
        [MethodImpl(MethodImplOptions.NoInlining)]
#endif
        [DoesNotReturn]
        public static void ArgumentException(string paramName, string? message)
            => throw new ArgumentException(message, paramName);

        /// <summary>
        /// Throws an <see cref="System.ArgumentNullException"/>.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the exception.</param>
#if !NET6_0_OR_GREATER
        [MethodImpl(MethodImplOptions.NoInlining)]
#endif
        [DoesNotReturn]
        public static void ArgumentNullException(string paramName)
            => throw new ArgumentNullException(paramName);


        /// <summary>
        /// Throws either an <see cref="System.ArgumentNullException"/> or an <see cref="System.ArgumentException"/>
        /// if the specified string is <see langword="null"/> or whitespace respectively.
        /// </summary>
        /// <param name="argument">String to be checked for <see langword="null"/> or whitespace.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        /// <returns>The original value of <paramref name="argument"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNull]
        public static string IfNullOrWhitespace([NotNull] string? argument, string paramName = "")
        {
#if !NETCOREAPP3_1_OR_GREATER
            if (argument == null)
            {
                ArgumentNullException(paramName);
            }
#endif

            if (string.IsNullOrWhiteSpace(argument))
            {
                if (argument == null)
                {
                    ArgumentNullException(paramName);
                }
                else
                {
                    ArgumentException(paramName, "Argument is whitespace");
                }
            }

            return argument;
        }


        /// <summary>
        /// Throws an <see cref="System.ArgumentNullException"/>.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the exception.</param>
        /// <param name="message">A message that describes the error.</param>
#if !NET6_0_OR_GREATER
        [MethodImpl(MethodImplOptions.NoInlining)]
#endif
        [DoesNotReturn]
        public static void ArgumentNullException(string paramName, string? message)
            => throw new ArgumentNullException(paramName, message);

        /// <summary>
        /// Throws an <see cref="System.InvalidOperationException"/>.
        /// </summary>
        /// <param name="message">A message that describes the error.</param>
#if !NET6_0_OR_GREATER
        [MethodImpl(MethodImplOptions.NoInlining)]
#endif
        [DoesNotReturn]
        public static void InvalidOperationException(string message)
            => throw new InvalidOperationException(message);

    }
}
