package com.microsoft.entity.event;

import java.io.Serializable;
import java.util.Map;

public class Notification implements Serializable {
    private Long id;
    private String type;
    private String eventCode;
    private String emailId;
    private String mobileNum;

    public Long getId() {
        return id;
    }

    public String getType() {
        return type;
    }

    public String getEventCode() {
        return eventCode;
    }

    public String getEmailId() {
        return emailId;
    }

    public String getMobileNum() {
        return mobileNum;
    }

    public String getCustomerId() {
        return customerId;
    }

    public Map<String, Object> getTemplateMappingData() {
        return templateMappingData;
    }

    private String customerId;
    private Map<String, Object> templateMappingData;

    private Notification() {
        super();
    }

    private Notification(NotificationEventBuilder notificationEventBuilder) {
        this.id = notificationEventBuilder.id;
        this.type = notificationEventBuilder.type;
        this.eventCode = notificationEventBuilder.eventCode;
        this.emailId = notificationEventBuilder.emailId;
        this.mobileNum = notificationEventBuilder.mobileNum;
        this.customerId = notificationEventBuilder.customerId;
        this.templateMappingData = notificationEventBuilder.templateMappingData;
    }

    public static class NotificationEventBuilder {
        private Long id;
        private String type;
        private String eventCode;
        private String emailId;
        private String mobileNum;
        private String customerId;
        private Map<String, Object> templateMappingData;

        public NotificationEventBuilder setId(Long id) {
            this.id = id;
            return this;
        }

        public NotificationEventBuilder setType(String type) {
            this.type = type;
            return this;
        }

        public NotificationEventBuilder setEventCode(String eventCode) {
            this.eventCode = eventCode;
            return this;
        }

        public NotificationEventBuilder setEmailId(String emailId) {
            this.emailId = emailId;
            return this;
        }

        public NotificationEventBuilder setMobileNum(String mobileNum) {
            this.mobileNum = mobileNum;
            return this;
        }

        public NotificationEventBuilder setCustomerId(String customerId) {
            this.customerId = customerId;
            return this;
        }

        public NotificationEventBuilder setTemplateMappingData(Map<String, Object> templateMappingData) {
            this.templateMappingData = templateMappingData;
            return this;
        }

        public Notification build() {
            return new Notification(this);
        }
    }
}
