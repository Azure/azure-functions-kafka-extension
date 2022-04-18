// package com.contoso.kafka;

// import java.util.*;
// import com.microsoft.azure.functions.annotation.*;
// import com.microsoft.azure.functions.*;

// import java.util.Optional;

// public class KafkaOutputMany {
//     @FunctionName("KafkaOutputMany")
//     public HttpResponseMessage run(
//             @HttpTrigger(name = "req", methods = {HttpMethod.GET, HttpMethod.POST}, authLevel = AuthorizationLevel.ANONYMOUS) HttpRequestMessage<Optional<String>> request,
//             @KafkaOutput(
//                 name = "kafkaOutput",
//                 topic = "message", 
//                 brokerList="%BrokerList%",
//                 username = "%ConfluentCloudUsername%", 
//                 password = "%ConfluentCloudPassword%",
//                 authenticationMode = BrokerAuthenticationMode.PLAIN,
//                 // sslCaLocation = "confluent_cloud_cacert.pem", // Enable this line for windows.  
//                 protocol = BrokerProtocol.SASLSSL
//             )  OutputBinding<String[]> output,
//             final ExecutionContext context) {
//         context.getLogger().info("Java HTTP trigger processed a request.");
//         String[] messages = new String[2];
//         messages[0] = "one";
//         messages[1] = "two";
//         output.setValue(messages);
//         return request.createResponseBuilder(HttpStatus.OK).body("Ok").build();
//     }
// }
