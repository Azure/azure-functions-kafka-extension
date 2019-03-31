using System;
namespace Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests
{
    internal static class Constants
    {
        internal const string Broker = "localhost:9092";
        internal const string EndToEndTestsGroupID = "endToEndTestsGroupID";
        internal const string StringTopicWithOnePartition = "stringTopicOnePartition";
        internal const string StringTopicWithTenPartitions = "stringTopicTenPartitions";
        internal const string StringTopicWithLongKeyAndTenPartitions = "stringTopicWithLongKeyTenPartitions";
        internal const string MyAvroRecordTopic = "myAvroRecordTopic";
        internal const string MyProtobufTopic = "myProtobufTopic";
    }
}
