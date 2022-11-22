using namespace System.Net

param($kafkaEvent, $TriggerMetadata)

Write-Output "Powershell Kafka trigger function called for message $kafkaEvent.Value"
