using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace KafkaCompatibility;

public static class CompatibilityFunctions
{
    [Function("ProduceCompatibilityMessage")]
    public static ProduceResult Produce(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData request)
    {
        using var reader = new StreamReader(request.Body);
        var message = reader.ReadToEnd();
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new InvalidOperationException("A correlation token is required.");
        }

        var response = request.CreateResponse(HttpStatusCode.Accepted);
        response.WriteString(message);
        return new ProduceResult
        {
            Message = message,
            Response = response,
        };
    }

    [Function("RoundTripCompatibilityMessage")]
    [KafkaOutput("kafka:29092", "compat-result")]
    public static string RoundTrip(
        [KafkaTrigger(
            "kafka:29092",
            "compat-input",
            ConsumerGroup = "compat-dotnet-isolated")]
        string message)
    {
        return message;
    }
}

public sealed class ProduceResult
{
    [KafkaOutput("kafka:29092", "compat-input")]
    public required string Message { get; init; }

    public required HttpResponseData Response { get; init; }
}
