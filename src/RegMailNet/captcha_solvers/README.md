# Captcha Solver Extensions

This directory must contain the browser extension files used by captcha solving services.

## Required Files

### Capsolver
- `capsolver-chrome-extension/` — Chrome extension directory (contains `manifest.json`, `background.js`, etc.)
- `capsolver_captcha_solver-1.10.4.xpi` — Firefox extension

### Nopecha
- `NopeCHA-CAPTCHA-Solver/` — Chrome extension directory (contains `manifest.json`, `background.js`, etc.)
- `noptcha-0.4.9.xpi` — Firefox extension

## How to Get the Files

**Option 1: Run the download script (from project root)**
```powershell
pwsh scripts/download-extensions.ps1
```

**Option 2: Manual download**

Download from the original ninjemail repository:
```
https://github.com/david96182/ninjemail/tree/main/ninjemail/captcha_solvers
```

Copy the `capsolver-chrome-extension/` directory, `NopeCHA-CAPTCHA-Solver/` directory,
and the `.xpi`/`.crx` files into this directory.

**Option 3: Install from browser extension stores**

1. Install Capsolver extension from Chrome Web Store, then copy the extension directory
   from your Chrome profile (`~/.config/google-chrome/Default/Extensions/`)
2. Download Nopecha from https://nopecha.com

## Directory Structure

After setup, this directory should look like:
```
captcha_solvers/
├── capsolver-chrome-extension/
│   ├── manifest.json
│   ├── background.js
│   ├── assets/
│   │   └── config.js        ← API key gets written here
│   └── ...
├── capsolver_captcha_solver-1.10.4.xpi
├── NopeCHA-CAPTCHA-Solver/
│   ├── manifest.json
│   ├── background.js
│   └── ...
└── noptcha-0.4.9.xpi
```
