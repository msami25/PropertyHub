param(
    [string]$ApiBaseUrl = "http://localhost:8081",
    [string]$WebBaseUrl = "http://localhost:3000"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http

function Get-ContainerEnvironment {
    param([Parameter(Mandatory)][string]$ContainerName)

    $entries = & $script:DockerPath inspect $ContainerName --format "{{json .Config.Env}}" |
        ConvertFrom-Json
    $values = @{}
    foreach ($entry in $entries) {
        $separator = $entry.IndexOf("=")
        if ($separator -gt 0) {
            $values[$entry.Substring(0, $separator)] = $entry.Substring($separator + 1)
        }
    }
    return $values
}

function Set-ComposeEnvironment {
    param(
        [Parameter(Mandatory)]$ApiEnvironment,
        [Parameter(Mandatory)]$SqlEnvironment,
        [Parameter(Mandatory)]$WebEnvironment
    )

    $apiNames = @(
        "ConnectionStrings__DefaultConnection",
        "ImageStorage__RootPath",
        "OpenMeteo__BaseUrl",
        "OpenMeteo__TimeoutSeconds",
        "OpenMeteo__CacheMinutes",
        "Jwt__Issuer",
        "Jwt__Audience",
        "Jwt__SigningKey",
        "Jwt__AccessTokenMinutes",
        "SeedAdmin__Email",
        "SeedAdmin__Password",
        "SeedAdmin__FullName",
        "Cors__AllowedOrigins__0")
    foreach ($name in $apiNames) {
        if (-not $ApiEnvironment.ContainsKey($name)) {
            throw "The running API does not contain required configuration $name."
        }
        [Environment]::SetEnvironmentVariable($name, $ApiEnvironment[$name], "Process")
    }

    if (-not $SqlEnvironment.ContainsKey("MSSQL_SA_PASSWORD")) {
        throw "The running SQL Server does not contain its required password configuration."
    }
    [Environment]::SetEnvironmentVariable(
        "MSSQL_SA_PASSWORD",
        $SqlEnvironment["MSSQL_SA_PASSWORD"],
        "Process")
    [Environment]::SetEnvironmentVariable(
        "API_INTERNAL_BASE_URL",
        $WebEnvironment["API_INTERNAL_BASE_URL"],
        "Process")
    [Environment]::SetEnvironmentVariable(
        "VITE_PUBLIC_API_BASE_URL",
        "http://localhost:8081",
        "Process")
}

function Send-Json {
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][string]$Path,
        [object]$Body,
        [string]$Token
    )

    $request = [System.Net.Http.HttpRequestMessage]::new(
        [System.Net.Http.HttpMethod]::new($Method),
        "$ApiBaseUrl$Path")
    if ($Token) {
        $request.Headers.Authorization =
            [System.Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $Token)
    }
    if ($null -ne $Body) {
        $request.Content = [System.Net.Http.StringContent]::new(
            ($Body | ConvertTo-Json -Depth 10 -Compress),
            [System.Text.Encoding]::UTF8,
            "application/json")
    }

    try {
        $response = $script:HttpClient.SendAsync($request).GetAwaiter().GetResult()
        $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        return [pscustomobject]@{
            Status = [int]$response.StatusCode
            Content = $content
            Json = if ($content) { $content | ConvertFrom-Json } else { $null }
        }
    }
    finally {
        $request.Dispose()
    }
}

function Assert-Status {
    param(
        [Parameter(Mandatory)]$Response,
        [Parameter(Mandatory)][int]$Expected,
        [Parameter(Mandatory)][string]$Step
    )

    if ($Response.Status -ne $Expected) {
        throw "$Step expected HTTP $Expected but received $($Response.Status)."
    }
}

function Get-MountName {
    param(
        [Parameter(Mandatory)][string]$ContainerName,
        [Parameter(Mandatory)][string]$Destination
    )

    $mounts = & $script:DockerPath inspect $ContainerName --format "{{json .Mounts}}" |
        ConvertFrom-Json
    return ($mounts | Where-Object { $_.Destination -eq $Destination }).Name
}

