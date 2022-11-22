using namespace System.Net

param($kafkaEvents, $TriggerMetadata)

$kafkaEvents
foreach ($kafkaEvent in $kafkaEvents) {
    $event = $kafkaEvent | ConvertFrom-Json -AsHashtable
    Write-Output "Powershell Kafka trigger function called for message $event.Value"
}