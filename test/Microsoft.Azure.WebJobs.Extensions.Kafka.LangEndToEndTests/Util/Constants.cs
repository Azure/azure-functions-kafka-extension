using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util
{
	public static class Constants
	{
		public const int BATCH_MESSAGE_COUNT = 5;
		public const string DOCKER_RUN = "docker run";
		public const string DOCKER_PORT_FLAG = "-p";
		public const string COLON_7071 = ":7071";
		public const string DOCKER_ENVVAR_FLAG = "-e";
		public const string CONFLUENT_USERNAME_VAR = "ConfluentCloudUserName";
		public const string CONFLUENT_PASSWORD_VAR = "ConfluentCloudPassword";
		public const string CONFLUENT_BROKERLIST_VAR = "ConfluentBrokerList";
		public const string EVENTHUB_CONSTRING_VAR = "EventhubConnectionString";
		public const string EVENTHUB_BROKERLIST_VAR = "EventhubBrokerList";
		public const string SPACE_CHAR = " ";
		public const string E2E = "e2e";
		public const string KAFKA = "";
		public const string PYTHON_SINGLE_APP_NAME = "";
		public const string FUNC_START = "func start";

		public const string HIPHEN = "-";
		public const string PYTHONAPP_CONFLUENT_PORT = "";
		public const string PYTHONAPP_CONFLUENT_IMAGE = "";
		public const int SINGLE_MESSAGE_COUNT = 1;
		public static Dictionary<Language, string> LanguagePortMapping = new Dictionary<Language, string>()
		{
			{Language.PYTHON, PYTHONAPP_CONFLUENT_PORT}
		};
		public static Dictionary<Language, string> LanguageImageMapping = new Dictionary<Language, string>()
		{
			{ Language.PYTHON, PYTHONAPP_CONFLUENT_IMAGE}
		};
	}
}
