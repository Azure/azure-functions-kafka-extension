package com.contoso.kafka;

import java.util.*;
import com.microsoft.azure.functions.annotation.*;

import org.json.JSONObject;

import com.google.gson.Gson;
import com.microsoft.azure.functions.*;

public class KafkaTriggerManyWithHeaders {
    @FunctionName("KafkaTriggerManyWithHeaders")
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
                dataType = "string",
                cardinality = Cardinality.MANY
             ) List<String> kafkaEvents,
            final ExecutionContext context) {
                Gson gson = new Gson(); 
                for (String keventstr: kafkaEvents) {
                    KafkaEntity kevent = gson.fromJson(keventstr, KafkaEntity.class);
                    context.getLogger().info("Java Kafka trigger function called for message: " + kevent.Value);
                    context.getLogger().info("Headers for the message:");
                    for (KafkaHeaders header : kevent.Headers) {
                        String decodedValue = new String(Base64.getDecoder().decode(header.Value));
                        context.getLogger().info("Key:" + header.Key + " Value:" + decodedValue);                    
                    }                
                }
            }     
}
