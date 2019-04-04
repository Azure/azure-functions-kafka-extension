// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
            AddBindingContractMember(contract, nameof(KafkaEventData.Key), typeof(object), isSingleDispatch);
            AddBindingContractMember(contract, nameof(KafkaEventData.Partition), typeof(int), isSingleDispatch);
            AddBindingContractMember(contract, nameof(KafkaEventData.Topic), typeof(string), isSingleDispatch);
            AddBindingContractMember(contract, nameof(KafkaEventData.Timestamp), typeof(DateTime), isSingleDispatch);
            AddBindingContractMember(contract, nameof(KafkaEventData.Offset), typeof(long), isSingleDispatch);

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
            int length = events.Length;
            var partitions = new int[length];
            var offsets = new long[length];
            var timestamps = new DateTime[length];
            var topics = new string[length];
            var keys = new object[length];

            bindingData.Add("PartitionArray", partitions);
            bindingData.Add("OffsetArray", offsets);
            bindingData.Add("TimestampArray", timestamps);
            bindingData.Add("TopicArray", topics);
            bindingData.Add("KeyArray", keys);

            for (int i = 0; i < events.Length; i++)
            {
                partitions[i] = events[i].Partition;
                offsets[i] = events[i].Offset;
                timestamps[i] = events[i].Timestamp;
                keys[i] = events[i].Key;
                topics[i] = events[i].Topic;
            }
        }

        private static void AddBindingData(Dictionary<string, object> bindingData, KafkaEventData eventData)
        {
            bindingData.Add(nameof(KafkaEventData.Key), eventData.Key);
            bindingData.Add(nameof(KafkaEventData.Partition), eventData.Partition);
            bindingData.Add(nameof(KafkaEventData.Topic), eventData.Topic);
            bindingData.Add(nameof(KafkaEventData.Timestamp), eventData.Timestamp);
            bindingData.Add(nameof(KafkaEventData.Offset), eventData.Offset);
        }
    }
}