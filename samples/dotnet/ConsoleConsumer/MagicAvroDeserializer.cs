using System;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace ConsoleConsumer
{
    /// <summary>
    /// Avro deserializer that adds the magic bytes 0 and int32 in order to Confluent resolve the schema.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class MagicAvroDeserializer<TValue> : IDeserializer<TValue>
    {
        private readonly IDeserializer<TValue> impl;

        public MagicAvroDeserializer(IDeserializer<TValue> impl)
        {
            this.impl = impl;
        }

        public TValue Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            // TODO: find a better way to solve this
            // Allocating memory in a critical path of the trigger
            const int Prefix = sizeof(byte) + sizeof(Int32);
            var data2 = new Memory<byte>(new byte[data.Length + Prefix]);
            var data2Span = data2.Span;
            data.TryCopyTo(data2Span.Slice(Prefix));

            return this.impl.Deserialize(data2Span, isNull, context);
        }
    }
}
