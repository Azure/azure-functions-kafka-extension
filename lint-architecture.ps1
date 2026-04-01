#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Architectural linter for the Azure Functions Kafka Extension.
    Validates dependency direction, public API stability, and invariants defined in Architecture.md.

.DESCRIPTION
    This script enforces architectural constraints to prevent coding agents and developers
    from accidentally introducing breaking changes. It analyzes C# source files and validates:
    
    - Layer dependency direction rules (no upward/cross-layer references)
    - Confluent.Kafka isolation (only approved classes use Confluent types)
    - Public API surface stability (no removals/renames of public types)
    - Enum value stability
    - Attribute constructor stability
    - Channel contract (FunctionExecutorBase configuration)
    - KafkaTriggerMetrics contract properties

.PARAMETER Path
    Path to the Kafka Extension source directory. Defaults to src/Microsoft.Azure.WebJobs.Extensions.Kafka/

.PARAMETER Strict
    If set, treat warnings as errors.

.EXAMPLE
    ./lint-architecture.ps1
    ./lint-architecture.ps1 -Strict
#>

[CmdletBinding()]
param(
    [string]$Path = (Join-Path $PSScriptRoot "src" "Microsoft.Azure.WebJobs.Extensions.Kafka"),
    [switch]$Strict
)

$ErrorCount = 0
$WarningCount = 0

