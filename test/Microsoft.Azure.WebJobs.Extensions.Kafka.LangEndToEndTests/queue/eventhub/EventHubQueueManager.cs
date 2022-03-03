using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.eventhub
{
    public class EventHubQueueManager : IQueueManager<string, string>
    {
        private readonly static int MAX_RETRY_COUNT = 3;
        private readonly string servicePrinciple;
        private readonly string connectionString;
        private static EventHubQueueManager instance = new EventHubQueueManager();

        public static EventHubQueueManager GetInstance()
        {
            return instance;
        }

        private EventHubQueueManager()
        {
            // TODO
            // 1. retrieve service principle from environment variables
            // 2. retrieve the namespace name & connection string from env vars
            // add the required params in constructor
        }

        public void clear(string queueName)
        {
            throw new NotImplementedException();
        }

        public void create(string queueName)
        {
            int count = 0;
            while (count < MAX_RETRY_COUNT)
            {
                try
                {
                    // TODO
                    // 1. check if already exists
                    //  1.1 clear the eventhub or delete that
                    // 2. create the new eventhub
                    // 2.1 if creation failed retry three times
                    // return if success
                } catch(Exception ex) {
                    if (count >= MAX_RETRY_COUNT)
                        throw ex;
                } finally {
                    count++;
                }
            }
            
        }

        public void delete(string queueName)
        {
            int count = 0;
            while(count < MAX_RETRY_COUNT)
            {
                try
                {
                    // TODO
                    // 1. check if exists
                    // 1.1. if doesn't exists throw the error
                    // 2. delete the eventhub
                }
                catch (Exception ex) {
                    if (count >= MAX_RETRY_COUNT)
                        throw ex;
                } finally
                {
                    count++;
                }
            }
            
        }

        public string read(int batchSize)
        {
            throw new NotImplementedException();
        }

        public string write(string messageEntity)
        {
            throw new NotImplementedException();
        }
    }
}
