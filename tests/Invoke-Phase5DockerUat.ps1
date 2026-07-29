$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http

$apiBaseUri = "http://localhost:8081"

function New-ApiClient {
    $client = [System.Net.Http.HttpClient]::new()
    $client.BaseAddress = [Uri]$apiBaseUri
    return $client
}

function Send-Json($client, $method, $path, $body) {
    $json = $body | ConvertTo-Json -Depth 8 -Compress
    $content = [System.Net.Http.StringContent]::new(
        $json,
        [Text.Encoding]::UTF8,
        "application/json")
    $request = [System.Net.Http.HttpRequestMessage]::new($method, $path)
    $request.Content = $content
    return $client.SendAsync($request).GetAwaiter().GetResult()
}

function Read-Json($response) {
    return ($response.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json)
}

function Assert-Status($response, [int]$expected, $label) {
    $actual = [int]$response.StatusCode
    if ($actual -ne $expected) {
        $body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        throw "$label expected $expected but received $actual. $body"
    }
    Write-Output "$label=$actual"
}

function Send-Images($client, $path, $files) {
    $form = [System.Net.Http.MultipartFormDataContent]::new()
    foreach ($file in $files) {
        $part = [System.Net.Http.ByteArrayContent]::new([byte[]]$file.Bytes)
        $part.Headers.ContentType =
            [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse($file.ContentType)
        $form.Add($part, "images", $file.Name)
    }
    return $client.PostAsync($path, $form).GetAwaiter().GetResult()
}

function Import-ComposeEnvironment($apiEnvironment) {
    $apiNames = @(
        "ConnectionStrings__DefaultConnection",
        "ImageStorage__RootPath",
        "Jwt__Issuer",
        "Jwt__Audience",
        "Jwt__SigningKey",
        "Jwt__AccessTokenMinutes",
        "SeedAdmin__Email",
        "SeedAdmin__Password",
        "SeedAdmin__FullName",
        "Cors__AllowedOrigins__0")
    foreach ($name in $apiNames) {
        $entry = $apiEnvironment |
            Where-Object { $_.StartsWith("$name=") } |
            Select-Object -First 1
        if (-not $entry) {
            throw "The running API does not contain required configuration $name."
        }
        [Environment]::SetEnvironmentVariable(
            $name,
            $entry.Substring($name.Length + 1),
            "Process")
    }

    $sqlEnvironment = docker inspect --format `
        '{{range .Config.Env}}{{println .}}{{end}}' propertyhub-sqlserver-1
    $sqlEntry = $sqlEnvironment |
        Where-Object { $_.StartsWith("MSSQL_SA_PASSWORD=") } |
        Select-Object -First 1
    if (-not $sqlEntry) {
        throw "The running SQL Server does not contain its required password configuration."
    }
    [Environment]::SetEnvironmentVariable(
        "MSSQL_SA_PASSWORD",
        $sqlEntry.Substring("MSSQL_SA_PASSWORD=".Length),
        "Process")
    $env:API_INTERNAL_BASE_URL = "http://api:8080"
    $env:VITE_PUBLIC_API_BASE_URL = "http://localhost:8081"
}

function Wait-ForApiHealth {
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        $state = docker inspect --format `
            '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' `
            propertyhub-api-1
        if ($state -eq "healthy") {
            return
        }
        Start-Sleep -Seconds 2
    }
    throw "The API did not return to healthy status."
}

$suffix = [Guid]::NewGuid().ToString("N")
$userEmail = "phase5-$suffix@propertyhub.test"
$userPassword = "StrongPass!123"
$anonymous = New-ApiClient

$register = Send-Json $anonymous ([System.Net.Http.HttpMethod]::Post) "/api/auth/register" @{
    fullName = "Phase Five Owner"
    email = $userEmail
    password = $userPassword
}
Assert-Status $register 201 "register"

$login = Send-Json $anonymous ([System.Net.Http.HttpMethod]::Post) "/api/auth/login" @{
    email = $userEmail
    password = $userPassword
}
Assert-Status $login 200 "owner_login"
$owner = New-ApiClient
$owner.DefaultRequestHeaders.Authorization =
    [System.Net.Http.Headers.AuthenticationHeaderValue]::new(
        "Bearer",
        (Read-Json $login).accessToken)

$cities = $anonymous.GetAsync("/api/cities").GetAwaiter().GetResult()
Assert-Status $cities 200 "cities"
$cityId = (Read-Json $cities).items[0].id
$create = Send-Json $owner ([System.Net.Http.HttpMethod]::Post) "/api/properties" @{
    title = "Phase 5 image home $($suffix.Substring(0, 8))"
    description = "A Docker UAT property used to verify secure local image upload behavior."
    purpose = "Sale"
    propertyType = "House"
    cityId = $cityId
    address = "Phase Five Test Avenue"
    price = 15000000
    area = 5
    areaUnit = "Marla"
    bedrooms = 3
    bathrooms = 2
    contactNumber = "03000000000"
}
Assert-Status $create 201 "property_create"
$property = Read-Json $create
$propertyId = $property.id

$hidden = $anonymous.GetAsync("/api/properties/$propertyId").GetAwaiter().GetResult()
Assert-Status $hidden 404 "public_hidden_before_image"
$invalid = Send-Images $owner "/api/properties/$propertyId/images" @(@{
    Name = "payload.jpg"
    ContentType = "image/jpeg"
    Bytes = [Text.Encoding]::ASCII.GetBytes("MZ executable")
})
Assert-Status $invalid 400 "invalid_signature"

$png = [Convert]::FromBase64String(
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=")
$upload = Send-Images $owner "/api/properties/$propertyId/images" @(
    @{ Name = "front.png"; ContentType = "image/png"; Bytes = $png },
    @{ Name = "rear.png"; ContentType = "image/png"; Bytes = $png })
Assert-Status $upload 200 "two_image_upload"
$images = (Read-Json $upload).images
if (@($images).Count -ne 2 -or @($images | Where-Object { $_.isPrimary }).Count -ne 1) {
    throw "Upload did not return exactly two images and one primary image."
}
Write-Output "image_count=2"
$first = $images[0]
$second = $images[1]

$ownerImage = $owner.GetAsync($first.url).GetAwaiter().GetResult()
Assert-Status $ownerImage 200 "owner_hidden_image_read"
if (-not $ownerImage.Headers.Contains("X-Content-Type-Options")) {
    throw "The image response did not include X-Content-Type-Options."
}
$anonymousImage = $anonymous.GetAsync($first.url).GetAwaiter().GetResult()
Assert-Status $anonymousImage 404 "anonymous_hidden_image"

$apiEnvironment = docker inspect --format `
    '{{range .Config.Env}}{{println .}}{{end}}' propertyhub-api-1
$adminEmailEntry = $apiEnvironment |
    Where-Object { $_.StartsWith("SeedAdmin__Email=") } |
    Select-Object -First 1
$adminPasswordEntry = $apiEnvironment |
    Where-Object { $_.StartsWith("SeedAdmin__Password=") } |
    Select-Object -First 1
$adminLogin = Send-Json $anonymous ([System.Net.Http.HttpMethod]::Post) "/api/auth/login" @{
    email = $adminEmailEntry.Substring("SeedAdmin__Email=".Length)
    password = $adminPasswordEntry.Substring("SeedAdmin__Password=".Length)
}
Assert-Status $adminLogin 200 "admin_login"
$admin = New-ApiClient
$admin.DefaultRequestHeaders.Authorization =
    [System.Net.Http.Headers.AuthenticationHeaderValue]::new(
        "Bearer",
        (Read-Json $adminLogin).accessToken)

$approve = Send-Json $admin ([System.Net.Http.HttpMethod]::Post) `
    "/api/admin/properties/$propertyId/moderation" @{
        status = "Approved"
        reason = $null
    }
Assert-Status $approve 200 "property_approve"
$publicDetail = $anonymous.GetAsync("/api/properties/$propertyId").GetAwaiter().GetResult()
Assert-Status $publicDetail 200 "public_detail_with_images"
$publicImage = $anonymous.GetAsync($first.url).GetAwaiter().GetResult()
Assert-Status $publicImage 200 "public_image_read"

$ssr = $anonymous.GetAsync(
    "http://localhost:3000/properties/$propertyId").GetAwaiter().GetResult()
Assert-Status $ssr 200 "ssr_property_detail"
$ssrHtml = $ssr.Content.ReadAsStringAsync().GetAwaiter().GetResult()
if (-not $ssrHtml.Contains($property.title) -or -not $ssrHtml.Contains($first.url)) {
    throw "The SSR response did not contain the property title and image URL."
}
Write-Output "ssr_image_markup=present"

$makePrimary = $owner.PutAsync(
    "/api/properties/$propertyId/images/$($second.id)/primary",
    $null).GetAwaiter().GetResult()
Assert-Status $makePrimary 200 "change_primary"
$deleteFirst = $owner.DeleteAsync(
    "/api/properties/$propertyId/images/$($first.id)").GetAwaiter().GetResult()
Assert-Status $deleteFirst 200 "delete_non_last_image"
$reapprove = Send-Json $admin ([System.Net.Http.HttpMethod]::Post) `
    "/api/admin/properties/$propertyId/moderation" @{
        status = "Approved"
        reason = $null
    }
Assert-Status $reapprove 200 "reapprove_after_image_change"
$deleteLast = $owner.DeleteAsync(
    "/api/properties/$propertyId/images/$($second.id)").GetAwaiter().GetResult()
Assert-Status $deleteLast 409 "prevent_last_image_delete"

$uploadVolume = ((docker inspect propertyhub-api-1 | ConvertFrom-Json)[0].Mounts |
    Where-Object { $_.Destination -eq "/app/uploads" }).Name
if (-not $uploadVolume) {
    throw "The API is not using a named upload volume."
}
Import-ComposeEnvironment $apiEnvironment
docker compose up -d --no-deps --force-recreate api | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Docker Compose could not recreate the API."
}
Wait-ForApiHealth
$volumeAfter = ((docker inspect propertyhub-api-1 | ConvertFrom-Json)[0].Mounts |
    Where-Object { $_.Destination -eq "/app/uploads" }).Name
if ($uploadVolume -ne $volumeAfter) {
    throw "The upload volume changed during API recreation."
}
$persisted = $anonymous.GetAsync($second.url).GetAwaiter().GetResult()
Assert-Status $persisted 200 "image_after_api_recreate"
Write-Output "upload_volume_preserved=$volumeAfter"
Write-Output "uat_property_id=$propertyId"

$anonymous.Dispose()
$owner.Dispose()
$admin.Dispose()
