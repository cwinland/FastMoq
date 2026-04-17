namespace FastMoq
{
    /// <summary>
    /// Controls whether <c>AddPropertyState&lt;TService&gt;(...)</c> keeps assigned values on the proxy only or also writes them through to the wrapped inner instance.
    /// </summary>
    public enum PropertyStateMode
    {
        /// <summary>
        /// Preserve the proxy-backed property value and also assign the same value to the wrapped inner instance.
        /// This keeps the original <c>AddPropertyState&lt;TService&gt;(...)</c> behavior.
        /// </summary>
        WriteThrough = 0,

        /// <summary>
        /// Preserve the proxy-backed property value without mutating the wrapped inner instance.
        /// Non-property members still forward to the wrapped instance.
        /// </summary>
        ProxyOnly = 1,
    }
}