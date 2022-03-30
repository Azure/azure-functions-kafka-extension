using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.brokers;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util
{
	public static class Constants
	{
		public const int BATCH_MESSAGE_COUNT = 3;
		public const int SINGLE_MESSAGE_COUNT = 1;

		public const string DOTNETISOLATED = "dotnet-isolated";

		public const string DOCKER_RUN = "docker run";
		public const string DOCKER_KILL = "docker rm -f";
		public const string DOCKER_PORT_FLAG = "-p";
		public const string COLON_7071 = ":7071";
		public const string DOCKER_ENVVAR_FLAG = "-e";
		public const string DOCKER_NAME_FLAG = "--name";

		public const string CONFLUENT_USERNAME_VAR = "ConfluentCloudUsername";
		public const string CONFLUENT_PASSWORD_VAR = "ConfluentCloudPassword";
		public const string CONFLUENT_BROKERLIST_VAR = "ConfluentBrokerList";
		public const string EVENTHUB_CONSTRING_VAR = "EventHubConnectionString";
		public const string EVENTHUB_BROKERLIST_VAR = "EventHubBrokerList";
		public const string AZURE_WEBJOBS_STORAGE = "AzureWebJobsStorage";
		public const string AZURE_CLIENT_ID = "AZURE_CLIENT_ID";
		public const string AZURE_CLIENT_SECRET = "AZURE_CLIENT_SECRET";
		public const string AZURE_TENANT_ID = "AZURE_TENANT_ID";
		public const string AZURE_SUBSCRIPTION_ID = "AZURE_SUBSCRIPTION_ID";

		public const string SPACE_CHAR = " ";
		public const string E2E = "e2e";
		public const string KAFKA = "kafka";
		public const string FUNC_START = "func start";
		public const string HIPHEN = "-";
		public const string RESOURCE_GROUP = "kafkagroup";
		public const string EVENTHUB_NAMESPACE = "kafkaextensiontest";
		public const string SINGLE = "single";
		public const string MULTI = "multi";

		public const string PYTHONAPP_CONFLUENT_PORT = "55701";
		public const string PYTHONAPP_EVENTHUB_PORT = "51701";
		public const string PYTHONAPP_CONFLUENT_IMAGE = "azure-functions-kafka-python-confluent";
		public const string PYTHONAPP_EVENTHUB_IMAGE = "azure-functions-kafka-python-eventhub";
		public const string PYTHON_SINGLE_APP_NAME = "SingleHttpTriggerKafkaOutput";
		public const string PYTHON_MULTI_APP_NAME = "MultiHttpTriggerKafkaOutput";
		public const string PYTHONAPP_WORKER_RUNTIME = "python";

		
		public const string DOTNETAPP_CONFLUENT_PORT = "";
		public const string DOTNETAPP_EVENTHUB_PORT = "";
		public const string DOTNETAPP_CONFLUENT_IMAGE = "";
		public const string DOTNETAPP_EVENTHUB_IMAGE = "";
		public const string DOTNET_SINGLE_APP_NAME = "";
		public const string DOTNET_MULTI_APP_NAME = "";
		public const string DOTNET_WORKER_RUNTIME = "dotnet";


		public const string DOTNETWORKERAPP_CONFLUENT_PORT = "50200";
		public const string DOTNETWORKERAPP_EVENTHUB_PORT = "59200";
		public const string DOTNETWORKERAPP_CONFLUENT_IMAGE = "azure-functions-kafka-dotnet-isolated-confluent";
		public const string DOTNETWORKERAPP_EVENTHUB_IMAGE = "azure-functions-kafka-dotnet-isolated-eventhub";
		public const string DOTNETWORKER_SINGLE_APP_NAME = "SingleHttpTriggerKafkaOutput";
		public const string DOTNETWORKER_MULTI_APP_NAME = "MultiHttpTriggerKafkaOutput";
		public const string DOTNETWORKER_WORKER_RUNTIME = "dotnet-isolated";

		public const string PWSHELL_CONFLUENT_PORT = "50501";
		public const string PWSHELL_EVENTHUB_PORT = "59501";
		public const string PWSHELL_CONFLUENT_IMAGE = "azure-functions-kafka-powershell-confluent";
		public const string PWSHELL_EVENTHUB_IMAGE = "azure-functions-kafka-powershell-eventhub";
		public const string PWSHELL_SINGLE_APP_NAME = "SingleHttpTriggerKafkaOutput";
		public const string PWSHELL_MULTI_APP_NAME = "MultiHttpTriggerKafkaOutput";
		public const string PWSHELL_WORKER_RUNTIME = "powershell";

		public const string JAVAAPP_CONFLUENT_PORT = "55601";
		public const string JAVAAPP_EVENTHUB_PORT = "51651";
		public const string JAVAAPP_CONFLUENT_IMAGE = "azure-functions-kafka-java-confluent";
		public const string JAVAAPP_EVENTHUB_IMAGE = "azure-functions-kafka-java-eventhub";
		public const string JAVA_SINGLE_APP_NAME = "SingleHttpTriggerKafkaOutput";
		public const string JAVA_MULTI_APP_NAME = "MultiHttpTriggerKafkaOutput";
		public const string JAVA_WORKER_RUNTIME = "java";

		public const string JSAPP_CONFLUENT_PORT = "50300";
		public const string JSAPP_EVENTHUB_PORT = "51300";
		public const string JSAPP_CONFLUENT_IMAGE = "azure-functions-kafka-javascript-confluent";
		public const string JSAPP_EVENTHUB_IMAGE = "azure-functions-kafka-javascript-eventhub";
		public const string JS_SINGLE_APP_NAME = "SingleHttpTriggerKafkaOutput";
		public const string JS_MULTI_APP_NAME = "MultiHttpTriggerKafkaOutput";
		public const string JS_WORKER_RUNTIME = "node";

		public const string TSAPP_CONFLUENT_PORT = "55402";
		public const string TSAPP_EVENTHUB_PORT = "51402";
		public const string TSAPP_CONFLUENT_IMAGE = "azure-functions-kafka-typescript-confluent";
		public const string TSAPP_EVENTHUB_IMAGE = "azure-functions-kafka-typescript-eventhub";
		public const string TS_SINGLE_APP_NAME = "SingleHttpTriggerKafkaOutput";
		public const string TS_MULTI_APP_NAME = "MultiHttpTriggerKafkaOutput";
		public const string TS_WORKER_RUNTIME = "node";

		public static Dictionary<Tuple<BrokerType, Language>, string> BrokerLanguagePortMapping = new Dictionary<Tuple<BrokerType, Language>, string>()
		{
			{ new Tuple<BrokerType, Language>(BrokerType.CONFLUENT, Language.PYTHON), PYTHONAPP_CONFLUENT_PORT },
			{ new Tuple<BrokerType, Language>(BrokerType.CONFLUENT, Language.DOTNET), DOTNETAPP_CONFLUENT_PORT },
			{ new Tuple<BrokerType, Language>(BrokerType.CONFLUENT, Language.DOTNETISOLATED), DOTNETWORKERAPP_CONFLUENT_PORT},
			{ new Tuple<BrokerType, Language>(BrokerType.CONFLUENT, Language.POWERSHELL), PWSHELL_CONFLUENT_PORT},
			{ new Tuple<BrokerType, Language>(BrokerType.CONFLUENT, Language.JAVA), JAVAAPP_CONFLUENT_PORT},
			{ new Tuple<BrokerType, Language>(BrokerType.CONFLUENT, Language.JAVASCRIPT), JSAPP_CONFLUENT_PORT},
			{ new Tuple<BrokerType, Language>(BrokerType.CONFLUENT, Language.TYPESCRIPT), TSAPP_CONFLUENT_PORT},
			{ new Tuple<BrokerType, Language>(BrokerType.EVENTHUB, Language.PYTHON), PYTHONAPP_EVENTHUB_PORT },
			{ new Tuple<BrokerType, Language>(BrokerType.EVENTHUB, Language.DOTNET), DOTNETAPP_EVENTHUB_PORT },
			{ new Tuple<BrokerType, Language>(BrokerType.EVENTHUB, Language.DOTNETISOLATED), DOTNETWORKERAPP_EVENTHUB_PORT},
			{ new Tuple<BrokerType, Language>(BrokerType.EVENTHUB, Language.POWERSHELL), PWSHELL_EVENTHUB_PORT},
			{ new Tuple<BrokerType, Language>(BrokerType.EVENTHUB, Language.JAVA), JAVAAPP_EVENTHUB_PORT},
			{ new Tuple<BrokerType, Language>(BrokerType.EVENTHUB, Language.JAVASCRIPT), JSAPP_EVENTHUB_PORT},
			{ new Tuple<BrokerType, Language>(BrokerType.EVENTHUB, Language.TYPESCRIPT), TSAPP_EVENTHUB_PORT}
		};

		/*public static Dictionary<Language, string> LanguageConfluentPortMapping = new Dictionary<Language, string>()
		{
			{ Language.PYTHON, PYTHONAPP_CONFLUENT_PORT },
			{ Language.DOTNET, DOTNETAPP_CONFLUENT_PORT },
			{ Language.DOTNET_WORKER, DOTNETWORKERRAPP_CONFLUENT_PORT},
			{ Language.POWERSHELL, PWSHELL_CONFLUENT_PORT},
			{ Language.JAVA, JAVAAPP_CONFLUENT_PORT},
			{ Language.JAVASCRIPT, JSAPP_CONFLUENT_PORT},
			{ Language.TYPESCRIPT, TSAPP_CONFLUENT_PORT}
		};
		public static Dictionary<Language, string> LanguageEventhubPortMapping = new Dictionary<Language, string>()
		{
			{ Language.PYTHON, PYTHONAPP_EVENTHUB_PORT },
			{ Language.DOTNET, DOTNETAPP_EVENTHUB_PORT },
			{ Language.DOTNET_WORKER, DOTNETWORKERRAPP_EVENTHUB_PORT},
			{ Language.POWERSHELL, PWSHELL_EVENTHUB_PORT},
			{ Language.JAVA, JAVAAPP_EVENTHUB_PORT},
			{ Language.JAVASCRIPT, JSAPP_EVENTHUB_PORT},
			{ Language.TYPESCRIPT, TSAPP_EVENTHUB_PORT}
		};
		public static Dictionary<Language, string> LanguageConfluentImageMapping = new Dictionary<Language, string>()
		{
			{ Language.PYTHON, PYTHONAPP_CONFLUENT_IMAGE },
			{ Language.DOTNET, DOTNETAPP_CONFLUENT_IMAGE },
			{ Language.DOTNET_WORKER, DOTNETWORKERAPP_CONFLUENT_IMAGE },
			{ Language.POWERSHELL, PWSHELL_CONFLUENT_IMAGE },
			{ Language.JAVA, JAVAAPP_CONFLUENT_IMAGE },
			{ Language.JAVASCRIPT, JSAPP_CONFLUENT_IMAGE },
			{ Language.TYPESCRIPT, TSAPP_CONFLUENT_IMAGE }
		};
		public static Dictionary<Language, string> LanguageEventhubImageMapping = new Dictionary<Language, string>()
		{
			{ Language.PYTHON, PYTHONAPP_EVENTHUB_IMAGE },
			{ Language.DOTNET, DOTNETAPP_EVENTHUB_IMAGE },
			{ Language.DOTNET_WORKER, DOTNETWORKERAPP_EVENTHUB_IMAGE },
			{ Language.POWERSHELL, PWSHELL_EVENTHUB_IMAGE },
			{ Language.JAVA, JAVAAPP_EVENTHUB_IMAGE },
			{ Language.JAVASCRIPT, JSAPP_EVENTHUB_IMAGE },
			{ Language.TYPESCRIPT, TSAPP_EVENTHUB_IMAGE }
		};*/

		public static Dictionary<Tuple<BrokerType, Language>, string> BrokerLanguageImageMapping = new Dictionary<Tuple<BrokerType, Language>, string>()
		{
			{ new Tuple<BrokerType, Language>(BrokerType.CONFLUENT, Language.PYTHON), PYTHONAPP_CONFLUENT_IMAGE },
			{ new Tuple<BrokerType, Language>(BrokerType.CONFLUENT, Language.DOTNET), DOTNETAPP_CONFLUENT_IMAGE },
			{ new Tuple<BrokerType, Language>(BrokerType.CONFLUENT, Language.DOTNETISOLATED), DOTNETWORKERAPP_CONFLUENT_IMAGE },
			{ new Tuple<BrokerType, Language>(BrokerType.CONFLUENT, Language.POWERSHELL), PWSHELL_CONFLUENT_IMAGE },
			{ new Tuple<BrokerType, Language>(BrokerType.CONFLUENT, Language.JAVA), JAVAAPP_CONFLUENT_IMAGE },
			{ new Tuple<BrokerType, Language>(BrokerType.CONFLUENT, Language.JAVASCRIPT), JSAPP_CONFLUENT_IMAGE},
			{ new Tuple<BrokerType, Language>(BrokerType.CONFLUENT, Language.TYPESCRIPT), TSAPP_CONFLUENT_IMAGE },
			{ new Tuple<BrokerType, Language>(BrokerType.EVENTHUB, Language.PYTHON), PYTHONAPP_EVENTHUB_IMAGE },
			{ new Tuple<BrokerType, Language>(BrokerType.EVENTHUB, Language.DOTNET), DOTNETAPP_EVENTHUB_IMAGE },
			{ new Tuple<BrokerType, Language>(BrokerType.EVENTHUB, Language.DOTNETISOLATED), DOTNETWORKERAPP_EVENTHUB_IMAGE },
			{ new Tuple<BrokerType, Language>(BrokerType.EVENTHUB, Language.POWERSHELL), PWSHELL_EVENTHUB_IMAGE },
			{ new Tuple<BrokerType, Language>(BrokerType.EVENTHUB, Language.JAVA), JAVAAPP_EVENTHUB_IMAGE },
			{ new Tuple<BrokerType, Language>(BrokerType.EVENTHUB, Language.JAVASCRIPT), JSAPP_EVENTHUB_IMAGE },
			{ new Tuple<BrokerType, Language>(BrokerType.EVENTHUB, Language.TYPESCRIPT), TSAPP_EVENTHUB_IMAGE }
		};

		public static Dictionary<Language, string> LanguageRuntimeMapping = new Dictionary<Language, string>()
		{
			{ Language.PYTHON, PYTHONAPP_WORKER_RUNTIME },
			{ Language.DOTNET, DOTNET_WORKER_RUNTIME },
			{ Language.DOTNETISOLATED, DOTNETWORKER_WORKER_RUNTIME },
			{ Language.POWERSHELL, PWSHELL_WORKER_RUNTIME },
			{ Language.JAVA, JAVA_WORKER_RUNTIME },
			{ Language.JAVASCRIPT, JS_WORKER_RUNTIME },
			{ Language.TYPESCRIPT, TS_WORKER_RUNTIME }
		};
	}
}
