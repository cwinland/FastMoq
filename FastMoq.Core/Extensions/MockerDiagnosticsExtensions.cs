using FastMoq.Models;
using FastMoq.Providers;
using System.Reflection;

namespace FastMoq.Extensions
{
    /// <summary>
    /// Provides provider-neutral diagnostics helpers over existing FastMoq state.
    /// </summary>
    public static class MockerDiagnosticsExtensions
    {
        /// <summary>
        /// Captures a reusable diagnostics snapshot from the supplied <see cref="Mocker" /> instance.
        /// </summary>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <returns>A provider-neutral diagnostics snapshot built from tracked mocks, constructor history, configured instance registrations, and captured log entries.</returns>
        public static MockerDiagnosticsSnapshot CreateDiagnosticsSnapshot(this Mocker mocker)
        {
            ArgumentNullException.ThrowIfNull(mocker);

            var trackedMocks = mocker.mockCollection
                .Select(model => new TrackedMockDiagnosticsEntry(
                    DescribeType(model.Type),
                    null,
                    DescribeType(model.FastMock.MockedType),
                    DescribeType(model.NativeMock.GetType()),
                    model.NonPublic))
                .Concat(mocker.KeyedMockModels.Select(entry => new TrackedMockDiagnosticsEntry(
                    DescribeType(entry.Key.ServiceType),
                    DescribeServiceKey(entry.Key.ServiceKey),
                    DescribeType(entry.Value.FastMock.MockedType),
                    DescribeType(entry.Value.NativeMock.GetType()),
                    entry.Value.NonPublic)))
                .OrderBy(entry => entry.ServiceType, StringComparer.Ordinal)
                .ThenBy(entry => entry.ServiceKey ?? string.Empty, StringComparer.Ordinal)
                .ToArray();

            var constructorSelections = mocker.ConstructorHistory
                .AsEnumerable()
                .SelectMany(pair => pair.Value.OfType<ConstructorModel>().Select(model => new ConstructorSelectionDiagnosticsEntry(
                    DescribeType(pair.Key),
                    DescribeConstructor(model.ConstructorInfo),
                    model.ParameterList.Select(DescribeValue).ToArray())))
                .OrderBy(entry => entry.RequestedType, StringComparer.Ordinal)
                .ThenBy(entry => entry.ConstructorSignature, StringComparer.Ordinal)
                .ToArray();

            var instanceRegistrations = mocker.typeMap
                .Values
                .OfType<IInstanceModel>()
                .Select(model => new InstanceRegistrationDiagnosticsEntry(
                    DescribeType(model.Type),
                    null,
                    DescribeType(model.InstanceType),
                    model.CreateFunc != null,
                    model.Arguments.Select(DescribeValue).ToArray()))
                .Concat(mocker.KeyedTypeModels.Select(entry => new InstanceRegistrationDiagnosticsEntry(
                    DescribeType(entry.Key.ServiceType),
                    DescribeServiceKey(entry.Key.ServiceKey),
                    DescribeType(entry.Value.InstanceType),
                    entry.Value.CreateFunc != null,
                    entry.Value.Arguments.Select(DescribeValue).ToArray())))
                .OrderBy(entry => entry.RequestedType, StringComparer.Ordinal)
                .ThenBy(entry => entry.ServiceKey ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(entry => entry.InstanceType, StringComparer.Ordinal)
                .ToArray();

            var logEntries = mocker.LogEntries
                .Select(entry => new CapturedLogDiagnosticsEntry(
                    entry.LogLevel,
                    entry.EventId.Id,
                    entry.Message,
                    entry.Exception?.GetType().FullName,
                    entry.Exception?.Message))
                .ToArray();

            return new MockerDiagnosticsSnapshot(
                ResolveSnapshotProviderName(mocker),
                trackedMocks,
                constructorSelections,
                instanceRegistrations,
                logEntries);
        }

        private static string ResolveSnapshotProviderName(Mocker mocker)
        {
            var providerNames = mocker.mockCollection
                .Select(model => GetProviderName(model.FastMock))
                .Concat(mocker.KeyedMockModels.Select(entry => GetProviderName(entry.Value.FastMock)))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .Cast<string>()
                .ToArray();

            return providerNames.Length switch
            {
                0 => MockingProviderRegistry.Default.GetType().Name,
                1 => providerNames[0],
                _ => $"Multiple ({string.Join(", ", providerNames)})",
            };
        }

        private static string? GetProviderName(IFastMock fastMock)
        {
            return fastMock is IProviderBoundFastMock providerBoundFastMock
                ? providerBoundFastMock.Provider.GetType().Name
                : null;
        }

        private static string DescribeType(Type? type) => type?.FullName ?? "<unknown>";

        private static string DescribeServiceKey(object? serviceKey)
        {
            return serviceKey switch
            {
                null => "<null>",
                string text => text,
                Type type => DescribeType(type),
                _ => serviceKey.ToString() ?? serviceKey.GetType().FullName ?? "<unknown>",
            };
        }

        private static string DescribeConstructor(ConstructorInfo? constructorInfo)
        {
            if (constructorInfo is null)
            {
                return "<no constructor>";
            }

            var parameters = constructorInfo.GetParameters();
            var signature = string.Join(", ", parameters.Select(parameter => DescribeType(parameter.ParameterType)));
            return $"{DescribeType(constructorInfo.DeclaringType)}({signature})";
        }

        private static string DescribeValue(object? value)
        {
            return value switch
            {
                null => "null",
                string text => $"string:{text}",
                Type type => DescribeType(type),
                _ => DescribeType(value.GetType()),
            };
        }
    }
}