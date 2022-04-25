package com.contoso.kafka;

import java.util.*;
import com.microsoft.azure.functions.annotation.*;
import com.microsoft.azure.functions.*;

/**
 * Azure Functions with HTTP Trigger.
 */
public class SampleKafkaTrigger {
    /**
     * This function consume KafkaEvents on the confluent cloud. Create a local.settings.json or configure AppSettings for configring
     * BrokerList and UserName, and Password. The value wrapped with `%` will be replaced with enviornment variables. 
     * For more details, refer https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-expressions-patterns#binding-expressions---app-settings
     * The function is a sample of consuming kafkaEvent.
     * @param kafkaEventData
     * @param context
     */
    @FunctionName("KafkaTrigger")
    public void runSingle(
            @KafkaTrigger(
                name = "KafkaTrigger",
                topic = "topic",  
                brokerList="%BrokerList%",
                consumerGroup="$Default", 
                username = "%ConfluentCloudUsername%", 
                password = "EventHubConnectionString",
                authenticationMode = BrokerAuthenticationMode.PLAIN,
                protocol = BrokerProtocol.SASLSSL,
                // sslCaLocation = "confluent_cloud_cacert.pem", // Enable this line for windows.
                dataType = "string"
             ) String kafkaEventData,
            final ExecutionContext context) {
            context.getLogger().info(kafkaEventData);
    }
}
