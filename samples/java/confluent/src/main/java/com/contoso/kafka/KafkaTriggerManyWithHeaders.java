// package com.contoso.kafka;

// import java.util.*;
// import com.microsoft.azure.functions.annotation.*;
// import com.microsoft.azure.functions.*;

// public class KafkaTriggerManyWithHeaders {
//     @FunctionName("KafkaTriggerManyWithHeaders")
//     public void runSingle(
//             @KafkaTrigger(
//                 name = "KafkaTrigger",
//                 topic = "topic", 
//                 brokerList="%BrokerList%",
//                 consumerGroup="$Default", 
//                 username = "ConfluentCloudUsername", 
//                 password = "ConfluentCloudPassword",
//                 authenticationMode = BrokerAuthenticationMode.PLAIN,
//                 protocol = BrokerProtocol.SASLSSL,
//                 // sslCaLocation = "confluent_cloud_cacert.pem", // Enable this line for windows.
//                 dataType = "string",
//                 cardinality = Cardinality.MANY
//              ) String[] kafkaEvents,
//             final ExecutionContext context) {
//                 for (String kevent: kafkaEvents) {
//                     context.getLogger().info(kevent);
//                 }
//             }     
// }
