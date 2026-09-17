package com.microsoft.azure.functions.kafka.compat;

import com.microsoft.azure.functions.ExecutionContext;
import com.microsoft.azure.functions.HttpMethod;
import com.microsoft.azure.functions.HttpRequestMessage;
import com.microsoft.azure.functions.HttpResponseMessage;
import com.microsoft.azure.functions.HttpStatus;
import com.microsoft.azure.functions.OutputBinding;
import com.microsoft.azure.functions.annotation.AuthorizationLevel;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.annotation.HttpTrigger;
import com.microsoft.azure.functions.annotation.KafkaOutput;
import com.microsoft.azure.functions.annotation.KafkaTrigger;
import java.util.Optional;

public class CompatibilityFunctions {
    @FunctionName("ProduceCompatibilityMessage")
    public HttpResponseMessage produce(
            @HttpTrigger(
                    name = "request",
                    methods = {HttpMethod.POST},
                    authLevel = AuthorizationLevel.ANONYMOUS)
                    HttpRequestMessage<Optional<String>> request,
            @KafkaOutput(
                    name = "output",
                    topic = "compat-input",
                    brokerList = "kafka:29092")
                    OutputBinding<String> output,
            ExecutionContext context) {
        String message = request.getBody().orElseThrow(
                () -> new IllegalArgumentException("A correlation token is required."));
        output.setValue(message);
        context.getLogger().info("Produced compatibility token: " + message);
        return request.createResponseBuilder(HttpStatus.ACCEPTED).body(message).build();
    }

    @FunctionName("RoundTripCompatibilityMessage")
    public void roundTrip(
            @KafkaTrigger(
                    name = "input",
                    topic = "compat-input",
                    brokerList = "kafka:29092",
                    consumerGroup = "compat-java")
                    String event,
            @KafkaOutput(
                    name = "output",
                    topic = "compat-result",
                    brokerList = "kafka:29092")
                    OutputBinding<String> output,
            ExecutionContext context) {
        output.setValue(event);
        context.getLogger().info("Forwarded compatibility event.");
    }
}
