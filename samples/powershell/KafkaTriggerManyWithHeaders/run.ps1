using namespace System.Net

param($kafkaEvents, $TriggerMetadata)

foreach ($kafkaEvent in $kafkaEvents) {
    $kevent = $kafkaEvent | ConvertFrom-Json -AsHashtable
    Write-Output "Powershell Kafka trigger function called for message $kevent.Value"
    Write-Output "Headers for this message:"
    foreach ($header in $kevent.Headers) {
        $DecodedValue = [System.Text.Encoding]::Unicode.GetString([System.Convert]::FromBase64String($header.Value))
        $Key = $header.Key
        Write-Output "Key: $Key Value: $DecodedValue"
    }
}
