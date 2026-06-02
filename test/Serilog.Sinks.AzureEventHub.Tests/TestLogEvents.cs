using System;
using System.Linq;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Parsing;

namespace Serilog.Sinks.AzureEventHub.Tests
{
    /// <summary>
    /// Helpers for constructing <see cref="LogEvent"/> instances and a deterministic
    /// formatter used across the sink tests.
    /// </summary>
    static class TestLogEvents
    {
        static readonly MessageTemplateParser Parser = new MessageTemplateParser();

        /// <summary>
        /// A simple formatter producing "{Level}|{Message}" so assertions can look for
        /// both the level and the rendered message in the emitted body.
        /// </summary>
        public static MessageTemplateTextFormatter Formatter { get; } =
            new MessageTemplateTextFormatter("{Level}|{Message}", null);

        public static LogEvent Create(LogEventLevel level, string message) =>
            new LogEvent(
                DateTimeOffset.UtcNow,
                level,
                exception: null,
                Parser.Parse(message),
                Enumerable.Empty<LogEventProperty>());
    }
}
