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

function Send-ApiRequest {
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][string]$Path,
        [string]$Token,
        [object]$Body,
        [string]$Version
    )

    $request = [System.Net.Http.HttpRequestMessage]::new(
        [System.Net.Http.HttpMethod]::new($Method),
        "$ApiBaseUrl$Path")
    if ($Token) {
        $request.Headers.Authorization =
            [System.Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $Token)
    }
    if ($Version) {
        [void]$request.Headers.TryAddWithoutValidation("If-Match", "`"$Version`"")
    }
    if ($null -ne $Body) {
        $json = $Body | ConvertTo-Json -Depth 10 -Compress
        $request.Content = [System.Net.Http.StringContent]::new(
            $json,
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
        throw "$Step expected HTTP $Expected but received $($Response.Status). Body: $($Response.Content)"
    }
}

function Login {
    param(
        [Parameter(Mandatory)][string]$Email,
        [Parameter(Mandatory)][string]$Password,
        [int]$ExpectedStatus = 200
    )

    $response = Send-ApiRequest -Method POST -Path "/api/auth/login" -Body @{
        email = $Email
        password = $Password
    }
    Assert-Status $response $ExpectedStatus "Login for managed account"
    return $response
}

function Find-AdminUser {
    param(
        [Parameter(Mandatory)][string]$Token,
        [Parameter(Mandatory)][string]$Search
    )

    $encodedSearch = [Uri]::EscapeDataString($Search)
    $response = Send-ApiRequest -Method GET `
        -Path "/api/admin/users?search=$encodedSearch&page=1&pageSize=20" `
        -Token $Token
    Assert-Status $response 200 "Admin user search"
    if ($response.Json.items.Count -ne 1) {
        throw "Expected one exact managed-user search result but found $($response.Json.items.Count)."
    }
    return $response.Json.items[0]
}

$docker = Get-Command docker -ErrorAction Stop
$script:DockerPath = $docker.Source
$script:HttpClient = [System.Net.Http.HttpClient]::new()
$script:HttpClient.Timeout = [TimeSpan]::FromSeconds(15)

try {
    $apiEnvironment = Get-ContainerEnvironment "propertyhub-api-1"
    $sqlEnvironment = Get-ContainerEnvironment "propertyhub-sqlserver-1"
    $adminEmail = $apiEnvironment["SeedAdmin__Email"]
    $adminPassword = $apiEnvironment["SeedAdmin__Password"]
    $sqlPassword = $sqlEnvironment["MSSQL_SA_PASSWORD"]
    $connectionString = $apiEnvironment["ConnectionStrings__DefaultConnection"]
    if (-not $adminEmail -or -not $adminPassword -or -not $sqlPassword -or -not $connectionString) {
        throw "Required container configuration is unavailable."
    }
    $connectionBuilder =
        [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connectionString)
    $databaseName = $connectionBuilder.InitialCatalog
    if (-not $databaseName) {
        throw "The configured SQL Server database name is unavailable."
    }

    $apiReady = Invoke-WebRequest -UseBasicParsing "$ApiBaseUrl/health/ready"
    $webReady = Invoke-WebRequest -UseBasicParsing "$WebBaseUrl/health"
    if ($apiReady.StatusCode -ne 200 -or $webReady.StatusCode -ne 200) {
        throw "API and web health checks must both return HTTP 200."
    }

    $suffix = [Guid]::NewGuid().ToString("N")
    $managedEmail = "phase7-$suffix@propertyhub.test"
    $managedPassword = "StrongPass!123"
    $registration = Send-ApiRequest -Method POST -Path "/api/auth/register" -Body @{
        fullName = "Phase Seven Managed User"
        email = $managedEmail
        password = $managedPassword
    }
    Assert-Status $registration 201 "Managed-user registration"
    $managedUserId = $registration.Json.id
    $initialLogin = Login $managedEmail $managedPassword
    $initialUserToken = $initialLogin.Json.accessToken

    $missingDashboard = Send-ApiRequest -Method GET -Path "/api/admin/dashboard"
    Assert-Status $missingDashboard 401 "Anonymous Admin dashboard access"
    $userDashboard = Send-ApiRequest -Method GET `
        -Path "/api/admin/dashboard" `
        -Token $initialUserToken
    Assert-Status $userDashboard 403 "User Admin dashboard access"

    $adminLogin = Login $adminEmail $adminPassword
    $adminToken = $adminLogin.Json.accessToken
    $dashboardBefore = Send-ApiRequest -Method GET `
        -Path "/api/admin/dashboard" `
        -Token $adminToken
    Assert-Status $dashboardBefore 200 "Admin dashboard metrics"

    $managedUser = Find-AdminUser $adminToken $managedEmail
    $promote = Send-ApiRequest -Method PATCH `
        -Path "/api/admin/users/$managedUserId/role" `
        -Token $adminToken `
        -Version $managedUser.version `
        -Body @{ role = "Admin" }
    Assert-Status $promote 200 "User promotion"
    if ($promote.Json.role -ne "Admin") {
        throw "Promoted account did not return the Admin role."
    }

    $invalidatedUserToken = Send-ApiRequest -Method GET `
        -Path "/api/auth/me" `
        -Token $initialUserToken
    Assert-Status $invalidatedUserToken 403 "Post-promotion stale User token"
    $promotedLogin = Login $managedEmail $managedPassword
    if ($promotedLogin.Json.user.role -ne "Admin") {
        throw "Promoted account did not receive an Admin JWT after login."
    }
    $promotedToken = $promotedLogin.Json.accessToken
    $promotedSelf = Find-AdminUser $promotedToken $managedEmail
    $selfDemotion = Send-ApiRequest -Method PATCH `
        -Path "/api/admin/users/$managedUserId/role" `
        -Token $promotedToken `
        -Version $promotedSelf.version `
        -Body @{ role = "User" }
    Assert-Status $selfDemotion 409 "Admin self-demotion"

    $promotedForDemotion = Find-AdminUser $adminToken $managedEmail
    $demote = Send-ApiRequest -Method PATCH `
        -Path "/api/admin/users/$managedUserId/role" `
        -Token $adminToken `
        -Version $promotedForDemotion.version `
        -Body @{ role = "User" }
    Assert-Status $demote 200 "Admin-to-User demotion"
    $invalidatedAdminToken = Send-ApiRequest -Method GET `
        -Path "/api/auth/me" `
        -Token $promotedToken
    Assert-Status $invalidatedAdminToken 403 "Post-demotion stale Admin token"
    $demotedLogin = Login $managedEmail $managedPassword
    if ($demotedLogin.Json.user.role -ne "User") {
        throw "Demoted account did not receive a User JWT after login."
    }
    $currentUserToken = $demotedLogin.Json.accessToken

    $managedForDisable = Find-AdminUser $adminToken $managedEmail
    $disable = Send-ApiRequest -Method PATCH `
        -Path "/api/admin/users/$managedUserId/status" `
        -Token $adminToken `
        -Version $managedForDisable.version `
        -Body @{
            status = "Disabled"
            reason = "Phase 7 live Docker access-control verification"
        }
    Assert-Status $disable 200 "Account disable"
    $disabledExistingToken = Send-ApiRequest -Method GET `
        -Path "/api/auth/me" `
        -Token $currentUserToken
    Assert-Status $disabledExistingToken 403 "Disabled account existing token"
    [void](Login $managedEmail $managedPassword 403)

    $adminUser = Find-AdminUser $adminToken $adminEmail
    $selfDisable = Send-ApiRequest -Method PATCH `
        -Path "/api/admin/users/$($adminUser.id)/status" `
        -Token $adminToken `
        -Version $adminUser.version `
        -Body @{
            status = "Disabled"
            reason = "Unsafe self-disable verification"
        }
    Assert-Status $selfDisable 409 "Seeded Admin self-disable"

    $dashboardDisabled = Send-ApiRequest -Method GET `
        -Path "/api/admin/dashboard" `
        -Token $adminToken
    Assert-Status $dashboardDisabled 200 "Dashboard after account disable"
    if ($dashboardDisabled.Json.users.disabled -ne ($dashboardBefore.Json.users.disabled + 1)) {
        throw "Disabled-user metric did not increase by one."
    }

    $disabledUser = Find-AdminUser $adminToken $managedEmail
    $enable = Send-ApiRequest -Method PATCH `
        -Path "/api/admin/users/$managedUserId/status" `
        -Token $adminToken `
        -Version $disabledUser.version `
        -Body @{
            status = "Active"
            reason = "Phase 7 live Docker verification completed"
        }
    Assert-Status $enable 200 "Account reactivation"
    $reactivatedLogin = Login $managedEmail $managedPassword
    if ($reactivatedLogin.Json.user.role -ne "User") {
        throw "Reactivated account did not retain its User role."
    }

    $dashboardRestored = Send-ApiRequest -Method GET `
        -Path "/api/admin/dashboard" `
        -Token $adminToken
    Assert-Status $dashboardRestored 200 "Dashboard after account reactivation"
    if ($dashboardRestored.Json.users.disabled -ne $dashboardBefore.Json.users.disabled) {
        throw "Disabled-user metric did not return to its baseline."
    }

    $safeUserId = $managedUserId.Replace("'", "''")
    $auditCount = & $script:DockerPath exec propertyhub-sqlserver-1 `
        /opt/mssql-tools18/bin/sqlcmd `
        -S localhost -U sa -P $sqlPassword -C -d $databaseName -h -1 -W `
        -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM UserStatusChanges WHERE TargetUserId = '$safeUserId';"
    $auditValue = $auditCount |
        ForEach-Object { $_.ToString().Trim() } |
        Where-Object { $_ -match "^\d+$" } |
        Select-Object -First 1
    if ([int]$auditValue -ne 2) {
        throw "Expected two immutable status-change audit rows."
    }

    $migrationCount = & $script:DockerPath exec propertyhub-sqlserver-1 `
        /opt/mssql-tools18/bin/sqlcmd `
        -S localhost -U sa -P $sqlPassword -C -d $databaseName -h -1 -W `
        -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId LIKE '%AddAdminUserManagement';"
    $migrationValue = $migrationCount |
        ForEach-Object { $_.ToString().Trim() } |
        Where-Object { $_ -match "^\d+$" } |
        Select-Object -First 1
    if ([int]$migrationValue -ne 1) {
        throw "The AddAdminUserManagement migration is not applied."
    }

    $adminSsr = Invoke-WebRequest -UseBasicParsing "$WebBaseUrl/admin"
    if ($adminSsr.StatusCode -ne 200 -or $adminSsr.Content -notmatch "Sign in required") {
        throw "Direct Admin SSR route did not return the safe signed-out shell."
    }

    $apiMounts = & $script:DockerPath inspect propertyhub-api-1 --format "{{json .Mounts}}" |
        ConvertFrom-Json
    $sqlMounts = & $script:DockerPath inspect propertyhub-sqlserver-1 --format "{{json .Mounts}}" |
        ConvertFrom-Json
    $uploadMount = ($apiMounts | Where-Object { $_.Destination -eq "/app/uploads" }).Name
    $sqlMount = ($sqlMounts | Where-Object { $_.Destination -eq "/var/opt/mssql" }).Name
    if (-not $uploadMount -or -not $sqlMount) {
        throw "Expected SQL Server and upload named volumes were not found."
    }

    [pscustomobject]@{
        ApiHealth = $apiReady.StatusCode
        WebHealth = $webReady.StatusCode
        AnonymousAdmin = $missingDashboard.Status
        UserAdmin = $userDashboard.Status
        Promotion = $promote.Status
        SelfDemotion = $selfDemotion.Status
        Demotion = $demote.Status
        Disable = $disable.Status
        DisabledExistingToken = $disabledExistingToken.Status
        SelfDisable = $selfDisable.Status
        Reactivation = $enable.Status
        StatusAuditRows = [int]$auditValue
        MigrationRows = [int]$migrationValue
        AccountsBefore = $dashboardBefore.Json.users.total
        ActiveBefore = $dashboardBefore.Json.users.active
        DisabledBefore = $dashboardBefore.Json.users.disabled
        ActiveAfterRestore = $dashboardRestored.Json.users.active
        DisabledAfterRestore = $dashboardRestored.Json.users.disabled
        TotalProperties = $dashboardRestored.Json.properties.total
        TotalCities = $dashboardRestored.Json.totalCities
        SqlVolume = $sqlMount
        UploadVolume = $uploadMount
    } | Format-List
}
finally {
    $script:HttpClient.Dispose()
}
