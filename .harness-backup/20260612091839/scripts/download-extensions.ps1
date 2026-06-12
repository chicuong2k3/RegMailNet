#!/usr/bin/env pwsh
# Downloads captcha solver extension files from the original ninjemail repo
# Run from the project root: pwsh scripts/download-extensions.ps1

$ErrorActionPreference = "Stop"
$base = "https://raw.githubusercontent.com/david96182/ninjemail/main/ninjemail/captcha_solvers"
$dest = "$PSScriptRoot/../src/RegMailNet/captcha_solvers"

function Download-File {
    param([string]$Url, [string]$Path)
    $dir = Split-Path $Path -Parent
    if (!(Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    Write-Host "  $Path"
    Invoke-WebRequest -Uri $Url -OutFile $Path -ErrorAction Stop
}

Write-Host "Downloading captcha solver extensions..."

# ── Capsolver Chrome Extension ──────────────────────────────────────────────
$capsolverFiles = @(
    "capsolver-chrome-extension/manifest.json",
    "capsolver-chrome-extension/background.js",
    "capsolver-chrome-extension/core-content-script.js",
    "capsolver-chrome-extension/dom.js",
    "capsolver-chrome-extension/hcaptcha-recognition.js",
    "capsolver-chrome-extension/recaptcha-recognition.js",
    "capsolver-chrome-extension/funcaptcha-recognition.js",
    "capsolver-chrome-extension/aws-recognition.js",
    "capsolver-chrome-extension/image-to-text.js",
    "capsolver-chrome-extension/my-content-script.js",
    "capsolver-chrome-extension/assets/config.js",
    "capsolver-chrome-extension/assets/content.css",
    "capsolver-chrome-extension/assets/inject/inject-aws.js",
    "capsolver-chrome-extension/assets/inject/inject-funcaptcha.js",
    "capsolver-chrome-extension/assets/inject/inject-hcaptcha.js",
    "capsolver-chrome-extension/assets/inject/inject-recaptcha.js",
    "capsolver-chrome-extension/assets/inject/injected.js",
    "capsolver-chrome-extension/assets/inject/solvedCallback.js",
    "capsolver-chrome-extension/_locales/en/messages.json",
    "capsolver-chrome-extension/_locales/es/messages.json",
    "capsolver-chrome-extension/_locales/ru/messages.json",
    "capsolver-chrome-extension/_locales/zh/messages.json"
)

foreach ($f in $capsolverFiles) {
    Download-File "$base/$f" "$dest/$f"
}

# ── Capsolver .xpi (Firefox) ────────────────────────────────────────────────
Download-File "$base/capsolver_captcha_solver-1.10.4.xpi" "$dest/capsolver_captcha_solver-1.10.4.xpi"

# ── NopeCHA Chrome Extension ────────────────────────────────────────────────
$nopechaFiles = @(
    "NopeCHA-CAPTCHA-Solver/manifest.json",
    "NopeCHA-CAPTCHA-Solver/background.js",
    "NopeCHA-CAPTCHA-Solver/eventhook.js",
    "NopeCHA-CAPTCHA-Solver/eventhook/loader.js",
    "NopeCHA-CAPTCHA-Solver/locate.js",
    "NopeCHA-CAPTCHA-Solver/popup.css",
    "NopeCHA-CAPTCHA-Solver/popup.html",
    "NopeCHA-CAPTCHA-Solver/popup.js",
    "NopeCHA-CAPTCHA-Solver/setup.html",
    "NopeCHA-CAPTCHA-Solver/pages/funcaptcha-demo.js",
    "NopeCHA-CAPTCHA-Solver/pages/integrate.js",
    "NopeCHA-CAPTCHA-Solver/pages/setup.js",
    "NopeCHA-CAPTCHA-Solver/captcha/awscaptcha.js",
    "NopeCHA-CAPTCHA-Solver/captcha/funcaptcha.js",
    "NopeCHA-CAPTCHA-Solver/captcha/hcaptcha.js",
    "NopeCHA-CAPTCHA-Solver/captcha/perimeterx.js",
    "NopeCHA-CAPTCHA-Solver/captcha/recaptcha.js",
    "NopeCHA-CAPTCHA-Solver/captcha/textcaptcha.js",
    "NopeCHA-CAPTCHA-Solver/captcha/turnstile.js"
)

foreach ($f in $nopechaFiles) {
    Download-File "$base/$f" "$dest/$f"
}

# ── NopeCHA .xpi (Firefox) ─────────────────────────────────────────────────
Download-File "$base/noptcha-0.4.9.xpi" "$dest/noptcha-0.4.9.xpi"

Write-Host ""
Write-Host "Done! Extension files downloaded to $dest"
