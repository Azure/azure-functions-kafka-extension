using namespace System.Net

# Input bindings are passed in via param block.
param($kafkaEvents, $TriggerMetadata)

$messages = @()
foreach ($kafkaEvent in $kafkaEvents) {
    $kafkaEvent | ConvertFrom-Json -AsHashtable -outvariable event
    $messages += $event.Value
}

$TriggerMetadata
$messages

# Associate values to output bindings by calling 'Push-OutputBinding'.
Push-OutputBinding -Name Msg -Value $messages
