using System;
namespace Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests
{
    internal static class Constants
    {
        internal const string EndToEndTestsGroupID = "endToEndTestsGroupID";
        internal const string StringTopicWithOnePartitionName = "stringTopicOnePartition";
        internal const string StringTopicWithTenPartitionsName = "stringTopicTenPartitions";
        internal const string StringTopicWithLongKeyAndTenPartitionsName = "stringTopicWithLongKeyTenPartitions";
        internal const string MyAvroRecordTopicName = "myAvroRecordTopic";
        internal const string MyKeyAvroRecordTopicName = "myKeyAvroRecordTopic";
        internal const string MyProtobufTopicName = "myProtobufTopic";
        internal const string SchemaRegistryTopicName = "schemaRegistryTopic";
        internal const string SchemaRegistryNoKeyTopicName = "schemaRegistryNoKeyTopic";
        internal const string AtLeastOnceTopicName = "atLeastOnceTopic";
        internal const string AtLeastOnceMaxRetriesTopicName = "atLeastOnceMaxRetriesTopic";
        internal const string ConsumerGroupID = "e2e_tests";
        internal const string AtLeastOnceConsumerGroupID = "at_least_once_e2e";
        internal const string AtLeastOnceMaxRetriesConsumerGroupID = "at_least_once_maxretries_e2e";
        internal const string SchemaRegistryUrl = "localhost:8081";
    }
}
