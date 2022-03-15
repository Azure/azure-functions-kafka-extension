using namespace System.Net

param($kafkaEvent, $TriggerMetadata)

$kafkaEvent 

$TriggerMetadata

$message = $kafkaEvent.Value

$message

Push-OutputBinding -Name Msg -Value $message
