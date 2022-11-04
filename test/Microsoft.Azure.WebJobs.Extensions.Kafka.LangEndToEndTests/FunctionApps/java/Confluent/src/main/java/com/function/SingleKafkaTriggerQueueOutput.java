package com.function;

import com.microsoft.azure.functions.annotation.*;
import com.microsoft.azure.functions.*;
import org.json.*;

public class SingleKafkaTriggerQueueOutput {
    /**
     * This function consume KafkaEvents on the localhost. Change the topic, ConfluentBrokerList, and consumerGroup to fit your enviornment.
     * The function is trigged one for each KafkaEvent
     * @param kafkaEventData
     * @param context
     */
    @FunctionName("SingleKafkaTriggerQueueOutput")
    public void runOne(
            @KafkaTrigger(name = "kafkaTrigger", 
                          topic = "e2e-kafka-java-single-confluent", 
                          brokerList = "ConfluentBrokerList",
                          username = "ConfluentCloudUsername",
                          password = "ConfluentCloudPassword",
                          authenticationMode = BrokerAuthenticationMode.PLAIN,
                          protocol = BrokerProtocol.SASLSSL,
                          consumerGroup="$Default") String kafkaEventData,
            @QueueOutput(name = "message", 
                         queueName = "e2e-java-single-confluent", 
                         connection = "AzureWebJobsStorage") OutputBinding<String> message,
            final ExecutionContext context) {
        context.getLogger().info(kafkaEventData);
        JSONObject kafkaEventJsonObj = new JSONObject(kafkaEventData);
        context.getLogger().info(kafkaEventJsonObj.getString("Value"));
        message.setValue(kafkaEventJsonObj.getString("Value"));
    }
}
