[CmdletBinding()]
param(
    [string]$RepoRoot = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
}

$edgeCandidates = @(
    'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe',
    'C:\Program Files\Google\Chrome\Application\chrome.exe',
    'C:\Program Files (x86)\Google\Chrome\Application\chrome.exe'
)

$browserPath = $edgeCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $browserPath) {
    throw 'Unable to find a Chromium-based browser for headless brand asset rendering.'
}

$brandRoot = Join-Path $RepoRoot 'assets\brand'
$appBrandRoot = Join-Path $RepoRoot 'app\Resources\Brand'
$tempRoot = Join-Path $env:TEMP 'regprobe-brand-build'

New-Item -ItemType Directory -Path $appBrandRoot -Force | Out-Null
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

function New-RenderPage {
    param(
        [string]$SvgPath,
        [string]$HtmlPath,
        [int]$Width,
        [int]$Height
    )

    $svgContent = Get-Content -Path $SvgPath -Raw
    $html = @"
<!doctype html>
<html>
<head>
  <meta charset="utf-8">
  <style>
    html, body {
      margin: 0;
      padding: 0;
      width: ${Width}px;
      height: ${Height}px;
      background: transparent;
      overflow: hidden;
    }
    body {
      display: grid;
      place-items: center;
    }
    svg {
      width: ${Width}px;
      height: ${Height}px;
      display: block;
    }
  </style>
</head>
<body>
$svgContent
</body>
</html>
"@
    Set-Content -Path $HtmlPath -Value $html -Encoding UTF8
}

function Invoke-HeadlessScreenshot {
    param(
        [string]$HtmlPath,
        [string]$OutputPath,
        [int]$Width,
        [int]$Height
    )

    $uri = [System.Uri]::new($HtmlPath).AbsoluteUri
    if (Test-Path $OutputPath) {
        Remove-Item -Path $OutputPath -Force
    }

    $arguments = @(
        '--headless=new',
        '--disable-gpu',
        '--hide-scrollbars',
        '--force-device-scale-factor=1',
        '--default-background-color=00000000',
        "--window-size=$Width,$Height",
        "--screenshot=$OutputPath",
        $uri
    )

    & $browserPath @arguments | Out-Null

    if (!(Test-Path $OutputPath)) {
        throw "Failed to render screenshot for $HtmlPath"
    }
}

function New-IcoFromPng {
    param(
        [string]$PngPath,
        [string]$IcoPath
    )

    Add-Type -AssemblyName System.Drawing

    $source = [System.Drawing.Image]::FromFile($PngPath)
    try {
        $sizes = @(16, 24, 32, 48, 64, 128, 256)
        $pngPayloads = New-Object System.Collections.Generic.List[byte[]]

        foreach ($size in $sizes) {
            $bitmap = New-Object System.Drawing.Bitmap $size, $size
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            try {
                $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))
                $graphics.DrawImage($source, 0, 0, $size, $size)

                $stream = New-Object System.IO.MemoryStream
                $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
                $pngPayloads.Add($stream.ToArray())
                $stream.Dispose()
            }
            finally {
                $graphics.Dispose()
                $bitmap.Dispose()
            }
        }

        $fileStream = [System.IO.File]::Open($IcoPath, [System.IO.FileMode]::Create)
        $writer = New-Object System.IO.BinaryWriter($fileStream)
        try {
            $writer.Write([UInt16]0)
            $writer.Write([UInt16]1)
            $writer.Write([UInt16]$pngPayloads.Count)

            $offset = 6 + (16 * $pngPayloads.Count)
            foreach ($index in 0..($sizes.Count - 1)) {
                $size = $sizes[$index]
                $payload = $pngPayloads[$index]
                $writer.Write([byte]($(if ($size -ge 256) { 0 } else { $size })))
                $writer.Write([byte]($(if ($size -ge 256) { 0 } else { $size })))
                $writer.Write([byte]0)
                $writer.Write([byte]0)
                $writer.Write([UInt16]1)
                $writer.Write([UInt16]32)
                $writer.Write([UInt32]$payload.Length)
                $writer.Write([UInt32]$offset)
                $offset += $payload.Length
            }

            foreach ($payload in $pngPayloads) {
                $writer.Write($payload)
            }
        }
        finally {
            $writer.Dispose()
            $fileStream.Dispose()
        }
    }
    finally {
        $source.Dispose()
    }
}

$markSvgPath = Join-Path $brandRoot 'regprobe-mark.svg'
$fullSvgPath = Join-Path $brandRoot 'regprobe-logo-full.svg'

$markHtmlPath = Join-Path $tempRoot 'regprobe-mark.html'
$fullHtmlPath = Join-Path $tempRoot 'regprobe-logo-full.html'

$markPngPath = Join-Path $appBrandRoot 'regprobe-mark.png'
$fullPngPath = Join-Path $appBrandRoot 'regprobe-logo-full.png'
$readmePngPath = Join-Path $brandRoot 'regprobe-logo-full.png'
$icoPath = Join-Path $appBrandRoot 'regprobe-mark.ico'

New-RenderPage -SvgPath $markSvgPath -HtmlPath $markHtmlPath -Width 1024 -Height 1024
New-RenderPage -SvgPath $fullSvgPath -HtmlPath $fullHtmlPath -Width 1024 -Height 1024

Invoke-HeadlessScreenshot -HtmlPath $markHtmlPath -OutputPath $markPngPath -Width 1024 -Height 1024
Invoke-HeadlessScreenshot -HtmlPath $fullHtmlPath -OutputPath $fullPngPath -Width 1024 -Height 1024
Copy-Item -Path $fullPngPath -Destination $readmePngPath -Force

New-IcoFromPng -PngPath $markPngPath -IcoPath $icoPath

[pscustomobject]@{
    browser = $browserPath
    mark_png = $markPngPath
    full_png = $fullPngPath
    readme_png = $readmePngPath
    icon = $icoPath
} | ConvertTo-Json -Depth 3
