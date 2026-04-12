namespace FastMoq
{
    /// <summary>
    /// Marks the constructor FastMoq should prefer during implicit constructor selection within the current visibility scope.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    public sealed class PreferredConstructorAttribute : Attribute
    {
    }
}