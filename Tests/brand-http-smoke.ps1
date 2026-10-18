param([string]$BaseUrl='http://127.0.0.1:5197')
$ErrorActionPreference='Stop'
$origin=[uri]$BaseUrl
if (!$origin.IsAbsoluteUri -or $origin.UserInfo -or $origin.AbsolutePath -ne '/' -or $origin.Query -or $origin.Fragment) { throw 'Use a root origin without credentials.' }
if ($origin.Scheme -ne 'https' -and $origin.Host -notin @('localhost','127.0.0.1')) { throw 'Remote checks require HTTPS.' }
function Assert($value,$label) { if (!$value) { throw "FAIL: $label" }; Write-Output "PASS: $label" }
foreach ($path in @('/','/shop/','/calendar/','/account/login','/admin/login')) {
    $page=Invoke-WebRequest ([uri]::new($origin,$path)) -TimeoutSec 20
    Assert ($page.StatusCode -eq 200 -and $page.Content.Contains('/images/brand/craftisma-logo.png?v=')) "New logo with cache version: $path"
    Assert ($page.Content.Contains('/icons/favicon-32.png?v=') -and $page.Content.Contains('/icons/favicon-16.png?v=') -and $page.Content.Contains('/favicon.ico?v=')) "Versioned favicons linked: $path"
    Assert (!$page.Content.Contains('<span class="brand-mark">C</span>') -and !$page.Content.Contains('<b>C</b><span>Craftisma Admin')) "Old letter mark removed: $path"
}
$handler=[Net.Http.HttpClientHandler]::new()
$handler.AutomaticDecompression=[Net.DecompressionMethods]::All
$client=[Net.Http.HttpClient]::new($handler)
$client.Timeout=[TimeSpan]::FromSeconds(20)
try {
    foreach ($asset in @('/images/brand/craftisma-logo.png','/icons/favicon-16.png','/icons/favicon-32.png','/favicon.ico','/icons/icon-192.png','/icons/icon-512.png','/icons/icon-maskable-512.png','/icons/apple-touch-icon.png')) {
        $bytes=$client.GetByteArrayAsync([uri]::new($origin,$asset)).GetAwaiter().GetResult()
        $remoteHash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes))
        $localPath=Join-Path $PSScriptRoot ('../KashefProject/KashefProject/wwwroot'+$asset)
        Assert ($bytes.Length -gt 0 -and $remoteHash -eq (Get-FileHash -LiteralPath $localPath -Algorithm SHA256).Hash) "Exact brand asset verified: $asset"
    }
} finally { $client.Dispose() }
