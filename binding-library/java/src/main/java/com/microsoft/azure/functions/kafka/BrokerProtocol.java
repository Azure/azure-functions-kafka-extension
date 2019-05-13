/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */
package com.microsoft.azure.functions.kafka;

public enum BrokerProtocol {
    NOTSET(-1),
    PLAINTEXT(0),
    SSL(1),
    SASLPLAINTEXT(2),
    SASLSSL(3);

    private int value;
    BrokerProtocol(final int value) {
        this.value = value;
    }
}
