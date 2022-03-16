using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util
{
	public static class Constants
	{
		public const int BATCH_MESSAGE_COUNT = 5;
		public const int SINGLE_MESSAGE_COUNT = 1;

		public const string DOCKER_RUN = "docker run";
		public const string DOCKER_PORT_FLAG = "-p";
		public const string COLON_7071 = ":7071";
		public const string DOCKER_ENVVAR_FLAG = "-e";

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
		public const string KAFKA = "";
		public const string FUNC_START = "func start";
		public const string HIPHEN = "-";
		
		public const string PYTHONAPP_CONFLUENT_PORT = "7072";
		public const string PYTHONAPP_EVENTHUB_PORT = "7074";
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

		public const string DOTNETWORKERRAPP_CONFLUENT_PORT = "";
		public const string DOTNETWORKERRAPP_EVENTHUB_PORT = "";
		public const string DOTNETWORKERAPP_CONFLUENT_IMAGE = "";
		public const string DOTNETWORKERAPP_EVENTHUB_IMAGE = "";
		public const string DOTNETWORKER_SINGLE_APP_NAME = "";
		public const string DOTNETWORKER_MULTI_APP_NAME = "";
		public const string DOTNETWORKER_WORKER_RUNTIME = "dotnet-isolated";

		public const string PWSHELL_CONFLUENT_PORT = "";
		public const string PWSHELL_EVENTHUB_PORT = "";
		public const string PWSHELL_CONFLUENT_IMAGE = "";
		public const string PWSHELL_EVENTHUB_IMAGE = "";
		public const string PWSHELL_SINGLE_APP_NAME = "";
		public const string PWSHELL_MULTI_APP_NAME = "";
		public const string PWSHELL_WORKER_RUNTIME = "powershell";

		public const string JAVAAPP_CONFLUENT_PORT = "";
		public const string JAVAAPP_EVENTHUB_PORT = "";
		public const string JAVAAPP_CONFLUENT_IMAGE = "";
		public const string JAVAAPP_EVENTHUB_IMAGE = "";
		public const string JAVA_SINGLE_APP_NAME = "";
		public const string JAVA_MULTI_APP_NAME = "";
		public const string JAVA_WORKER_RUNTIME = "java";

		public const string JSAPP_CONFLUENT_PORT = "";
		public const string JSAPP_EVENTHUB_PORT = "";
		public const string JSAPP_CONFLUENT_IMAGE = "";
		public const string JSAPP_EVENTHUB_IMAGE = "";
		public const string JS_SINGLE_APP_NAME = "";
		public const string JS_MULTI_APP_NAME = "";
		public const string JS_WORKER_RUNTIME = "node";

		public const string TSAPP_CONFLUENT_PORT = "";
		public const string TSAPP_EVENTHUB_PORT = "";
		public const string TSAPP_CONFLUENT_IMAGE = "";
		public const string TSAPP_EVENTHUB_IMAGE = "";
		public const string TS_SINGLE_APP_NAME = "";
		public const string TS_MULTI_APP_NAME = "";
		public const string TS_WORKER_RUNTIME = "node";

		public static Dictionary<Language, string> LanguagePortMapping = new Dictionary<Language, string>()
		{
			{ Language.PYTHON, PYTHONAPP_CONFLUENT_PORT },
			{ Language.DOTNET, DOTNETAPP_CONFLUENT_PORT },
			{ Language.DOTNET_WORKER, DOTNETWORKERRAPP_CONFLUENT_PORT},
			{ Language.POWERSHELL, PWSHELL_CONFLUENT_PORT},
			{ Language.JAVA, JAVAAPP_CONFLUENT_PORT},
			{ Language.JAVASCRIPT, JSAPP_CONFLUENT_PORT},
			{ Language.TYPESCRIPT, TSAPP_CONFLUENT_PORT}
		};
		public static Dictionary<Language, string> LanguageImageMapping = new Dictionary<Language, string>()
		{
			{ Language.PYTHON, PYTHONAPP_CONFLUENT_IMAGE },
			{ Language.DOTNET, DOTNETAPP_CONFLUENT_IMAGE },
			{ Language.DOTNET_WORKER, DOTNETWORKERAPP_CONFLUENT_IMAGE },
			{ Language.POWERSHELL, PWSHELL_CONFLUENT_IMAGE },
			{ Language.JAVA, JAVAAPP_CONFLUENT_IMAGE },
			{ Language.JAVASCRIPT, JSAPP_CONFLUENT_IMAGE },
			{ Language.TYPESCRIPT, TSAPP_CONFLUENT_IMAGE }
		};
		public static Dictionary<Language, string> LanguageRuntimeMapping = new Dictionary<Language, string>()
		{
			{ Language.PYTHON, PYTHONAPP_WORKER_RUNTIME },
			{ Language.DOTNET, DOTNET_WORKER_RUNTIME },
			{ Language.DOTNET_WORKER, DOTNETWORKER_WORKER_RUNTIME },
			{ Language.POWERSHELL, PWSHELL_WORKER_RUNTIME },
			{ Language.JAVA, JAVA_WORKER_RUNTIME },
			{ Language.JAVASCRIPT, JS_WORKER_RUNTIME },
			{ Language.TYPESCRIPT, TS_WORKER_RUNTIME }
		};
	}
}
