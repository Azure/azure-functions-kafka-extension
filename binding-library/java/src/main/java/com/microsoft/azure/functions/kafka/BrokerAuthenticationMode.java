/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */
package com.microsoft.azure.functions.kafka;

/**
 * Defines the broker authentication modes
 */
public enum BrokerAuthenticationMode {
    NOTSET(-1),
    GSSAPI(0),
    PLAIN(1),
    SCRAMSHA256(2),
    SCRAMSHA512(3);

    private final int value;

    BrokerAuthenticationMode(final int value) {
        this.value = value;
    }

    public int getValue() { return value; }
}
