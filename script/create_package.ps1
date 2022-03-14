# Build the package
dotnet pack -o temp --include-symbols src/Microsoft.Azure.WebJobs.Extensions.Kafka/Microsoft.Azure.WebJobs.Extensions.Kafka.csproj /p:Version=100.100.100-pre

# Create a Dockerfile

# docker build -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\server\java8\Dockerfile -t jv8test . 
# docker build -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\server\python38\Dockerfile -t py38test . 
#docker build -f ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/dotnet/EventHub/Dockerfile -t azure-functions-kafka-dotnet-eventhub .
docker build -f ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/dotnet/Confluent/Dockerfile -t azure-functions-kafka-dotnet-confluent .
# Docker Compose directory for local test
# cd test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\server