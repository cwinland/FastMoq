namespace FastMoq.Providers
{
    /// <summary>
    /// Identifies the verification strategy represented by a <see cref="TimesSpec"/> value.
    /// </summary>
    public enum TimesSpecMode
    {
        /// <summary>
        /// Verifies that the invocation occurred at least once.
        /// </summary>
        AtLeastOnce = 0,

        /// <summary>
        /// Verifies that the invocation occurred an exact number of times.
        /// </summary>
        Exactly = 1,

        /// <summary>
        /// Verifies that the invocation occurred at least the specified number of times.
        /// </summary>
        AtLeast = 2,

        /// <summary>
        /// Verifies that the invocation occurred at most the specified number of times.
        /// </summary>
        AtMost = 3,

        /// <summary>
        /// Verifies that the invocation never occurred.
        /// </summary>
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

        /// <summary>
        /// Gets the verification mode.
        /// </summary>
        public TimesSpecMode Mode { get; }

        /// <summary>
        /// Gets the expected invocation count when the selected <see cref="Mode"/> requires one.
        /// </summary>
        public int? Count { get; }

        /// <summary>
        /// Gets a specification that requires exactly one invocation.
        /// </summary>
        public static TimesSpec Once => Exactly(1);

        /// <summary>
        /// Gets a specification that requires zero invocations.
        /// </summary>
        public static TimesSpec NeverCalled => Never();

        /// <summary>
        /// Creates a specification that requires exactly <paramref name="count"/> invocations.
        /// </summary>
        /// <param name="count">The required invocation count.</param>
        /// <returns>A verification specification for an exact count.</returns>
        public static TimesSpec Exactly(int count) => new(TimesSpecMode.Exactly, count);

        /// <summary>
        /// Creates a specification that requires at least <paramref name="count"/> invocations.
        /// </summary>
        /// <param name="count">The minimum required invocation count.</param>
        /// <returns>A verification specification for a minimum count.</returns>
        public static TimesSpec AtLeast(int count) => new(TimesSpecMode.AtLeast, count);

        /// <summary>
        /// Creates a specification that allows at most <paramref name="count"/> invocations.
        /// </summary>
        /// <param name="count">The maximum allowed invocation count.</param>
        /// <returns>A verification specification for a maximum count.</returns>
        public static TimesSpec AtMost(int count) => new(TimesSpecMode.AtMost, count);

        /// <summary>
        /// Creates a specification that requires zero invocations.
        /// </summary>
        /// <returns>A verification specification for no invocations.</returns>
        public static TimesSpec Never() => new(TimesSpecMode.Never);
    }
}