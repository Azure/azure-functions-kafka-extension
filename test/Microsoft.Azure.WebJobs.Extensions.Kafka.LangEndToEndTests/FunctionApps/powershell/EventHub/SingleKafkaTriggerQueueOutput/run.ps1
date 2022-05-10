using namespace System.Net

# Input bindings are passed in via param block.
param($kafkaEvent, $TriggerMetadata)

$kafkaEvent 

$TriggerMetadata

$message = $kafkaEvent.Value

$message

# Associate values to output bindings by calling 'Push-OutputBinding'.
Push-OutputBinding -Name Msg -Value $message
