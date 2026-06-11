# RegMailNet

A .NET library for automated email account creation across multiple providers.

## Supported Providers

| Provider | Captcha Solver | SMS Verification |
|----------|---------------|-----------------|
| Outlook / Hotmail | Capsolver, Nopecha | -- |
| Gmail | -- | Required |
| Yahoo | Capsolver, Nopecha | Required |

## Supported Browsers

- Chrome
- Firefox
- Undetected Chrome (via Selenium.UndetectedChromeDriver)

## Installation

```bash
dotnet add package RegMailNet
```

Or add a `<PackageReference>` to your `.csproj`:

```xml
<PackageReference Include="RegMailNet" Version="*" />
```

## Configuration

Add an `appsettings.json` section:

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

## Usage

### Dependency Injection

```csharp
services.AddRegMailNet(configuration);
```

### Manual Instantiation

```csharp
var manager = new RegMailNetManager(
    browser: "chrome",
    captchaKeys: new Dictionary<string, string>
    {
        ["capsolver"] = "your-capsolver-api-key"
    },
    smsKeys: new Dictionary<string, Dictionary<string, string>>
    {
        ["smspool"] = new() { ["apiKey"] = "your-smspool-key" }
    },
    smsServiceFactory: smsServiceFactory,
    proxies: new List<string> { "http://user:pass@host:port" }
);
```

### Create an Outlook Account

```csharp
var result = manager.CreateOutlookAccount(
    username: "myusername",
    password: "MyP@ssw0rd!",
    firstName: "John",
    lastName: "Doe",
    country: "United States of America",
    birthdate: "6-15-1995",
    hotmail: false  // true for @hotmail.com instead of @outlook.com
);

Console.WriteLine($"Created: {result.Email}");
```

### Create a Gmail Account

```csharp
var result = manager.CreateGmailAccount(
    username: "myusername",
    password: "MyP@ssw0rd!",
    firstName: "Jane",
    lastName: "Smith"
);
```

### Create a Yahoo Account

```csharp
var result = manager.CreateYahooAccount(
    username: "myusername",
    password: "MyP@ssw0rd!"
);
```

All parameters are optional -- any missing values are auto-generated using [Bogus](https://github.com/bchavez/Bogus).

## SMS Services

Gmail and Yahoo accounts require phone verification. The following SMS services are supported:

- **5Sim** -- `5sim`
- **GetsmsCode** -- `getsmscode`
- **SmsPool** -- `smspool`

Provide API keys via the `smsKeys` dictionary. The configured `DefaultSmsService` is used first; if unavailable, a random configured service is selected.

## Proxy Support

Pass a list of proxy URLs to rotate through randomly, or set `autoProxy: true` for free proxy fetching (not yet implemented).

```csharp
var manager = new RegMailNetManager(
    browser: "chrome",
    smsServiceFactory: factory,
    proxies: new List<string>
    {
        "http://user:pass@proxy1:8080",
        "http://user:pass@proxy2:8080"
    }
);
```

Set `useProxy: false` on individual calls to skip proxying for that request.

## Project Structure

```
src/RegMailNet/
  CaptchaSolvers/      # Capsolver and Nopecha browser extension integration
  Configuration/        # Options binding and DI extension methods
  EmailProviders/       # Outlook, Gmail, Yahoo account creation logic
  SmsServices/          # 5Sim, GetsmsCode, SmsPool phone verification
  Utilities/            # Fake data generation (Bogus), date helpers
  WebDriver/            # Selenium WebDriver factory with proxy auth support
tests/RegMailNet.Tests/
```

## Building

```bash
dotnet build
```

## Testing

```bash
dotnet test
```

## Dependencies

- [Selenium WebDriver](https://www.nuget.org/packages/Selenium.WebDriver) -- browser automation
- [Selenium.UndetectedChromeDriver](https://www.nuget.org/packages/Selenium.UndetectedChromeDriver) -- anti-detection Chrome
- [Bogus](https://www.nuget.org/packages/Bogus) -- fake data generation
- [WebDriverManager](https://www.nuget.org/packages/WebDriverManager) -- automatic driver management
