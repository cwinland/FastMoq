using System;

namespace FastMoq
{
    [Flags]
    public enum BuiltInTypeResolutionFlags
    {
        None = 0,
        FileSystem = 1 << 0,
        HttpClient = 1 << 1,
        Uri = 1 << 2,
        DbContext = 1 << 3,
        All = FileSystem | HttpClient | Uri | DbContext,
        StrictCompatibilityDefaults = DbContext,
        LenientDefaults = All,
    }
}