namespace FastMoq.Providers
{
    public enum TimesSpecMode
    {
        AtLeastOnce = 0,
        Exactly = 1,
        AtLeast = 2,
        AtMost = 3,
        Never = 4,
    }

    /// <summary>
    /// Provider agnostic verification specification.
    /// Uses a single verification mode with an optional count where applicable.
    /// </summary>
    public readonly record struct TimesSpec
    {
        private TimesSpec(TimesSpecMode mode, int? count = null)
        {
            if (mode == TimesSpecMode.Never)
            {
                if (count is not null)
                {
                    throw new ArgumentException("TimesSpec.Never does not accept a count.");
                }
            }

            if (mode is TimesSpecMode.Exactly or TimesSpecMode.AtLeast or TimesSpecMode.AtMost)
            {
                if (count is null)
                {
                    throw new ArgumentException($"TimesSpec mode '{mode}' requires a count.");
                }

                if (count < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(count), "TimesSpec count cannot be negative.");
                }
            }

            Mode = mode;
            Count = count;
        }

        public TimesSpecMode Mode { get; }
        public int? Count { get; }

        public static TimesSpec Once => Exactly(1);
        public static TimesSpec NeverCalled => Never();

        public static TimesSpec Exactly(int count) => new(TimesSpecMode.Exactly, count);
        public static TimesSpec AtLeast(int count) => new(TimesSpecMode.AtLeast, count);
        public static TimesSpec AtMost(int count) => new(TimesSpecMode.AtMost, count);
        public static TimesSpec Never() => new(TimesSpecMode.Never);
    }
}