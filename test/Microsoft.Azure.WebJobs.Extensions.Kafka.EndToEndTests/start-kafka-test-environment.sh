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
RETRY_INTERVAL=15
# Topic name to test
TEST_TOPIC="test-topic"
BOOTSTRAP_SERVER="localhost:9092"

# Function to get Kafka container name
get_kafka_container_name() {
    # Look for the Kafka container from the running containers
    docker ps --format "{{.Names}}" | grep -i microsoftazurewebjobsextensionskafkaendtoendtests_kafka | head -n 1
}

# Function to check if Kafka is ready
check_kafka_ready() {
    # Get the current Kafka container name
    KAFKA_CONTAINER_NAME=$(get_kafka_container_name)
    
    if [ -z "$KAFKA_CONTAINER_NAME" ]; then
        echo "No Kafka container found. Checking if docker-compose service is running..."
        docker-compose -f ./kafka-singlenode-compose.yaml ps
        return 1
    fi
    
    echo "Kafka container found: $KAFKA_CONTAINER_NAME"
    echo "Attempting to create test topic: $TEST_TOPIC"
    
    # Try to create a topic and capture only actual errors
    output=$(docker exec -e LOG_LEVEL=ERROR $KAFKA_CONTAINER_NAME kafka-topics --create --if-not-exists --topic $TEST_TOPIC --bootstrap-server $BOOTSTRAP_SERVER 2>&1)
    result=$?
    
    # Only show output if there was an error
    if [ $result -ne 0 ]; then
        echo "Error creating topic:"
        echo "$output"
    fi
    
    return $result
}   

# Retry loop with topic creation attempts
retry_count=0
topic_creation_attempts=0
max_topic_creation_attempts=5

until check_kafka_ready; do
    retry_count=$((retry_count+1))
    
    if [ $retry_count -ge $MAX_RETRIES ]; then
        echo "Failed to connect to Kafka after $MAX_RETRIES attempts. Exiting."
        exit 1
    fi
    
    # Check if this is a topic creation issue or a container availability issue
    KAFKA_CONTAINER_NAME=$(get_kafka_container_name)
    if [ ! -z "$KAFKA_CONTAINER_NAME" ]; then
        topic_creation_attempts=$((topic_creation_attempts+1))
        if [ $topic_creation_attempts -ge $max_topic_creation_attempts ]; then
            echo "Failed to create topic after $max_topic_creation_attempts attempts, but container is running."
            echo "Container logs:"
            docker logs $KAFKA_CONTAINER_NAME --tail 20
            exit 1
        fi
    fi
    
    echo "Kafka not ready yet. Waiting $RETRY_INTERVAL seconds before retry ($retry_count/$MAX_RETRIES)..."
    sleep $RETRY_INTERVAL
done

# Get the final container name for display purposes
KAFKA_CONTAINER_NAME=$(get_kafka_container_name)
echo "Successfully created test topic. Kafka is ready!"
echo "Kafka broker name: $KAFKA_CONTAINER_NAME"
echo "Total wait time: $((retry_count * RETRY_INTERVAL)) seconds"

# Optional: List topics to confirm
echo "Listing available Kafka topics:"
docker exec -e LOG_LEVEL=ERROR $KAFKA_CONTAINER_NAME kafka-topics --list --bootstrap-server $BOOTSTRAP_SERVER