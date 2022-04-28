package com.contoso.kafka;

import com.microsoft.azure.functions.annotation.*;
import com.contoso.kafka.entity.KafkaEntity;
import com.contoso.kafka.entity.KafkaHeaders;
import com.microsoft.azure.functions.*;

import java.util.Optional;

public class KafkaOutputWithHeaders {
    @FunctionName("KafkaOutputWithHeaders")
    public HttpResponseMessage run(
            @HttpTrigger(name = "req", methods = {HttpMethod.GET, HttpMethod.POST}, authLevel = AuthorizationLevel.ANONYMOUS) HttpRequestMessage<Optional<String>> request,
            @KafkaOutput(
                name = "kafkaOutput",
                topic = "topic",  
                brokerList="%BrokerList%",
                username = "%ConfluentCloudUsername%", 
                password = "ConfluentCloudPassword",
                authenticationMode = BrokerAuthenticationMode.PLAIN,
                // sslCaLocation = "confluent_cloud_cacert.pem", // Enable this line for windows.  
                protocol = BrokerProtocol.SASLSSL
            )  OutputBinding<KafkaEntity> output,
            final ExecutionContext context) {
                context.getLogger().info("Java HTTP trigger processed a request.");
        
                // Parse query parameter
                String query = request.getQueryParameters().get("message");
                String message = request.getBody().orElse(query);
                KafkaHeaders[] headers = new KafkaHeaders[1];
                headers[0] = new KafkaHeaders("test", "java");
                KafkaEntity kevent = new KafkaEntity(364, 0, "topic", "2022-04-09T03:20:06.591Z", message, headers);
                output.setValue(kevent);
                return request.createResponseBuilder(HttpStatus.OK).body("Ok").build();
            }
}
