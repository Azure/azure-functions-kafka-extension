package com.contoso.kafka;

import java.util.*;
import com.microsoft.azure.functions.annotation.*;
import com.microsoft.azure.functions.*;

public class KafkaTriggerWithHeaders {
    @FunctionName("KafkaTriggerWithHeaders")
    public void runSingle(
            @KafkaTrigger(
                name = "KafkaTrigger",
                topic = "kafkaeventhubtest1", 
                brokerList="%BrokerList%",
                consumerGroup="$Default", 
                username= "$ConnectionString",
                password="KafkaPassword",
                authenticationMode = BrokerAuthenticationMode.PLAIN,
                protocol = BrokerProtocol.SASLSSL,
                // sslCaLocation = "confluent_cloud_cacert.pem", // Enable this line for windows.
                dataType = "string"
             ) String kafkaEventData,
            final ExecutionContext context) {
            context.getLogger().info(kafkaEventData);
    }
}
