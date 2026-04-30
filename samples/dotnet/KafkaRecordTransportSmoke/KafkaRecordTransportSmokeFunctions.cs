// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Azure.WebJobs.Extensions.Kafka.Serialization;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Logging;

namespace KafkaRecordTransportSmoke
{
    public static class KafkaRecordTransportSmokeFunctions
    {
        private static readonly ConcurrentQueue<ReceivedKafkaRecord> ReceivedRecords = new ConcurrentQueue<ReceivedKafkaRecord>();

        [FunctionName(nameof(KafkaRecordTransportProducer))]
        public static IActionResult KafkaRecordTransportProducer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "produce")] HttpRequest req,
            [Kafka("LocalBroker", "%KafkaRecordSmokeTopic%")] out string kafkaEventData,
            ILogger log)
        {
            var message = req.Query["message"].FirstOrDefault();
            if (string.IsNullOrEmpty(message))
            {
                message = new StreamReader(req.Body).ReadToEnd();
            }

            if (string.IsNullOrEmpty(message))
            {
                message = "kafkarecord-transport-smoke-" + Guid.NewGuid().ToString("N");
            }

            kafkaEventData = message;
            log.LogInformation("Produced KafkaRecord transport smoke message: {Message}", message);

            return new OkObjectResult(new { message });
        }

        [FunctionName(nameof(KafkaRecordTransportTrigger))]
        public static void KafkaRecordTransportTrigger(
            [KafkaTrigger("LocalBroker", "%KafkaRecordSmokeTopic%", ConsumerGroup = "%KafkaRecordSmokeConsumerGroup%")] ParameterBindingData bindingData,
            ILogger log)
        {
            var proto = KafkaRecordProto.Parser.ParseFrom(bindingData.Content.ToArray());
            var value = proto.HasValue ? Encoding.UTF8.GetString(proto.Value.ToByteArray()) : null;
            var key = proto.HasKey ? Encoding.UTF8.GetString(proto.Key.ToByteArray()) : null;

            var record = new ReceivedKafkaRecord
            {
                Version = bindingData.Version,
                Source = bindingData.Source,
                ContentType = bindingData.ContentType,
                Topic = proto.Topic,
                Partition = proto.Partition,
                Offset = proto.Offset,
                Key = key,
                Value = value,
                TimestampUnixMs = proto.Timestamp?.UnixTimestampMs ?? 0,
                TimestampType = proto.Timestamp?.Type ?? 0,
                HeaderCount = proto.Headers.Count,
                LeaderEpoch = proto.HasLeaderEpoch ? proto.LeaderEpoch : (int?)null
            };

            ReceivedRecords.Enqueue(record);
            while (ReceivedRecords.Count > 100 && ReceivedRecords.TryDequeue(out _))
            {
            }

            log.LogInformation(
                "Received KafkaRecord transport smoke payload: source={Source}, contentType={ContentType}, topic={Topic}, partition={Partition}, offset={Offset}, value={Value}",
                record.Source,
                record.ContentType,
                record.Topic,
                record.Partition,
                record.Offset,
                record.Value);
        }

        [FunctionName(nameof(KafkaRecordTransportStatus))]
        public static IActionResult KafkaRecordTransportStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "status")] HttpRequest req,
            ILogger log)
        {
            var message = req.Query["message"].FirstOrDefault();
            var records = ReceivedRecords.ToArray();
            var matchingRecord = string.IsNullOrEmpty(message)
                ? records.LastOrDefault()
                : records.LastOrDefault(record => record.Value == message);

            return new OkObjectResult(new
            {
                count = records.Length,
                matched = matchingRecord != null,
                record = matchingRecord
            });
        }
    }

    public class ReceivedKafkaRecord
    {
        public string Version { get; set; }

        public string Source { get; set; }

        public string ContentType { get; set; }

        public string Topic { get; set; }

        public int Partition { get; set; }

        public long Offset { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }

        public long TimestampUnixMs { get; set; }

        public int TimestampType { get; set; }

        public int HeaderCount { get; set; }

        public int? LeaderEpoch { get; set; }
    }
}
