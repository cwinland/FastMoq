﻿using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#pragma warning disable CS8777 // Parameter must have a non-null value when exiting.

namespace FastMoq.Extensions
{
    /// <summary>
    ///     Class ObjectExtensions.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        ///     Adds or updates (if key exists) the value in the dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of the t key.</typeparam>
        /// <typeparam name="TValue">The type of the t value.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            dictionary[key] = value;
        }

        /// <summary>
        ///     Raises if predicate is true.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        /// <param name="name">The name.</param>
        /// <param name="path">The path.</param>
        /// <param name="line">The line.</param>
        /// <param name="exp">The exp.</param>
        /// <returns><c>true</c> if expression is true, <c>false</c> otherwise.</returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public static bool RaiseIf(Func<bool> predicate, string name, string path, int line, string exp) =>
            predicate() ? throw new InvalidOperationException($"{exp} in {name} is invalid on line {line} of {path}") : true;

        /// <summary>
        ///     Raises if null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing">The thing.</param>
        /// <param name="name">The name.</param>
        /// <param name="path">The path.</param>
        /// <param name="line">The line.</param>
        /// <param name="exp">The exp.</param>
        /// <returns>Raises if null.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.InvalidOperationException"></exception>
        public static T RaiseIfNull<T>([NotNull] this T? thing, [CallerMemberName] string? name = null, [CallerFilePath] string? path = null,
            [CallerLineNumber] int? line = null, [CallerArgumentExpression(nameof(thing))] string? exp = null)
            where T : class
        {
            RaiseIf(() => thing is null, name ?? string.Empty, path ?? string.Empty, line ?? 0, exp ?? string.Empty);

            ArgumentNullException.ThrowIfNull(thing); // This is to tell the compiler this will never be null.

            return thing;
        }

        /// <summary>
        ///     Raises if null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing">The thing.</param>
        /// <param name="name">The name.</param>
        /// <param name="path">The path.</param>
        /// <param name="line">The line.</param>
        /// <param name="exp">The exp.</param>
        /// <exception cref="System.InvalidOperationException"></exception>
        public static void RaiseIfNull<T>([NotNull] this T? thing, [CallerMemberName] string? name = null, [CallerFilePath] string? path = null,
            [CallerLineNumber] int? line = null, [CallerArgumentExpression(nameof(thing))] string? exp = null)
            where T : struct
            => RaiseIf(() => thing is null, name ?? string.Empty, path ?? string.Empty, line ?? 0, exp ?? string.Empty);

        /// <summary>
        ///     Raises if null or empty.
        /// </summary>
        /// <param name="thing">The thing.</param>
        /// <param name="name">The name.</param>
        /// <param name="path">The path.</param>
        /// <param name="line">The line.</param>
        /// <param name="exp">The exp.</param>
        public static void RaiseIfNullOrEmpty([NotNull] this string? thing, [CallerMemberName] string? name = null,
            [CallerFilePath] string? path = null, [CallerLineNumber] int? line = null, [CallerArgumentExpression(nameof(thing))] string? exp = null)
            => RaiseIf(() => string.IsNullOrEmpty(thing), name ?? string.Empty, path ?? string.Empty, line ?? 0, exp ?? string.Empty);

        /// <summary>
        ///     Raises if null or whitespace.
        /// </summary>
        /// <param name="thing">The thing.</param>
        /// <param name="name">The name.</param>
        /// <param name="path">The path.</param>
        /// <param name="line">The line.</param>
        /// <param name="exp">The exp.</param>
        public static void RaiseIfNullOrWhitespace([NotNull] this string? thing, [CallerMemberName] string? name = null,
            [CallerFilePath] string? path = null, [CallerLineNumber] int? line = null, [CallerArgumentExpression(nameof(thing))] string? exp = null)
            => RaiseIf(() => string.IsNullOrWhiteSpace(thing), name ?? string.Empty, path ?? string.Empty, line ?? 0, exp ?? string.Empty);
    }
}
