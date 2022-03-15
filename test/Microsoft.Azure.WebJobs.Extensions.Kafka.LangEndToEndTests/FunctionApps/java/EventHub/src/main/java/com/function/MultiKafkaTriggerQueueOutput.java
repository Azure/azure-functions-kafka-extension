package com.function;

import com.microsoft.azure.functions.annotation.*;

import java.util.*;

import com.microsoft.azure.functions.*;
import org.json.*;

public class MultiKafkaTriggerQueueOutput {
    /**
     * This function consume KafkaEvents on the localhost. Change the topic, EventHubBrokerList, and consumerGroup to fit your enviornment.
     * The function is trigged one for each KafkaEvent
     * @param kafkaEventData
     * @param context
     */
    @FunctionName("MultiKafkaTriggerQueueOutput")
    public void runOne(
            @KafkaTrigger(name = "kafkaTrigger", 
                          topic = "e2e-kafka-java-multi-eventhub", 
                          brokerList="EventHubBrokerList",
                          username = "$ConnectionString",
                          password = "%EventHubConnectionString%",
                          authenticationMode = BrokerAuthenticationMode.PLAIN,
                          protocol = BrokerProtocol.SASLSSL,
                          consumerGroup="$Default",
                          dataType = "string", 
                          cardinality = Cardinality.MANY) List<String> kafkaEvents,
            @QueueOutput(name = "message", 
                         queueName = "e2e-java-multi-eventhub", 
                         connection = "AzureWebJobsStorage") OutputBinding<List<String>> messages,
            final ExecutionContext context) {
        List<String> queueMessages = new ArrayList<String>();
        for (String kafkaEventData : kafkaEvents) {
            context.getLogger().info(kafkaEventData);
            JSONObject kafkaEventJsonObj = new JSONObject(kafkaEventData);
            String message = kafkaEventJsonObj.getString("Value");   
            queueMessages.add(message);         
        }
        messages.setValue(queueMessages);
    }
}
