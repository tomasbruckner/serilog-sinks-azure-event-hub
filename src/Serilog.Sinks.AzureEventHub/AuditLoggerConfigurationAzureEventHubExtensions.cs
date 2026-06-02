// Copyright 2019 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Diagnostics.CodeAnalysis;
using Azure.Messaging.EventHubs.Producer;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.AzureEventHub;

namespace Serilog
{
    /// <summary>
    /// Adds the `AuditTo.AzureEventHub()` extension methods to <see cref="LoggerAuditSinkConfiguration"/>.
    /// </summary>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public static class AuditLoggerConfigurationAzureEventHubExtensions
    {
        const string DefaultOutputTemplate = EventHubSinkConfigurationHelper.DefaultOutputTemplate;

        /// <summary>
        /// A sink that puts log events into a provided Azure Event Hub.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="eventHubClient">The Event Hub to use to insert the log entries to.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="outputTemplate">A message template describing the format used to write to the sink.
        /// the default is "{Timestamp} [{Level}] {Message}{NewLine}{Exception}".</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="shouldIncludeProperties">Whether to add the log event's properties to the Event Hub event data. Defaults to false.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration AzureEventHub(
            this LoggerAuditSinkConfiguration loggerConfiguration,
            EventHubProducerClient eventHubClient,
            string outputTemplate = DefaultOutputTemplate,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            bool shouldIncludeProperties = false
            )
        {
            EventHubSinkConfigurationHelper.EnsureNotNull(loggerConfiguration, nameof(loggerConfiguration));
            EventHubSinkConfigurationHelper.EnsureNotNull(eventHubClient, nameof(eventHubClient));
            EventHubSinkConfigurationHelper.EnsureNotNull(outputTemplate, nameof(outputTemplate));

            var formatter = EventHubSinkConfigurationHelper.CreateFormatter(outputTemplate, formatProvider);

            return AzureEventHub(loggerConfiguration, formatter, eventHubClient, restrictedToMinimumLevel, shouldIncludeProperties);
        }

        /// <summary>
        /// A sink that puts log events into a provided Azure Event Hub.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="formatter">Formatter used to convert log events to text.</param>
        /// <param name="eventHubClient">The Event Hub to use to insert the log entries to.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="shouldIncludeProperties">Whether to add the log event's properties to the Event Hub event data. Defaults to false.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration AzureEventHub(
            this LoggerAuditSinkConfiguration loggerConfiguration,
            ITextFormatter formatter,
            EventHubProducerClient eventHubClient,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            bool shouldIncludeProperties = false)
        {
            EventHubSinkConfigurationHelper.EnsureNotNull(loggerConfiguration, nameof(loggerConfiguration));
            EventHubSinkConfigurationHelper.EnsureNotNull(formatter, nameof(formatter));
            EventHubSinkConfigurationHelper.EnsureNotNull(eventHubClient, nameof(eventHubClient));

            return ConfigureSink(loggerConfiguration, formatter, eventHubClient, ownsClient: false, restrictedToMinimumLevel, shouldIncludeProperties);
        }

        /// <summary>
        /// A sink that puts log events into a provided Azure Event Hub.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="connectionString">The Event Hub connection string.</param>
        /// <param name="eventHubName">The Event Hub name.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="outputTemplate">A message template describing the format used to write to the sink.
        /// the default is "{Timestamp} [{Level}] {Message}{NewLine}{Exception}".</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="shouldIncludeProperties">Whether to add the log event's properties to the Event Hub event data. Defaults to false.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="connectionString"/> or <paramref name="eventHubName"/> is empty or whitespace.</exception>
        public static LoggerConfiguration AzureEventHub(
            this LoggerAuditSinkConfiguration loggerConfiguration,
            string connectionString,
            string eventHubName,
            string outputTemplate = DefaultOutputTemplate,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            bool shouldIncludeProperties = false
            )
        {
            EventHubSinkConfigurationHelper.EnsureNotNull(loggerConfiguration, nameof(loggerConfiguration));
            EventHubSinkConfigurationHelper.EnsureNotNullOrWhiteSpace(connectionString, nameof(connectionString));
            EventHubSinkConfigurationHelper.EnsureNotNullOrWhiteSpace(eventHubName, nameof(eventHubName));
            EventHubSinkConfigurationHelper.EnsureNotNull(outputTemplate, nameof(outputTemplate));

            var formatter = EventHubSinkConfigurationHelper.CreateFormatter(outputTemplate, formatProvider);
            var client = EventHubSinkConfigurationHelper.CreateClient(connectionString, eventHubName);

            return ConfigureSink(loggerConfiguration, formatter, client, ownsClient: true, restrictedToMinimumLevel, shouldIncludeProperties);
        }

        /// <summary>
        /// A sink that puts log events into a provided Azure Event Hub.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="formatter">Formatter used to convert log events to text.</param>
        /// <param name="connectionString">The Event Hub connection string.</param>
        /// <param name="eventHubName">The Event Hub name.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="shouldIncludeProperties">Whether to add the log event's properties to the Event Hub event data. Defaults to false.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="connectionString"/> or <paramref name="eventHubName"/> is empty or whitespace.</exception>
        public static LoggerConfiguration AzureEventHub(
            this LoggerAuditSinkConfiguration loggerConfiguration,
            ITextFormatter formatter,
            string connectionString,
            string eventHubName,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            bool shouldIncludeProperties = false
        )
        {
            EventHubSinkConfigurationHelper.EnsureNotNull(loggerConfiguration, nameof(loggerConfiguration));
            EventHubSinkConfigurationHelper.EnsureNotNull(formatter, nameof(formatter));
            EventHubSinkConfigurationHelper.EnsureNotNullOrWhiteSpace(connectionString, nameof(connectionString));
            EventHubSinkConfigurationHelper.EnsureNotNullOrWhiteSpace(eventHubName, nameof(eventHubName));

            var client = EventHubSinkConfigurationHelper.CreateClient(connectionString, eventHubName);

            return ConfigureSink(loggerConfiguration, formatter, client, ownsClient: true, restrictedToMinimumLevel, shouldIncludeProperties);
        }

        // Audit logging always uses the synchronous, non-batching sink so that send failures
        // propagate to the caller; the batching sink (which buffers and only self-logs failures)
        // must never be used here.
        static LoggerConfiguration ConfigureSink(
            LoggerAuditSinkConfiguration loggerConfiguration,
            ITextFormatter formatter,
            EventHubProducerClient eventHubClient,
            bool ownsClient,
            LogEventLevel restrictedToMinimumLevel,
            bool shouldIncludeProperties)
        {
            var sink = new AzureEventHubSink(eventHubClient, formatter, shouldIncludeProperties, ownsClient);
            return loggerConfiguration.Sink(sink, restrictedToMinimumLevel);
        }
    }
}
