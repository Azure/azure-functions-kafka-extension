// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

[assembly: InternalsVisibleTo("Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]
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

        private object BuildKafkaEventData(JObject dataObj)
        {
            if (dataObj["Key"] != null)
            {
                return BuildKafkaEventDataForKeyValue(dataObj);
            }
            else
            {
                return BuildKafkaEventDataForValue(dataObj);
            }
        }

        private static object BuildKafkaEventDataForValue(JObject dataObj)
        {
            KafkaEventData<string> messageToSend = new KafkaEventData<string>((string)dataObj["Value"]);
            messageToSend.Timestamp = (DateTime)dataObj["Timestamp"];
            messageToSend.Partition = (int)dataObj["Partition"];
            JArray headerList = (JArray)dataObj["Headers"];
            foreach (JObject header in headerList)
            {
                messageToSend.Headers.Add((string)header["Key"], Encoding.UTF8.GetBytes((string)header["Value"]));
            }
            return messageToSend;
        }

        private static object BuildKafkaEventDataForKeyValue(JObject dataObj)
        {
            KafkaEventData<string, string> messageToSend = new KafkaEventData<string, string>((string)dataObj["Key"], (string)dataObj["Value"]);
            messageToSend.Timestamp = (DateTime)dataObj["Timestamp"];
            messageToSend.Partition = (int)dataObj["Partition"];
            JArray headerList = (JArray)dataObj["Headers"];
            foreach (JObject header in headerList)
            {
                messageToSend.Headers.Add((string)header["Key"], Encoding.UTF8.GetBytes((string)header["Value"]));
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