// Copyright 2014 Serilog Contributors
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
using Azure.Messaging.EventHubs.Producer;
using Serilog.Formatting.Display;

namespace Serilog.Sinks.AzureEventHub
{
    /// <summary>
    /// Pieces shared by the <c>WriteTo.AzureEventHub</c> and <c>AuditTo.AzureEventHub</c>
    /// extension methods so the default template, client/formatter construction, and argument
    /// validation live in one place.
    /// </summary>
    static class EventHubSinkConfigurationHelper
    {
        internal const string DefaultOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}";

        internal static MessageTemplateTextFormatter CreateFormatter(string outputTemplate, IFormatProvider formatProvider) =>
            new MessageTemplateTextFormatter(outputTemplate, formatProvider);

        internal static EventHubProducerClient CreateClient(string connectionString, string eventHubName) =>
            new EventHubProducerClient(connectionString, eventHubName);

        internal static void EnsureNotNull(object value, string parameterName)
        {
            if (value == null)
                throw new ArgumentNullException(parameterName);
        }

        internal static void EnsureNotNullOrWhiteSpace(string value, string parameterName)
        {
            if (value == null)
                throw new ArgumentNullException(parameterName);
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be empty or consist only of whitespace.", parameterName);
        }
    }
}
