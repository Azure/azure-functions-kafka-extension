package com.contoso.kafka.entity;

public class KafkaHeaders{
    public String Key;
    public String Value;

    public KafkaHeaders(String key, String value) {
        this.Key = key;
        this.Value = value;
    }

}