// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    // For more information related to activity tags for Kafka,
    // please refer here - https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/messaging.md#apache-kafka
    internal class ActivityTags
    {
        // Required
        // A string identifying the messaging system. For example: kafka
        public const string System = "messaging.system";

        // Required
        // The message destination name. This might be equal to the span name but is required nevertheless.
        public const string DestinationName = "messaging.destination";

        // Required
        //The kind of message destination. It would be topic in our case.
        public const string DestinationKind = "messaging.destination_kind";

        //A string identifying the kind of message consumption as defined in the Operation names section above.
        //If the operation is “send”, this attribute MUST NOT be set, since the operation can be inferred from the span kind in that case.
        public const string Operation = "messaging.operation";

        // Message keys in Kafka are used for grouping alike messages to ensure they're processed on the same partition.
        // They differ from messaging.message_id in that they're not unique.
        // If the key is null, the attribute MUST NOT be set. 
        public const string KafkaMessageKey = "messaging.kafka.message_key";

        //Name of the Kafka Consumer Group that is handling the message. Only applies to consumers, not producers.
        public const string KafkaConsumerGroup = "messaging.kafka.consumer_group";

        // Not sure about this
        // Client Id for the Consumer or Producer that is handling the message.
        public const string KafkaClientId = "messaging.kafka.client_id";

        // Partition the message is sent to.
        public const string KafkaPartition = "messaging.kafka.partition";        
    }
}
