# RegMailNet

A .NET library for automated email account creation on Gmail, Outlook, and Yahoo. It uses Selenium WebDriver for browser automation and integrates with third-party SMS and captcha-solving services to handle phone verification and captcha challenges.

## Features

- Automated account creation for Gmail, Outlook/Hotmail, and Yahoo
- Auto-generated random user details (names, passwords, countries, birthdates)
- Proxy support — authenticated proxies, proxy lists, and free proxy auto-fetch
- Captcha solving via Capsolver and Nopecha browser extensions
- SMS verification via SmsPool, 5sim, and GetsmsCode
- Dependency injection with `Microsoft.Extensions.DependencyInjection`

## Project Structure

```
RegMailNet/
├── src/
│   └── RegMailNet/                          # Main library
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
│       ├── WebDriver/                       # Selenium driver lifecycle
│       │   ├── IWebDriverFactory.cs
│       │   ├── WebDriverFactory.cs
│       │   └── ProxyAuthExtensionBuilder.cs
│       ├── Utilities/                       # Data generation + Selenium helpers
│       │   ├── DataGenerator.cs
│       │   ├── WebHelpers.cs
│       │   └── MonthsMapping.cs
│       ├── RegMailNetManager.cs             # Main facade / entry point
│       └── appsettings.json                 # Default configuration
├── tests/
│   └── RegMailNet.Tests/                    # xUnit tests
│       ├── RegMailNetManagerTests.cs
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
│  IWebDriverFactory          ISmsServiceFactory       │
│  (Selenium + proxies)       (SMS verification APIs)  │
└──────────────────────────────────────────────────────┘
```

### Layers

| Layer | Responsibility |
|---|---|
| **Configuration** | `RegMailNetOptions` loaded from `appsettings.json` via the Options pattern. Defines supported browsers, SMS services, captcha solvers, and per-provider solver mappings. |
| **Manager** | `RegMailNetManager` is the single entry point. It selects the right browser, proxy, captcha key, and SMS service, then delegates to a provider. |
| **Email Providers** | Each provider (`GmailProvider`, `OutlookProvider`, `YahooProvider`) implements the Selenium steps for one platform — navigating signup forms, handling captchas, requesting SMS codes. |
| **SMS Services** | Factory-based. `SmsServiceFactory` resolves a concrete service (`SmsPoolService`, `FiveSimService`, `GetsmsCodeService`) by name. Each service wraps a third-party API for renting phone numbers and reading verification codes. |
| **Captcha Solvers** | `ICaptchaSolver` implementations configure browser extensions (Capsolver, Nopecha) that intercept captcha challenges during Selenium sessions. |
| **WebDriver** | `WebDriverFactory` creates Chrome, Firefox, or Undetected ChromeDriver instances with optional proxy auth and captcha extensions. `ProxyAuthExtensionBuilder` generates a Chrome extension to inject proxy credentials. |
| **Utilities** | `DataGenerator` (uses the Bogus library) produces random user profiles. `FreeProxyService` fetches free proxies from public lists. `WebHelpers` provides reusable Selenium interaction methods. |

### Account Creation Flow

1. Consumer calls `RegMailNetManager.CreateGmailAccount(...)` (or Outlook/Yahoo).
2. Manager validates the browser choice, resolves captcha keys and SMS credentials from configuration.
3. `IWebDriverFactory.CreateDriver(...)` launches a browser with the requested proxy and captcha extensions.
4. `DataGenerator.GenerateMissingInfo(...)` fills in any fields the caller left blank (name, password, country, birthdate).
5. The provider navigates the signup page, fills forms, solves captchas, requests an SMS code via `ISmsService`, and submits the verification code.
6. On success, returns `AccountCreationResult(Email, Password)`. On failure, throws `AccountCreationException`.
7. The WebDriver is disposed in a `finally` block.

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

This registers `DataGenerator`, `WebDriverFactory`, `ProxyAuthExtensionBuilder`, all three email providers, and `SmsServiceFactory` as singletons.

## Supported Services

| Category | Options |
|---|---|
| **Browsers** | `chrome`, `firefox`, `undetected-chrome` |
| **Captcha Solvers** | `capsolver`, `nopecha` |
| **SMS Services** | `smspool`, `5sim`, `getsmscode` |
| **Email Providers** | Gmail, Outlook/Hotmail, Yahoo |

## Key Dependencies

| Package | Purpose |
|---|---|
| `Selenium.WebDriver` 4.44.0 | Browser automation |
| `Selenium.UndetectedChromeDriver` 1.1.4 | Anti-detection Chrome driver |
| `WebDriverManager` 2.17.7 | Automatic driver binary management |
| `Bogus` 35.6.5 | Fake user data generation |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | DI support |
| `Microsoft.Extensions.Options` | Options pattern |
| `Microsoft.Extensions.Http` | `IHttpClientFactory` for SMS APIs |

## Captcha Solver Extensions

The library loads browser extensions for captcha solving at runtime. You need to populate the `captcha_solvers/` directory before using captcha-dependent providers (Outlook, Yahoo).

```powershell
pwsh scripts/download-extensions.ps1
```

Or download manually from the [ninjemail repo](https://github.com/david96182/ninjemail/tree/main/ninjemail/captcha_solvers). See `src/RegMailNet/captcha_solvers/README.md` for details.

## Testing

```bash
dotnet test
```

Tests use xUnit, Moq, and FluentAssertions. The suite covers:
- **Manager** — initialization, browser validation, captcha/SMS key resolution, proxy handling, account creation delegation
- **SMS services** — `SmsPoolService`, `FiveSimService`, `GetsmsCodeService` (HTTP request/response, retry logic, error handling)
- **SMS factory** — service resolution, per-provider service IDs
- **Utilities** — `DataGenerator`, `MonthsMapping`, `FreeProxyService`

## Ported From

This project is a .NET port of [ninjemail](https://github.com/Drewski454/ninjemail), a Python library for automated email account creation.
