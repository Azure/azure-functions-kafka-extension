/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */
package com.microsoft.azure.functions.kafka.annotation;

import com.microsoft.azure.functions.annotation.CustomBinding;
import com.microsoft.azure.functions.kafka.BrokerAuthenticationMode;
import com.microsoft.azure.functions.kafka.BrokerProtocol;

import com.microsoft.azure.functions.annotation.Cardinality;

import java.lang.annotation.Retention;
import java.lang.annotation.Target;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.ElementType;


/**
 * <p>Annotation for KafkaTrigger bindings</p>
 */
@Target(ElementType.PARAMETER)
@Retention(RetentionPolicy.RUNTIME)
@CustomBinding(direction = "in", name = "kafkaEvents", type = "kafkaTrigger")
public @interface KafkaTrigger {
    /**
     * The variable name used in function.json.
     * @return The variable name used in function.json.
     */
    String name();
    
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
     * Cardinality of the trigger input.
     * Choose 'One' if the input is a single message or 'Many' if the input is an array of messages.
     * If you choose 'Many', please set a dataType. 
     * Default: 'One'
     */
    Cardinality cardinality() default Cardinality.ONE;
    /**
     * DataType for the Cardinality settings. If you set the cardinality as Cardinality.MANY, Azure Functions Host will deserialize
     * the kafka events as an array of this type.
     * Allowed values: string, binary, stream
     * Default: ""
     */
    String dataType() default "";

    /**
     * Gets or sets the consumer group.
     */
    String consumerGroup();

    /**
     * SASL mechanism to use for authentication.
     * Allowed values: Gssapi, Plain, ScramSha256, ScramSha512
     * Default: PLAIN
     */
    BrokerAuthenticationMode authenticationMode() default BrokerAuthenticationMode.NOTSET;

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

    /**
     * Path to CA certificate file for verifying the broker's certificate.
     * ssl.ca.location in librdkafka
     */
    String sslCaLocation() default "";

    /**
     * Path to client's certificate.
     * ssl.certificate.location in librdkafka
     */
    String sslCertificateLocation() default "";

    /**
     * Password for client's certificate.
     * ssl.key.password in librdkafka
     */
    String sslKeyPassword() default "";
}
