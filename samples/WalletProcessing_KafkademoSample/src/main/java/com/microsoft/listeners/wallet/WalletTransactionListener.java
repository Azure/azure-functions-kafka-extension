package com.microsoft.listeners.wallet;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.microsoft.azure.functions.BrokerAuthenticationMode;
import com.microsoft.azure.functions.BrokerProtocol;
import com.microsoft.azure.functions.ExecutionContext;
import com.microsoft.azure.functions.OutputBinding;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.annotation.KafkaOutput;
import com.microsoft.azure.functions.annotation.KafkaTrigger;
import com.microsoft.converter.ResponseConverter;
import com.microsoft.converter.WalletNotificationResponseConverter;
import com.microsoft.entity.KafkaEntity;
import com.microsoft.entity.event.WalletTransaction;

import java.io.IOException;

public class WalletTransactionListener {

    private static final String walletSchema = "{\"type\":\"record\",\"name\":\"WalletTransaction\",\"namespace\":\"io.confluent.examples.clients.entity\",\"fields\":[{\"name\":\"id\",\"type\":\"string\"},{\"name\":\"amount\",\"type\":\"double\"},{\"name\":\"type\",\"type\":\"string\"},{\"name\":\"payoutId\",\"type\":\"string\"},{\"name\":\"payoutStatus\",\"type\":\"string\"},{\"name\":\"currency\",\"type\":\"string\"},{\"name\":\"fee\",\"type\":\"double\"},{\"name\":\"sourceId\",\"type\":\"string\"},{\"name\":\"sourceType\",\"type\":\"string\"},{\"name\":\"processedAt\",\"type\":\"string\"},{\"name\":\"customerId\",\"type\":\"string\"},{\"name\":\"walletId\",\"type\":\"string\"}]}";

    private static final String onCompleteWalletTxnEvent = "onCompleteWalletTxn";
    private static final String notificationTxnEvent = "notificationEvent";

    private ResponseConverter<WalletTransaction, KafkaEntity> walletTransactionNotificationConverter = new WalletNotificationResponseConverter();

    @FunctionName("walletTxnTrigger")
    public void onCompleteWalletTransaction(
            @KafkaTrigger(name = onCompleteWalletTxnEvent,
                    topic = "walletTopic",
                    brokerList = "BrokerList",
                    username = "KafkaUserName",
                    password = "%KafkaPassword%",
                    avroSchema = walletSchema,
                    authenticationMode = BrokerAuthenticationMode.PLAIN,
                    protocol = BrokerProtocol.SASLSSL,
                    consumerGroup="$Default") WalletTransaction walletTxn,
            @KafkaOutput(name = notificationTxnEvent,
                    topic = "notification_event_topic",
                    brokerList="BrokerList",
                    username = "KafkaUserName",
                    password = "%KafkaPassword%",
                    authenticationMode = BrokerAuthenticationMode.PLAIN,
                    protocol = BrokerProtocol.SASLSSL) OutputBinding<KafkaEntity> output,
            final ExecutionContext context) throws IOException {
        context.getLogger().info(walletTxn.toString());
        context.getLogger().info("passing next step for notification");
        output.setValue(walletTransactionNotificationConverter.convert(walletTxn));
    }

}
