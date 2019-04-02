// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public class KafkaTriggerBindingStrategy : ITriggerBindingStrategy<KafkaEventData, KafkaTriggerInput>
    {
        /// <summary>
        /// Given a raw string, convert to a TTriggerValue.
        /// This is primarily used in the "invoke from dashboard" path. 
        /// </summary>
        public KafkaTriggerInput ConvertFromString(string input)
        {
            // Need to dig up to see how "invoke from dashboard" works.
            // Returning null for now, since it is not being called
            return null;
        }

        // Single instance: Core --> EventData
        public KafkaEventData BindSingle(KafkaTriggerInput value, ValueBindingContext context)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            return value.GetSingleEventData();
        }

        public KafkaEventData[] BindMultiple(KafkaTriggerInput value, ValueBindingContext context)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            return value.Events;
        }

        public Dictionary<string, Type> GetBindingContract(bool isSingleDispatch = true)
        {
            var contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            //contract.Add("PartitionContext", typeof(PartitionContext));
            AddBindingContractMember(contract, nameof(KafkaEventData.Key), typeof(object), isSingleDispatch);
            AddBindingContractMember(contract, nameof(KafkaEventData.Partition), typeof(int), isSingleDispatch);
            AddBindingContractMember(contract, nameof(KafkaEventData.Topic), typeof(string), isSingleDispatch);
            AddBindingContractMember(contract, nameof(KafkaEventData.Timestamp), typeof(DateTime), isSingleDispatch);

            return contract;
        }

        private static void AddBindingContractMember(Dictionary<string, Type> contract, string name, Type type, bool isSingleDispatch)
        {
            if (!isSingleDispatch)
            {
                name += "Array";
            }
            contract.Add(name, isSingleDispatch ? type : type.MakeArrayType());
        }

        public Dictionary<string, object> GetBindingData(KafkaTriggerInput value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            //SafeAddValue(() => bindingData.Add(nameof(value.PartitionContext), value.PartitionContext));

            if (value.IsSingleDispatch)
            {
                AddBindingData(bindingData, value.GetSingleEventData());
            }
            else
            {
                AddBindingData(bindingData, value.Events);
            }

            return bindingData;
        }

        internal static void AddBindingData(Dictionary<string, object> bindingData, KafkaEventData[] events)
        {
            //int length = events.Length;
            //var partitionKeys = new string[length];
            //var offsets = new string[length];
            //var sequenceNumbers = new long[length];
            //var enqueuedTimesUtc = new DateTime[length];
            //var properties = new IDictionary<string, object>[length];
            //var systemProperties = new IDictionary<string, object>[length];

            //SafeAddValue(() => bindingData.Add("PartitionKeyArray", partitionKeys));
            //SafeAddValue(() => bindingData.Add("OffsetArray", offsets));
            //SafeAddValue(() => bindingData.Add("SequenceNumberArray", sequenceNumbers));
            //SafeAddValue(() => bindingData.Add("EnqueuedTimeUtcArray", enqueuedTimesUtc));
            //SafeAddValue(() => bindingData.Add("PropertiesArray", properties));
            //SafeAddValue(() => bindingData.Add("SystemPropertiesArray", systemProperties));

            // for (int i = 0; i < events.Length; i++)
            // {
            //     partitionKeys[i] = events[i].SystemProperties?.PartitionKey;
            //     offsets[i] = events[i].SystemProperties?.Offset;
            //     sequenceNumbers[i] = events[i].SystemProperties?.SequenceNumber ?? 0;
            //     enqueuedTimesUtc[i] = events[i].SystemProperties?.EnqueuedTimeUtc ?? DateTime.MinValue;
            //     properties[i] = events[i].Properties;
            //     systemProperties[i] = events[i].SystemProperties?.ToDictionary();
            // }
        }

        private static void AddBindingData(Dictionary<string, object> bindingData, KafkaEventData eventData)
        {
            bindingData.Add(nameof(KafkaEventData.Key), eventData.Key);
            bindingData.Add(nameof(KafkaEventData.Partition), eventData.Partition);
            bindingData.Add(nameof(KafkaEventData.Topic), eventData.Topic);
            bindingData.Add(nameof(KafkaEventData.Timestamp), eventData.Timestamp);
        }

        private static void SafeAddValue(Action addValue)
        {
            try
            {
                addValue();
            }
            catch
            {
                // some message propery getters can throw, based on the
                // state of the message
            }
        }
    }
}