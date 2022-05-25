using namespace System.Net

param($kafkaEvent, $TriggerMetadata)

#Kafka Event received from trigger binding
$kafkaEvent 

$TriggerMetadata

#Queue output to be sent with output binding
$message = $kafkaEvent.Value

$message

#Pushing output binding message
Push-OutputBinding -Name Msg -Value $message
