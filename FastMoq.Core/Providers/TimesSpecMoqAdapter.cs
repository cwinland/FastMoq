using System;
using Moq;

namespace FastMoq.Providers
{
    /// <summary>
    /// Adapter helpers bridging provider-first <see cref="TimesSpec"/> with Moq's <see cref="Times"/>.
    /// Lives behind Moq reference (will no-op when values not set).
    /// </summary>
    internal static class TimesSpecMoqAdapter
    {
        /// <summary>
        /// Converts a <see cref="TimesSpec"/> to a <see cref="Times"/> instance.
        /// Null => <c>Times.AtLeastOnce()</c> (Moq default-ish) unless Never flag set.
        /// </summary>
        internal static Times ToMoq(this TimesSpec? spec)
        {
            if (spec is null) return Times.AtLeastOnce();
            var value = spec.Value;
            if (value.Never) return Times.Never();
            if (value.Exactly.HasValue) return Times.Exactly(value.Exactly.Value);
            if (value.AtLeast.HasValue) return Times.AtLeast(value.AtLeast.Value);
            if (value.AtMost.HasValue) return Times.AtMost(value.AtMost.Value);
            // Fallback (treat unset as at least once)
            return Times.AtLeastOnce();
        }

        /// <summary>
        /// Creates a <see cref="TimesSpec"/> from a <see cref="Times"/> where possible.
        /// Only common mappings are supported; others fall back to a simple Exactly guess using reflection.
        /// </summary>
        internal static TimesSpec ToSpec(this Times times)
        {
            // Moq uses internal representation; attempt limited extraction.
            var field = typeof(Times).GetField("callCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var callCount = field?.GetValue(times) as int?;
            var toString = times.ToString();
            if (string.Equals(toString, "Never", StringComparison.OrdinalIgnoreCase)) return new TimesSpec(Never: true);
            if (string.Equals(toString, "Once", StringComparison.OrdinalIgnoreCase)) return new TimesSpec(Exactly: 1);
            if (callCount.HasValue)
            {
                // Heuristic – assume Exactly N
                return new TimesSpec(Exactly: callCount.Value);
            }
            return new TimesSpec(AtLeast: 1); // safe default
        }
    }
}
