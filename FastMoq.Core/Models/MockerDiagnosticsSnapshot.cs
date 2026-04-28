using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace FastMoq.Models
{
    /// <summary>
    /// Represents a provider-neutral diagnostics snapshot captured from a <see cref="Mocker" /> instance.
    /// </summary>
    public sealed record MockerDiagnosticsSnapshot
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockerDiagnosticsSnapshot" /> class.
        /// </summary>
        /// <param name="providerName">The active provider name used when the snapshot was created.</param>
        /// <param name="trackedMocks">The tracked mocks currently registered in the mocker.</param>
        /// <param name="constructorSelections">The constructor selections recorded during component creation.</param>
        /// <param name="instanceRegistrations">The current instance registrations configured on the mocker.</param>
        /// <param name="logEntries">The captured log entries currently stored on the mocker.</param>
        public MockerDiagnosticsSnapshot(
            string providerName,
            IReadOnlyList<TrackedMockDiagnosticsEntry> trackedMocks,
            IReadOnlyList<ConstructorSelectionDiagnosticsEntry> constructorSelections,
            IReadOnlyList<InstanceRegistrationDiagnosticsEntry> instanceRegistrations,
            IReadOnlyList<CapturedLogDiagnosticsEntry> logEntries)
        {
            ProviderName = providerName ?? throw new ArgumentNullException(nameof(providerName));
            TrackedMocks = trackedMocks ?? throw new ArgumentNullException(nameof(trackedMocks));
            ConstructorSelections = constructorSelections ?? throw new ArgumentNullException(nameof(constructorSelections));
            InstanceRegistrations = instanceRegistrations ?? throw new ArgumentNullException(nameof(instanceRegistrations));
            LogEntries = logEntries ?? throw new ArgumentNullException(nameof(logEntries));
        }

        /// <summary>
        /// Gets the active provider name used when the snapshot was created.
        /// </summary>
        public string ProviderName { get; }

        /// <summary>
        /// Gets the tracked mocks currently registered in the mocker.
        /// </summary>
        public IReadOnlyList<TrackedMockDiagnosticsEntry> TrackedMocks { get; }

        /// <summary>
        /// Gets the constructor selections recorded during component creation.
        /// </summary>
        public IReadOnlyList<ConstructorSelectionDiagnosticsEntry> ConstructorSelections { get; }

        /// <summary>
        /// Gets the current instance registrations configured on the mocker.
        /// </summary>
        public IReadOnlyList<InstanceRegistrationDiagnosticsEntry> InstanceRegistrations { get; }

        /// <summary>
        /// Gets the captured log entries currently stored on the mocker.
        /// </summary>
        public IReadOnlyList<CapturedLogDiagnosticsEntry> LogEntries { get; }

        /// <summary>
        /// Formats the snapshot into a readable multi-line diagnostics view.
        /// </summary>
        /// <returns>A readable multi-line diagnostics dump.</returns>
        public string ToDebugView()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Provider: {ProviderName}");
            AppendSection(builder, "Tracked mocks", TrackedMocks.Select(entry =>
                $"{entry.ServiceType}{FormatServiceKey(entry.ServiceKey)} => {entry.MockedType} via {entry.NativeMockType}{(entry.NonPublic ? " [non-public]" : string.Empty)}"));
            AppendSection(builder, "Constructor selections", ConstructorSelections.Select(entry =>
                $"{entry.RequestedType} => {entry.ConstructorSignature} [{string.Join(", ", entry.ArgumentSummaries)}]"));
            AppendSection(builder, "Instance registrations", InstanceRegistrations.Select(entry =>
                $"{entry.RequestedType}{FormatServiceKey(entry.ServiceKey)} => {entry.InstanceType} {(entry.HasFactory ? "factory" : "direct")} [{string.Join(", ", entry.ArgumentSummaries)}]"));
            AppendSection(builder, "Log entries", LogEntries.Select(entry =>
                $"{entry.LogLevel} ({entry.EventId}): {entry.Message}{FormatException(entry)}"));
            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// Serializes the snapshot to JSON for machine-readable diagnostics output.
        /// </summary>
        /// <param name="indented">True to format the JSON with indentation.</param>
        /// <returns>A JSON representation of the current diagnostics snapshot.</returns>
        public string ToJson(bool indented = true)
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = indented,
            });
        }

        private static void AppendSection(StringBuilder builder, string title, IEnumerable<string> lines)
        {
            builder.AppendLine(title + ":");
            var wroteLine = false;
            foreach (var line in lines)
            {
                builder.AppendLine($"  - {line}");
                wroteLine = true;
            }

            if (!wroteLine)
            {
                builder.AppendLine("  - <none>");
            }
        }

        private static string FormatException(CapturedLogDiagnosticsEntry entry)
        {
            if (entry.ExceptionType is null)
            {
                return string.Empty;
            }

            return $" [{entry.ExceptionType}: {entry.ExceptionMessage}]";
        }

        private static string FormatServiceKey(string? serviceKey)
        {
            return serviceKey is null ? string.Empty : $" [key: {serviceKey}]";
        }
    }

    /// <summary>
    /// Describes a tracked mock currently registered in a <see cref="Mocker" /> instance.
    /// </summary>
    /// <param name="ServiceType">The requested service type that owns the tracked mock.</param>
    /// <param name="ServiceKey">The formatted DI-style service key when the tracked mock is keyed.</param>
    /// <param name="MockedType">The mocked type exposed by the provider abstraction.</param>
    /// <param name="NativeMockType">The underlying provider-native mock type.</param>
    /// <param name="NonPublic">True when the tracked mock allows non-public construction.</param>
    public sealed record TrackedMockDiagnosticsEntry(string ServiceType, string? ServiceKey, string MockedType, string NativeMockType, bool NonPublic);

    /// <summary>
    /// Describes a constructor selection recorded in <see cref="ConstructorHistory" />.
    /// </summary>
    /// <param name="RequestedType">The requested type that was being created.</param>
    /// <param name="ConstructorSignature">The selected constructor signature.</param>
    /// <param name="ArgumentSummaries">A summary of the supplied or resolved constructor arguments.</param>
    public sealed record ConstructorSelectionDiagnosticsEntry(string RequestedType, string ConstructorSignature, IReadOnlyList<string> ArgumentSummaries);

    /// <summary>
    /// Describes an instance registration currently configured on a <see cref="Mocker" /> instance.
    /// </summary>
    /// <param name="RequestedType">The requested service type that resolved through an instance registration.</param>
    /// <param name="ServiceKey">The formatted DI-style service key when the registration is keyed.</param>
    /// <param name="InstanceType">The concrete instance type produced by the registration.</param>
    /// <param name="HasFactory">True when the registration used a factory delegate.</param>
    /// <param name="ArgumentSummaries">A summary of any stored registration arguments.</param>
    public sealed record InstanceRegistrationDiagnosticsEntry(string RequestedType, string? ServiceKey, string InstanceType, bool HasFactory, IReadOnlyList<string> ArgumentSummaries);

    /// <summary>
    /// Describes a captured log entry stored on a <see cref="Mocker" /> instance.
    /// </summary>
    /// <param name="LogLevel">The captured log level.</param>
    /// <param name="EventId">The captured event id.</param>
    /// <param name="Message">The captured log message.</param>
    /// <param name="ExceptionType">The exception type when a matching exception was captured.</param>
    /// <param name="ExceptionMessage">The exception message when a matching exception was captured.</param>
    public sealed record CapturedLogDiagnosticsEntry(LogLevel LogLevel, int EventId, string Message, string? ExceptionType, string? ExceptionMessage);
}