package com.function;

import com.microsoft.azure.functions.annotation.*;
import com.microsoft.azure.functions.*;


import java.util.Optional;

public class SingleHttpTriggerKafkaOutput {
    @FunctionName("SingleHttpTriggerKafkaOutput")
    public HttpResponseMessage input(
            @HttpTrigger(name = "req", methods = {HttpMethod.GET, HttpMethod.POST}, authLevel = AuthorizationLevel.ANONYMOUS) HttpRequestMessage<Optional<String>> request,
            @KafkaOutput(name = "kafkaOutput",
                         topic = "e2e-kafka-java-single-confluent", 
                         brokerList="ConfluentBrokerList",
                         username = "ConfluentCloudUsername",
                         password = "ConfluentCloudPassword",
                         authenticationMode = BrokerAuthenticationMode.PLAIN,
                         protocol = BrokerProtocol.SASLSSL)  OutputBinding<String> output,
            final ExecutionContext context) {
        context.getLogger().info("Java HTTP trigger processed a request.");

        // Parse query parameter
        String query = request.getQueryParameters().get("message");
        String message = request.getBody().orElse(query);
        context.getLogger().info("Message:" + message);
        output.setValue(message);
        return request.createResponseBuilder(HttpStatus.OK).body("Message Sent, " + message).build();
    }
}