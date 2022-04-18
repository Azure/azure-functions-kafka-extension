using namespace System.Net

param($kafkaEvent, $TriggerMetadata)

# $kafkaEvent
Write-Output "Headers: "
$kafkaEvent.Headers

# $TriggerMetadata