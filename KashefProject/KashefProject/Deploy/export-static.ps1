param(
    [string]$PreviewUrl = "http://127.0.0.1:5180/"
)

$projectRoot = Split-Path -Parent $PSScriptRoot
$distRoot = Join-Path $projectRoot "dist"
$assetsRoot = Join-Path $distRoot "assets"
$serverRoot = Join-Path $distRoot "server"

New-Item -ItemType Directory -Force $assetsRoot | Out-Null
New-Item -ItemType Directory -Force $serverRoot | Out-Null

$page = Invoke-WebRequest -UseBasicParsing $PreviewUrl
[System.IO.File]::WriteAllText((Join-Path $assetsRoot "index.html"), $page.Content, [System.Text.UTF8Encoding]::new($false))

Copy-Item -Recurse -Force (Join-Path $projectRoot "wwwroot\css") $assetsRoot
Copy-Item -Recurse -Force (Join-Path $projectRoot "wwwroot\js") $assetsRoot
Copy-Item -Recurse -Force (Join-Path $projectRoot "wwwroot\images") $assetsRoot
Copy-Item -Force (Join-Path $PSScriptRoot "worker.js") (Join-Path $serverRoot "index.js")

Write-Output $distRoot
