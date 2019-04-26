/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */
package com.microsoft.azure.functions.kafka.annotation;

import com.microsoft.azure.functions.annotation.CustomBinding;
import com.microsoft.azure.functions.kafka.BrokerAuthenticationMode;
import com.microsoft.azure.functions.kafka.BrokerProtocol;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * <p>Annotation for Kafka output bindings</p>
 */
@Target(ElementType.PARAMETER)
@Retention(RetentionPolicy.RUNTIME)
@CustomBinding(direction = "out", name = "kafkaOutput", type = "kafka")
public @interface KafkaOutput { // TODO Should I name it as KafkaOutput?
    /**
     * Gets the Topic.
     * @return
     */
    String topic();

    /**
     * Gets or sets the BrokerList.
     */
    String brokerList();

    /**
     * Gets or sets the Maximum transmit message size. Default: 1MB
     */
    int maxMessageBytes() default 1000012; // Follow the kafka spec https://kafka.apache.org/documentation/

    /**
     * Maximum number of messages batched in one MessageSet. default: 10000
     */
    int batchSize() default 10000;

    /**
     * When set to `true`, the producer will ensure that messages are successfully produced exactly once and in the original produce order. default: false
     */
    boolean enableIdempotence() default false;

    /**
     * Local message timeout. This value is only enforced locally and limits the time a produced message waits for successful delivery. A time of 0 is infinite. This is the maximum time used to deliver a message (including retries). Delivery error occurs when either the retry count or the message timeout are exceeded. default: 300000
     */
    int messageTimeoutMs() default 300000;

    /**
     * The ack timeout of the producer request in milliseconds. default: 5000
     */
    int requestTimeoutMs() default 5000;

    /**
     * How many times to retry sending a failing Message. **Note:** default: 2
     * Retrying may cause reordering unless EnableIdempotence is set to true.
     * @see #enableIdempotence()
     */
    int maxRetries() default 2;

    /**
     * SASL mechanism to use for authentication.
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
     * Default is plaintext
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
