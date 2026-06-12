# Blazor Hybrid to Blazor WASM Migration

## Summary

Convert the `RegMailNet.Ui` project from a MAUI Blazor Hybrid application (multi-targeting Android, iOS, MacCatalyst, Windows) to a standalone Blazor WebAssembly application that runs in the browser. The WASM frontend communicates with the existing `RegMailNet.Api` backend for all operations.

## Motivation

Browser automation (Playwright/Camoufox) cannot run in the browser. The MAUI hybrid approach added unnecessary complexity — the app doesn't use native platform features. A Blazor WASM frontend calling the API is simpler and equally functional.

## Decisions

- **Persistence:** Settings and account history stored server-side via API endpoints (file-based JSON storage on the API host).
- **UI library:** Keep BlazorBlueprint — no visual changes.
- **Browser automation:** All handled by the API. WASM frontend only triggers operations via HTTP calls.
- **Project strategy:** Replace the MAUI project in-place (not alongside).
- **Service pattern:** Thin HTTP client wrappers injected into pages.

## Architecture

```
Browser (Blazor WASM)          Server (ASP.NET Core API)
┌─────────────────────┐        ┌──────────────────────┐
│ Pages (.razor)      │        │ FastEndpoints         │
│  - Home             │───────>│  - POST /accounts/*   │
│  - CreateAccount    │  HTTP  │  - GET/PUT /settings  │
│  - History          │<───────│  - GET/POST /history  │
│  - Proxies          │        │  - GET /health        │
│  - Settings         │        │                       │
│  - TestPage         │        │ RegMailNetManager     │
│  - NotFound         │        │  (Playwright/Camoufox)│
├─────────────────────┤        │                       │
│ Services (HTTP)     │        │ File Storage          │
│  - SettingsClient   │        │  - data/settings.json │
│  - HistoryClient    │        │  - data/history.json  │
│  - AccountClient    │        └──────────────────────┘
├─────────────────────┤
│ wwwroot/            │
│  - index.html       │
│  - css/theme.css    │
└─────────────────────┘
```

## Project Structure Changes

### Delete from RegMailNet.Ui
- `MauiProgram.cs`
- `App.xaml`, `App.xaml.cs`
- `MainPage.xaml`, `MainPage.xaml.cs`
- `Platforms/` (Android, iOS, MacCatalyst, Windows)
- `Resources/` (icons, splash, fonts, images)
- `Properties/launchSettings.json`
- `wwwroot/RegMailNet.Ui.styles.css` (MAUI-generated)

### Keep from RegMailNet.Ui (modified)
- `Components/Pages/` — all 7 razor pages (modified for async API calls)
- `Components/Layout/` — MainLayout.razor, NavMenu.razor (unchanged)
- `Components/Routes.razor` — update assembly reference from `MauiProgram` to `Program`
- `Components/_Imports.razor` — unchanged
- `wwwroot/index.html` — swap `blazor.webview.js` for `blazor.web.js`, remove MAUI-specific markup
- `wwwroot/css/theme.css` — unchanged

### Add to RegMailNet.Ui
- `Program.cs` — WebAssemblyHostBuilder entry point
- `Services/ISettingsService.cs` + `SettingsService.cs` — API-backed
- `Services/IAccountHistoryService.cs` + `AccountHistoryService.cs` — API-backed
- `Services/IAccountCreationService.cs` + `AccountCreationService.cs` — API-backed

### Add to RegMailNet.Api
- `Endpoints/GetSettings.cs` — GET /api/settings
- `Endpoints/SaveSettings.cs` — PUT /api/settings
- `Endpoints/GetHistory.cs` — GET /api/history
- `Endpoints/AddHistory.cs` — POST /api/history
- `Services/FileSettingsService.cs` — reads/writes `data/settings.json`
- `Services/FileHistoryService.cs` — reads/writes `data/history.json`
- CORS policy in `Program.cs`

## Service Interfaces

### ISettingsService (WASM client)
```csharp
public interface ISettingsService
{
    Task<AppSettings> GetSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
}
```

### IAccountHistoryService (WASM client)
```csharp
public interface IAccountHistoryService
{
    Task<IReadOnlyList<AccountHistoryEntry>> GetHistoryAsync();
    Task AddAsync(AccountHistoryEntry entry);
}
```

### IAccountCreationService (WASM client)
```csharp
public interface IAccountCreationService
{
    Task<AccountCreatedResponse> CreateOutlookAsync(bool useProxy);
    Task<AccountCreatedResponse> CreateGmailAsync(bool useProxy);
    Task<AccountCreatedResponse> CreateYahooAsync(bool useProxy);
}
```

## Page Modifications

### CreateAccount.razor
- Replace `@inject RegMailNetManager Manager` with `@inject IAccountCreationService AccountService`
- Replace `Manager.CreateXxxAsync()` calls with `AccountService.CreateXxxAsync()`
- Replace `History.Add()` / `History.AddFailure()` with `await HistoryService.AddAsync()`

### Home.razor
- Replace `@inject SettingsService` / `@inject AccountHistoryService` with async API service injections
- Load settings and history in `OnInitializedAsync`
- Add loading state

### History.razor
- Load entries from `IAccountHistoryService.GetHistoryAsync()` in `OnInitializedAsync`
- Add loading state

### Proxies.razor
- Load/save proxies through `ISettingsService` instead of direct `SettingsService.Save()`

### Settings.razor
- Load settings via `await settingsService.GetSettingsAsync()` in `OnInitializedAsync`
- Save via `await settingsService.SaveSettingsAsync()`

### TestPage.razor — No changes
### NotFound.razor — No changes
### Layout components — No changes

## API Backend

### Settings Endpoints
- `GET /api/settings` — Returns `AppSettings` from `data/settings.json`
- `PUT /api/settings` — Saves `AppSettings` to `data/settings.json`

### History Endpoints
- `GET /api/history` — Returns list from `data/history.json`
- `POST /api/history` — Appends entry to `data/history.json`

### CORS
- Allow the WASM origin (configurable, default `https://localhost:5001`)
- Allow all methods and headers

### Existing Account Creation Endpoints
- After successful creation, write to history automatically
- Return `AccountCreatedResponse` as before

## Data Models

Shared between WASM client and API (duplicated in each project since they are separate):

```csharp
public class AppSettings
{
    public string Browser { get; set; } = "firefox";
    public string DefaultSmsService { get; set; } = "smspool";
    public string CapsolverKey { get; set; } = "";
    public string NopechaKey { get; set; } = "";
    public string SmsPoolToken { get; set; } = "";
    public string FiveSimToken { get; set; } = "";
    public string GetsmsCodeUser { get; set; } = "";
    public string GetsmsCodeToken { get; set; } = "";
    public List<string> Proxies { get; set; } = new();
}

public class AccountHistoryEntry
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Provider { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "";
}
```

## WASM Program.cs

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddBlazorBlueprintComponents();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IAccountHistoryService, AccountHistoryService>();
builder.Services.AddScoped<IAccountCreationService, AccountCreationService>();

await builder.Build().RunAsync();
```

## Development Setup

The WASM app and API run on separate ports during development. The WASM `HttpClient` base address must point to the API origin. Two options:
1. **Proxy via ASP.NET Core** — Host the WASM app within the API project (add `UseBlazorFrameworkFiles()` middleware). Single origin, no CORS needed.
2. **Separate origins** — WASM on port 5001, API on port 5000. Configure CORS on the API to allow port 5001.

Recommended: Option 2 (separate origins) for clean separation. The API's CORS policy will allow `https://localhost:5001`.
