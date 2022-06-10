package com.microsoft.entity;

import java.util.Arrays;

public class KafkaEntity {
    private int Offset;
    private int Partition;
    private  String Topic;
    private String Timestamp;
    private String Value;
    private Header[] Headers;

    private KafkaEntity(KafkaEntityBuilder kafkaEntityBuilder) {
        this.Offset = kafkaEntityBuilder.offset;
        this.Partition = kafkaEntityBuilder.partition;
        this.Topic = kafkaEntityBuilder.topic;
        this.Timestamp = kafkaEntityBuilder.timestamp;
        this.Value = kafkaEntityBuilder.value;
        this.Headers = kafkaEntityBuilder.headers;
    }

    public String getValue() { return Value; }

    public Header[] getHeaders() {
        return Headers;
    }

    @Override
    public String toString() {
        return "KafkaEntity{" +
                "offSet=" + Offset +
                ", partition=" + Partition +
                ", topic='" + Topic + '\'' +
                ", timeStamp='" + Timestamp + '\'' +
                ", value='" + Value + '\'' +
                ", headers=" + Arrays.toString(Headers) +
                '}';
    }

    public static class KafkaEntityBuilder {
        private int offset;
        private int partition;
        private  String topic;
        private String timestamp;
        private String value;
        private Header[] headers;

        public KafkaEntityBuilder setOffset(int offset) {
            this.offset = offset;
            return this;
        }

        public KafkaEntityBuilder setPartition(int partition) {
            this.partition = partition;
            return this;
        }

        public KafkaEntityBuilder setTopic(String topic) {
            this.topic = topic;
            return this;
        }

        public KafkaEntityBuilder setTimestamp(String timestamp) {
            this.timestamp = timestamp;
            return this;
        }

        public KafkaEntityBuilder setValue(String value) {
            this.value = value;
            return this;
        }

        public KafkaEntityBuilder setHeaders(Header[] headers) {
            this.headers = headers;
            return this;
        }

        public KafkaEntity build(){
            return new KafkaEntity(this);
        }
    }
}
