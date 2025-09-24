using System;

namespace FastMoq.Providers
{
    /// <summary>
    /// Options to influence mock creation in a provider agnostic manner.
    /// </summary>
    public sealed record MockCreationOptions(
        bool Strict = false,
        bool CallBase = true,
        object?[]? ConstructorArgs = null,
        bool AllowNonPublic = false // NEW: permit provider to use non-public (e.g. protected) ctors when available
    );
}
