using System;

namespace FastMoq
{
    /// <summary>
    /// Controls how <see cref="Mocker.CreateInstance{T}(InstanceCreationOptions, object?[])"/> selects and creates an instance.
    /// This provides a single entry point for behaviors that were previously spread across multiple CreateInstance* methods.
    /// </summary>
    public sealed class InstanceCreationOptions
    {
        /// <summary>
        /// When true, returns the predefined file system instance when the requested type resolves to <c>IFileSystem</c>.
        /// Default is true to preserve the existing CreateInstance behavior.
        /// </summary>
        public bool UsePredefinedFileSystem { get; set; } = true;

        /// <summary>
        /// When true, constructor resolution may consider non-public constructors.
        /// Default is false to preserve the existing CreateInstance behavior.
        /// </summary>
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