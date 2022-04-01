// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class KafkaProducerAsyncCollector<T> : IAsyncCollector<T>
    {
        private readonly KafkaProducerEntity entity;
        private readonly Guid functionInstanceId;

        public KafkaProducerAsyncCollector(KafkaProducerEntity entity, Guid functionInstanceId)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            this.entity = entity;
            this.functionInstanceId = functionInstanceId;
        }

        public Task AddAsync(T item, CancellationToken cancellationToken)
        {            
            if (item == null)
            {
                throw new InvalidOperationException("Cannot produce a null message instance.");
            }

            object messageToSend = item;

            if (item.GetType() == typeof(string))
            {
                messageToSend = ConvertToKafkaEventData(item);
            }

            if (item.GetType() == typeof(byte[]))
            {
                messageToSend = new KafkaEventData<T>(item);
            }

            return entity.SendAndCreateEntityIfNotExistsAsync(messageToSend, functionInstanceId, cancellationToken);
        }

        private object ConvertToKafkaEventData(T item)
        {
            try
            {
                return BuildKafkaDataEvent(item);
            }
            catch (Exception)
            {
                return new KafkaEventData<T>(item);
            }
        }

        private object BuildKafkaDataEvent(T item)
        {
            JObject dataObj = JObject.Parse(item.ToString());
            if (dataObj == null)
            {
                return new KafkaEventData<T>(item);
            }

            if (dataObj.ContainsKey("Offset") && dataObj.ContainsKey("Partition") && dataObj.ContainsKey("Topic")
                && dataObj.ContainsKey("Timestamp") && dataObj.ContainsKey("Value") && dataObj.ContainsKey("Headers"))
            {
                return BuildKafkaEventData(dataObj);
            }
            
            return new KafkaEventData<T>(item);
        }

        private KafkaEventData<string> BuildKafkaEventData(JObject dataObj)
        {
            KafkaEventData<string> messageToSend = new KafkaEventData<string>((string)dataObj["Value"]);
            messageToSend.Timestamp = (DateTime)dataObj["Timestamp"];
            messageToSend.Partition = (int)dataObj["Partition"];
            JArray headerList = (JArray)dataObj["Headers"];
            foreach (JObject header in headerList) {
                messageToSend.Headers.Add((string)header["Key"], Encoding.Unicode.GetBytes((string)header["Value"]));
            }
            return messageToSend;
        }

        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Batching not supported. 
            return Task.FromResult(0);
        }
    }
}