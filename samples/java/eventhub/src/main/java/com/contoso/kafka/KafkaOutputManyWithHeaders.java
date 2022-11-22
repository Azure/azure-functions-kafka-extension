package com.contoso.kafka;

import com.microsoft.azure.functions.annotation.*;
import com.microsoft.azure.functions.*;

import java.util.Optional;

public class KafkaOutputManyWithHeaders {
    @FunctionName("KafkaOutputManyWithHeaders")
    public HttpResponseMessage run(
            @HttpTrigger(name = "req", methods = {HttpMethod.GET, HttpMethod.POST}, authLevel = AuthorizationLevel.ANONYMOUS) HttpRequestMessage<Optional<String>> request,
            @KafkaOutput(
                name = "kafkaOutput",
                topic = "topic",  
                brokerList="%BrokerList%",
                username = "$ConnectionString", 
                password = "EventHubConnectionString",
                authenticationMode = BrokerAuthenticationMode.PLAIN,
                // sslCaLocation = "confluent_cloud_cacert.pem", // Enable this line for windows.  
                protocol = BrokerProtocol.SASLSSL
            )  OutputBinding<KafkaEntity[]> output,
            final ExecutionContext context) {
        context.getLogger().info("Java HTTP trigger processed a request.");
        KafkaEntity[] kevents = new KafkaEntity[2];
        KafkaHeaders[] headersForEvent1 = new KafkaHeaders[1];
        headersForEvent1[0] = new KafkaHeaders("test", "java");
        KafkaEntity kevent1 = new KafkaEntity(364, 0, "topic", "2022-04-09T03:20:06.591Z", "one", headersForEvent1);

        KafkaHeaders[] headersForEvent2 = new KafkaHeaders[1];
        headersForEvent2[0] = new KafkaHeaders("test1", "java");
        KafkaEntity kevent2 = new KafkaEntity(364, 0, "topic", "2022-04-09T03:20:06.591Z", "two", headersForEvent2);
        
        kevents[0] = kevent1;
        kevents[1] = kevent2;
        output.setValue(kevents);
        return request.createResponseBuilder(HttpStatus.OK).body("Ok").build();
    }
}
