# Build the package
dotnet pack -o temp --include-symbols src/Microsoft.Azure.WebJobs.Extensions.Kafka/Microsoft.Azure.WebJobs.Extensions.Kafka.csproj /p:Version=100.100.100-pre
dotnet nuget add source $PWD/temp --name local  
dotnet nuget list source

#docker build -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\java\EventHub\Dockerfile -t azure-functions-kafka-java-eventhub .
#docker build -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\python\EventHub\Dockerfile -t azure-functions-kafka-python-eventhub .

#docker build -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\java\Confluent\Dockerfile -t azure-functions-kafka-java-confluent .
#docker build -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\python\Confluent\Dockerfile -t azure-functions-kafka-python-confluent .
