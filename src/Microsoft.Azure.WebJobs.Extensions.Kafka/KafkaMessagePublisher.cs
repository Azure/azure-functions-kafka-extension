using System.Threading;
using Confluent.Kafka;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Kafka message publisher.
    /// </summary>
    public interface IKafkaMessagePublisher<TKey, TValue>
    {
        void Publish(ConsumeResult<TKey, TValue> consumeResult, CancellationToken cancellationToken);
    }
}