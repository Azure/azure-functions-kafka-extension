package com.contoso.kafka;

import java.util.*;
import com.microsoft.azure.functions.annotation.*;
import com.microsoft.azure.functions.*;

public class KafkaTriggerMany {
    @FunctionName("KafkaTriggerMany")
    public void runMany(
            @KafkaTrigger(
                name = "kafkaTriggerMany",
                topic = "message", 
                brokerList="%BrokerList%",
                consumerGroup="$Default", 
                username = "%ConfluentCloudUsername%", 
                password = "%ConfluentCloudPassword%",
                authenticationMode = BrokerAuthenticationMode.PLAIN,
                protocol = BrokerProtocol.SASLSSL,
                // sslCaLocation = "confluent_cloud_cacert.pem", // Enable this line for windows.
                cardinality = Cardinality.MANY,
                dataType = "string"
             ) String[] kafkaEvents,
            final ExecutionContext context) {
            for (String kevent: kafkaEvents) {
                context.getLogger().info(kevent);
            }    
    }
}
