# Make sure run PowerShell cmd with administrator

param (
    [Parameter(Mandatory = $true)]
    [string]$SubscriptionId,

    [Parameter(Mandatory = $true)]
    [string]$TenantId
)

# Disable warning for get access token
function Get-PlainAccessToken {
    $prev = $WarningPreference
    $WarningPreference = 'SilentlyContinue' 
    $token = (Get-AzAccessToken).Token
    $WarningPreference = $prev

    $plainToken = [Runtime.InteropServices.Marshal]::PtrToStringBSTR(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($token)
    ) 
    return $plainToken
}

function Get-AllLinkedServices {
    param (
        [string]$Uri
    )

    $allLinkedServices = New-Object System.Collections.ArrayList
    $nextUri = $Uri
    $token = Get-PlainAccessToken

    $headers = @{
        Authorization = "Bearer $token"
        "Content-Type" = "application/json"
    }

    while ($nextUri) {
        try {
            $response = Invoke-RestMethod -Method GET -Uri $nextUri -Headers $headers
        }
        catch {
            Write-Warning "Failed to fetch: $nextUri"
            break
        }

        if ($response.value -ne $null -and $response.value.Count -gt 0) {
            #Write-Host "   Retrieved $($response.value.Count) linked services"
            foreach ($item in $response.value) {
                [void]$allLinkedServices.Add($item)
            }
        }
        else {
            #Write-Warning "No linked services found in current batch."
            break
        }

        $nextUri = if ($response.PSObject.Properties.Name -contains 'nextLink') { $response.nextLink } else { $null }

        # # Optional: delay to avoid throttling
        # Start-Sleep -Milliseconds 300
    }

    return $allLinkedServices
}

function Print-Result {
    param (
        [string]$dfName,
        [string]$lsName,
        [string]$lsType,
        [string]$outputFileName
    )
    Write-Host "$dfName, $lsName, $lsType"


    # Output it to a file
    $data = [PSCustomObject]@{
        DataFactoryName = $dfName
        LinkedServiceName = $lsName
        Type = $lsType
    }
    $data | Export-Csv -Path $outputFileName -NoTypeInformation -Append
}

# Step 1: Install/Update Required Modules
Write-Host "Checking for Az modules..." -ForegroundColor Cyan
Install-Module -Name Az.DataFactory -Force -AllowClobber
Import-Module Az.DataFactory
# If you hit incompatible version, try Install-Module -Name Az.Accounts -RequiredVersion <your version>, and reopen the cmd

# Show the version in use
$module = Get-Module -Name Az.DataFactory
if ($module) {
    Write-Host "Az.DataFactory version in use: $($module.Version)" -ForegroundColor Green
} else {
    Write-Host "Az.DataFactory module not loaded." -ForegroundColor Red
}

# Step 2: Log into Azure
Write-Host "Connecting to Azure..." -ForegroundColor Cyan
Connect-AzAccount -ErrorAction Stop -subscription $SubscriptionId -tenantId $TenantId

# Step 3: Get all Data Factories in the subscription
Write-Host "Retrieving Data Factories..." -ForegroundColor Cyan
$dataFactories = Get-AzDataFactoryV2

if ($dataFactories.Count -eq 0) {
    Write-Host "No Data Factories found in subscription $SubscriptionId"
    exit
}

#Step 4: Found legacy linked services, we can extend this list
$LegacyV1LSTypes = @(
    # Disabled:
    "AmazonMWS",
    # End of support:
    "Zoho",
    "SalesforceMarketingCloud",
    "Phoenix",
    "PayPal",
    "OracleServiceCloud",
    "Responsys",
    "Eloqua",
    "Marketo",
    "Magento",
    "HBase",
    "Drill",
    "Couchbase",
    "Concur",
    "AzureMariaDB",
    "GoogleBigQuery",
    "PostgreSql", 
    "ServiceNow", 
    "Snowflake", 
    "Salesforce", 
    "SalesforceServiceCloud"
)

