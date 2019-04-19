/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */
package main.com.microsoft.azure.functions.kafka;

/**
 * KafkaOutput Event Data to use with KafkaOutput binding
 */
public class KafkaEventData {
    /**
     * Constructor
     */
    public KafkaEventData() {

    }

    // .NET has other constructor which has an interface called IConsumerResultData.

    public Object key;

    public long offset;

    public int partition;

    public String topic;

    public String timestamp;  // TODO this is DateTime in C#. Can we use Date for this? or String?

    public Object value;

}
