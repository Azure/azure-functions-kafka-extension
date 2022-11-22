package com.microsoft.entity.event;

public class WalletTransaction {

    private String id;
    private double amount;
    private String type;
    private String payoutId;
    private String payoutStatus;
    private String currency;
    private double fee;
    private String sourceId;
    private String sourceType;
    private String processedAt;
    private String walletId;
    private String customerId;

    public WalletTransaction() {
        super();
    }

    public WalletTransaction(String id, double amount, String type, String payoutId, String payoutStatus, String currency,
                             double fee, String sourceId, String sourceType, String processedAt, String walletId, String customerId) {
        this.id = id;
        this.amount = amount;
        this.type = type;
        this.payoutId = payoutId;
        this.payoutStatus = payoutStatus;
        this.currency = currency;
        this.fee = fee;
        this.sourceId = sourceId;
        this.sourceType = sourceType;
        this.processedAt = processedAt;
        this.walletId = walletId;
        this.customerId = customerId;
    }

    @Override
    public String toString() {
        return "WalletTransaction{" +
                "id='" + id + '\'' +
                ", amount=" + amount +
                ", type='" + type + '\'' +
                ", payoutId='" + payoutId + '\'' +
                ", payoutStatus='" + payoutStatus + '\'' +
                ", currency='" + currency + '\'' +
                ", fee=" + fee +
                ", sourceId='" + sourceId + '\'' +
                ", sourceType='" + sourceType + '\'' +
                ", processedAt='" + processedAt + '\'' +
                '}';
    }

    public String getId() {
        return id;
    }

    public double getAmount() {
        return amount;
    }

    public String getType() {
        return type;
    }

    public String getPayoutId() {
        return payoutId;
    }

    public String getPayoutStatus() {
        return payoutStatus;
    }

    public String getCurrency() {
        return currency;
    }

    public double getFee() {
        return fee;
    }

    public String getSourceId() {
        return sourceId;
    }

    public String getSourceType() {
        return sourceType;
    }

    public String getProcessedAt() {
        return processedAt;
    }

    public String getWalletId() {
        return walletId;
    }

    public String getCustomerId() {
        return customerId;
    }
}
