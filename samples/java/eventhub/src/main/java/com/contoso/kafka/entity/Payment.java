package com.contoso.kafka.entity;

public class Payment {
    private String id;
    private Double amount;
    private String type;

    public Payment() {

    }

    public Payment(String id, Double amount, String type) {
        this.id = id;
        this.amount = amount;
        this.type = type;
    }

    public String getId() {
        return id;
    }

    public Double getAmount() {
        return amount;
    }

    public String getType() {
        return type;
    }

    @Override
    public String toString() {
        return "id:: "+id+" amount :: "+amount+" type :: "+type;
    }
}
