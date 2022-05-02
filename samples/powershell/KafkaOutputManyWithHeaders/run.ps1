using namespace System.Net

# Input bindings are passed in via param block.
param($Request, $TriggerMetadata)

# Write to the Azure Functions log stream.
Write-Host "PowerShell HTTP trigger function processed a request."

# Interact with query parameters or the body of the request.
$message = $Request.Query.Message
if (-not $message) {
    $message = $Request.Body.Message
}

$kevent = @{
    Offset = 364
    Partition = 0
    Topic = "kafkaeventhubtest1"
    Timestamp = "2022-04-09T03:20:06.591Z"
    Value = $message
    Headers= @(@{
        Key= "test"
        Value= "powershell"
    }
    )
}

Push-OutputBinding -Name Message -Value $kevent

# Associate values to output bindings by calling 'Push-OutputBinding'.
Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
    StatusCode = [HttpStatusCode]::OK
    Body = 'ok'
})
