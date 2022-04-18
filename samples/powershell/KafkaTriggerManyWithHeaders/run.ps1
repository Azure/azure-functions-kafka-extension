using namespace System.Net

param($kafkaEvents, $TriggerMetadata)

foreach ($kafkaEvent in $kafkaEvents) {
    $kevent = $kafkaEvent | ConvertFrom-Json -AsHashtable
    $kevent.Headers
}
# $TriggerMetadata