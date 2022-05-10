package com.contoso.kafka.entity;

public class KafkaEntity {
    public int Offset;
    public int Partition;
    public String Timestamp;
    public String Topic;
    public String Value;
    public KafkaHeaders Headers[];

    public KafkaEntity(int Offset, int Partition, String Topic, String Timestamp, String Value,KafkaHeaders[] headers) {
        this.Offset = Offset;
        this.Partition = Partition;
        this.Topic = Topic;
        this.Timestamp = Timestamp;
        this.Value = Value;
        this.Headers = headers;
    }
}