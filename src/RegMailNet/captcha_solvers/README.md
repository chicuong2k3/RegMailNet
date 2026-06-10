# Captcha Solver Extensions

This directory must contain the browser extension files used by captcha solving services.

## Required Files

### Capsolver
- `capsolver_captcha_solver-1.10.4.xpi` — Firefox extension

### Nopecha
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

Copy the `.xpi` files into this directory.

**Option 3: Install from browser extension stores**

1. Download Capsolver Firefox extension from https://www.capsolver.com/
2. Download Nopecha Firefox extension from https://nopecha.com/

## Directory Structure

After setup, this directory should look like:
```
captcha_solvers/
├── capsolver_captcha_solver-1.10.4.xpi
└── noptcha-0.4.9.xpi
```

## Note

This library uses Camoufox (Firefox-based), so only Firefox `.xpi` extensions are supported.
Chrome extensions are not compatible.
