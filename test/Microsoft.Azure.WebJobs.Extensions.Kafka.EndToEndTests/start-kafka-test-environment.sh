#!/bin/bash

export COMPOSE_INTERACTIVE_NO_CLI=1

echo "Installing docker-compose..."
sudo apt install -y docker-compose

echo "Starting Kafka using docker-compose..."
docker-compose -f ./kafka-singlenode-compose.yaml up --build -d

echo "Waiting for Kafka to be ready..."

# Maximum number of retries
MAX_RETRIES=30
# Time to wait between retries in seconds
RETRY_INTERVAL=30
# Topic name to test
TEST_TOPIC="test-topic"

# Function to check if Kafka is ready
check_kafka_ready() {
    echo "Attempting to create test topic: $TEST_TOPIC"
    # Try to create a topic and capture the exit code
    docker exec -it kafka-broker kafka-topics --create --if-not-exists --topic $TEST_TOPIC --bootstrap-server localhost:9092 2>&1
}

# Retry loop
retry_count=0
until check_kafka_ready; do
    retry_count=$((retry_count+1))
    if [ $retry_count -ge $MAX_RETRIES ]; then
        echo "Failed to connect to Kafka after $MAX_RETRIES attempts. Exiting."
        exit 1
    fi
    
    echo "Kafka not ready yet. Waiting $RETRY_INTERVAL seconds before retry ($retry_count/$MAX_RETRIES)..."
    sleep $RETRY_INTERVAL
done

echo "Successfully created test topic. Kafka is ready!"
echo "Total wait time: $((retry_count * RETRY_INTERVAL)) seconds"

# Optional: List topics to confirm
echo "Listing available Kafka topics:"
docker exec -it kafka-broker kafka-topics --list --bootstrap-server localhost:9092