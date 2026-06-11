# RegMailNet

A .NET library for automated email account creation on Gmail, Outlook, and Yahoo. It uses **CamoufoxNet** (anti-detect Firefox with C++ level fingerprint spoofing) via Playwright for browser automation, and integrates with third-party SMS and captcha-solving services to handle phone verification and captcha challenges.

## Features

- Automated account creation for Gmail, Outlook/Hotmail, and Yahoo
- **Anti-detect browser** via Camoufox — fingerprints patched at C++ level (not JS injection)
- Auto-generated random user details (names, passwords, countries, birthdates)
- Proxy support — authenticated proxies, proxy lists, and free proxy auto-fetch
- Human-like mouse movement and interaction patterns
- WebRTC blocking to prevent IP leaks
- Captcha solving via Capsolver and Nopecha browser extensions
- SMS verification via SmsPool, 5sim, and GetsmsCode
- Fully async API with `async`/`await` throughout
- Dependency injection with `Microsoft.Extensions.DependencyInjection`

## Project Structure

```
RegMailNet/
├── src/
│   └── RegMailNet/                          # Main library
│       ├── Browser/                         # Camoufox browser lifecycle
│       │   ├── IBrowserFactory.cs
│       │   ├── CamoufoxBrowserFactory.cs
│       │   └── CaptchaKeyInfo.cs
│       ├── Configuration/                   # Options model + DI extensions
│       │   ├── RegMailNetOptions.cs
│       │   └── ServiceCollectionExtensions.cs
│       ├── EmailProviders/                  # Per-provider account creation logic
│       │   ├── IEmailProvider.cs
│       │   ├── AccountCreationResult.cs
│       │   ├── AccountCreationException.cs
│       │   ├── GmailProvider.cs
│       │   ├── OutlookProvider.cs
│       │   └── YahooProvider.cs
│       ├── SmsServices/                     # SMS verification API integrations
│       │   ├── ISmsService.cs
│       │   ├── ISmsServiceFactory.cs
│       │   ├── SmsServiceFactory.cs
│       │   ├── GetsmsCodeService.cs
│       │   ├── SmsPoolService.cs
│       │   └── FiveSimService.cs
│       ├── CaptchaSolvers/                  # Captcha solver browser extensions
│       │   ├── ICaptchaSolver.cs
│       │   ├── CapsolverExtension.cs
│       │   └── NopechaExtension.cs
│       ├── Utilities/                       # Data generation + Playwright helpers
│       │   ├── DataGenerator.cs
│       │   ├── WebHelpers.cs
│       │   ├── MonthsMapping.cs
│       │   └── FreeProxyService.cs
│       ├── RegMailNetManager.cs             # Main facade / entry point
│       └── appsettings.json                 # Default configuration
├── tests/
│   └── RegMailNet.Tests/                    # xUnit tests
│       ├── RegMailNetManagerTests.cs
│       ├── SmsServices/
│       └── Utilities/
└── RegMailNet.slnx
```

## Architecture

The library follows a layered architecture with dependency injection throughout.

```
┌──────────────────────────────────────────────────────┐
│                  Consumer Code                       │
│              (your application)                      │
└──────────────┬───────────────────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────────────────┐
│              RegMailNetManager                       │
│         (facade / orchestrator)                      │
│  - Resolves captcha + SMS keys                       │
│  - Generates missing user data                       │
│  - Delegates to the correct provider                 │
└───────┬──────────┬───────────────────┬───────────────┘
        │          │                   │
        ▼          ▼                   ▼
┌────────────┐ ┌────────────┐  ┌────────────────────┐
│   Gmail    │ │  Outlook   │  │       Yahoo        │
│  Provider  │ │  Provider  │  │      Provider      │
└─────┬──────┘ └─────┬──────┘  └────────┬───────────┘
      │              │                   │
      ▼              ▼                   ▼
┌──────────────────────────────────────────────────────┐
│  IBrowserFactory           ISmsServiceFactory        │
│  (Camoufox + Playwright)   (SMS verification APIs)   │
└──────────────────────────────────────────────────────┘
```

### Layers

| Layer | Responsibility |
|---|---|
| **Browser** | `IBrowserFactory` creates anti-detect browser instances via CamoufoxNet. Fingerprint spoofing at C++ level, human-like mouse movement, WebRTC blocking, proxy support. |
| **Configuration** | `RegMailNetOptions` loaded from `appsettings.json` via the Options pattern. Defines SMS services, captcha solvers, and per-provider solver mappings. |
| **Manager** | `RegMailNetManager` is the single entry point. It selects the right proxy, captcha key, and SMS service, then delegates to a provider. |
| **Email Providers** | Each provider (`GmailProvider`, `OutlookProvider`, `YahooProvider`) implements the Playwright steps for one platform — navigating signup forms, handling captchas, requesting SMS codes. |
| **SMS Services** | Factory-based. `SmsServiceFactory` resolves a concrete service (`SmsPoolService`, `FiveSimService`, `GetsmsCodeService`) by name. Each service wraps a third-party API for renting phone numbers and reading verification codes. |
| **Captcha Solvers** | `ICaptchaSolver` implementations configure browser extensions (Capsolver, Nopecha) that intercept captcha challenges during browser sessions. |
| **Utilities** | `DataGenerator` (uses the Bogus library) produces random user profiles. `FreeProxyService` fetches free proxies from public lists. `WebHelpers` provides reusable Playwright interaction methods. |

### Account Creation Flow

