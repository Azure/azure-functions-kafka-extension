package com.microsoft.producer;

import com.microsoft.entity.wallet.WalletTransaction;
import io.confluent.kafka.serializers.AbstractKafkaSchemaSerDeConfig;
import io.confluent.kafka.serializers.KafkaAvroSerializer;
import org.apache.kafka.clients.producer.KafkaProducer;
import org.apache.kafka.clients.producer.ProducerConfig;
import org.apache.kafka.clients.producer.ProducerRecord;
import org.apache.kafka.clients.producer.RecordMetadata;
import org.apache.kafka.common.errors.SerializationException;
import org.apache.kafka.common.serialization.StringSerializer;

import java.io.FileInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.net.URISyntaxException;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.time.LocalDateTime;
import java.util.Properties;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.Future;
import java.util.logging.Level;
import java.util.logging.Logger;

public class WalletProducer {
    private static final String TOPIC = "wallet_event";
    private static final Properties props = new Properties();

    private static final String BOOTSTRAP_SERVER = "bootstrap.servers";
    private static final String SCHEMA_REGISTRY_URL = "schema.registry.url";
    private static final String JAAS_CONFIG = "sasl.jaas.config";
    private static final String CONFIG_FILE_NAME = "producer-config.properties";
    private static final String BASIC_AUTH_CRED_SOURCE = "USER_INFO";
    private static final String BASIC_AUTH_CRED_SOURCE_KEY = "basic.auth.credentials.source";
    private static final String BASIC_AUTH_USER_INFO  = "basic.auth.user.info";

    public static void main(final String[] args) throws IOException {
        Logger logger = Logger.getLogger(WalletProducer.class.getName());
        Properties prop = getBrokerProperties(logger);

        props.put(ProducerConfig.BOOTSTRAP_SERVERS_CONFIG, prop.getProperty(BOOTSTRAP_SERVER));
        props.put(AbstractKafkaSchemaSerDeConfig.SCHEMA_REGISTRY_URL_CONFIG, prop.getProperty(SCHEMA_REGISTRY_URL));
        if(prop.getProperty(BASIC_AUTH_USER_INFO) != null) {
            props.put(BASIC_AUTH_CRED_SOURCE_KEY, BASIC_AUTH_CRED_SOURCE);
            props.put(AbstractKafkaSchemaSerDeConfig.SCHEMA_REGISTRY_USER_INFO_CONFIG, prop.getProperty(BASIC_AUTH_USER_INFO));
        }
        props.put("security.protocol", "SASL_SSL");
        props.put("sasl.mechanism", "PLAIN");
        props.put("sasl.jaas.config", prop.getProperty(JAAS_CONFIG));
        props.put(ProducerConfig.ACKS_CONFIG, "all");
        props.put(ProducerConfig.RETRIES_CONFIG, 0);
        props.put(ProducerConfig.KEY_SERIALIZER_CLASS_CONFIG, StringSerializer.class);
        props.put(ProducerConfig.VALUE_SERIALIZER_CLASS_CONFIG, KafkaAvroSerializer.class);

        try (KafkaProducer<String, WalletTransaction> producer = new KafkaProducer<String, WalletTransaction>(props)) {


            for (long i = 0; i < 1; i++) {
                final WalletTransaction walletTransaction = WalletTransaction.newBuilder().setAmount(220d).setId("123")
                        .setType("charge").setCurrency("USD").setFee(3.07d).setPayoutStatus("scheduled").setPayoutId("1234")
                        .setSourceId("65656").setSourceType("CreditCard").setProcessedAt(LocalDateTime.now().toString())
                        .setWalletId("Wal3434").setCustomerId("345454").build();
                final ProducerRecord<String, WalletTransaction> record = new ProducerRecord<String, WalletTransaction>(TOPIC,
                        walletTransaction.getId().toString(), walletTransaction);
                Future<RecordMetadata> recordMetadataFuture = producer.send(record);
                RecordMetadata recordMetadata = recordMetadataFuture.get();
                System.out.println("response -- "+ recordMetadata.topic()+" : "+
                        recordMetadata.partition()+" : "+recordMetadata.offset());
                Thread.sleep(1000L);
            }
            producer.flush();
            System.out.printf("Successfully produced messages to a topic called %s%n", TOPIC);

        } catch (final SerializationException e) {
            logger.log(Level.SEVERE, e.getMessage(), e);
        } catch (final InterruptedException e) {
            logger.log(Level.SEVERE, e.getMessage(), e);
        } catch (final ExecutionException e) {
            logger.log(Level.SEVERE, e.getMessage(), e);
        }

    }

    private static Properties getBrokerProperties(Logger logger) {
        try (InputStream input = WalletProducer.class.getClassLoader().getResourceAsStream(CONFIG_FILE_NAME)) {
            Properties prop = new Properties();
            prop.load(input);
            return prop;
        } catch (IOException ex) {
            logger.log(Level.SEVERE, ex.getMessage(), ex);
        }
        return null;
    }
}
