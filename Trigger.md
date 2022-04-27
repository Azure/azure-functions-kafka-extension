# Public Documentation

**NOTE:** The Kafka bindings are only fully supported on [Premium](https://docs.microsoft.com/en-us/azure/azure-functions/functions-premium-plan) and [Dedicated App Service](https://docs.microsoft.com/en-us/azure/azure-functions/dedicated-plan) plans. Consumption plans are not supported.

**NOTE:** Kafka bindings are only supported for Azure Functions version 3.x and later versions.

# Examples

# C# Attributes

|Setting|Description|
|-|-|
|Topic|Topic Name used for Kafka Trigger|
|BrokerList|Server Address for kafka broker|
|ConsumerGroup|Name for the Consumer Group|
|AvroSchema|Should be used only if a generic record should be generated|
|LagThreshold|Threshold for lag(Default 1000)|

For connection to a secure Kafka Broker -

|Authentication Setting|librdkafka property|Description|
|-|-|-|
|AuthenticationMode|sasl.mechanism|SASL mechanism to use for authentication|
|Username|sasl.username|SASL username for use with the PLAIN and SASL-SCRAM|
|Password|sasl.password|SASL password for use with the PLAIN and SASL-SCRAM|
|Protocol|security.protocol|Security protocol used to communicate with brokers|
|SslKeyLocation|ssl.key.location|Path to client's private key (PEM) used for authentication|
|SslKeyPassword|ssl.key.password|Password for client's certificate|
|SslCertificateLocation|ssl.certificate.location|Path to client's certificate|
|SslCaLocation|ssl.ca.location|Path to CA certificate file for verifying the broker's certificate|

# Java Annotations
|Parameter|Description|
|-|-|
|name|The variable name used in function code for the request or request body.|
|topic|Defines the topic.|
|brokerList|Defines the broker list.|
|consumerGroup|Name for the Consumer Group.|
|cardinality|Cardinality of the trigger input. Choose 'One' if the input is a single message or 'Many' if the input is an array of messages. If you choose 'Many', please set a dataType. Default: 'One'|
|dataType| <p>Defines how Functions runtime should treat the parameter value. Possible values are:</p><ul><li>""(Default): Get the value as a string, and try to deserialize to actual parameter type like POJO.</li><li>string: Always get the value as a string</li><li>binary: Get the value as a binary data, and try to deserialize to actual parameter type byte[].</li></ul>|
|avroSchema|Avro schema for generic record deserialization|

For connection to a secure Kafka Broker -

|Authentication Setting|librdkafka property|Description|
|-|-|-|
|authenticationMode|sasl.mechanism|SASL mechanism to use for authentication|
|username|sasl.username|SASL username for use with the PLAIN and SASL-SCRAM|
|password|sasl.password|SASL password for use with the PLAIN and SASL-SCRAM|
|protocol|security.protocol|Security protocol used to communicate with brokers|
|sslKeyLocation|ssl.key.location|Path to client's private key (PEM) used for authentication|
|sslKeyPassword|ssl.key.password|Password for client's certificate|
|sslCertificateLocation|ssl.certificate.location|Path to client's certificate|
|sslCaLocation|ssl.ca.location|Path to CA certificate file for verifying the broker's certificate|

# JS/TS/PS/Python Configuration

The following tables explain the binding configuration properties that you set in the [function.json](https://docs.microsoft.com/en-us/azure/azure-functions/functions-reference?tabs=blob#function-code) file -

|function.json property|Description|
|-|-|
|type|Must be set to kafkaTrigger.|
|direction|Must be set to in.|
|name|Name of the variable that represents  request or request body in the function code.|
|brokerList|Defines the broker list.|
|cardinality|Cardinality of the trigger input. Choose 'One' if the input is a single message or 'Many' if the input is an array of messages. If you choose 'Many', please set a dataType. Default: 'One'|
|dataType|<p>Defines how Functions runtime should treat the parameter value. Possible values are:</p><ul><li>""(Default): Get the value as a string, and try to deserialize to actual parameter type like POJO.</li><li>string: Always get the value as a string</li><li>binary: Get the value as a binary data, and try to deserialize to actual parameter type byte[].</li></ul>|

For connection to a secure Kafka Broker -

|function.json property|librdkafka property|Description|
|-|-|-|
|authenticationMode|sasl.mechanism|SASL mechanism to use for authentication|
|username|sasl.username|SASL username for use with the PLAIN and SASL-SCRAM|
|password|sasl.password|SASL password for use with the PLAIN and SASL-SCRAM|
|protocol|security.protocol|Security protocol used to communicate with brokers|
|sslKeyLocation|ssl.key.location|Path to client's private key (PEM) used for authentication|
|sslKeyPassword|ssl.key.password|Password for client's certificate|
|sslCertificateLocation|ssl.certificate.location|Path to client's certificate|
|sslCaLocation|ssl.ca.location|Path to CA certificate file for verifying the broker's certificate|

 When you are developing locally, add your application settings in the [local.settings.json](https://docs.microsoft.com/en-us/azure/azure-functions/functions-develop-local#local-settings-file) file in the Values collection.

**NOTE:** Username and password should reference a Azure function configuration variable and not be hardcoded.


# host.json settings
|Setting|Description|Default Value
|-|-|-|
|MaxBatchSize|Maximum batch size when calling a Kafka trigger function|64
|SubscriberIntervalInSeconds|Defines the minimum frequency in which messages will be executed by function. Only if the message volume is less than MaxBatchSize / SubscriberIntervalInSeconds|1
|ExecutorChannelCapacity|Defines the channel capacity in which messages will be sent to functions. Once the capacity is reached the Kafka subscriber will pause until the function catches up|1
|ChannelFullRetryIntervalInMs|Defines the interval in milliseconds in which the subscriber should retry adding items to channel once it reaches the capacity|50

# Enable Runtime Scaling
In order for the Kafka trigger to scale out to multiple instances, the Runtime Scale Monitoring setting must be enabled.

In the portal, this setting can be found under Configuration > Function runtime settings for your function app.

<!---Insert Screenshot--->

In the CLI, you can enable Runtime Scale Monitoring by using the following command:

```az resource update -g <resource_group> -n <function_app_name>/config/web --set properties.functionsRuntimeScaleMonitoringEnabled=1 --resource-type Microsoft.Web/sites```