using namespace System.Net

param($kafkaEvents, $TriggerMetadata)

foreach ($kafkaEvent in $kafkaEvents) {
    event = $kafkaEvent | ConvertFrom-Json -AsHashtable
    $event.Value
}