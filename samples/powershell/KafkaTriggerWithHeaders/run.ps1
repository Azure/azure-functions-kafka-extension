using namespace System.Net

param($kafkaEvent, $TriggerMetadata)

# $kafkaEvent
Write-Output "Powershell Kafka trigger function called for message" + $kafkaEvent.Value
Write-Output "Headers for this message:"
foreach ($header in $kafkaEvent.Headers) {
    $DecodedValue = [System.Text.Encoding]::Unicode.GetString([System.Convert]::FromBase64String($header.Value))
    $Key = $header.Key
    Write-Output "Key: $Key Value: $DecodedValue"
}
# $TriggerMetadata