$LegacyV1VersionLSTypes = @(
    # End of support
    "MySql",
    "MariaDB",
    # V2 GA
    "Vertica", 
    "Oracle",
    "Greenplum",
    "AzurePostgreSql", 
    "Teradata", 
    "AmazonRdsForOracle", 
    "Hive", 
    "Impala",
    "Spark",
    "Presto",
    "Cassandra",
    # V2 Preview
    "QuickBooks"
)

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$filename = "output_$timestamp.csv"

foreach ($df in $dataFactories) {
    $resourceGroup = $df.ResourceGroupName
    $dataFactoryName = $df.DataFactoryName

    $uri = "https://management.azure.com/subscriptions/$SubscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.DataFactory/factories/$dataFactoryName/linkedservices?api-version=2018-06-01"

    try {
        $response = Invoke-AzRestMethod -Method GET -Uri $uri
        $linkedServices = Get-AllLinkedServices -Uri $uri


        if ($linkedServices.Count -eq 0) {
            continue
        }

        foreach ($ls in $linkedServices) {
            $name = $ls.name
            $type = $ls.properties.type
            $version = $ls.properties.version
            $typeProps = $ls.properties.typeProperties | ConvertTo-Json -Depth 10 | ConvertFrom-Json

            if ($LegacyV1LSTypes -contains $type) {
                Print-Result -dfName $dataFactoryName -lsName $name -lsType $type -outputFileName $filename
            }

            # Find v1 versions. 
            if ($LegacyV1VersionLSTypes -contains $type) {
                if ($version -ne "2.0") { # Skip version 2.0, it must be non-legacy
                    switch ($type) { # MySql and MariaDB are not following the version design, hence, need some custom logic here
                        {($_ -eq "MariaDB") -or ($_ -eq "MySql")} { 
                            $connectionString = $typeProps.connectionString
                            if (-not [string]::IsNullOrEmpty($connectionString)) {
                                Print-Result -dfName $dataFactoryName -lsName $name -lsType $type -outputFileName $filename
                            }
                            break
                        }
                        default {
                            Print-Result -dfName $dataFactoryName -lsName $name -lsType $type -outputFileName $filename
                            break
                        }
                    }
                }
            }
        }
    } catch {
        Write-Host "Failed to fetch linked services for $dataFactoryName" -ForegroundColor Red
    }
}

# Sometimes, Get-AzDataFactoryV2LinkedService will fail to deserialize the payload due to invalid payload, or out-of-date version, let's use Rest API instead
# foreach ($df in $dataFactories) {
#     $dataFactoryName = $df.DataFactoryName
#     $resourceGroup = $df.ResourceGroupName
#     Write-Host "Data Factory $dataFactoryName (Resource Group $resourceGroup)" -ForegroundColor Green

#     # Get all Linked Services for this Data Factory
#     $linkedServices = Get-AzDataFactoryV2LinkedService -ResourceGroupName $resourceGroup -DataFactoryName $dataFactoryName

#     if ($linkedServices.Count -gt 0) {
#         foreach ($ls in $linkedServices) {
#             $type = $ls.Properties.GetType().Name -replace 'LinkedService$', ''
#             # Find all the legacy types
#             if ($LegacyLSTypes -contains $type) {
#                 Write-Host "$dataFactoryName, $($ls.Name), $type"
#             }

#             # Find v1 versions. 
#             if ($LSWithV1Types -contains $type) {
#                 $version = $ls.Properties.version
#                 if ($version -ne "2.0") { # skip version 2.0
#                     switch ($type) { # We need some custom logic per types
#                         "MySql" {
#                             $connectionString = $ls.Properties.ConnectionString
#                             if (-not [string]::IsNullOrEmpty($connectionString)) {
#                                 Write-Host "$dataFactoryName, $($ls.Name), $type"
#                             }
#                             break
#                         }
#                         default {
#                             Write-Host "$dataFactoryName, $($ls.Name), $type"
#                             break
#                         }
#                     }
#                 }
#             }
            
#         }
#     }
# }
