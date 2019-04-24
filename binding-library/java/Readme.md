# Kafka extension Library for Azure Java Functions

This repo contains Kafka extension library for building Azure Java Functions. Visit the [complete documentation of Azure Functions - Java Devloper Guide](https://docs.microsoft.com/en-us/azure/azure-functions/functions-reference-java) for more details.

# Prerequisites
* Java 8
* [Azure Function Core Tools](https://github.com/Azure/azure-functions-core-tools) (V2)
* Maven 3.0 or above
* [Azure Function Maven Plugin](https://github.com/Microsoft/azure-maven-plugins/) (1.3.0-SNAPSHOT or above)
* [librdkafka](https://github.com/edenhill/librdkafka#installing-prebuilt-packages)

# Sample

Here is an example of the KafkaTrigger Azure Function using Kafka extension in Java. 

```java
package com.contoso.kafka;

import java.util.*;
import com.microsoft.azure.functions.annotation.*;
import com.microsoft.azure.functions.*;

import com.microsoft.azure.functions.kafka.annotation.*;
import com.microsoft.azure.functions.kafka.*;

/**
 * Azure Functions with HTTP Trigger.
 */
public class Function {
    @FunctionName("KafkaTrigger-Java")
    public void run(
            @KafkaTrigger(topic = "pageviews", brokerList="broker",consumerGroup="$Default") String kafkaEventData,
            final ExecutionContext context) {
        context.getLogger().info(kafkaEventData);
    }
}
```

NOTE: This is the frist version. I'll add more information the following PR. 

# Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
