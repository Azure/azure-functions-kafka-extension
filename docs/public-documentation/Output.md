# Output Binding

**NOTE:**  The Kafka bindings are only fully supported on [Premium](https://docs.microsoft.com/en-us/azure/azure-functions/functions-premium-plan) and [Dedicated App Service](https://docs.microsoft.com/en-us/azure/azure-functions/dedicated-plan) plans. Consumption plans are not supported. Kafka bindings are only supported for Azure Functions version 3.x and later versions

Use the Kafka output binding to send messages to a Kafka topic.
 For information on setup and configuration details, see the overview.

# Examples

# C#

- [In-process class library](https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-class-library): compiled C# function that runs in the same process as the Functions runtime.
- [Isolated process class library](https://docs.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide): compiled C# function that runs in a process isolated from the runtime. Isolated process is required to support C# functions running on .NET 5.0.

\&lt;-- placeholder for the examples --\&gt;

| Setting | Description |
| --- | --- |
| Topic | Topic Name used for writing message on Kafka topic in Kafka Output Annotation |
| BrokerList | Server address for Kafka Broker |
|
 |
 |
|
 |
 |
|
 |
 |

| **Setting** | **librdkafka property** | **Description** |
| --- | --- | --- |
| AuthenticationMode | sasl.mechanism | SASL mechanism to use for authentication |
| --- | --- | --- |
| Username | sasl.username | SASL username for use with the PLAIN and SASL-SCRAM |
| Password | sasl.password | SASL password for use with the PLAIN and SASL-SCRAM |
| Protocol | security.protocol | Security protocol used to communicate with brokers |
| SslKeyLocation | ssl.key.location | Path to client&#39;s private key (PEM) used for authentication |
| SslKeyPassword | ssl.key.password | Password for client&#39;s certificate |
| SslCertificateLocation | ssl.certificate.location | Path to client&#39;s certificate |
| SslCaLocation | ssl.ca.location | Path to CA certificate file for verifying the broker&#39;s certificate |

Username and password should reference a Azure function configuration variable and not be hardcoded.

# Java

The following Java function uses the @KafkaOutput annotation from the Azure function Java Client library to describe the configuration for a Kafka topic output binding. The function sends a message to the Kafka topic

\&lt;-- placeholder for the examples --\&gt;

## Annotation

| **Setting** | **librdkafka property** | **Description** |
| --- | --- | --- |
| authenticationMode | sasl.mechanism | SASL mechanism to use for authentication |
| --- | --- | --- |
| username | sasl.username | SASL username for use with the PLAIN and SASL-SCRAM |
| password | sasl.password | SASL password for use with the PLAIN and SASL-SCRAM |
| protocol | security.protocol | Security protocol used to communicate with brokers |
| sslKeyLocation | ssl.key.location | Path to client&#39;s private key (PEM) used for authentication |
| sslKeyPassword | ssl.key.password | Password for client&#39;s certificate |
| sslCertificateLocation | ssl.certificate.location | Path to client&#39;s certificate |
| sslCaLocation | ssl.ca.location | Path to CA certificate file for verifying the broker&#39;s certificate |
| dataType |
-
 | Defines how Functions runtime should treat the parameter value. Possible values are :-
- Binary
- String
 |
| cardinality |
-
 | Set to many in order to enable batching. If omitted or set to one, a single message is passed to the function. For Java functions, if you set &quot;MANY&quot;, you need to set a dataType.
- ONE
- MANY

 |

# JavaScript/TypeScript/Python/Powershell

The following example shows a Kafka output binding in a _function.json_ file and a [JavaScript function](https://docs.microsoft.com/en-us/azure/azure-functions/functions-reference-node) that uses the binding. The function reads in the message from an HTTP trigger and outputs it to the Kafka topic.

Here&#39;s the binding data in the _function.json_ file:

\&lt;-- placeholder for the examples --\&gt;

For connection to a secure Kafka Broker -

| **function.json property** | **librdkafka property** | **Description** |
| --- | --- | --- |
| authenticationMode | sasl.mechanism | SASL mechanism to use for authentication |
| --- | --- | --- |
| username | sasl.username | SASL username for use with the PLAIN and SASL-SCRAM |
| password | sasl.password | SASL password for use with the PLAIN and SASL-SCRAM |
| protocol | security.protocol | Security protocol used to communicate with brokers |
| sslKeyLocation | ssl.key.location | Path to client&#39;s private key (PEM) used for authentication |
| sslKeyPassword | ssl.key.password | Password for client&#39;s certificate |
| sslCertificateLocation | ssl.certificate.location | Path to client&#39;s certificate |
| sslCaLocation | ssl.ca.location | Path to CA certificate file for verifying the broker&#39;s certificate |

# USAGE