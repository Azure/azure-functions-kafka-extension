using namespace System.Net

# Input bindings are passed in via param block.
param($Request, $TriggerMetadata)

# Write to the Azure Functions log stream.
Write-Host "PowerShell HTTP trigger function processed a request."

$kevent1 = @{
    Offset = 364
    Partition = 0
    Topic = "kafkaeventhubtest1"
    Timestamp = "2022-04-09T03:20:06.591Z"
    Value = "one"
    Headers= @(@{
        Key= "test"
        Value= "powershell"
    }
    )
}

$kevent2 = @{
    Offset = 364
    Partition = 0
    Topic = "kafkaeventhubtest1"
    Timestamp = "2022-04-09T03:20:06.591Z"
    Value = "two"
    Headers= @(@{
        Key= "test"
        Value= "powershell"
    }
    )
}

$kevent= @($kevent1, $kevent2)
Push-OutputBinding -Name Message -Value $kevent

# Associate values to output bindings by calling 'Push-OutputBinding'.
Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
    StatusCode = [HttpStatusCode]::OK
    Body = 'ok'
})