function Write-LintError {
    param([string]$Rule, [string]$File, [int]$Line, [string]$Message)
    $script:ErrorCount++
    $relativePath = $File.Replace($Path, "").TrimStart('\', '/')
    Write-Host "ERROR [$Rule] $relativePath" -ForegroundColor Red -NoNewline
    if ($Line -gt 0) { Write-Host ":$Line" -ForegroundColor Red -NoNewline }
    Write-Host " - $Message" -ForegroundColor Red
}

function Write-LintWarning {
    param([string]$Rule, [string]$File, [int]$Line, [string]$Message)
    $script:WarningCount++
    $relativePath = $File.Replace($Path, "").TrimStart('\', '/')
    Write-Host "WARN  [$Rule] $relativePath" -ForegroundColor Yellow -NoNewline
    if ($Line -gt 0) { Write-Host ":$Line" -ForegroundColor Yellow -NoNewline }
    Write-Host " - $Message" -ForegroundColor Yellow
}

# ==============================================================================
# Classify files into layers
# ==============================================================================

function Get-Layer {
    param([string]$FilePath)
    $rel = $FilePath.Replace($Path, "").TrimStart('\', '/').Replace('\', '/')
    
    if ($rel -match "^Trigger/") { return "Trigger" }
    if ($rel -match "^Output/") { return "Output" }
    if ($rel -match "^Listeners/") { return "Listener" }
    if ($rel -match "^Config/") { return "Config" }
    if ($rel -match "^Serialization/") { return "Serialization" }
    if ($rel -match "^Diagnostics/") { return "Diagnostics" }
    if ($rel -match "^Extensions/") { return "Util" }
    if ($rel -match "^Properties/") { return "Util" }
    
    # Root-level files
    if ($rel -match "KafkaWebJobsStartup") { return "Bootstrap" }
    if ($rel -match "IKafkaEventData|KafkaEventData|Header") { return "DataModel" }
    
    return "Root"
}

# ==============================================================================
# RULE 1: Dependency Direction (Dynamic Discovery)
# ==============================================================================

# Forbidden dependency matrix: source layer → list of forbidden target layers.
# If a file in "Trigger" references a type defined in "Output", it's a violation.
$ForbiddenDependencies = @{
    "Trigger"       = @("Output")
    "Output"        = @("Trigger", "Listener")
    "DataModel"     = @("Trigger", "Output", "Listener", "Config", "Bootstrap", "Serialization")
    "Serialization" = @("Trigger", "Output", "Listener", "Config", "Bootstrap")
}

function Build-TypeLayerMap {
    <#
    .SYNOPSIS
        Pass 1: Scan all .cs files and build a hashtable mapping type names to their layer.
        Discovers class, interface, enum, and struct declarations dynamically.
    #>
    $map = @{}
    $csFiles = Get-ChildItem -Path $Path -Filter "*.cs" -Recurse
    
    foreach ($file in $csFiles) {
        $layer = Get-Layer $file.FullName
        $content = Get-Content $file.FullName -Raw
        
        # Match: public/internal/private [sealed|abstract|static] class/interface/enum/struct Name[<T>]
        $pattern = '(?:public|internal|private|protected)\s+(?:sealed\s+|abstract\s+|static\s+|partial\s+)*(?:class|interface|enum|struct)\s+(\w+)'
        $matches = [regex]::Matches($content, $pattern)
        
        foreach ($m in $matches) {
            $typeName = $m.Groups[1].Value
            if (-not $map.ContainsKey($typeName)) {
                $map[$typeName] = $layer
            }
        }
    }
    
    return $map
}

function Test-DependencyDirection {
    Write-Host "`n=== RULE: Dependency Direction (dynamic) ===" -ForegroundColor Cyan
    
    # Pass 1: Build type → layer map from actual source
    $typeLayerMap = Build-TypeLayerMap
    
    $typeCount = $typeLayerMap.Count
    Write-Host "  Discovered $typeCount types across layers" -ForegroundColor Gray
    
    # Pre-compute: for each source layer, build a regex matching all forbidden type names
    $forbiddenRegexByLayer = @{}
    foreach ($srcLayer in $ForbiddenDependencies.Keys) {
        $forbiddenLayers = $ForbiddenDependencies[$srcLayer]
        $forbiddenTypes = @()
        foreach ($entry in $typeLayerMap.GetEnumerator()) {
            if ($entry.Value -in $forbiddenLayers) {
                $forbiddenTypes += $entry.Key
            }
        }
        if ($forbiddenTypes.Count -gt 0) {
            # Sort longest first to avoid partial matches, then build alternation
            $sorted = $forbiddenTypes | Sort-Object { $_.Length } -Descending
            $forbiddenRegexByLayer[$srcLayer] = "\b(" + ($sorted -join "|") + ")\b"
        }
    }
    
    # Pass 2: Check each file for forbidden type references
    $csFiles = Get-ChildItem -Path $Path -Filter "*.cs" -Recurse
    
    foreach ($file in $csFiles) {
        $layer = Get-Layer $file.FullName
        
        # Only check layers that have forbidden dependencies
        if (-not $forbiddenRegexByLayer.ContainsKey($layer)) { continue }
        
        $regex = $forbiddenRegexByLayer[$layer]
        $lines = Get-Content $file.FullName
        $ownTypesInFile = @()
        
        # Collect types declared in this file (to skip self-references)
        $content = Get-Content $file.FullName -Raw
        $declPattern = '(?:public|internal|private|protected)\s+(?:sealed\s+|abstract\s+|static\s+|partial\s+)*(?:class|interface|enum|struct)\s+(\w+)'
        $declMatches = [regex]::Matches($content, $declPattern)
        foreach ($dm in $declMatches) {
            $ownTypesInFile += $dm.Groups[1].Value
        }
        
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            
            # Skip comments and using directives (using Confluent.Kafka is checked separately)
            $trimmed = $line.TrimStart()
            if ($trimmed.StartsWith("//") -or $trimmed.StartsWith("*") -or $trimmed.StartsWith("/*")) { continue }
            if ($trimmed.StartsWith("using ") -and $trimmed -notmatch "using\s+\w+\s*=") { continue }
            # Skip XML doc comments
            if ($trimmed.StartsWith("///")) { continue }
            # Skip string literals (rough heuristic: lines that are just string content)
            if ($trimmed.StartsWith('"')) { continue }
            
            $refMatches = [regex]::Matches($line, $regex)
            foreach ($rm in $refMatches) {
                $refType = $rm.Groups[1].Value
                
                # Skip self-references (type declared in this same file)
                if ($refType -in $ownTypesInFile) { continue }
                
                $refLayer = $typeLayerMap[$refType]
                $forbiddenLayers = $ForbiddenDependencies[$layer]
                
                Write-LintError "DEP-001" $file.FullName ($i + 1) "[$layer] references '$refType' from [$refLayer]. Forbidden: $layer -> $($forbiddenLayers -join ', ')."
            }
        }
    }
}

# ==============================================================================
# RULE 2: Confluent.Kafka Isolation
# ==============================================================================

function Test-ConfluentKafkaIsolation {
    Write-Host "`n=== RULE: Confluent.Kafka Isolation ===" -ForegroundColor Cyan
    
    # Files allowed to reference Confluent.Kafka
    # These are the integration points with the Confluent SDK.
    # Adding new files here requires architectural review.
    $allowedFiles = @(
        # Listener & Consumer
        "KafkaListener.cs",
        # Scaler (Listeners/Scaler/)
        "KafkaGenericTopicScaler.cs",
        "KafkaGenericTargetScaler.cs",
        "KafkaObjectTopicScaler.cs",
        "KafkaObjectTargetScaler.cs",
        "KafkaMetricsProvider.cs",
        "KafkaScalerProvider.cs",
        # Producer
        "KafkaProducerFactory.cs",
        "KafkaProducer.cs",
        "KafkaMessageBuilder.cs",
        # Data Models (wrapping ConsumeResult/Headers)
        "KafkaEventData.cs",
        "KafkaEventDataHeaders.cs",
        "KafkaEventDataHeader.cs",
        # Serialization
        "SerializationHelper.cs",
        "ProtobufDeserializer.cs",
        "ProtobufSerializer.cs",
        "LocalSchemaRegistry.cs",
        # Offset commit (consumer.StoreOffset)
        "AsyncCommitStrategy.cs",
        "ICommitStrategy.cs",
        # Config & Enums (maps to Confluent types)
        "KafkaListenerConfiguration.cs",
        "BrokerAuthenticationMode.cs",
        "BrokerProtocol.cs",
        "KafkaOptions.cs",
        "KafkaExtensionConfigProvider.cs",
        "AzureFunctionsFileHelper.cs",
        "ConfigurationExtensions.cs",
        "KafkaWebJobsBuilderExtensions.cs",
        # Attributes (reference Confluent types for SaslMechanism, etc.)
        "KafkaTriggerAttribute.cs",
        "KafkaAttribute.cs",
        "IKafkaProducer.cs",
        # Binding providers (need Confluent types for generic type resolution)
        "KafkaTriggerAttributeBindingProvider.cs",
        "KafkaAttributeBindingProvider.cs",
        "KafkaAttributeBinding.cs",
        "AsyncCollectorArgumentBindingProvider.cs",
        # Executors (need TopicPartitionOffset for offset tracking)
        "FunctionExecutorBase.cs",
        "SingleItemFunctionExecutor.cs",
        "MultipleItemFunctionExecutor.cs"
    )
    
    $csFiles = Get-ChildItem -Path $Path -Filter "*.cs" -Recurse
    
    foreach ($file in $csFiles) {
        $fileName = $file.Name
        $lines = Get-Content $file.FullName
        
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            
            # Check for Confluent.Kafka using directives
            if ($line -match "^\s*using\s+Confluent\.(Kafka|SchemaRegistry)") {
                if ($fileName -notin $allowedFiles) {
                    Write-LintError "CFK-001" $file.FullName ($i + 1) "Confluent.Kafka reference in non-approved file '$fileName'. See Architecture.md CONSTRAINT-2 for the allow list."
                }
            }
        }
    }
}

# ==============================================================================
# RULE 3: Public API Surface Stability
# ==============================================================================

function Test-PublicAPISurface {
    Write-Host "`n=== RULE: Public API Surface ===" -ForegroundColor Cyan
    
    # Tier 1: User-facing public types that MUST exist
    $requiredPublicTypes = @(
        @{ Name = "KafkaTriggerAttribute"; File = "Trigger/KafkaTriggerAttribute.cs"; Kind = "class" },
        @{ Name = "KafkaAttribute"; File = "Output/KafkaAttribute.cs"; Kind = "class" },
        @{ Name = "IKafkaEventData"; File = "IKafkaEventData.cs"; Kind = "interface" },
        @{ Name = "KafkaEventData"; File = "KafkaEventData.cs"; Kind = "class" },
        @{ Name = "IKafkaEventDataHeaders"; File = "IKafkaEventDataHeaders.cs"; Kind = "interface" },
        @{ Name = "IKafkaEventDataHeader"; File = "IKafkaEventDataHeader.cs"; Kind = "interface" },
        @{ Name = "KafkaEventDataHeaders"; File = "KafkaEventDataHeaders.cs"; Kind = "class" },
        @{ Name = "KafkaOptions"; File = "Config/KafkaOptions.cs"; Kind = "class" },
        @{ Name = "BrokerAuthenticationMode"; File = "Config/BrokerAuthenticationMode.cs"; Kind = "enum" },
        @{ Name = "BrokerProtocol"; File = "Config/BrokerProtocol.cs"; Kind = "enum" },
        @{ Name = "IKafkaProducer"; File = "Output/IKafkaProducer.cs"; Kind = "interface" },
        @{ Name = "IKafkaProducerFactory"; File = "Output/IKafkaProducerFactory.cs"; Kind = "interface" }
    )
    
    # Tier 2: Host/ScaleController contract types
    $requiredHostTypes = @(
        @{ Name = "KafkaTriggerMetrics"; File = "Trigger/KafkaTriggerMetrics.cs"; Kind = "class" },
        @{ Name = "KafkaWebJobsStartup"; File = "KafkaWebJobsStartup.cs"; Kind = "class" },
        @{ Name = "KafkaExtensionConfigProvider"; File = "Config/KafkaExtensionConfigProvider.cs"; Kind = "class" },
        @{ Name = "KafkaWebJobsBuilderExtensions"; File = "Config/KafkaWebJobsBuilderExtensions.cs"; Kind = "class" }
    )
    
    foreach ($type in ($requiredPublicTypes + $requiredHostTypes)) {
        $filePath = Join-Path $Path $type.File
        if (-not (Test-Path $filePath)) {
            Write-LintError "API-001" $filePath 0 "Required public type '$($type.Name)' file is missing. This is a breaking change."
            continue
        }
        
        $content = Get-Content $filePath -Raw
        $kind = $type.Kind
        $name = $type.Name
        
        if ($content -notmatch "public\s+(sealed\s+|abstract\s+|static\s+)?($kind)\s+$name") {
            Write-LintError "API-002" $filePath 0 "Type '$name' is not declared as public $kind. Changing visibility is a breaking change."
        }
    }
}

# ==============================================================================
# RULE 4: KafkaTriggerMetrics Contract
# ==============================================================================

function Test-MetricsContract {
    Write-Host "`n=== RULE: KafkaTriggerMetrics Contract ===" -ForegroundColor Cyan
    
    $metricsFile = Join-Path $Path "Trigger" "KafkaTriggerMetrics.cs"
    if (-not (Test-Path $metricsFile)) {
        Write-LintError "MET-001" $metricsFile 0 "KafkaTriggerMetrics.cs is missing. Scale Controller depends on this class."
        return
    }
    
    $content = Get-Content $metricsFile -Raw
    
    # Must extend ScaleMetrics
    if ($content -notmatch "class\s+KafkaTriggerMetrics\s*:\s*ScaleMetrics") {
        Write-LintError "MET-002" $metricsFile 0 "KafkaTriggerMetrics must inherit from ScaleMetrics. Scale Controller contract."
    }
    
    # Must have TotalLag property
    if ($content -notmatch "public\s+long\s+TotalLag\s*\{") {
        Write-LintError "MET-003" $metricsFile 0 "KafkaTriggerMetrics.TotalLag (long) property is missing or has wrong type. Scale Controller contract."
    }
    
    # Must have PartitionCount property
    if ($content -notmatch "public\s+long\s+PartitionCount\s*\{") {
        Write-LintError "MET-004" $metricsFile 0 "KafkaTriggerMetrics.PartitionCount (long) property is missing or has wrong type. Scale Controller contract."
    }
}

# ==============================================================================
# RULE 5: Enum Value Stability
# ==============================================================================

function Test-EnumStability {
    Write-Host "`n=== RULE: Enum Value Stability ===" -ForegroundColor Cyan
    
    # BrokerAuthenticationMode required values
    $authModeFile = Join-Path $Path "Config" "BrokerAuthenticationMode.cs"
    if (Test-Path $authModeFile) {
        $content = Get-Content $authModeFile -Raw
        $requiredValues = @("NotSet", "Gssapi", "Plain", "ScramSha256", "ScramSha512")
        foreach ($val in $requiredValues) {
            if ($content -notmatch "\b$val\b") {
                Write-LintError "ENUM-001" $authModeFile 0 "BrokerAuthenticationMode is missing enum value '$val'. Removing enum values is a breaking change (cross-repo: language workers, function.json)."
            }
        }
    }
    
    # BrokerProtocol required values
    $protocolFile = Join-Path $Path "Config" "BrokerProtocol.cs"
    if (Test-Path $protocolFile) {
        $content = Get-Content $protocolFile -Raw
        $requiredValues = @("NotSet", "Plaintext", "Ssl", "SaslPlaintext", "SaslSsl")
        foreach ($val in $requiredValues) {
            if ($content -notmatch "\b$val\b") {
                Write-LintError "ENUM-002" $protocolFile 0 "BrokerProtocol is missing enum value '$val'. Removing enum values is a breaking change (cross-repo: language workers, function.json)."
            }
        }
    }
}

# ==============================================================================
# RULE 6: Attribute Constructor Stability
# ==============================================================================

function Test-AttributeConstructors {
    Write-Host "`n=== RULE: Attribute Constructor Stability ===" -ForegroundColor Cyan
    
    # KafkaTriggerAttribute must have (string brokerList, string topic, string consumerGroup) constructor
    $triggerAttrFile = Join-Path $Path "Trigger" "KafkaTriggerAttribute.cs"
    if (Test-Path $triggerAttrFile) {
        $content = Get-Content $triggerAttrFile -Raw
        if ($content -notmatch "KafkaTriggerAttribute\s*\(\s*string\s+\w+\s*,\s*string\s+\w+\s*\)") {
            Write-LintError "ATTR-001" $triggerAttrFile 0 "KafkaTriggerAttribute is missing required constructor (string brokerList, string topic). Changing constructors is a binary breaking change."
        }
    }
    
    # KafkaAttribute must have (string brokerList, string topic) constructor
    $outputAttrFile = Join-Path $Path "Output" "KafkaAttribute.cs"
    if (Test-Path $outputAttrFile) {
        $content = Get-Content $outputAttrFile -Raw
        if ($content -notmatch "KafkaAttribute\s*\(\s*string\s+\w+\s*,\s*string\s+\w+\s*\)") {
            Write-LintError "ATTR-002" $outputAttrFile 0 "KafkaAttribute is missing required constructor (string, string). Changing constructors is a binary breaking change."
        }
        # Must also have parameterless constructor
        if ($content -notmatch "KafkaAttribute\s*\(\s*\)") {
            Write-LintError "ATTR-003" $outputAttrFile 0 "KafkaAttribute is missing parameterless constructor. Removing constructors is a binary breaking change."
        }
    }
}

# ==============================================================================
# RULE 7: IKafkaEventData Interface Properties
# ==============================================================================

function Test-EventDataInterface {
    Write-Host "`n=== RULE: IKafkaEventData Interface ===" -ForegroundColor Cyan
    
    $interfaceFile = Join-Path $Path "IKafkaEventData.cs"
    if (-not (Test-Path $interfaceFile)) {
        Write-LintError "EVT-001" $interfaceFile 0 "IKafkaEventData.cs is missing."
        return
    }
    
    $content = Get-Content $interfaceFile -Raw
    $requiredProperties = @(
        @{ Name = "Value"; Type = "object" },
        @{ Name = "Key"; Type = "object" },
        @{ Name = "Offset"; Type = "long" },
        @{ Name = "Partition"; Type = "int" },
        @{ Name = "Topic"; Type = "string" },
        @{ Name = "Timestamp"; Type = "DateTime" },
        @{ Name = "LeaderEpoch"; Type = "int\?" },
        @{ Name = "IsPartitionEOF"; Type = "bool" }
    )
    
    foreach ($prop in $requiredProperties) {
        if ($content -notmatch "$($prop.Type)\s+$($prop.Name)\s*\{") {
            Write-LintError "EVT-002" $interfaceFile 0 "IKafkaEventData is missing property '$($prop.Type) $($prop.Name)'. This is a user-facing API breaking change."
        }
    }
    
    # Must have Headers property
    if ($content -notmatch "IKafkaEventDataHeaders\s+Headers\s*\{") {
        Write-LintError "EVT-003" $interfaceFile 0 "IKafkaEventData is missing 'IKafkaEventDataHeaders Headers' property."
    }
}

# ==============================================================================
# RULE 8: WebJobsStartup Assembly Attribute
# ==============================================================================

function Test-WebJobsStartup {
    Write-Host "`n=== RULE: WebJobsStartup Assembly Attribute ===" -ForegroundColor Cyan
    
    $startupFile = Join-Path $Path "KafkaWebJobsStartup.cs"
    if (-not (Test-Path $startupFile)) {
        Write-LintError "WJS-001" $startupFile 0 "KafkaWebJobsStartup.cs is missing. Host cannot discover the extension."
        return
    }
    
    $content = Get-Content $startupFile -Raw
    
    if ($content -notmatch "\[assembly:\s*WebJobsStartup\s*\(\s*typeof\s*\(\s*KafkaWebJobsStartup\s*\)\s*\)\s*\]") {
        Write-LintError "WJS-002" $startupFile 0 "[assembly: WebJobsStartup(typeof(KafkaWebJobsStartup))] attribute is missing. Host will not load the extension."
    }
    
    if ($content -notmatch "class\s+KafkaWebJobsStartup\s*:\s*IWebJobsStartup") {
        Write-LintError "WJS-003" $startupFile 0 "KafkaWebJobsStartup must implement IWebJobsStartup."
    }
}

# ==============================================================================
# RULE 9: Channel Contract
# ==============================================================================

function Test-ChannelContract {
    Write-Host "`n=== RULE: Channel Contract ===" -ForegroundColor Cyan
    
    $executorFile = Join-Path $Path "Trigger" "FunctionExecutorBase.cs"
    if (-not (Test-Path $executorFile)) {
        Write-LintError "CHN-001" $executorFile 0 "FunctionExecutorBase.cs is missing."
        return
    }
    
    $content = Get-Content $executorFile -Raw
    
    # Verify Channel configuration
    if ($content -match "Channel\.CreateBounded" -or $content -match "CreateBounded") {
        if ($content -match "SingleReader\s*=\s*false") {
            Write-LintError "CHN-002" $executorFile 0 "Channel SingleReader must be true. Multiple readers break the processing pipeline."
        }
        if ($content -match "SingleWriter\s*=\s*false") {
            Write-LintError "CHN-003" $executorFile 0 "Channel SingleWriter must be true. Multiple writers break the processing pipeline."
        }
    }
}

# ==============================================================================
# RULE 10: No New Assembly-Level InternalsVisibleTo (except test projects)
# ==============================================================================

function Test-InternalsVisibleTo {
    Write-Host "`n=== RULE: InternalsVisibleTo ===" -ForegroundColor Cyan
    
    $csFiles = Get-ChildItem -Path $Path -Filter "*.cs" -Recurse
    
    foreach ($file in $csFiles) {
        $lines = Get-Content $file.FullName
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            if ($line -match "\[assembly:\s*InternalsVisibleTo\s*\(") {
                if ($line -notmatch "UnitTests|EndToEndTests|Tests\.Common|DynamicProxyGenAssembly2") {
                    Write-LintWarning "IVT-001" $file.FullName ($i + 1) "InternalsVisibleTo for non-test assembly detected. Internal APIs exposed to external assemblies become hard to change."
                }
            }
        }
    }
}

