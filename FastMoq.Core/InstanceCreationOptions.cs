using System;
using System.ComponentModel;

namespace FastMoq
{
    /// <summary>
    /// Controls how <see cref="Mocker.CreateInstance{T}(InstanceCreationOptions, object?[])"/> selects and creates an instance.
    /// This provides a single entry point for behaviors that were previously spread across multiple CreateInstance* methods.
    /// </summary>
    public sealed class InstanceCreationOptions
    {
        /// <summary>
        /// Obsolete compatibility flag retained for source compatibility.
        /// File-system resolution now follows <see cref="Mocker.Policy"/> and <see cref="MockerPolicyOptions.EnabledBuiltInTypeResolutions"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Ignored. File-system resolution now follows Mocker.Policy.EnabledBuiltInTypeResolutions.")]
        public bool UsePredefinedFileSystem { get; set; }

        /// <summary>
        /// Obsolete compatibility flag retained for source compatibility.
        /// Constructor selection now follows the normal public-first pattern and uses <see cref="FallbackToNonPublicConstructors"/> to permit non-public fallback.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Ignored for CreateInstance. Constructor selection is public-first and uses FallbackToNonPublicConstructors for non-public fallback.")]
        public bool AllowNonPublicConstructors { get; set; }

        /// <summary>
        /// Controls whether FastMoq may fall back from a public-constructor search to a non-public-constructor search.
        /// When not set, FastMoq uses the current <see cref="MockerPolicyOptions.DefaultFallbackToNonPublicConstructors"/> policy.
        /// Compatibility helpers such as <c>Strict</c> and preset methods can update that default policy.
        /// </summary>
        public bool? FallbackToNonPublicConstructors { get; set; }

        /// <summary>
        /// Optional explicit constructor signature used to select a constructor by parameter types.
        /// When supplied, constructor lookup uses these types instead of inferring from argument values.
        /// </summary>
        public Type?[]? ConstructorParameterTypes { get; set; }

        /// <summary>
        /// Controls how optional constructor parameters are resolved when FastMoq supplies missing arguments.
        /// Default preserves legacy behavior by using declared defaults or null.
        /// </summary>
        public OptionalParameterResolutionMode OptionalParameterResolution { get; set; } = OptionalParameterResolutionMode.UseDefaultOrNull;
    }
}