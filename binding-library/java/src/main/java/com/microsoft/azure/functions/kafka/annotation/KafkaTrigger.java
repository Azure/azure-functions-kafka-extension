/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */
package com.microsoft.azure.functions.kafka.annotation;

import com.microsoft.azure.functions.annotation.CustomBinding;
import com.microsoft.azure.functions.kafka.BrokerAuthenticationMode;
import com.microsoft.azure.functions.kafka.BrokerProtocol;

import java.lang.annotation.Retention;
import java.lang.annotation.Target;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.ElementType;


/**
 * <p>Annotation for KafkaOutput bindings</p>
 */
@Target(ElementType.PARAMETER)
@Retention(RetentionPolicy.RUNTIME)
@CustomBinding(direction = "in", name = "kafkaEvents", type = "kafkaTrigger")
public @interface KafkaTrigger {
    /**
     * Gets the Topic.
     */
    String topic();

    /**
     * Gets or sets the BrokerList.
     */
    String brokerList();

    /**
     * Gets or sets the EventHub connection string when using KafkaOutput protocol header feature of Azure EventHubs.
     */
    String eventHubConnectionString() default "";

    /**
     * Gets or sets the consumer group.
     */
    String consumerGroup();

    /**
     * Gets or sets the KeyType
     * This method is used internally. Don't pass the value to this method.
     */
    // String keyType();  // TODO Originally Type type. Should I pass the serialized value for them?

    /**
     * Gets or sets the ValueType
     * This method is used internally. Don't pass the value to this method.
     */
    // String valueType(); // TODO Originally Type type. Should I pass the serialized value for them?

    /**
     * Gets or sets the Avro schema.
     * Should be used only if a generic record should be generated
     */
    String avroScema() default "";

    /**
     * SASL mechanism to use for authentication.
     * Allowed values: Gssapi, Plain, ScramSha256, ScramSha512
     * Default: PLAIN
     */
    BrokerAuthenticationMode authenticationMode() default BrokerAuthenticationMode.NOTSET; // TODO double check if it is OK

    /**
     * SASL username with the PLAIN and SASL-SCRAM-.. mechanisms
     * Default: ""
     */
    String username() default "";

    /**
     * SASL password with the PLAIN and SASL-SCRAM-.. mechanisms
     * Default: ""
     *
     * security.protocol in librdkafka
     */
    String password() default "";

    /**
     * Gets or sets the security protocol used to communicate with brokers
     * default is PLAINTEXT
     */
    BrokerProtocol protocol() default BrokerProtocol.NOTSET;

    /**
     * Path to client's private key (PEM) used for authentication.
     * Default ""
     * ssl.key.location in librdkafka
     */
    String sslKeyLocation() default "";
}
