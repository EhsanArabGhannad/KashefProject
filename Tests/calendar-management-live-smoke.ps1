param([string]$BaseUrl = 'https://craftisma.net')
$ErrorActionPreference = 'Stop'
$origin = [uri]$BaseUrl
if (!$origin.IsAbsoluteUri -or $origin.UserInfo -or $origin.AbsolutePath -ne '/' -or $origin.Query -or $origin.Fragment) { throw 'Use a root origin without credentials.' }
if ($origin.Scheme -ne 'https' -and $origin.Host -notin @('localhost', '127.0.0.1')) { throw 'Remote checks require HTTPS.' }
function Assert($value, $label) { if (!$value) { throw "FAIL: $label" }; Write-Output "PASS: $label" }
# Anonymous GETs only: no admin sign-in, mutations, image uploads or customer data.
foreach ($path in @('/admin/calendar-events', '/admin/calendar-events/create', '/admin/calendar-events/1/edit', '/admin/calendar-events/1/delete')) {
    $response = Invoke-WebRequest ([uri]::new($origin, $path)) -SkipHttpErrorCheck -TimeoutSec 20
    Assert ($response.StatusCode -eq 200 -and $response.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/admin/login') "Admin role protects $path"
    Assert (($response.Headers.'Cache-Control' -join ',') -match 'no-store') "Admin redirect/login is not cached: $path"
}
foreach ($asset in @('css/calendar.css', 'css/admin.css', 'js/calendar-admin.js')) {
    $response = Invoke-WebRequest ([uri]::new($origin, '/' + $asset)) -TimeoutSec 20
    $local = Get-Content -LiteralPath (Join-Path $PSScriptRoot ('../KashefProject/KashefProject/wwwroot/' + $asset)) -Raw
    Assert (($response.Content -replace "`r`n", "`n") -ceq ($local -replace "`r`n", "`n")) "Published calendar management asset matches: $asset"
}
Write-Output 'Calendar management live checks completed (read-only).'
