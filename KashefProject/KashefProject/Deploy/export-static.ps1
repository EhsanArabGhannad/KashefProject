param(
    [string]$PreviewUrl = "http://127.0.0.1:5180/"
)

$projectRoot = Split-Path -Parent $PSScriptRoot
$distRoot = Join-Path $projectRoot "dist"
$assetsRoot = Join-Path $distRoot "assets"
$serverRoot = Join-Path $distRoot "server"

New-Item -ItemType Directory -Force $assetsRoot | Out-Null
New-Item -ItemType Directory -Force $serverRoot | Out-Null

$pages = @(
    @{ Route = ""; Output = "index.html"; Depth = 0 },
    @{ Route = "gallery/"; Output = "gallery\index.html"; Depth = 1 },
    @{ Route = "gallery/sculptures/"; Output = "gallery\sculptures\index.html"; Depth = 2 },
    @{ Route = "gallery/vases/"; Output = "gallery\vases\index.html"; Depth = 2 },
    @{ Route = "gallery/wall-art/"; Output = "gallery\wall-art\index.html"; Depth = 2 },
    @{ Route = "shop/"; Output = "shop\index.html"; Depth = 1 },
    @{ Route = "shop/wave-sculpture/"; Output = "shop\wave-sculpture\index.html"; Depth = 2 },
    @{ Route = "shop/poly-vase/"; Output = "shop\poly-vase\index.html"; Depth = 2 },
    @{ Route = "shop/persian-relief-panel/"; Output = "shop\persian-relief-panel\index.html"; Depth = 2 },
    @{ Route = "contact/"; Output = "contact\index.html"; Depth = 1 }
)

foreach ($pageDefinition in $pages) {
    $pageUrl = [Uri]::new([Uri]$PreviewUrl, $pageDefinition.Route).AbsoluteUri
    $page = Invoke-WebRequest -UseBasicParsing $pageUrl
    $prefix = if ($pageDefinition.Depth -eq 0) { "./" } else { "../" * $pageDefinition.Depth }
    $staticHtml = $page.Content -replace '(href|src)="/', "`$1=`"$prefix"
    $staticHtml = $staticHtml -replace '(?m)[ \t]+$', ''
    $outputPath = Join-Path $assetsRoot $pageDefinition.Output
    New-Item -ItemType Directory -Force (Split-Path -Parent $outputPath) | Out-Null
    [System.IO.File]::WriteAllText($outputPath, $staticHtml, [System.Text.UTF8Encoding]::new($false))
}

Copy-Item -Recurse -Force (Join-Path $projectRoot "wwwroot\css") $assetsRoot
Copy-Item -Recurse -Force (Join-Path $projectRoot "wwwroot\js") $assetsRoot
Copy-Item -Recurse -Force (Join-Path $projectRoot "wwwroot\images") $assetsRoot
Copy-Item -Force (Join-Path $PSScriptRoot "worker.js") (Join-Path $serverRoot "index.js")
[System.IO.File]::WriteAllText((Join-Path $assetsRoot ".nojekyll"), "", [System.Text.UTF8Encoding]::new($false))

Write-Output $distRoot
