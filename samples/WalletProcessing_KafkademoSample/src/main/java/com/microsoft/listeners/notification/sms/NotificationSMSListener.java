package com.microsoft.listeners.notification.sms;

import com.microsoft.azure.functions.BrokerAuthenticationMode;
import com.microsoft.azure.functions.BrokerProtocol;
import com.microsoft.azure.functions.ExecutionContext;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.annotation.KafkaTrigger;
import com.microsoft.entity.KafkaEntity;

public class NotificationSMSListener {

    @FunctionName("NotificationSMSFn")
    public void runOne(
            @KafkaTrigger(name = "NotificationSMSTrigger",
                    topic = "notification_event_sms_topic",
                    brokerList = "BrokerList",
                    username = "KafkaUserName",
                    password = "%KafkaPassword%",
                    authenticationMode = BrokerAuthenticationMode.PLAIN,
                    protocol = BrokerProtocol.SASLSSL,
                    consumerGroup="$Default") KafkaEntity kafkaEventData,
            final ExecutionContext context) {
        context.getLogger().info("in sms notification trigger");
        context.getLogger().info(kafkaEventData.toString());
    }
}
