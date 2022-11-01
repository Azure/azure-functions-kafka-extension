// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.Diagnostics
{
    internal class ActivityTags
    {
        // Required
        // A string identifying the messaging system. For example: kafka
        public const string SYSTEM = "messaging.system";

        // Required
        // The message destination name. This might be equal to the span name but is required nevertheless.
        public const string DESTINATIONNAME = "messaging.destination";

        // Required
        //The kind of message destination. It would be topic in our case.
        public const string DESTINATIONKIND = "messaging.destination_kind";

        // Partition the message is sent to.
        public const string KAFKAPARTITION = "messaging.kafka.partition";

        // Client Id for the Consumer or Producer that is handling the message.
        public const string KAFKACLIENTID = "messaging.kafka.client_id";

        //Name of the Kafka Consumer Group that is handling the message. Only applies to consumers, not producers.
        public const string KAFKACONSUMERGROUP = "messaging.kafka.consumer_group";

        //A string identifying the kind of message consumption as defined in the Operation names section above.
        //If the operation is “send”, this attribute MUST NOT be set, since the operation can be inferred from the span kind in that case.
        public const string MESSAGINGOPERATION = "messaging.operation";
    }
}
