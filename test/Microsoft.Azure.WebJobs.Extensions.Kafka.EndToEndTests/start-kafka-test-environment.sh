#!/bin/bash

export COMPOSE_INTERACTIVE_NO_CLI=1

echo "Installing docker-compose..."
sudo apt install -y docker-compose

echo "Starting Kafka using docker-compose..."
docker-compose -f ./kafka-singlenode-compose.yaml up --build -d
sleep 15

# Topic name to test
TEST_TOPIC="test-topic"
BOOTSTRAP_SERVER="localhost:9092"

# Default names for containers
ZOOKEEPER_CONTAINER_NAME="zookeeper"
KAFKA_CONTAINER_NAME="kafka"
SCHEMA_REGISTRY_CONTAINER_NAME="schema-registry"

# Function to check if all containers are running and start them if not
check_containers() {
    echo "Checking if all containers are running..."
    docker-compose -f ./kafka-singlenode-compose.yaml ps
    # Get the list of services from docker-compose
    services=$(docker-compose -f ./kafka-singlenode-compose.yaml config --services)
    
    # Check each service
    all_running=true
    for service in $services; do
        services_running=$(docker-compose -f ./kafka-singlenode-compose.yaml ps --services --filter "status=running" $service)

        if [ "$service" == "kafka" ]; then
            KAFKA_CONTAINER_NAME=$(docker-compose -f ./kafka-singlenode-compose.yaml ps -q $service | xargs docker inspect -f '{{.Name}}' | sed 's/^\///')
            echo "Found Kafka container: $KAFKA_CONTAINER_NAME"
        elif [ "$service" == "zookeeper" ]; then
            ZOOKEEPER_CONTAINER_NAME=$(docker-compose -f ./kafka-singlenode-compose.yaml ps -q $service | xargs docker inspect -f '{{.Name}}' | sed 's/^\///')
            echo "Found Zookeeper container: $ZOOKEEPER_CONTAINER_NAME"
        elif [ "$service" == "schema-registry" ]; then
            SCHEMA_REGISTRY_CONTAINER_NAME=$(docker-compose -f ./kafka-singlenode-compose.yaml ps -q $service | xargs docker inspect -f '{{.Name}}' | sed 's/^\///')
            echo "Found Schema Registry container: $SCHEMA_REGISTRY_CONTAINER_NAME"
        fi

        if [[ ! " $services_running " =~ (^|[[:space:]])$service($|[[:space:]]) ]]; then
            echo "Container for service '$service' is not running."
            all_running=false
            
            # Try to start the individual container
            start_container_with_retry "$service"

            if [ $? -eq 0 ]; then
                echo "Container for service '$service' is running."
                all_running=true
            else 
                echo "Failed to start container for service '$service'."
                all_running=false
            fi
        else
            echo "Container for service '$service' is running."
        fi
    done
    
    if [ "$all_running" = true ]; then
        # Store container names for later use
        return 0
    else
        echo "All services are not running."
        docker-compose -f ./kafka-singlenode-compose.yaml ps 
        return 1
    fi
}

# Function to start a container with retry mechanism
start_container_with_retry() {
    local service=$1
    local max_attempts=3
    local attempt=1
    local wait_time=15
    
    while [ $attempt -le $max_attempts ]; do
        echo "Starting '$service' - attempt $attempt of $max_attempts..."
        
        # Stop the container first if it exists but in a bad state
        docker-compose -f ./kafka-singlenode-compose.yaml stop $service 2>/dev/null
        
        # Start the container
        docker-compose -f ./kafka-singlenode-compose.yaml up -d $service
        
        sleep 15

        services_running=$(docker-compose -f ./kafka-singlenode-compose.yaml ps --services --filter "status=running" $service)

        if [[ " $services_running " =~ (^|[[:space:]])$service($|[[:space:]]) ]]; then
            echo "Successfully started '$service' on attempt $attempt."
            return 0
        fi

        echo "Failed to start '$service' on attempt $attempt."
        
        # Show logs to help diagnose the issue
        echo "Container logs for '$service':"
        docker-compose -f ./kafka-singlenode-compose.yaml logs --tail=20 $service
        
        attempt=$((attempt + 1))
    done
    
    echo "Failed to start '$service' after $max_attempts attempts."
    return 1
}

# Function to create test topic 
create_test_topic() {
    local attempts=0
    local max_attempts=5
    local wait_time=10
    
    echo "Attempting to create test topic: $TEST_TOPIC"
    
    while [ $attempts -lt $max_attempts ]; do
        echo "Creating topic - attempt $((attempts+1)) of $max_attempts"
        
        # Try to create the topic and capture output
        output=$(docker exec -e LOG_LEVEL=ERROR $KAFKA_CONTAINER_NAME kafka-topics --create --if-not-exists --topic $TEST_TOPIC --bootstrap-server $BOOTSTRAP_SERVER --partitions 1 --replication-factor 1 2>&1)
        result=$?
        
        if [ $result -eq 0 ]; then
            echo "Successfully created topic: $TEST_TOPIC"
            return 0
        else
            attempts=$((attempts+1))
            echo "Failed to create topic (attempt $attempts/$max_attempts):"
            echo "$output"
            
            if [ $attempts -ge $max_attempts ]; then
                echo "Maximum number of attempts reached. Cannot create topic."
                return 1
            fi
            
            echo "Waiting $wait_time seconds before retrying..."
            sleep $wait_time
        fi
    done
}

# Create a test topic if all containers are running
# Fix: Correct syntax for the if statement with check_containers
if ! check_containers; then
    echo "Not all containers are running. Exiting."
    exit 1
fi
if ! create_test_topic; then
    echo "Failed to create test topic after multiple attempts. Exiting."
    exit 1
fi 

# List topics to confirm
echo "Listing available Kafka topics:"
docker exec -e LOG_LEVEL=ERROR $KAFKA_CONTAINER_NAME kafka-topics --list --bootstrap-server $BOOTSTRAP_SERVER