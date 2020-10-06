package com.contoso.kafka;

import java.util.*;
import com.microsoft.azure.functions.annotation.*;
import com.microsoft.azure.functions.*;

/**
 * Azure Functions with HTTP Trigger.
 */
public class TriggerFunction {
    /**
     * This function consume KafkaEvents on the localhost. Change the topic, brokerList, and consumerGroup to fit your enviornment.
     * The function is trigged one for each KafkaEvent
     * @param kafkaEventData
     * @param context
     */
    @FunctionName("KafkaTrigger-Java")
    public void runOne(
            @KafkaTrigger(name = "kafkaTrigger", topic = "users", brokerList="broker:29092",consumerGroup="functions") String kafkaEventData,
            final ExecutionContext context) {
        context.getLogger().info(kafkaEventData);
    }
}
