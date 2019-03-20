using System;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Avro deserializer that adds the magic bytes 0 and int32 in order to Confluent resolve the schema.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class MagicAvroDeserializer<TValue> : IAsyncDeserializer<TValue>
    {
        private readonly IAsyncDeserializer<TValue> impl;

        public MagicAvroDeserializer(IAsyncDeserializer<TValue> impl)
        {
            this.impl = impl;
        }
        public Task<TValue> DeserializeAsync(ReadOnlyMemory<byte> data, bool isNull, bool isKey, MessageMetadata messageMetadata, TopicPartition source)
        {
            // TODO: find a better way to solve this
            // Allocating memory in a critical path of the trigger
            const int Prefix = sizeof(byte) + sizeof(Int32);
            var data2 = new Memory<byte>(new byte[data.Length + Prefix]);
            var data2Span = data2.Span;
            var dataSpan = data.Span;
            dataSpan.TryCopyTo(data2Span.Slice(Prefix));

            return this.impl.DeserializeAsync(data2, isNull, isKey, messageMetadata, source);
        }
    }
}
