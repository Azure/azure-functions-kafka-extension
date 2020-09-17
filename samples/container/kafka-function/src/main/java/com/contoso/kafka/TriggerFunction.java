package com.contoso.kafka;

import java.util.*;
import com.microsoft.azure.functions.annotation.*;
import com.microsoft.azure.functions.*;

/**
 * Azure Functions with HTTP Trigger.
 */
public class TriggerFunction {
    /**
     * This function consume KafkaEvents on the confluent cloud. Create a local.settings.json or configure AppSettings for configring
     * BrokerList and UserName, and Password. The value wrapped with `%` will be replaced with enviornment variables. 
     * For more details, refer https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-expressions-patterns#binding-expressions---app-settings
     * The function is a sample of consuming kafkaEvent on batch.
     * @param kafkaEventData
     * @param context
     */
    @FunctionName("KafkaTrigger-Java-Many")
    public void runMany(
            @KafkaTrigger(
                name = "kafkaTrigger",
                topic = "message", 
                brokerList="%BrokerList%",
                consumerGroup="$Default", 
                username = "%ConfluentCloudUsername%", 
                password = "%ConfluentCloudPassword%",
                authenticationMode = BrokerAuthenticationMode.PLAIN,
                protocol = BrokerProtocol.SASLSSL,
                cardinality = Cardinality.MANY,
                dataType = "string"
             ) String[] kafkaEventData,
            final ExecutionContext context) {
            for (String message: kafkaEventData) {
                context.getLogger().info(message);
            }    
    }
}
