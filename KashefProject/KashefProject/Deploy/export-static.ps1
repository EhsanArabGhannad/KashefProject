param(
    [string]$PreviewUrl = "http://127.0.0.1:5180/"
)

$projectRoot = Split-Path -Parent $PSScriptRoot
$distRoot = Join-Path $projectRoot "dist"
$assetsRoot = Join-Path $distRoot "assets"
$serverRoot = Join-Path $distRoot "server"

New-Item -ItemType Directory -Force $assetsRoot | Out-Null
New-Item -ItemType Directory -Force $serverRoot | Out-Null

$assetsRootFull = [System.IO.Path]::GetFullPath($assetsRoot) + [System.IO.Path]::DirectorySeparatorChar
foreach ($generatedDirectory in @("gallery", "shop", "contact")) {
    $targetDirectory = [System.IO.Path]::GetFullPath((Join-Path $assetsRoot $generatedDirectory))
    if ($targetDirectory.StartsWith($assetsRootFull, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $targetDirectory)) {
        Remove-Item -Recurse -Force -LiteralPath $targetDirectory
    }
}

$pages = @(
    @{ Route = ""; Output = "index.html"; Depth = 0 },
    @{ Route = "gallery/"; Output = "gallery\index.html"; Depth = 1 },
    @{ Route = "gallery/calligraphy/"; Output = "gallery\calligraphy\index.html"; Depth = 2 },
    @{ Route = "gallery/portraits/"; Output = "gallery\portraits\index.html"; Depth = 2 },
    @{ Route = "gallery/heritage/"; Output = "gallery\heritage\index.html"; Depth = 2 },
    @{ Route = "shop/"; Output = "shop\index.html"; Depth = 1 },
    @{ Route = "shop/golden-calligraphy-panel/"; Output = "shop\golden-calligraphy-panel\index.html"; Depth = 2 },
    @{ Route = "shop/royal-signature-panel/"; Output = "shop\royal-signature-panel\index.html"; Depth = 2 },
    @{ Route = "shop/royal-calligraphy-panel/"; Output = "shop\royal-calligraphy-panel\index.html"; Depth = 2 },
    @{ Route = "shop/shahbanu-portrait/"; Output = "shop\shahbanu-portrait\index.html"; Depth = 2 },
    @{ Route = "shop/shahyad-tower-panel/"; Output = "shop\shahyad-tower-panel\index.html"; Depth = 2 },
    @{ Route = "shop/lion-and-sun-panel/"; Output = "shop\lion-and-sun-panel\index.html"; Depth = 2 },
    @{ Route = "shop/reza-shah-portrait/"; Output = "shop\reza-shah-portrait\index.html"; Depth = 2 },
    @{ Route = "shop/ataturk-portrait/"; Output = "shop\ataturk-portrait\index.html"; Depth = 2 },
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
