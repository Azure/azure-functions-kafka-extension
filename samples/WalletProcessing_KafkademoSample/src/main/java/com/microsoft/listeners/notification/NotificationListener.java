package com.microsoft.listeners.notification;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.microsoft.azure.functions.BrokerAuthenticationMode;
import com.microsoft.azure.functions.BrokerProtocol;
import com.microsoft.azure.functions.ExecutionContext;
import com.microsoft.azure.functions.OutputBinding;
import com.microsoft.azure.functions.annotation.FunctionName;
import com.microsoft.azure.functions.annotation.KafkaOutput;
import com.microsoft.azure.functions.annotation.KafkaTrigger;
import com.microsoft.commons.Constant;
import com.microsoft.entity.Header;
import com.microsoft.entity.KafkaEntity;
import com.microsoft.entity.event.WalletTransaction;
import com.microsoft.entity.type.NotificationType;

import java.util.Base64;

public class NotificationListener {

    @FunctionName("triggerNotification")
    public void triggerNotification(
            @KafkaTrigger(name = "triggerNotification",
                    topic = "notification_event_topic",
                    brokerList = "BrokerList",
                    username = "KafkaUserName",
                    password = "%KafkaPassword%",
                    authenticationMode = BrokerAuthenticationMode.PLAIN,
                    protocol = BrokerProtocol.SASLSSL,
                    consumerGroup="$Default") KafkaEntity kafkaEntity,
            @KafkaOutput(name = "notificationByEmail",
                    topic = "notification_event_email_topic",
                    brokerList="BrokerList",
                    username = "KafkaUserName",
                    password = "%KafkaPassword%",
                    authenticationMode = BrokerAuthenticationMode.PLAIN,
                    protocol = BrokerProtocol.SASLSSL) OutputBinding<KafkaEntity> outputEmail,
            @KafkaOutput(name = "notificationBySms",
                    topic = "notification_event_sms_topic",
                    brokerList="BrokerList",
                    username = "KafkaUserName",
                    password = "%KafkaPassword%",
                    authenticationMode = BrokerAuthenticationMode.PLAIN,
                    protocol = BrokerProtocol.SASLSSL) OutputBinding<KafkaEntity> outputSms,
            @KafkaOutput(name = "notificationByInApp",
                    topic = "notification_event_inapp_topic",
                    brokerList="BrokerList",
                    username = "KafkaUserName",
                    password = "%KafkaPassword%",
                    authenticationMode = BrokerAuthenticationMode.PLAIN,
                    protocol = BrokerProtocol.SASLSSL) OutputBinding<KafkaEntity> outputInApp,
            final ExecutionContext context) throws JsonProcessingException {
        context.getLogger().info(kafkaEntity.toString());
        Header[] headerArr = kafkaEntity.getHeaders();
        for (Header header: headerArr) {
            context.getLogger().info("header key :: "+header.getKey());
            if(header.getKey() == null || header.getKey().isEmpty() ||
                    !Constant.NOTIFICATION_HEADER_KEY.equals(header.getKey()))
                continue;
            setOutputData(kafkaEntity, outputEmail, outputSms, outputInApp, header, context);
        }
    }

    private void setOutputData(KafkaEntity kafkaEntity, OutputBinding<KafkaEntity> outputEmail, OutputBinding<KafkaEntity> outputSms,
                               OutputBinding<KafkaEntity> outputInApp, Header header, final ExecutionContext context) {
        String notificationVal = header.getValue();
        if(notificationVal == null || notificationVal.isEmpty())
            return;
        notificationVal = new String(Base64.getDecoder().decode(header.getValue()));
        if(NotificationType.EMAIL.toString().equals(notificationVal)) {
            context.getLogger().info("notification value :: "+notificationVal);
            outputEmail.setValue(kafkaEntity);
        } else if(NotificationType.SMS.toString().equals(notificationVal)) {
            context.getLogger().info("notification value :: "+notificationVal);
            outputSms.setValue(kafkaEntity);
        } else {
            context.getLogger().info("else notification value :: "+notificationVal);
            outputInApp.setValue(kafkaEntity);
        }
    }
}
