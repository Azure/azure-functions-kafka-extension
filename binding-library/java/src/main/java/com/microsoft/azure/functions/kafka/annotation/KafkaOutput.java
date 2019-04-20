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

@Target(ElementType.PARAMETER)
@Retention(RetentionPolicy.RUNTIME)
@CustomBinding(direction = "in", name = "", type = "KafkaOutput")
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
     * Json format
     * Default: Plain*
     */
    String AvroScema();

    /**
     * Gets or sets the Maximum transmit message size. Default: 1MB
     */
    int maxMessageBytes();

    /**
     * Maximum number of messages batched in one MessageSet. default: 10000
     */
    int batchSize();

    /**
     * When set to `true`, the producer will ensure that messages are successfully produced exactly once and in the original produce order. default: false
     */
    boolean enableIdempotence();

    /**
     * Local message timeout. This value is only enforced locally and limits the time a produced message waits for successful delivery. A time of 0 is infinite. This is the maximum time used to deliver a message (including retries). Delivery error occurs when either the retry count or the message timeout are exceeded. default: 300000
     */
    int messageTimeoutMs();

    /**
     * The ack timeout of the producer request in milliseconds. default: 5000
     */
    int requestTimeoutMs();

    /**
     * How many times to retry sending a failing Message. **Note:** default: 2
     * Retrying may cause reordering unless EnableIdempotence is set to true.
     * @see #enableIdempotence()
     */
    int maxRetries();

    /**
     * SASL mechanism to use for authentication.
     * Default: PLAIN
     */
    BrokerAuthenticationMode authenticationMode(); // TODO double check if it is OK

    /**
     * SASL username with the PLAIN and SASL-SCRAM-.. mechanisms
     * Default: ""
     */
    String username();

    /**
     * SASL password with the PLAIN and SASL-SCRAM-.. mechanisms
     * Default is plaintext
     *
     * security.protocol in librdkafka
     */
    String password();

    /**
     * Gets or sets the security protocol used to communicate with brokers
     * default is PLAINTEXT
     */
    BrokerProtocol protocol();

    /**
     * Path to client's private key (PEM) used for authentication.
     * Default ""
     * ssl.key.location in librdkafka
     */
    String sslKeyLocation();
}
