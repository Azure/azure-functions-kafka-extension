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
        internal const string MyProtobufTopicName = "myProtobufTopic";
    }
}
