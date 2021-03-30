#ENTRY POINT MAIN()
Param(
    [Parameter(Mandatory=$True)]
    [String]
    $oldVersion,

    [Parameter(Mandatory=$True)]
    [String]
    $newVersion,

    [Parameter(Mandatory=$True)]
    [String]
    $oldExtensionVersion,

    [Parameter(Mandatory=$True)]
    [String]
    $newExtensionVersion
)

function Update-HostVersion {
     param (
        $newVersion,
        $oldVersion,
        $Directory
    )
    
    $csprojFiles = Get-ChildItem  ./$Directory/*.csproj -rec
    foreach ($f in $csprojFiles)
    {
        $content = Get-Content $f.PSPath
        Write-Host "Updating version in $($f.PSPath) to $newVersion"
        
        $old = '"Confluent.Kafka" Version="'+ $oldVersion
        $new = '"Confluent.Kafka" Version="'+ $newVersion

        $newContent = $content | % { $_ -replace $old, $new }

        $old = '"Confluent.SchemaRegistry" Version="' + $oldVersion
        $new = '"Confluent.SchemaRegistry" Version="' + $newVersion

        $newContent = $newContent | % { $_ -replace $old, $new }

        $old = '"Confluent.SchemaRegistry.Serdes.Avro" Version="' + $oldVersion
        $new = '"Confluent.SchemaRegistry.Serdes.Avro" Version="' + $newVersion

        $newContent = $newContent | % { $_ -replace $old, $new }

        $old = '"Confluent.SchemaRegistry.Serdes.Protobuf" Version="' + $oldVersion
        $new = '"Confluent.SchemaRegistry.Serdes.Protobuf" Version="' + $newVersion
 
        $newContent = $newContent | % { $_ -replace $old, $new }

        Set-Content -Path $f.PSPath -Value $newContent
    }
}

function Update-ExtensionVersion{
    param(
        $oldExtensionVersion,
        $newExtensionVersion
    )
    $path = ".\build\common.props"
    $old = "<Version>" + $oldExtensionVersion
    $new = "<Version>" + $newExtensionVersion
    $newContent = Get-Content $path | % { $_ -replace $old, $new }
    Set-Content -Path $path -Value $newContent
}

$PSDefaultParameterValues['*:Encoding'] = 'utf8'

Update-HostVersion -oldVersion $oldVersion -newVersion $newVersion -Directory .
Update-ExtensionVersion -oldExtensionVersion $oldExtensionVersion -newExtensionVersion $newExtensionVersion
    