# Output Binding

**NOTE:**  The Kafka bindings are only fully supported on [Premium](https://docs.microsoft.com/en-us/azure/azure-functions/functions-premium-plan) and [Dedicated App Service](https://docs.microsoft.com/en-us/azure/azure-functions/dedicated-plan) plans. Consumption plans are not supported. Kafka bindings are only supported for Azure Functions version 3.x and later versions

Use the Kafka output binding to send messages to a Kafka topic.
 For information on setup and configuration details, see the overview page.

# Examples

# C#

The C# function can be created using one of the following C# modes:

- [In-process class library](https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-class-library): compiled C# function that runs in the same process as the Functions runtime.
- [Isolated process class library](https://docs.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide): compiled C# function that runs in a process isolated from the runtime. Isolated process is required to support C# functions running on .NET 5.0.



### InProcess

The following example shows a C# function that sends the message to the Kafka topic, using data provided in HTTP GET request.

```csharp
[FunctionName("KafkaOutput")]
public static IActionResult Output(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
    [Kafka("BrokerList",
            "topic",
            Username = "ConfluentCloudUserName",
            Password = "ConfluentCloudPassword",
            Protocol = BrokerProtocol.SaslSsl,
            AuthenticationMode = BrokerAuthenticationMode.Plain
    )] out string eventData,
    ILogger log)
{
    log.LogInformation("C# HTTP trigger function processed a request.");

    string message = req.Query["message"];

    string responseMessage = "Ok";            
    eventData = message;

    return new OkObjectResult(responseMessage);
}
```

### IsolatedProcess

The following example shows a C# function that writes a message string to Kafka, using the method return value as the output. The function gets triggered on a HTTP GET request. This example has a custom return type which is MultipleOutputType that consists of HTTP response and Kafka output. In class MultipleOutputType, Kevent is the output binding variable for Kafka. 

```csharp
using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace Confluent
{
    public class KafkaOutput
    {
        [Function("KafkaOutput")]
        
        public static MultipleOutputType Output(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req,
            FunctionContext executionContext)
        {
            var log = executionContext.GetLogger("HttpFunction");
            log.LogInformation("C# HTTP trigger function processed a request.");

            string message = req.FunctionContext
                                .BindingContext
                                .BindingData["message"]
                                .ToString();

            var response = req.CreateResponse(HttpStatusCode.OK);
            return new MultipleOutputType()
            {
                Kevent = message,
                HttpResponse = response
            };
        }
    }

    public class MultipleOutputType
    {
        [KafkaOutput("BrokerList",
                    "topic",
                    Username = "ConfluentCloudUserName",
                    Password = "ConfluentCloudPassword",
            Protocol = BrokerProtocol.SaslSsl,
            AuthenticationMode = BrokerAuthenticationMode.Plain
        )]        
        public string Kevent { get; set; }

        public HttpResponseData HttpResponse { get; set; }
    }
}
```

|Setting|Description|
|-|-|
|Topic|Topic Name used for Kafka Trigger|
|BrokerList|Server Address for kafka broker|
|AvroSchema|Should be used only if a generic record should be generated|
|MaxMessageBytes|Maximum transmit message size. Default: 1MB|
|BatchSize|Maximum number of messages batched in one MessageSet. default: 10000|
|EnableIdempotence|When set to `true`, the producer will ensure that messages are successfully produced exactly once and in the original produce order. default: false|
|MessageTimeoutMs|Local message timeout. This value is only enforced locally and limits the time a produced message waits for successful delivery. A time of 0 is infinite. This is the maximum time used to deliver a message (including retries). Delivery error occurs when either the retry count or the message timeout are exceeded. default: 300000|
|RequestTimeoutMs|The acknowledgement timeout of the producer request in milliseconds. default: 5000|
|MaxRetries|How many times to retry sending a failing Message. **Note:** default: 2. <remarks>Retrying may cause reordering unless <c>EnableIdempotence</c> is set to <c>true</c>.</remarks>|


|Setting|librdkafka property|Description|
|-|-|-|
| AuthenticationMode | sasl.mechanism | SASL mechanism to use for authentication |
| Username | sasl.username | SASL username for use with the PLAIN and SASL-SCRAM |
| Password | sasl.password | SASL password for use with the PLAIN and SASL-SCRAM |
| Protocol | security.protocol | Security protocol used to communicate with brokers |
| SslKeyLocation | ssl.key.location | Path to client&#39;s private key (PEM) used for authentication |
| SslKeyPassword | ssl.key.password | Password for client&#39;s certificate |
| SslCertificateLocation | ssl.certificate.location | Path to client&#39;s certificate |
| SslCaLocation | ssl.ca.location | Path to CA certificate file for verifying the broker&#39;s certificate |

Username and password should reference a Azure function configuration variable and not be hardcoded.

# Java

The following Java function uses the @KafkaOutput annotation from the Azure function Java Client library to describe the configuration for a Kafka topic output binding. The function sends a message to the Kafka topic.

```java
@FunctionName("KafkaOutput")
public HttpResponseMessage run(
        @HttpTrigger(name = "req", methods = {HttpMethod.GET}, authLevel = AuthorizationLevel.ANONYMOUS) HttpRequestMessage<Optional<String>> request,
        @KafkaOutput(
            name = "kafkaOutput",
            topic = "topic",  
            brokerList="%BrokerList%",
            username = "%ConfluentCloudUsername%", 
            password = "ConfluentCloudPassword",
            authenticationMode = BrokerAuthenticationMode.PLAIN
            protocol = BrokerProtocol.SASLSSL
        )  OutputBinding<String> output,
        final ExecutionContext context) {
    context.getLogger().info("Java HTTP trigger processed a request.");

    // Parse query parameter
    String message = request.getQueryParameters().get("message");
    context.getLogger().info("Message:" + message);
    output.setValue(message);
    return request.createResponseBuilder(HttpStatus.OK).body("Ok").build();
}
```

## Annotation

|Parameter|Description|
|-|-|
|name|The variable name used in function code for the request or request body.|
|dataType| <p>Defines how Functions runtime should treat the parameter value. Possible values are:</p><ul><li>"" or string: treat it as a string whose value is serialized from the parameter</li><li>binary: treat it as a binary data whose value comes from for example OutputBinding&lt;byte[]&lt;</li></ul>|
|topic|Defines the topic.|
|brokerList|Defines the broker list.|
|maxMessageBytes|Defines the maximum transmit message size. Default: 1MB|
|batchSize|Defines the maximum number of messages batched in one MessageSet. default: 10000|
|enableIdempotence|When set to `true`, the producer will ensure that messages are successfully produced exactly once and in the original produce order. default: false|
|messageTimeoutMs|Local message timeout. This value is only enforced locally and limits the time a produced message waits for successful delivery. A time of 0 is infinite. This is the maximum time used to deliver a message (including retries). Delivery error occurs when either the retry count or the message timeout are exceeded. default: 300000|
|requestTimeoutMs|The acknowledge timeout of the producer request in milliseconds. default: 5000|
|maxRetries|How many times to retry sending a failing Message. **Note:** default: 2. Retrying may cause reordering unless EnableIdempotence is set to true.|

For connection to a secure Kafka Broker -

|Setting|librdkafka property|Description|
|-|-|-|
| authenticationMode | sasl.mechanism | SASL mechanism to use for authentication |
| username | sasl.username | SASL username for use with the PLAIN and SASL-SCRAM |
| password | sasl.password | SASL password for use with the PLAIN and SASL-SCRAM |
| protocol | security.protocol | Security protocol used to communicate with brokers |
| sslKeyLocation | ssl.key.location | Path to client&#39;s private key (PEM) used for authentication |
| sslKeyPassword | ssl.key.password | Password for client&#39;s certificate |
| sslCertificateLocation | ssl.certificate.location | Path to client&#39;s certificate |
| sslCaLocation | ssl.ca.location | Path to CA certificate file for verifying the broker&#39;s certificate |
| dataType |

# JavaScript/TypeScript/Python/Powershell


### JavaScript

The following example shows a Kafka output binding in a _function.json_ file and a [JavaScript function](https://docs.microsoft.com/en-us/azure/azure-functions/functions-reference-node) that uses the binding. The function reads in the message from an HTTP trigger and outputs it to the Kafka topic.

Here&#39;s the binding data in the _function.json_ file:

```json
{
  "bindings": [
    {
      "authLevel": "function",
      "type": "httpTrigger",
      "direction": "in",
      "name": "req",
      "methods": [
        "get"
      ]
    },
    {
      "type": "kafka",
      "name": "outputKafkaMessage",
      "brokerList": "BrokerList",
      "topic": "topic",
      "username": "ConfluentCloudUsername",
      "password": "ConfluentCloudPassword",
      "protocol": "SASLSSL",
      "authenticationMode": "PLAIN",
      "direction": "out"
    },
    {
      "type": "http",
      "direction": "out",
      "name": "res"
    }
  ]
}
```

Here is JavaScript code:

```js
module.exports = async function (context, req) {
    context.log('JavaScript HTTP trigger function processed a request.');

    const message = (req.query.message);
    context.bindings.outputKafkaMessage = message;
    context.res = {
        body: 'Ok'
    };
}
```

### Powershell
The following example shows a Kafka output binding in a function.json file and a Powershell function that uses the binding. The function reads in the message from an HTTP trigger and outputs it to the Kafka topic.

Here is the binding data in the _function.json_ file:
```json
{
  "bindings": [
    {
      "authLevel": "function",
      "type": "httpTrigger",
      "direction": "in",
      "name": "Request",
      "methods": [
        "get"
      ]
    },
    {
      "type": "kafka",
      "name": "outputMessage",
      "brokerList": "BrokerList",
      "topic": "topic",
      "username" : "%ConfluentCloudUserName%",
      "password" : "%ConfluentCloudPassword%",
      "protocol": "SASLSSL",
      "authenticationMode": "PLAIN",
      "direction": "out"
    },
    {
      "type": "http",
      "direction": "out",
      "name": "Response"
    }
  ]
}
```
In your function, use the Push-OutputBinding to send an event through the Kafka output binding.


```ps1
using namespace System.Net

# Input bindings are passed in via param block.
param($Request, $TriggerMetadata)

# Write to the Azure Functions log stream.
Write-Host "PowerShell HTTP trigger function processed a request."

# Interact with query parameters or the body of the request.
$message = $Request.Query.Message

$message

Push-OutputBinding -Name outputMessage -Value ($message)

# Associate values to output bindings by calling 'Push-OutputBinding'.
Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
    StatusCode = [HttpStatusCode]::OK
})
```

### Python

The following example shows a Kafka output binding in a function.json file and a Python function that uses the binding. The function reads in the message from an HTTP trigger and outputs it to the Kafka topic.

Here is the binding data in the _function.json_ file:

```json
{
  "scriptFile": "main.py",
  "bindings": [
    {
      "authLevel": "function",
      "type": "httpTrigger",
      "direction": "in",
      "name": "req",
      "methods": [
        "get"
      ]
    },
    {
      "type": "kafka",
      "direction": "out",
      "name": "outputMessage",
      "brokerList": "BrokerList",
      "topic": "topic",
      "username": "%ConfluentCloudUserName%",
      "password": "%ConfluentCloudPassword%",
      "protocol": "SASLSSL",
      "authenticationMode": "PLAIN"
    },
    {
      "type": "http",
      "direction": "out",
      "name": "$return"
    }
  ]
}
```
Here is the Python script code:

In _init_.py:
```py
import logging

import azure.functions as func


def main(req: func.HttpRequest, outputMessage: func.Out[str]) -> func.HttpResponse:
    input_msg = req.params.get('message')
    outputMessage.set(input_msg)
    return 'OK'
```

### Typescript

The following example shows a Kafka output binding in a function.json file and a Typescript function that uses the binding. The function reads in the message from an HTTP trigger and outputs it to the Kafka topic.

Here is the binding data in the _function.json_ file:

```json
{
  "bindings": [
    {
      "authLevel": "function",
      "type": "httpTrigger",
      "direction": "in",
      "name": "req",
      "methods": [
        "get"      
       ]
    },
    {
      "type": "kafka",
      "name": "outputKafkaMessage",
      "topic": "topic",
      "brokerList": "%BrokerList%",
      "username": "%ConfluentCloudUserName%",
      "password": "%ConfluentCloudPassword%",
      "protocol": "SASLSSL",
      "authenticationMode": "PLAIN",
      "direction": "out"
    },
    {
      "type": "http",
      "direction": "out",
      "name": "res"
    }
  ],
  "scriptFile": "../dist/KafkaOutput/index.js"
}
```

Here's the typescript script code:

```ts
import { AzureFunction, Context, HttpRequest } from "@azure/functions"

const kafkaOutput: AzureFunction = async function (context: Context, req: HttpRequest): Promise<void> {
    const message = req.query.message;
    const responseMessage = 'Ok'
    context.bindings.outputKafkaMessage = message;
    context.res = {
        body: responseMessage
    };

};

export default kafkaOutput;
```


| **function.json property** | **Description** |
|-|-|
|type|Must be set to kafkaOutput.|
|direction|Must be set to out.|
|name|Name of the variable that represents  request or request body in the function code.|
|brokerList|Defines the broker list.|

For connection to a secure Kafka Broker -

| **function.json property** | **librdkafka property** | **Description** |
|-|-|-|
| authenticationMode | sasl.mechanism | SASL mechanism to use for authentication |
| username | sasl.username | SASL username for use with the PLAIN and SASL-SCRAM |
| password | sasl.password | SASL password for use with the PLAIN and SASL-SCRAM |
| protocol | security.protocol | Security protocol used to communicate with brokers |
| sslKeyLocation | ssl.key.location | Path to client&#39;s private key (PEM) used for authentication |
| sslKeyPassword | ssl.key.password | Password for client&#39;s certificate |
| sslCertificateLocation | ssl.certificate.location | Path to client&#39;s certificate |
| sslCaLocation | ssl.ca.location | Path to CA certificate file for verifying the broker&#39;s certificate |
