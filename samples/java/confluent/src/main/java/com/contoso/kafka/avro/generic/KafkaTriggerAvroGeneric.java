package com.contoso.kafka.avro.generic;

import com.contoso.kafka.entity.Payment;
import com.microsoft.azure.functions.BrokerAuthenticationMode;
import com.microsoft.azure.functions.BrokerProtocol;
import com.microsoft.azure.functions.ExecutionContext;
import com.microsoft.azure.functions.annotation.Cardinality;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.annotation.KafkaTrigger;

import java.util.List;

public class KafkaTriggerAvroGeneric {

    private static final String schema = "{\"type\":\"record\",\"name\":\"Payment\",\"namespace\":\"io.confluent.examples.clients.basicavro\",\"fields\":[{\"name\":\"id\",\"type\":\"string\"},{\"name\":\"amount\",\"type\":\"double\"},{\"name\":\"type\",\"type\":\"string\"}]}";

    @FunctionName("KafkaAvroGenericTrigger")
    public void runOne(
            @KafkaTrigger(
                    name = "kafkaAvroGenericSingle",
                    topic = "topic",
                    brokerList="%BrokerList%",
                    consumerGroup="$Default",
                    username = "ConfluentCloudUsername",
                    password = "ConfluentCloudPassword",
                    avroSchema = schema,
                    authenticationMode = BrokerAuthenticationMode.PLAIN,
                    protocol = BrokerProtocol.SASLSSL) Payment payment,
            final ExecutionContext context) {
        context.getLogger().info(payment.toString());
    }

    @FunctionName("KafkaAvroGenericTriggerMany")
    public void runMany(
            @KafkaTrigger(
                    name = "kafkaAvroGenericMany",
                    topic = "topic",
                    brokerList="%BrokerList%",
                    consumerGroup="$Default",
                    username = "ConfluentCloudUsername",
                    password = "ConfluentCloudPassword",
                    avroSchema = schema,
                    authenticationMode = BrokerAuthenticationMode.PLAIN,
                    cardinality = Cardinality.MANY,
                    dataType = "string",
                    protocol = BrokerProtocol.SASLSSL) List<String> paymentArr,
            final ExecutionContext context) {
        for (String paymentStr: paymentArr) {
            context.getLogger().info(paymentStr);
        }
    }
}