# ==============================================================================
# RULE 11: Target Framework
# ==============================================================================

function Test-TargetFramework {
    Write-Host "`n=== RULE: Target Framework ===" -ForegroundColor Cyan
    
    $csprojFile = Join-Path $Path "Microsoft.Azure.WebJobs.Extensions.Kafka.csproj"
    if (-not (Test-Path $csprojFile)) {
        Write-LintError "TFM-001" $csprojFile 0 "Project file is missing."
        return
    }
    
    $content = Get-Content $csprojFile -Raw
    
    if ($content -notmatch "<TargetFramework>netstandard2\.0</TargetFramework>") {
        Write-LintWarning "TFM-002" $csprojFile 0 "Target framework is not netstandard2.0. Changing TFM affects all consumers (Host, Scale Controller, Workers). Requires coordinated release."
    }
}

# ==============================================================================
# RULE 12: AddKafkaScaleForTrigger Method Existence
# ==============================================================================

function Test-ScaleControllerEntryPoint {
    Write-Host "`n=== RULE: Scale Controller Entry Point ===" -ForegroundColor Cyan
    
    $extensionsFile = Join-Path $Path "Config" "KafkaWebJobsBuilderExtensions.cs"
    if (-not (Test-Path $extensionsFile)) {
        Write-LintError "SC-001" $extensionsFile 0 "KafkaWebJobsBuilderExtensions.cs is missing."
        return
    }
    
    $content = Get-Content $extensionsFile -Raw
    
    if ($content -notmatch "AddKafka\b") {
        Write-LintError "SC-002" $extensionsFile 0 "AddKafka() method is missing. Scale Controller and Host both call this."
    }
    
    # AddKafkaScaleForTrigger is called by Scale Controller via reflection.
    # It may be defined in a different file or added in a newer version.
    # Check if it exists anywhere in the source directory.
    $allCsContent = Get-ChildItem -Path $Path -Filter "*.cs" -Recurse | ForEach-Object { Get-Content $_.FullName -Raw }
    $hasScaleMethod = $allCsContent | Where-Object { $_ -match "AddKafkaScaleForTrigger" }
    if (-not $hasScaleMethod) {
        Write-LintWarning "SC-003" $extensionsFile 0 "AddKafkaScaleForTrigger() method not found in source. Scale Controller invokes this via reflection. Verify it is provided by the extension or host integration."
    }
}

