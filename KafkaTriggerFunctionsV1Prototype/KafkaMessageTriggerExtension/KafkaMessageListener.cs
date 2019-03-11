using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Confluent.Kafka;

namespace KafkaMessageTriggerExtension
{
    public class KafkaMesssageListener : IListener
    {      
        private KafkaMessageTriggerAttribute attribute;
        private System.Timers.Timer _timer;

        private Consumer<Ignore, string> consumer;

        public KafkaMesssageListener(ITriggeredFunctionExecutor executor, KafkaMessageTriggerAttribute attribute)
        {
            Executor = executor;
            this.attribute = attribute;
        }

        public ITriggeredFunctionExecutor Executor { get; }

        public void Cancel() {}

        public void Dispose() {}

        public Task StartAsync(CancellationToken cancellationToken)
        {            
            var config = new ConsumerConfig
            {
                BootstrapServers = AmbientConnectionStringProvider.Instance.GetConnectionString(attribute.BrokerServers),
                GroupId = AmbientConnectionStringProvider.Instance.GetConnectionString(attribute.ConsumerGroup),
                EnableAutoCommit = false,
                StatisticsIntervalMs = 5000,
                SessionTimeoutMs = 6000,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnablePartitionEof = true
            };
            consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe(AmbientConnectionStringProvider.Instance.GetConnectionString(attribute.TopicName));
            _timer = new System.Timers.Timer(1000);
            // Hook up the Elapsed event for the timer. 
            _timer.Elapsed += OnTimedEvent;
            _timer.AutoReset = true;
            _timer.Enabled = true;

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {           
            consumer.Close();
            consumer.Dispose();
            return Task.CompletedTask;
        }    

        private void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            //Console.WriteLine("The Elapsed event was raised at {0}", e.SignalTime);
            var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(500));
            if (consumeResult != null  && consumeResult.IsPartitionEOF == false)
            {
                var triggerData = new TriggeredFunctionData
                {
                    TriggerValue = consumeResult
                };
                Executor.TryExecuteAsync(triggerData, CancellationToken.None);
                consumer.Commit(consumeResult);
            }            
        }        
    }
}