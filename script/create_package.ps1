# Build the package
dotnet pack -o temp --include-symbols src/Microsoft.Azure.WebJobs.Extensions.Kafka/Microsoft.Azure.WebJobs.Extensions.Kafka.csproj /p:Version=100.100.100-pre

# Create a Dockerfile

docker build -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\server\java8\Dockerfile -t jv8test . 
docker build -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\server\python38\Dockerfile -t py38test . 

docker build -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\dotnet-isolated\EventHub\Dockerfile -t azure-functions-kafka-dotnet-isolated-eventhub .
docker build -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\java\EventHub\Dockerfile -t azure-functions-kafka-java-eventhub .
docker build -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\javascript\EventHub\Dockerfile -t azure-functions-kafka-javascript-eventhub .
docker build -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\powershell\EventHub\Dockerfile -t azure-functions-kafka-powershell-eventhub . 
docker build -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\python\EventHub\Dockerfile -t azure-functions-kafka-python-eventhub .
docker build -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\typescript\EventHub\Dockerfile -t azure-functions-kafka-typescript-eventhub .

docker build -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\dotnet-isolated\Confluent\Dockerfile -t azure-functions-kafka-dotnet-isolated-confluent .
docker build -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\java\Confluent\Dockerfile -t azure-functions-kafka-java-confluent .
docker build -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\javascript\Confluent\Dockerfile -t azure-functions-kafka-javascript-confluent .
docker build -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\powershell\Confluent\Dockerfile -t azure-functions-kafka-powershell-confluent . 
docker build -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\python\Confluent\Dockerfile -t azure-functions-kafka-python-confluent .
docker build -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\typescript\Confluent\Dockerfile -t azure-functions-kafka-typescript-confluent .