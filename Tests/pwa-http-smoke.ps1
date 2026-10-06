param([string]$BaseUrl = 'https://craftisma.net')
$ErrorActionPreference = 'Stop'
$origin = [uri]$BaseUrl
if (!$origin.IsAbsoluteUri -or $origin.UserInfo -or $origin.AbsolutePath -ne '/' -or $origin.Query -or $origin.Fragment) {
    throw 'BaseUrl must be a root origin without credentials, a path, query or fragment.'
}
if ($origin.Scheme -ne 'https' -and $origin.Host -notin @('localhost', '127.0.0.1')) {
    throw 'Remote PWA checks require HTTPS with certificate validation.'
}
function Assert($condition, $label) {
    if (!$condition) { throw "FAIL: $label" }
    Write-Output "PASS: $label"
}
# Keep PASS output outside the pipeline used to return HTTP response objects.
function Fetch($path) {
    $response = Invoke-WebRequest ([uri]::new($origin, $path)) -TimeoutSec 30
    if ($response.StatusCode -ne 200) { throw "FAIL: $path is unreachable" }
    return $response
}
$guide = Fetch '/app/'
Assert ($guide.Content.Contains('Add from Safari') -and $guide.Content.Contains('Add from Chrome')) 'installation guide served by the live MVC store'
Assert (($guide.Headers.'Cache-Control' -join ',') -match 'no-store') 'installation page does not cache personalized HTML'
Assert ($guide.Content.Contains('href="/manifest.webmanifest"') -and $guide.Content.Contains('href="/icons/apple-touch-icon.png"')) 'manifest and Apple icon linked'
$script = [regex]::Match($guide.Content, 'src="(/js/pwa[^"\s]*)"').Groups[1].Value
Assert (![string]::IsNullOrEmpty($script)) 'PWA registration script linked'
$js = Fetch ([System.Net.WebUtility]::HtmlDecode($script))
Assert ($js.Content.Contains('/service-worker.js') -and $js.Content.Contains('updateViaCache: "none"')) 'registration script is reachable and checks fresh worker versions'
$manifestResponse = Fetch '/manifest.webmanifest'
Assert (($manifestResponse.Headers.'Content-Type' -join ',') -match '^application/manifest\+json') 'manifest content type'
Assert (($manifestResponse.Headers.'Cache-Control' -join ',') -match 'no-cache') 'manifest revalidation'
$manifestText = if ($manifestResponse.Content -is [byte[]]) {
    [System.Text.Encoding]::UTF8.GetString($manifestResponse.Content)
} else { $manifestResponse.Content }
$manifest = $manifestText | ConvertFrom-Json
Assert ($manifest.id -eq '/' -and $manifest.scope -eq '/' -and $manifest.display -eq 'standalone' -and $manifest.start_url -eq '/shop/?source=pwa') 'stable identity and standalone shop launch'
Assert ([bool]($manifest.icons | Where-Object { $_.purpose -eq 'maskable' })) 'maskable icon declared'
foreach ($icon in @($manifest.icons) + @([pscustomobject]@{src='/icons/apple-touch-icon.png';sizes='180x180'})) {
    $image = Fetch $icon.src
    Assert (($image.Headers.'Content-Type' -join ',') -match '^image/png') "$($icon.src) PNG content type"
    [byte[]]$bytes = $image.Content
    Assert ($bytes.Length -ge 24 -and [System.Text.Encoding]::ASCII.GetString($bytes, 1, 3) -eq 'PNG') "$($icon.src) PNG signature"
    $width = [System.Net.IPAddress]::NetworkToHostOrder([BitConverter]::ToInt32($bytes, 16))
    $height = [System.Net.IPAddress]::NetworkToHostOrder([BitConverter]::ToInt32($bytes, 20))
    Assert ("${width}x${height}" -eq $icon.sizes) "$($icon.src) advertised dimensions"
}
$worker = Fetch '/service-worker.js'
Assert (($worker.Headers.'Cache-Control' -join ',') -match 'no-cache') 'worker revalidation'
Assert (($worker.Headers.'Content-Type' -join ',') -match 'javascript') 'worker JavaScript content type'
$localWorker = Join-Path $PSScriptRoot '../KashefProject/KashefProject/wwwroot/service-worker.js'
Assert (($worker.Content -replace "`r`n", "`n") -ceq ((Get-Content -LiteralPath $localWorker -Raw) -replace "`r`n", "`n")) 'published worker matches the reviewed release'
foreach ($asset in @('/offline.html', '/css/offline.css', '/js/offline.js')) {
    $response = Fetch $asset
    Assert ($response.RawContentLength -gt 0) "$asset served"
}
foreach ($page in @('/shop/', '/cart/', '/account/login', '/checkout/')) {
    $response = Fetch $page
    Assert (($response.Headers.'Cache-Control' -join ',') -match 'no-store') "$page keeps fresh account and bag HTML"
}
Write-Output 'HTTPS PWA checks completed. Physical-device installation and payment acceptance are separate checks.'