# ==============================================================================
# Execute all rules
# ==============================================================================

Write-Host "============================================" -ForegroundColor White
Write-Host "  Kafka Extension Architecture Linter" -ForegroundColor White
Write-Host "  Source: $Path" -ForegroundColor Gray
Write-Host "============================================" -ForegroundColor White

if (-not (Test-Path $Path)) {
    Write-Host "ERROR: Source directory not found: $Path" -ForegroundColor Red
    exit 1
}

Test-DependencyDirection
Test-ConfluentKafkaIsolation
Test-PublicAPISurface
Test-MetricsContract
Test-EnumStability
Test-AttributeConstructors
Test-EventDataInterface
Test-WebJobsStartup
Test-ChannelContract
Test-InternalsVisibleTo
Test-TargetFramework
Test-ScaleControllerEntryPoint

# ==============================================================================
# Summary
# ==============================================================================

Write-Host "`n============================================" -ForegroundColor White
Write-Host "  Results" -ForegroundColor White
Write-Host "============================================" -ForegroundColor White
Write-Host "  Errors:   $ErrorCount" -ForegroundColor $(if ($ErrorCount -gt 0) { "Red" } else { "Green" })
Write-Host "  Warnings: $WarningCount" -ForegroundColor $(if ($WarningCount -gt 0) { "Yellow" } else { "Green" })

if ($ErrorCount -gt 0) {
    Write-Host "`nFAILED: $ErrorCount architectural violation(s) detected." -ForegroundColor Red
    exit 1
}

if ($Strict -and $WarningCount -gt 0) {
    Write-Host "`nFAILED (strict mode): $WarningCount warning(s) treated as errors." -ForegroundColor Red
    exit 1
}

Write-Host "`nPASSED: All architectural constraints satisfied." -ForegroundColor Green
exit 0
