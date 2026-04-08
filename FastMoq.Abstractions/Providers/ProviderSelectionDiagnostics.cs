namespace FastMoq.Providers
{
    /// <summary>
    /// Builds consistent provider-selection guidance for provider-specific FastMoq APIs.
    /// This keeps runtime messages aligned across the compatibility surface and provider-specific escape hatches.
    /// </summary>
    public static class ProviderSelectionDiagnostics
    {
        /// <summary>
        /// Creates a standardized provider mismatch message that names the expected provider,
        /// the inferred active provider, and the common bootstrap options.
        /// Use this to keep runtime guidance aligned with the analyzer guidance for provider bootstrap.
        /// </summary>
        public static string BuildProviderMismatchMessage(
            string expectedProviderName,
            Type mockedType,
            object? nativeMock,
            object? instance,
            string apiName,
            string providerNeutralAlternative)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(expectedProviderName);
            ArgumentNullException.ThrowIfNull(mockedType);
            ArgumentException.ThrowIfNullOrWhiteSpace(apiName);
            ArgumentException.ThrowIfNullOrWhiteSpace(providerNeutralAlternative);

            var activeProviderName = InferActiveProviderName(mockedType, nativeMock, instance);
            var nativeType = nativeMock?.GetType().FullName ?? "null";

            return $"API '{apiName}' requires the '{expectedProviderName}' provider, but the active provider is '{activeProviderName}'. " +
                   $"Tracked mock for '{mockedType.FullName}' is backed by '{nativeType}'. " +
                                     $"You can declare [assembly: FastMoqDefaultProvider(\"{expectedProviderName}\")] when the provider name is already resolvable, " +
                                     $"declare [assembly: FastMoqRegisterProvider(\"{expectedProviderName}\", typeof(...), SetAsDefault = true)] to register and select it at assembly scope, " +
                   $"Select the '{expectedProviderName}' provider via MockingProviderRegistry.Push(\"{expectedProviderName}\"), " +
                   $"MockingProviderRegistry.SetDefault(\"{expectedProviderName}\"), or MockingProviderRegistry.Register(\"{expectedProviderName}\", ..., setAsDefault: true), " +
                   $"or use {providerNeutralAlternative}.";
        }

        private static string InferActiveProviderName(Type mockedType, object? nativeMock, object? instance)
        {
            var nativeType = nativeMock?.GetType() ?? instance?.GetType();
            if (nativeType is null)
            {
                return "unknown";
            }

            if (LooksLikeMoq(nativeType))
            {
                return "moq";
            }

            if (LooksLikeReflection(mockedType, nativeType))
            {
                return "reflection";
            }

            if (ReferenceEquals(nativeMock, instance))
            {
                return "nsubstitute";
            }

            return "unknown";
        }

        private static bool LooksLikeMoq(Type type)
        {
            for (var current = type; current is not null; current = current.BaseType!)
            {
                if (current.FullName == "Moq.Mock" || current.FullName == "Moq.Mock`1")
                {
                    return true;
                }
            }

            return false;
        }

        private static bool LooksLikeReflection(Type mockedType, Type nativeType)
        {
            if (nativeType == mockedType)
            {
                return true;
            }

            for (var current = nativeType; current is not null; current = current.BaseType!)
            {
                if (current.FullName == "FastMoq.Providers.ReflectionProvider.ReflectionMockingProvider+TrackingDispatchProxy")
                {
                    return true;
                }
            }

            return false;
        }
    }
}