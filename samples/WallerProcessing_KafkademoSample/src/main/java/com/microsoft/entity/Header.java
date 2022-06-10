package com.microsoft.entity;

import java.io.UnsupportedEncodingException;
import java.nio.charset.StandardCharsets;
import java.util.Base64;

public class Header {
    private String Key;
    private String Value;

    public Header(String key, String value) {
        this.Key = key;
        try {
            this.Value = new String(value.getBytes("UTF-8"), StandardCharsets.UTF_8);
            //System.out.println("post value :: "+this.Value);
        } catch (UnsupportedEncodingException e) {
            e.printStackTrace();
            this.Value = value;
        }
    }

    public String getKey() {
        return this.Key;
    }

    public String getValue() {
        return this.Value;
    }

    @Override
    public String toString() {
        return "Header{" +
                "Key='" + Key + '\'' +
                ", Value='" + new String(new String(Base64.getDecoder().decode(this.Value), StandardCharsets.UTF_8)) + '\'' +
                '}';
    }
}
