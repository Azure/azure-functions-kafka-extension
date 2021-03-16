package com.contoso.kafka;

import java.util.*;
import com.microsoft.azure.functions.annotation.*;
import com.microsoft.azure.functions.*;

/**
 * Java worker End to End testing
 */
public class KafkaTriggerTests {

    @FunctionName("HttpTriggerAndKafkaOutput")
    public HttpResponseMessage httpTriggerAndKafkaOutput(
            @HttpTrigger(name = "req", methods = {HttpMethod.GET, HttpMethod.POST}, authLevel = AuthorizationLevel.ANONYMOUS) HttpRequestMessage<Optional<String>> request,
            @KafkaOutput(name = "kafkaOutput", topic = "java8topic", brokerList="broker:29092")  OutputBinding<String> output,
            final ExecutionContext context) {
        context.getLogger().info("Java HTTP trigger processed a request.");

        // Parse query parameter
        String query = request.getQueryParameters().get("message");
        String message = request.getBody().orElse(query);
        context.getLogger().info("Message:" + message);
        output.setValue(message);
        return request.createResponseBuilder(HttpStatus.OK).body("Message Sent, " + message).build();
    }

    @FunctionName("KafkaTriggerAndKafkaOutput")
    public void kafkaTriggerAndKafkaOutput(
            @KafkaTrigger(name = "kafkaTrigger", topic = "java8topic", brokerList="broker:29092",consumerGroup="functions") String kafkaEventData,
            @KafkaOutput(name = "kafkaOutput", topic = "java8result", brokerList="broker:29092")  OutputBinding<String> output,
            final ExecutionContext context) {
        context.getLogger().info(kafkaEventData);
        output.setValue(kafkaEventData);
        context.getLogger().info("SendMessage to java8result topic");
    }
}