1. Consumer calls `RegMailNetManager.CreateGmailAccountAsync(...)` (or Outlook/Yahoo).
2. Manager resolves captcha keys and SMS credentials from configuration.
3. `IBrowserFactory.CreatePageAsync(...)` launches a Camoufox browser with the requested proxy and humanization settings.
4. `DataGenerator.GenerateMissingInfo(...)` fills in any fields the caller left blank (name, password, country, birthdate).
5. The provider navigates the signup page, fills forms, solves captchas, requests an SMS code via `ISmsService`, and submits the verification code.
6. On success, returns `AccountCreationResult(Email, Password)`. On failure, throws `AccountCreationException`.
7. The browser context is disposed automatically via `await using`.

## Prerequisites

- **.NET 9.0+**
- **Python 3.10+** with `camoufox` package installed:
  ```bash
  pip install camoufox
  camoufox fetch
  ```

## Configuration

Configuration lives in `appsettings.json` under the `RegMailNet` section:

```json
{
  "RegMailNet": {
    "CaptchaServicesSupported": ["capsolver", "nopecha"],
    "DefaultCaptchaService": "capsolver",
    "SmsServicesSupported": ["getsmscode", "smspool", "5sim"],
    "DefaultSmsService": "smspool",
    "SupportedBrowsers": ["firefox", "chrome", "undetected-chrome"],
    "SupportedSolversByEmail": [
      {
        "EmailService": "outlook",
        "Solvers": ["capsolver", "nopecha"]
      },
      {
        "EmailService": "yahoo",
        "Solvers": ["capsolver", "nopecha"]
      }
    ]
  }
}
```

## Dependency Injection

Register the library with the DI container:

```csharp
services.AddRegMailNet(configuration);
```

This registers `DataGenerator`, `CamoufoxBrowserFactory`, all three email providers, `FreeProxyService`, and `SmsServiceFactory` as singletons.

## Usage

```csharp
using RegMailNet;
using RegMailNet.Browser;

// Create the browser factory
var browserFactory = new CamoufoxBrowserFactory(logger);

// Create the manager
var manager = new RegMailNetManager(
    browserFactory: browserFactory,
    smsServiceFactory: smsServiceFactory,
    captchaKeys: new Dictionary<string, string> { ["capsolver"] = "your-api-key" },
    smsKeys: new Dictionary<string, Dictionary<string, string>>
    {
        ["smspool"] = new() { ["token"] = "your-sms-token" }
    });

// Create accounts
var gmail = await manager.CreateGmailAccountAsync(firstName: "John", lastName: "Doe");
var outlook = await manager.CreateOutlookAccountAsync(country: "US");
var yahoo = await manager.CreateYahooAccountAsync();
```

## Supported Services

| Category | Options |
|---|---|
| **Browser** | Camoufox (anti-detect Firefox via Playwright) |
| **Captcha Solvers** | `capsolver`, `nopecha` |
| **SMS Services** | `smspool`, `5sim`, `getsmscode` |
| **Email Providers** | Gmail, Outlook/Hotmail, Yahoo |

## Key Dependencies

| Package | Purpose |
|---|---|
| `CamoufoxNet` 0.1.0 | Anti-detect browser (C++ fingerprint spoofing, Playwright compatible) |
| `Microsoft.Playwright` 1.50.0+ | Browser automation API |
| `Bogus` 35.6.5 | Fake user data generation |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | DI support |
| `Microsoft.Extensions.Options` | Options pattern |
| `Microsoft.Extensions.Http` | `IHttpClientFactory` for SMS APIs |

## Anti-Detection

Camoufox provides superior anti-detection compared to Selenium-based approaches:

| Approach | Anti-Detection Level | How It Works |
|---|---|---|
| Selenium WebDriver | ⭐ | Easily detected via `navigator.webdriver` flag |
| SeleniumUndetectedChromeDriver | ⭐⭐⭐ | Patches chromedriver binary to remove flag |
| Playwright + Stealth JS | ⭐⭐⭐ | Injects 31 JavaScript patches (detectable) |
| **Camoufox (this library)** | ⭐⭐⭐⭐⭐ | **Firefox C++ build** — fingerprints spoofed at implementation level |

## Testing

```bash
dotnet test
```

Tests use xUnit, Moq, and FluentAssertions. The suite covers:
- **Manager** — initialization, captcha/SMS key resolution, proxy handling, account creation delegation
- **SMS services** — `SmsPoolService`, `FiveSimService`, `GetsmsCodeService` (HTTP request/response, retry logic, error handling)
- **SMS factory** — service resolution, per-provider service IDs
- **Utilities** — `DataGenerator`, `MonthsMapping`, `FreeProxyService`

## Migration from Selenium

This library was migrated from Selenium WebDriver to CamoufoxNet. Key changes:

| Before (Selenium) | After (CamoufoxNet) |
|---|---|
| `IWebDriverFactory` | `IBrowserFactory` |
| `IWebDriver` | `IPage` (Playwright) |
| `WebDriverWait` | Playwright auto-wait (built-in) |
| `By.Id("x")` | `"#x"` (CSS selector) |
| `element.SendKeys("text")` | `await locator.FillAsync("text")` |
| `Thread.Sleep(ms)` | `await Task.Delay(ms)` |
| `driver.Quit()` | `await using var page = ...` |
| Synchronous API | Fully async |

## Ported From

This project is a .NET port of [ninjemail](https://github.com/Drewski454/ninjemail), a Python library for automated email account creation.
