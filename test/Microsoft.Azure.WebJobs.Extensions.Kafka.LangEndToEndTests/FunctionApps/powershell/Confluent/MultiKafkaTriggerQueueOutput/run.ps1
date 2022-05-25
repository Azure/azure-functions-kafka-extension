using namespace System.Net

param($kafkaEvents, $TriggerMetadata)

$messages = @()

#Iterating over batch kafka events
foreach ($kafkaEvent in $kafkaEvents) {
    $kafkaEvent | ConvertFrom-Json -AsHashtable -outvariable event
    #Adding kafka events to messages
    $messages += $event.Value
}

$TriggerMetadata
$messages

#Pushing output binding message
Push-OutputBinding -Name Msg -Value $messages
