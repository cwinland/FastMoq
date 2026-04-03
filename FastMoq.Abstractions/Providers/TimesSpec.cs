namespace FastMoq.Providers
{
    /// <summary>
    /// Provider agnostic verification specification.
    /// Only one dimension should normally be set (Exactly, AtLeast, AtMost, Never).
    /// </summary>
    public readonly record struct TimesSpec(int? Exactly = null, int? AtLeast = null, int? AtMost = null, bool Never = false)
    {
        public static TimesSpec Once => new(Exactly: 1);
        public static TimesSpec NeverCalled => new(Never: true);
    }
}