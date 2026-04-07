using Moq;

namespace FastMoq.Providers
{
    internal static class TimesSpecMoqAdapter
    {
        internal static Times ToMoq(this TimesSpec? spec)
        {
            if (spec is null)
            {
                return Times.AtLeastOnce();
            }

            var value = spec.Value;
            return value.Mode switch
            {
                TimesSpecMode.Never => Times.Never(),
                TimesSpecMode.Exactly => Times.Exactly(value.Count ?? throw new InvalidOperationException("TimesSpec.Exactly requires a count.")),
                TimesSpecMode.AtLeast => Times.AtLeast(value.Count ?? throw new InvalidOperationException("TimesSpec.AtLeast requires a count.")),
                TimesSpecMode.AtMost => Times.AtMost(value.Count ?? throw new InvalidOperationException("TimesSpec.AtMost requires a count.")),
                _ => Times.AtLeastOnce(),
            };
        }
    }
}