package com.contoso.kafka;

public class KafkaEntity {
    int Offset;
    int Partition;
    String Timestamp;
    String Topic;
    String Value;
    KafkaHeaders Headers[];

    public KafkaEntity(int Offset, int Partition, String Topic, String Timestamp, String Value,KafkaHeaders[] headers) {
        this.Offset = Offset;
        this.Partition = Partition;
        this.Topic = Topic;
        this.Timestamp = Timestamp;
        this.Value = Value;
        this.Headers = headers;
    }
}

class KafkaHeaders{
    String Key;
    String Value;

    public KafkaHeaders(String key, String value) {
        this.Key = key;
        this.Value = value;
    }

}