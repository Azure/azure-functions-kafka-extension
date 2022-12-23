// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.Trigger
{
    /// <summary>
    /// Attribute to set properties for the Confluent schema registry configuration.
    /// More information about the options can be found in <see cref="Confluent.SchemaRegistry.CachedSchemaRegistryClient"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true)]
    [Binding]
    public class SchemaRegistryConfigAttribute : Attribute
    {
        public SchemaRegistryConfigAttribute(string key, string value)
        {
            Key = key;
            Value = value;
        }


        /// <summary>
        /// The key of the Confluent schema registry configuration
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// The value of the Confluent schema registry configuration
        /// </summary>
        public string Value { get; }
    }
}