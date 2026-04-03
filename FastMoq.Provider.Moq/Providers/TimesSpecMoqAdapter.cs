using System;
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
            if (value.Never)
            {
                return Times.Never();
            }

            if (value.Exactly.HasValue)
            {
                return Times.Exactly(value.Exactly.Value);
            }

            if (value.AtLeast.HasValue)
            {
                return Times.AtLeast(value.AtLeast.Value);
            }

            if (value.AtMost.HasValue)
            {
                return Times.AtMost(value.AtMost.Value);
            }

            return Times.AtLeastOnce();
        }
    }
}