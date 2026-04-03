using System;
using Microsoft.Extensions.Logging;

namespace FastMoq.Models
{
    /// <summary>
    /// Captured provider-agnostic logger event emitted by an ILogger mock configured through FastMoq.
    /// </summary>
    public sealed record LogEntry(LogLevel LogLevel, EventId EventId, string Message, Exception? Exception = null);
}