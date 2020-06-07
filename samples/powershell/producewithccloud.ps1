#run on wsl
pwsh-preview
function Get-TempRandom {
    $tempfake = get-random -Minimum 1 -Maximum 100    
    $obj = [PSCustomObject]@{
        temperature =  $tempfake
        scale = "C"
    }
    return $obj
}
ccloud login #login 
ccloud kafka cluster use <resource>
ccloud api-key use <api-key> --resource <resource>

1..10 |`
ForEach-Object -Process {-join ":"+(Get-TempRandom | convertto-json -Compress) |`
 ccloud kafka topic produce <topic> --cluster  <resource> }