function Wait-ForHealthyContainer {
    param([Parameter(Mandatory)][string]$ContainerName)

    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        $state = & $script:DockerPath inspect $ContainerName `
            --format "{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}"
        if ($state -eq "healthy" -or $state -eq "running") {
            return
        }
        Start-Sleep -Seconds 2
    }
    throw "$ContainerName did not become healthy after the Compose restart."
}

function Get-Sha256 {
    param([Parameter(Mandatory)][byte[]]$Bytes)

    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [BitConverter]::ToString($algorithm.ComputeHash($Bytes)).Replace("-", "")
    }
    finally {
        $algorithm.Dispose()
    }
}

$docker = Get-Command docker -ErrorAction Stop
$script:DockerPath = $docker.Source
$script:HttpClient = [System.Net.Http.HttpClient]::new()
$script:HttpClient.Timeout = [TimeSpan]::FromSeconds(15)

try {
    $apiEnvironment = Get-ContainerEnvironment "propertyhub-api-1"
    $sqlEnvironment = Get-ContainerEnvironment "propertyhub-sqlserver-1"
    $webEnvironment = Get-ContainerEnvironment "propertyhub-web-1"
    Set-ComposeEnvironment $apiEnvironment $sqlEnvironment $webEnvironment

    $adminLogin = Send-Json -Method POST -Path "/api/auth/login" -Body @{
        email = $apiEnvironment["SeedAdmin__Email"]
        password = $apiEnvironment["SeedAdmin__Password"]
    }
    Assert-Status $adminLogin 200 "Admin login before restart"
    $adminToken = $adminLogin.Json.accessToken

    $dashboardBefore = Send-Json -Method GET -Path "/api/admin/dashboard" -Token $adminToken
    Assert-Status $dashboardBefore 200 "Dashboard before restart"
    $publicList = Send-Json -Method GET -Path "/api/properties?page=1&pageSize=50"
    Assert-Status $publicList 200 "Public properties before restart"
    $evidenceProperty = @($publicList.Json.items | Where-Object { $_.primaryImageUrl }) |
        Select-Object -First 1
    if (-not $evidenceProperty) {
        throw "Persistence UAT requires one approved, available property with an image."
    }

    $propertyBefore = Send-Json -Method GET -Path "/api/properties/$($evidenceProperty.id)"
    Assert-Status $propertyBefore 200 "Property before restart"
    $weatherBefore = Send-Json -Method GET `
        -Path "/api/properties/$($evidenceProperty.id)/weather"
    Assert-Status $weatherBefore 200 "Weather before restart"
    $imageUrl = $propertyBefore.Json.images[0].url
    $imageBytesBefore = $script:HttpClient.GetByteArrayAsync(
        "$ApiBaseUrl$imageUrl").GetAwaiter().GetResult()
    $imageHashBefore = Get-Sha256 $imageBytesBefore

    $sqlVolumeBefore = Get-MountName "propertyhub-sqlserver-1" "/var/opt/mssql"
    $uploadVolumeBefore = Get-MountName "propertyhub-api-1" "/app/uploads"
    if (-not $sqlVolumeBefore -or -not $uploadVolumeBefore) {
        throw "Expected named SQL Server and upload volumes were not found."
    }

    & $script:DockerPath compose restart
    if ($LASTEXITCODE -ne 0) {
        throw "Docker Compose restart failed."
    }
    Wait-ForHealthyContainer "propertyhub-sqlserver-1"
    Wait-ForHealthyContainer "propertyhub-api-1"
    Wait-ForHealthyContainer "propertyhub-web-1"

    $apiReady = Invoke-WebRequest -UseBasicParsing "$ApiBaseUrl/health/ready"
    $webReady = Invoke-WebRequest -UseBasicParsing "$WebBaseUrl/health"
    if ($apiReady.StatusCode -ne 200 -or $webReady.StatusCode -ne 200) {
        throw "API and web health checks did not recover after restart."
    }

    $adminLoginAfter = Send-Json -Method POST -Path "/api/auth/login" -Body @{
        email = $apiEnvironment["SeedAdmin__Email"]
        password = $apiEnvironment["SeedAdmin__Password"]
    }
    Assert-Status $adminLoginAfter 200 "Admin login after restart"
    $dashboardAfter = Send-Json `
        -Method GET `
        -Path "/api/admin/dashboard" `
        -Token $adminLoginAfter.Json.accessToken
    Assert-Status $dashboardAfter 200 "Dashboard after restart"
    $propertyAfter = Send-Json -Method GET -Path "/api/properties/$($evidenceProperty.id)"
    Assert-Status $propertyAfter 200 "Property after restart"
    $weatherAfter = Send-Json -Method GET `
        -Path "/api/properties/$($evidenceProperty.id)/weather"
    Assert-Status $weatherAfter 200 "Weather after restart"
    $imageBytesAfter = $script:HttpClient.GetByteArrayAsync(
        "$ApiBaseUrl$imageUrl").GetAwaiter().GetResult()
    $imageHashAfter = Get-Sha256 $imageBytesAfter

    $ssrAfter = Invoke-WebRequest -UseBasicParsing `
        "$WebBaseUrl/properties/$($evidenceProperty.id)"
    if ($ssrAfter.StatusCode -ne 200 -or
        -not $ssrAfter.Content.Contains($propertyAfter.Json.title) -or
        $ssrAfter.Content.Contains('"contactNumber"') -or
        $ssrAfter.Content.Contains('"accessToken"')) {
        throw "The post-restart SSR response failed its public-data safety checks."
    }

    $sqlVolumeAfter = Get-MountName "propertyhub-sqlserver-1" "/var/opt/mssql"
    $uploadVolumeAfter = Get-MountName "propertyhub-api-1" "/app/uploads"
    if ($sqlVolumeAfter -ne $sqlVolumeBefore -or $uploadVolumeAfter -ne $uploadVolumeBefore) {
        throw "A named volume changed during the Compose restart."
    }
    if ($imageHashAfter -ne $imageHashBefore) {
        throw "The persisted image changed during the Compose restart."
    }
    if ($dashboardAfter.Json.users.total -ne $dashboardBefore.Json.users.total -or
        $dashboardAfter.Json.properties.total -ne $dashboardBefore.Json.properties.total -or
        $dashboardAfter.Json.totalCities -ne $dashboardBefore.Json.totalCities) {
        throw "Live database metrics changed unexpectedly during the Compose restart."
    }

    [pscustomobject]@{
        ApiHealth = $apiReady.StatusCode
        WebHealth = $webReady.StatusCode
        PropertyBeforeRestart = $propertyBefore.Status
        PropertyAfterRestart = $propertyAfter.Status
        ImageHashPreserved = $imageHashAfter -eq $imageHashBefore
        WeatherAfterRestart = $weatherAfter.Status
        SsrAfterRestart = $ssrAfter.StatusCode
        UserCountPreserved = $dashboardAfter.Json.users.total
        PropertyCountPreserved = $dashboardAfter.Json.properties.total
        CityCountPreserved = $dashboardAfter.Json.totalCities
        SqlVolume = $sqlVolumeAfter
        UploadVolume = $uploadVolumeAfter
    } | Format-List
}
finally {
    $script:HttpClient.Dispose()
}
