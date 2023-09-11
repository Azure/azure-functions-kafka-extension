using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Azure.WebJobs.Extensions.Storage;
using Microsoft.Extensions.Logging;
using System;

namespace Confluent
{
	public class KafkaTriggerSchemaRegistry
	{
		[FunctionName("KafkaTriggerSchemaRegistry")]
		public static void Run(
			[KafkaTrigger("BrokerList",
						  "topic",
						  Username = "ConfluentCloudUserName",
						  Password = "ConfluentCloudPassword",
						  Protocol = BrokerProtocol.SaslSsl,
						  AuthenticationMode = BrokerAuthenticationMode.Plain,
						  ConsumerGroup = "$Default",
						  SchemaRegistryUrl = "ConfluentSchemaRegistryUrl",
						  SchemaRegistryUsername = "ConfleuntSchemaRegistryUsername",
						  SchemaRegistryPassword = "ConfluentSchemaRegistryPassword")] KafkaEventData<string> kevent, ILogger log)
		{
			log.LogInformation($"C# Kafka trigger function processed a message: {kevent.Value}");
		}
	}
}