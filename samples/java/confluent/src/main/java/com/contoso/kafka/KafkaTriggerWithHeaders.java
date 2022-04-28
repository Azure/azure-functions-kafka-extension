package com.contoso.kafka;

import java.util.*;
import com.microsoft.azure.functions.annotation.*;
import com.contoso.kafka.entity.KafkaEntity;
import com.contoso.kafka.entity.KafkaHeaders;
import com.microsoft.azure.functions.*;

public class KafkaTriggerWithHeaders {
    @FunctionName("KafkaTriggerWithHeaders")
    public void runSingle(
            @KafkaTrigger(
                name = "KafkaTrigger",
                topic = "topic",  
                brokerList="%BrokerList%",
                consumerGroup="$Default", 
                username = "%ConfluentCloudUsername%", 
                password = "ConfluentCloudPassword",
                authenticationMode = BrokerAuthenticationMode.PLAIN,
                protocol = BrokerProtocol.SASLSSL,
                // sslCaLocation = "confluent_cloud_cacert.pem", // Enable this line for windows.
                dataType = "string"
             ) KafkaEntity kafkaEventData,
            final ExecutionContext context) {
            context.getLogger().info("Java Kafka trigger function called for message: " + kafkaEventData.Value);
            context.getLogger().info("Headers for the message:");
            for (KafkaHeaders header : kafkaEventData.Headers) {
                String decodedValue = new String(Base64.getDecoder().decode(header.Value));
                context.getLogger().info("Key:" + header.Key + " Value:" + decodedValue);                    
            }
    }
}
