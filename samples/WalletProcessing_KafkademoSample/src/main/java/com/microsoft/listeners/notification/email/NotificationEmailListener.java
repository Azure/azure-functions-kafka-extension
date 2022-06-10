package com.microsoft.listeners.notification.email;

import com.microsoft.azure.functions.BrokerAuthenticationMode;
import com.microsoft.azure.functions.BrokerProtocol;
import com.microsoft.azure.functions.ExecutionContext;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.annotation.KafkaTrigger;
import com.microsoft.entity.KafkaEntity;
import com.microsoft.entity.event.Notification;

public class NotificationEmailListener {

    @FunctionName("NotificationEmailFn")
    public void runOne(
            @KafkaTrigger(name = "NotificationEmailTrigger",
                    topic = "notification_event_email_topic",
                    brokerList = "BrokerList",
                    username = "KafkaUserName",
                    password = "%KafkaPassword%",
                    authenticationMode = BrokerAuthenticationMode.PLAIN,
                    protocol = BrokerProtocol.SASLSSL,
                    consumerGroup="$Default") KafkaEntity kafkaEventData,
            final ExecutionContext context) {
        context.getLogger().info("in email notification trigger");
        context.getLogger().info(kafkaEventData.toString());
    }
}
