using namespace System.Net

param($kafkaEvents, $TriggerMetadata)

$messages = @()
foreach ($kafkaEvent in $kafkaEvents) {
    $kafkaEvent | ConvertFrom-Json -AsHashtable -outvariable event
    $messages += $event.Value
}

$TriggerMetadata
$messages

Push-OutputBinding -Name Msg -Value $messages
