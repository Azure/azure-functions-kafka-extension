package com.microsoft.listeners.notification.inapp;

import com.microsoft.azure.functions.BrokerAuthenticationMode;
import com.microsoft.azure.functions.BrokerProtocol;
import com.microsoft.azure.functions.ExecutionContext;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.annotation.KafkaTrigger;
import com.microsoft.entity.KafkaEntity;

public class NotificationInAppListener {

    @FunctionName("NotificationInAppFn")
    public void runOne(
            @KafkaTrigger(name = "NotificationInAppTrigger",
                    topic = "notification_event_inapp_topic",
                    brokerList = "BrokerList",
                    username = "KafkaUserName",
                    password = "%KafkaPassword%",
                    authenticationMode = BrokerAuthenticationMode.PLAIN,
                    protocol = BrokerProtocol.SASLSSL,
                    consumerGroup="$Default") KafkaEntity kafkaEventData,
            final ExecutionContext context) {
        context.getLogger().info("in in app notification trigger");
        context.getLogger().info(kafkaEventData.toString());
    }
}
