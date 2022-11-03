package com.function;

import com.microsoft.azure.functions.annotation.*;
import com.microsoft.azure.functions.*;


import java.util.Optional;
import java.util.*;

public class MultiHttpTriggerKafkaOutput {
    @FunctionName("MultiHttpTriggerKafkaOutput")
    public HttpResponseMessage input(
            @HttpTrigger(name = "req", methods = {HttpMethod.GET}, authLevel = AuthorizationLevel.ANONYMOUS) HttpRequestMessage<Optional<String>> request,
            @KafkaOutput(name = "kafkaOutput",
                         topic = "e2e-kafka-java-multi-eventhub", 
                         brokerList="EventHubBrokerList",
                         username = "$ConnectionString",
                         password = "%EventHubConnectionString%",
                         authenticationMode = BrokerAuthenticationMode.PLAIN,
                         protocol = BrokerProtocol.SASLSSL)  OutputBinding<List<String>> output,
            final ExecutionContext context) {
        context.getLogger().info("Java HTTP trigger processed a request.");

        // Parse query parameter
        String message = request.getQueryParameters().get("message");
        String message1 = request.getQueryParameters().get("message1");
        String message2 = request.getQueryParameters().get("message2");

        context.getLogger().info("Message:" + message + " Message1: " + message1 + " Message2: " + message2);

        List<String> allMessages = new ArrayList<String>();
        allMessages.add(message);
        allMessages.add(message1);
        allMessages.add(message2);
        output.setValue(allMessages);
        return request.createResponseBuilder(HttpStatus.OK).body("Message Sent, " + message +" " + message1 + " " + message2).build();
    }
}