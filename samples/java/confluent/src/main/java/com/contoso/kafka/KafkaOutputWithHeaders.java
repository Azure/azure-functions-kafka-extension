package com.contoso.kafka;

import com.microsoft.azure.functions.annotation.*;
import com.microsoft.azure.functions.*;

import java.util.Optional;

public class KafkaOutputWithHeaders {
    @FunctionName("KafkaOutput")
    public HttpResponseMessage run(
            @HttpTrigger(name = "req", methods = {HttpMethod.GET, HttpMethod.POST}, authLevel = AuthorizationLevel.ANONYMOUS) HttpRequestMessage<Optional<String>> request,
            @KafkaOutput(
                name = "kafkaOutput",
                topic = "kafkaeventhubtest1", 
                brokerList="%BrokerList%",
                username= "$ConnectionString",
                password="KafkaPassword",
                authenticationMode = BrokerAuthenticationMode.PLAIN,
                // sslCaLocation = "confluent_cloud_cacert.pem", // Enable this line for windows.  
                protocol = BrokerProtocol.SASLSSL
            )  OutputBinding<String> output,
            final ExecutionContext context) {
        context.getLogger().info("Java HTTP trigger processed a request.");

        // Parse query parameter
        String query = request.getQueryParameters().get("message");
        String message = request.getBody().orElse(query);
        context.getLogger().info("Message:" + message);
        String kevent = "{ \"Offset\":364,\"Partition\":0,\"Topic\":\"kafkaeventhubtest1\",\"Timestamp\":\"2022-04-09T03:20:06.591Z\", \"Value\": \""+ message + "\", \"Headers\": [{ \"Key\": \"test\", \"Value\": \"java\" }] }";
        output.setValue(kevent);
        return request.createResponseBuilder(HttpStatus.OK).body("Ok").build();
    }
}
