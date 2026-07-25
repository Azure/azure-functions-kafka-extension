param(
    [string]$ExtensionSource = "package",
    [string]$ExtensionBundleVersion = "4.3.2"
)

# Build the package
dotnet pack -o temp --include-symbols src/Microsoft.Azure.WebJobs.Extensions.Kafka/Microsoft.Azure.WebJobs.Extensions.Kafka.csproj /p:Version=100.100.100-pre

docker build --build-arg EXTENSION_SOURCE=$ExtensionSource --build-arg EXTENSION_BUNDLE_VERSION=$ExtensionBundleVersion -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\java\EventHub\Dockerfile -t azure-functions-kafka-java-eventhub .
docker build --build-arg EXTENSION_SOURCE=$ExtensionSource --build-arg EXTENSION_BUNDLE_VERSION=$ExtensionBundleVersion -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\python\EventHub\Dockerfile -t azure-functions-kafka-python-eventhub .

docker build --build-arg EXTENSION_SOURCE=$ExtensionSource --build-arg EXTENSION_BUNDLE_VERSION=$ExtensionBundleVersion -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\java\Confluent\Dockerfile -t azure-functions-kafka-java-confluent .
docker build --build-arg EXTENSION_SOURCE=$ExtensionSource --build-arg EXTENSION_BUNDLE_VERSION=$ExtensionBundleVersion -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\python\Confluent\Dockerfile -t azure-functions-kafka-python-confluent .
docker build --build-arg EXTENSION_SOURCE=$ExtensionSource --build-arg EXTENSION_BUNDLE_VERSION=$ExtensionBundleVersion -f .\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\javascript\Confluent\Dockerfile -t azure-functions-kafka-javascript-confluent .
