using namespace System.Net

# Input bindings are passed in via param block.
param($Request, $TriggerMetadata)

# Write to the Azure Functions log stream.
Write-Host "PowerShell HTTP trigger function processed a request."

# Interact with query parameters or the body of the request.
$message = $Request.Query.Message
$message1 = $Request.Query.Message1
$message2 = $Request.Query.Message2

$body = "This HTTP triggered function executed successfully. Pass a message in the query string or in the request body for a personalized response."

if ($message -And $message1 -And $message2) {
    $messages = ($message, $message1, $message2)
    $body = "Message received:  $message $message1 $message2. The message transfered to the kafka broker."
    Push-OutputBinding -Name Message -Value $messages
}

# Associate values to output bindings by calling 'Push-OutputBinding'.
Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
    StatusCode = [HttpStatusCode]::OK
    Body = $body
})
