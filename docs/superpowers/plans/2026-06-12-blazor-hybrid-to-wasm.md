# Blazor Hybrid to Blazor WASM Migration Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert RegMailNet.Ui from a MAUI Blazor Hybrid app to a Blazor WebAssembly app that communicates with the RegMailNet.Api backend.

**Architecture:** The WASM frontend runs entirely in the browser. All state (settings, account history) is persisted server-side through new API endpoints. Account creation is delegated to existing API endpoints. Thin HTTP client services replace direct RegMailNetManager usage.

**Tech Stack:** Blazor WebAssembly (.NET 9), BlazorBlueprint, FastEndpoints (API), System.Text.Json

---

## File Map

### RegMailNet.Ui — Delete
- `MauiProgram.cs`
- `App.xaml`, `App.xaml.cs`
- `MainPage.xaml`, `MainPage.xaml.cs`
- `Platforms/` (entire directory)
- `Resources/` (entire directory)
- `Properties/launchSettings.json`

### RegMailNet.Ui — Create
- `Program.cs` — WASM host builder
- `Services/ISettingsService.cs` — settings client interface
- `Services/SettingsService.cs` — HTTP-backed settings client
- `Services/IAccountHistoryService.cs` — history client interface
- `Services/AccountHistoryService.cs` — HTTP-backed history client
- `Services/IAccountCreationService.cs` — account creation client interface
- `Services/AccountCreationService.cs` — HTTP-backed account creation client
- `Models/AppSettings.cs` — settings DTO
- `Models/AccountHistoryEntry.cs` — history entry DTO

### RegMailNet.Ui — Modify
- `RegMailNet.Ui.csproj` — replace MAUI SDK with WASM SDK
- `wwwroot/index.html` — swap blazor.webview.js for blazor.web.js
- `Components/Routes.razor` — update assembly reference
- `Components/Pages/CreateAccount.razor` — use API services
- `Components/Pages/Home.razor` — use API services
- `Components/Pages/History.razor` — use API services
- `Components/Pages/Proxies.razor` — use API services
- `Components/Pages/Settings.razor` — use API services

### RegMailNet.Api — Create
- `Models/AppSettings.cs` — settings DTO (server copy)
- `Models/AccountHistoryEntry.cs` — history entry DTO (server copy)
- `Services/FileSettingsService.cs` — file-backed settings storage
- `Services/FileHistoryService.cs` — file-backed history storage
- `Endpoints/GetSettings.cs` — GET /api/settings
- `Endpoints/SaveSettings.cs` — PUT /api/settings
- `Endpoints/GetHistory.cs` — GET /api/history
- `Endpoints/AddHistory.cs` — POST /api/history
- `Requests/SaveSettingsRequest.cs` — settings request DTO
- `Requests/AddHistoryRequest.cs` — history request DTO
- `Properties/launchSettings.json` — configure dev ports

### RegMailNet.Api — Modify
- `Program.cs` — add CORS, register file services, register new endpoints
- `RegMailNet.Api.csproj` — no changes needed (already Web SDK)

### RegMailNet.slnx — Modify
- Add `src/RegMailNet.Api/RegMailNet.Api.csproj` to solution

---

## Task 1: API — Add Data Models and File Storage Services

**Files:**
- Create: `src/RegMailNet.Api/Models/AppSettings.cs`
- Create: `src/RegMailNet.Api/Models/AccountHistoryEntry.cs`
- Create: `src/RegMailNet.Api/Services/FileSettingsService.cs`
- Create: `src/RegMailNet.Api/Services/FileHistoryService.cs`

- [ ] **Step 1: Create AppSettings model**

Create `src/RegMailNet.Api/Models/AppSettings.cs`:

```csharp
namespace RegMailNet.Api.Models;

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
    public List<string> Proxies { get; set; } = [];
}
```

- [ ] **Step 2: Create AccountHistoryEntry model**

Create `src/RegMailNet.Api/Models/AccountHistoryEntry.cs`:

```csharp
namespace RegMailNet.Api.Models;

public class AccountHistoryEntry
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Provider { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "";
}
```

- [ ] **Step 3: Create FileSettingsService**

Create `src/RegMailNet.Api/Services/FileSettingsService.cs`:

```csharp
using System.Text.Json;
using RegMailNet.Api.Models;

namespace RegMailNet.Api.Services;

public class FileSettingsService
{
    private readonly string _filePath = Path.Combine("data", "settings.json");
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(_filePath))
            return new AppSettings();

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
```

- [ ] **Step 4: Create FileHistoryService**

Create `src/RegMailNet.Api/Services/FileHistoryService.cs`:

```csharp
using System.Text.Json;
using RegMailNet.Api.Models;

namespace RegMailNet.Api.Services;

public class FileHistoryService
{
    private readonly string _filePath = Path.Combine("data", "history.json");
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<List<AccountHistoryEntry>> LoadAsync()
    {
        if (!File.Exists(_filePath))
            return [];

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<AccountHistoryEntry>>(json) ?? [];
    }

    public async Task AddAsync(AccountHistoryEntry entry)
    {
        await _lock.WaitAsync();
        try
        {
            var entries = await LoadAsync();
            entries.Insert(0, entry);
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var json = JsonSerializer.Serialize(entries, _jsonOptions);
            await File.WriteAllTextAsync(_filePath, json);
        }
        finally
        {
            _lock.Release();
        }
    }
}
```

- [ ] **Step 5: Commit**

```bash
git add src/RegMailNet.Api/Models/ src/RegMailNet.Api/Services/
git commit -m "feat(api): add data models and file storage services for settings/history"
```

---

## Task 2: API — Add Settings and History Endpoints

**Files:**
- Create: `src/RegMailNet.Api/Requests/SaveSettingsRequest.cs`
- Create: `src/RegMailNet.Api/Requests/AddHistoryRequest.cs`
- Create: `src/RegMailNet.Api/Endpoints/GetSettings.cs`
- Create: `src/RegMailNet.Api/Endpoints/SaveSettings.cs`
- Create: `src/RegMailNet.Api/Endpoints/GetHistory.cs`
- Create: `src/RegMailNet.Api/Endpoints/AddHistory.cs`

- [ ] **Step 1: Create request DTOs**

Create `src/RegMailNet.Api/Requests/SaveSettingsRequest.cs`:

```csharp
using RegMailNet.Api.Models;

namespace RegMailNet.Api.Requests;

public sealed class SaveSettingsRequest
{
    public AppSettings Settings { get; init; } = new();
}
```

Create `src/RegMailNet.Api/Requests/AddHistoryRequest.cs`:

```csharp
using RegMailNet.Api.Models;

namespace RegMailNet.Api.Requests;

public sealed class AddHistoryRequest
{
    public AccountHistoryEntry Entry { get; init; } = new();
}
```

- [ ] **Step 2: Create GetSettings endpoint**

Create `src/RegMailNet.Api/Endpoints/GetSettings.cs`:

```csharp
using FastEndpoints;
using RegMailNet.Api.Models;
using RegMailNet.Api.Services;

namespace RegMailNet.Api.Endpoints;

public sealed class GetSettings : EndpointWithoutRequest<AppSettings>
{
    public override void Configure()
    {
        Get("/api/settings");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var service = Resolve<FileSettingsService>();
        var settings = await service.LoadAsync();
        await SendOkAsync(settings, ct);
    }
}
```

- [ ] **Step 3: Create SaveSettings endpoint**

Create `src/RegMailNet.Api/Endpoints/SaveSettings.cs`:

```csharp
using FastEndpoints;
using RegMailNet.Api.Requests;
using RegMailNet.Api.Services;

namespace RegMailNet.Api.Endpoints;

public sealed class SaveSettings : Endpoint<SaveSettingsRequest>
{
    public override void Configure()
    {
        Put("/api/settings");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SaveSettingsRequest req, CancellationToken ct)
    {
        var service = Resolve<FileSettingsService>();
        await service.SaveAsync(req.Settings);
        await SendOkAsync(ct);
    }
}
```

- [ ] **Step 4: Create GetHistory endpoint**

Create `src/RegMailNet.Api/Endpoints/GetHistory.cs`:

```csharp
using System.Text.Json.Serialization;
using FastEndpoints;
using RegMailNet.Api.Services;

namespace RegMailNet.Api.Endpoints;

public sealed class GetHistory : EndpointWithoutRequest<HistoryResponse>
{
    public override void Configure()
    {
        Get("/api/history");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var service = Resolve<FileHistoryService>();
        var entries = await service.LoadAsync();
        await SendOkAsync(new HistoryResponse { Entries = entries }, ct);
    }
}

public sealed class HistoryResponse
{
    [JsonPropertyName("entries")]
    public List<Models.AccountHistoryEntry> Entries { get; set; } = [];
}
```

- [ ] **Step 5: Create AddHistory endpoint**

Create `src/RegMailNet.Api/Endpoints/AddHistory.cs`:

```csharp
using FastEndpoints;
using RegMailNet.Api.Requests;
using RegMailNet.Api.Services;

namespace RegMailNet.Api.Endpoints;

public sealed class AddHistory : Endpoint<AddHistoryRequest>
{
    public override void Configure()
    {
        Post("/api/history");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AddHistoryRequest req, CancellationToken ct)
    {
        var service = Resolve<FileHistoryService>();
        await service.AddAsync(req.Entry);
        await SendOkAsync(ct);
    }
}
```

- [ ] **Step 6: Commit**

```bash
git add src/RegMailNet.Api/Requests/ src/RegMailNet.Api/Endpoints/
git commit -m "feat(api): add settings and history endpoints"
```

---

## Task 3: API — Update Program.cs with CORS and Service Registration

**Files:**
- Modify: `src/RegMailNet.Api/Program.cs`
- Create: `src/RegMailNet.Api/Properties/launchSettings.json`

- [ ] **Step 1: Add launchSettings.json for the API**

Create `src/RegMailNet.Api/Properties/launchSettings.json`:

```json
{
  "profiles": {
    "RegMailNet.Api": {
      "commandName": "project",
      "applicationUrl": "https://localhost:5000;http://localhost:5002"
    }
  }
}
```

- [ ] **Step 2: Update Program.cs to register services and add CORS**

Replace `src/RegMailNet.Api/Program.cs` with:

```csharp
using FastEndpoints;
using FastEndpoints.Swagger;
using RegMailNet;
using RegMailNet.Api.Configuration;
using RegMailNet.Api.Services;
using RegMailNet.Browser;
using RegMailNet.Configuration;
using RegMailNet.EmailProviders;
using RegMailNet.SmsServices;
using RegMailNet.Utilities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRegMailNet(builder.Configuration);
builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection(ApiOptions.SectionName));

builder.Services.AddSingleton(sp =>
{
    var regMailNetOptions = Microsoft.Extensions.Options.Options.Create(
        builder.Configuration.GetSection(RegMailNetOptions.SectionName).Get<RegMailNetOptions>() ?? new());

    var apiConfig = builder.Configuration.GetSection(ApiOptions.SectionName).Get<ApiOptions>() ?? new();

    return new RegMailNetManager(
        captchaKeys: apiConfig.CaptchaKeys,
        smsKeys: apiConfig.SmsKeys,
        proxies: apiConfig.Proxies.Count > 0 ? apiConfig.Proxies : null,
        autoProxy: apiConfig.AutoProxy,
        headless: apiConfig.Headless,
        browserFactory: sp.GetRequiredService<IBrowserFactory>(),
        smsServiceFactory: sp.GetRequiredService<ISmsServiceFactory>(),
        freeProxyService: sp.GetRequiredService<IFreeProxyService>(),
        outlookProvider: sp.GetRequiredService<OutlookProvider>(),
        gmailProvider: sp.GetRequiredService<GmailProvider>(),
        yahooProvider: sp.GetRequiredService<YahooProvider>(),
        dataGenerator: sp.GetRequiredService<DataGenerator>(),
        options: regMailNetOptions);
});

// File storage services
builder.Services.AddSingleton<FileSettingsService>();
builder.Services.AddSingleton<FileHistoryService>();

builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:5001", "http://localhost:5003")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();
app.UseFastEndpoints();
app.UseSwaggerGen();

app.Run();
```

- [ ] **Step 3: Commit**

```bash
git add src/RegMailNet.Api/Program.cs src/RegMailNet.Api/Properties/launchSettings.json
git commit -m "feat(api): add CORS policy and register file storage services"
```

---

## Task 4: UI — Delete MAUI Artifacts and Replace Project File

**Files:**
- Delete: `src/RegMailNet.Ui/MauiProgram.cs`
- Delete: `src/RegMailNet.Ui/App.xaml`
- Delete: `src/RegMailNet.Ui/App.xaml.cs`
- Delete: `src/RegMailNet.Ui/MainPage.xaml`
- Delete: `src/RegMailNet.Ui/MainPage.xaml.cs`
- Delete: `src/RegMailNet.Ui/Platforms/` (entire directory)
- Delete: `src/RegMailNet.Ui/Resources/` (entire directory)
- Delete: `src/RegMailNet.Ui/Properties/` (entire directory)
- Delete: `src/RegMailNet.Ui/wwwroot/RegMailNet.Ui.styles.css` (if exists)
- Replace: `src/RegMailNet.Ui/RegMailNet.Ui.csproj`

- [ ] **Step 1: Delete MAUI-specific files and directories**

```bash
rm src/RegMailNet.Ui/MauiProgram.cs
rm src/RegMailNet.Ui/App.xaml
rm src/RegMailNet.Ui/App.xaml.cs
rm src/RegMailNet.Ui/MainPage.xaml
rm src/RegMailNet.Ui/MainPage.xaml.cs
rm -rf src/RegMailNet.Ui/Platforms/
rm -rf src/RegMailNet.Ui/Resources/
rm -rf src/RegMailNet.Ui/Properties/
rm -f src/RegMailNet.Ui/wwwroot/RegMailNet.Ui.styles.css
```

- [ ] **Step 2: Delete old service files**

Delete the old local-file-based services (they will be replaced with API-backed versions):

```bash
rm src/RegMailNet.Ui/Services/SettingsService.cs
rm src/RegMailNet.Ui/Services/AccountHistoryService.cs
```

- [ ] **Step 3: Replace the .csproj file**

Replace `src/RegMailNet.Ui/RegMailNet.Ui.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>RegMailNet.Ui</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BlazorBlueprint.Components" Version="3.11.0" />
    <PackageReference Include="BlazorBlueprint.Icons.Lucide" Version="2.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="9.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="9.0.0" PrivateAssets="all" />
  </ItemGroup>

</Project>
```

Note: The project reference to `RegMailNet.csproj` is removed — the WASM app doesn't need the core library. All operations go through the API via HTTP.

- [ ] **Step 4: Commit**

```bash
git add -A src/RegMailNet.Ui/
git commit -m "refactor(ui): remove MAUI artifacts and replace csproj with Blazor WASM SDK"
```

---

## Task 5: UI — Create Models and API Client Services

**Files:**
- Create: `src/RegMailNet.Ui/Models/AppSettings.cs`
- Create: `src/RegMailNet.Ui/Models/AccountHistoryEntry.cs`
- Create: `src/RegMailNet.Ui/Services/ISettingsService.cs`
- Create: `src/RegMailNet.Ui/Services/SettingsService.cs`
- Create: `src/RegMailNet.Ui/Services/IAccountHistoryService.cs`
- Create: `src/RegMailNet.Ui/Services/AccountHistoryService.cs`
- Create: `src/RegMailNet.Ui/Services/IAccountCreationService.cs`
- Create: `src/RegMailNet.Ui/Services/AccountCreationService.cs`

- [ ] **Step 1: Create AppSettings model**

Create `src/RegMailNet.Ui/Models/AppSettings.cs`:

```csharp
namespace RegMailNet.Ui.Models;

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
    public List<string> Proxies { get; set; } = [];
}
```

- [ ] **Step 2: Create AccountHistoryEntry model**

Create `src/RegMailNet.Ui/Models/AccountHistoryEntry.cs`:

```csharp
namespace RegMailNet.Ui.Models;

public class AccountHistoryEntry
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Provider { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "";
}
```

- [ ] **Step 3: Create ISettingsService and SettingsService**

Create `src/RegMailNet.Ui/Services/ISettingsService.cs`:

```csharp
using RegMailNet.Ui.Models;

namespace RegMailNet.Ui.Services;

public interface ISettingsService
{
    Task<AppSettings> GetSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
}
```

Create `src/RegMailNet.Ui/Services/SettingsService.cs`:

```csharp
using System.Net.Http.Json;
using RegMailNet.Ui.Models;

namespace RegMailNet.Ui.Services;

public sealed class SettingsService : ISettingsService
{
    private readonly HttpClient _http;

    public SettingsService(HttpClient http)
    {
        _http = http;
    }

    public async Task<AppSettings> GetSettingsAsync()
    {
        var result = await _http.GetFromJsonAsync<AppSettings>("api/settings");
        return result ?? new AppSettings();
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        var response = await _http.PutAsJsonAsync("api/settings", new { Settings = settings });
        response.EnsureSuccessStatusCode();
    }
}
```

- [ ] **Step 4: Create IAccountHistoryService and AccountHistoryService**

Create `src/RegMailNet.Ui/Services/IAccountHistoryService.cs`:

```csharp
using RegMailNet.Ui.Models;

namespace RegMailNet.Ui.Services;

public interface IAccountHistoryService
{
    Task<IReadOnlyList<AccountHistoryEntry>> GetHistoryAsync();
    Task AddAsync(AccountHistoryEntry entry);
}
```

Create `src/RegMailNet.Ui/Services/AccountHistoryService.cs`:

```csharp
using System.Net.Http.Json;
using RegMailNet.Ui.Models;

namespace RegMailNet.Ui.Services;

public sealed class AccountHistoryService : IAccountHistoryService
{
    private readonly HttpClient _http;
    private IReadOnlyList<AccountHistoryEntry> _cached = [];

    public AccountHistoryService(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<AccountHistoryEntry>> GetHistoryAsync()
    {
        var result = await _http.GetFromJsonAsync<HistoryResponse>("api/history");
        _cached = result?.Entries ?? [];
        return _cached;
    }

    public async Task AddAsync(AccountHistoryEntry entry)
    {
        var response = await _http.PostAsJsonAsync("api/history", new { Entry = entry });
        response.EnsureSuccessStatusCode();
        // Invalidate cache
        _cached = [];
    }

    private class HistoryResponse
    {
        public List<AccountHistoryEntry> Entries { get; set; } = [];
    }
}
```

- [ ] **Step 5: Create IAccountCreationService and AccountCreationService**

Create `src/RegMailNet.Ui/Services/IAccountCreationService.cs`:

```csharp
namespace RegMailNet.Ui.Services;

public interface IAccountCreationService
{
    Task<AccountCreatedResult> CreateOutlookAsync(bool useProxy);
    Task<AccountCreatedResult> CreateGmailAsync(bool useProxy);
    Task<AccountCreatedResult> CreateYahooAsync(bool useProxy);
}

public sealed class AccountCreatedResult
{
    public string Email { get; init; } = "";
    public string Password { get; init; } = "";
    public string Provider { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public bool Success { get; init; }
}
```

Create `src/RegMailNet.Ui/Services/AccountCreationService.cs`:

```csharp
using System.Net.Http.Json;

namespace RegMailNet.Ui.Services;

public sealed class AccountCreationService : IAccountCreationService
{
    private readonly HttpClient _http;

    public AccountCreationService(HttpClient http)
    {
        _http = http;
    }

    public async Task<AccountCreatedResult> CreateOutlookAsync(bool useProxy)
        => await CreateAccountAsync("api/accounts/outlook", useProxy);

    public async Task<AccountCreatedResult> CreateGmailAsync(bool useProxy)
        => await CreateAccountAsync("api/accounts/gmail", useProxy);

    public async Task<AccountCreatedResult> CreateYahooAsync(bool useProxy)
        => await CreateAccountAsync("api/accounts/yahoo", useProxy);

    private async Task<AccountCreatedResult> CreateAccountAsync(string url, bool useProxy)
    {
        var response = await _http.PostAsJsonAsync(url, new { UseProxy = useProxy });

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            return new AccountCreatedResult
            {
                Provider = url.Split('/').Last(),
                Success = false
            };
        }

        var result = await response.Content.ReadFromJsonAsync<AccountCreatedResult>();
        return result with { Success = true };
    }
}
```

- [ ] **Step 6: Commit**

```bash
git add src/RegMailNet.Ui/Models/ src/RegMailNet.Ui/Services/
git commit -m "feat(ui): add models and API client services for WASM"
```

---

## Task 6: UI — Create Program.cs and Update index.html

**Files:**
- Create: `src/RegMailNet.Ui/Program.cs`
- Modify: `src/RegMailNet.Ui/wwwroot/index.html`
- Modify: `src/RegMailNet.Ui/Components/Routes.razor`
- Create: `src/RegMailNet.Ui/Properties/launchSettings.json`

- [ ] **Step 1: Create Program.cs**

Create `src/RegMailNet.Ui/Program.cs`:

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RegMailNet.Ui.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddBlazorBlueprintComponents();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IAccountHistoryService, AccountHistoryService>();
builder.Services.AddScoped<IAccountCreationService, AccountCreationService>();

await builder.Build().RunAsync();
```

- [ ] **Step 2: Update index.html**

Replace `src/RegMailNet.Ui/wwwroot/index.html` with:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>RegMailNet</title>
    <base href="/" />
    <link rel="stylesheet" href="css/theme.css" />
    <link rel="stylesheet" href="_content/BlazorBlueprint.Components/blazorblueprint.css" />
    <link rel="icon" href="data:,">
</head>
<body>
    <div id="app">Loading...</div>
    <script src="_framework/blazor.web.js"></script>
</body>
</html>
```

- [ ] **Step 3: Update Routes.razor**

Replace `src/RegMailNet.Ui/Components/Routes.razor` with:

```razor
<Router AppAssembly="typeof(Program).Assembly" NotFoundPage="typeof(Pages.NotFound)">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>
```

- [ ] **Step 4: Create launchSettings.json for the UI**

Create `src/RegMailNet.Ui/Properties/launchSettings.json`:

```json
{
  "profiles": {
    "RegMailNet.Ui": {
      "commandName": "project",
      "applicationUrl": "https://localhost:5001;http://localhost:5003"
    }
  }
}
```

- [ ] **Step 5: Commit**

```bash
git add src/RegMailNet.Ui/Program.cs src/RegMailNet.Ui/wwwroot/index.html src/RegMailNet.Ui/Components/Routes.razor src/RegMailNet.Ui/Properties/launchSettings.json
git commit -m "feat(ui): add WASM Program.cs, update index.html and routing"
```

---

## Task 7: UI — Create App.razor Entry Component

**Files:**
- Create: `src/RegMailNet.Ui/App.razor`

Blazor WASM needs an `App.razor` component (unlike MAUI hybrid which wires routing differently). The `Program.cs` references `App` as the root component.

- [ ] **Step 1: Create App.razor**

Create `src/RegMailNet.Ui/App.razor`:

```razor
<RegMailNet.Ui.Components.Routes />
```

- [ ] **Step 2: Commit**

```bash
git add src/RegMailNet.Ui/App.razor
git commit -m "feat(ui): add App.razor root component for WASM"
```

---

## Task 8: UI — Update Razor Pages to Use API Services

**Files:**
- Modify: `src/RegMailNet.Ui/Components/Pages/CreateAccount.razor`
- Modify: `src/RegMailNet.Ui/Components/Pages/Home.razor`
- Modify: `src/RegMailNet.Ui/Components/Pages/History.razor`
- Modify: `src/RegMailNet.Ui/Components/Pages/Proxies.razor`
- Modify: `src/RegMailNet.Ui/Components/Pages/Settings.razor`

- [ ] **Step 1: Update CreateAccount.razor**

Replace `src/RegMailNet.Ui/Components/Pages/CreateAccount.razor` with:

```razor
@page "/create"
@inject Services.IAccountCreationService AccountService
@inject Services.IAccountHistoryService HistoryService
@using RegMailNet.Ui.Models

<div class="max-w-2xl space-y-6">
    <div>
        <h1 class="text-3xl font-bold tracking-tight text-foreground">Create Account</h1>
        <p class="text-muted-foreground mt-1">Select a provider and create an account instantly.</p>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <BbCard Class="@($"cursor-pointer transition-all {(selectedProvider == "outlook" ? "ring-2 ring-primary" : "")}")"
                @onclick="@(() => selectedProvider = "outlook")">
            <BbCardContent class="text-center py-6">
                <div class="text-3xl mb-2">Outlook</div>
                <div class="text-sm text-muted-foreground">@@outlook.com</div>
                <div class="text-xs text-green-600 mt-2">Captcha solver required</div>
            </BbCardContent>
        </BbCard>

        <BbCard Class="@($"cursor-pointer transition-all {(selectedProvider == "gmail" ? "ring-2 ring-primary" : "")}")"
                @onclick="@(() => selectedProvider = "gmail")">
            <BbCardContent class="text-center py-6">
                <div class="text-3xl mb-2">Gmail</div>
                <div class="text-sm text-muted-foreground">@@gmail.com</div>
                <div class="text-xs text-amber-600 mt-2">SMS verification required</div>
            </BbCardContent>
        </BbCard>

        <BbCard Class="@($"cursor-pointer transition-all {(selectedProvider == "yahoo" ? "ring-2 ring-primary" : "")}")"
                @onclick="@(() => selectedProvider = "yahoo")">
            <BbCardContent class="text-center py-6">
                <div class="text-3xl mb-2">Yahoo</div>
                <div class="text-sm text-muted-foreground">@@yahoo.com</div>
                <div class="text-xs text-amber-600 mt-2">Captcha + SMS required</div>
            </BbCardContent>
        </BbCard>
    </div>

    <BbCard>
        <BbCardHeader>
            <BbCardTitle>Options</BbCardTitle>
        </BbCardHeader>
        <BbCardContent>
            <div class="flex items-center">
                <label class="flex items-center gap-2 text-sm cursor-pointer">
                    <input type="checkbox" @bind="useProxy" class="rounded" />
                    Use proxy
                </label>
            </div>
        </BbCardContent>
    </BbCard>

    @if (!string.IsNullOrEmpty(errorMessage))
    {
        <BbAlert Variant="AlertVariant.Danger">
            <BbAlertDescription>@errorMessage</BbAlertDescription>
        </BbAlert>
    }

    @if (!string.IsNullOrEmpty(successEmail))
    {
        <BbAlert Variant="AlertVariant.Success">
            <BbAlertDescription>
                Account created: <strong>@successEmail</strong> / <strong>@successPassword</strong>
            </BbAlertDescription>
        </BbAlert>
    }

    <div class="flex justify-center">
        <BbButton Size="ButtonSize.Large" Disabled="@(isCreating || string.IsNullOrEmpty(selectedProvider))"
                  @onclick="CreateAccountAsync">
            @if (isCreating)
            {
                <span>Creating...</span>
            }
            else
            {
                <span>Create @(selectedProvider?.ToUpperInvariant() ?? "") Account</span>
            }
        </BbButton>
    </div>
</div>

@code {
    private string? selectedProvider;
    private bool useProxy = true;
    private bool isCreating;
    private string? errorMessage;
    private string? successEmail;
    private string? successPassword;

    private async Task CreateAccountAsync()
    {
        if (string.IsNullOrEmpty(selectedProvider)) return;

        isCreating = true;
        errorMessage = null;
        successEmail = null;
        successPassword = null;

        try
        {
            var result = selectedProvider switch
            {
                "outlook" => await AccountService.CreateOutlookAsync(useProxy: useProxy),
                "gmail" => await AccountService.CreateGmailAsync(useProxy: useProxy),
                "yahoo" => await AccountService.CreateYahooAsync(useProxy: useProxy),
                _ => throw new ArgumentException("Unknown provider")
            };

            if (result.Success)
            {
                successEmail = result.Email;
                successPassword = result.Password;
                await HistoryService.AddAsync(new AccountHistoryEntry
                {
                    Email = result.Email,
                    Password = result.Password,
                    Provider = selectedProvider!.Substring(0, 1).ToUpper() + selectedProvider.Substring(1),
                    CreatedAt = DateTime.Now,
                    Status = "Success"
                });
            }
            else
            {
                errorMessage = "Account creation failed. Check API logs for details.";
                await HistoryService.AddAsync(new AccountHistoryEntry
                {
                    Email = "-",
                    Password = "-",
                    Provider = selectedProvider!,
                    CreatedAt = DateTime.Now,
                    Status = "Failed"
                });
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            await HistoryService.AddAsync(new AccountHistoryEntry
            {
                Email = "-",
                Password = "-",
                Provider = selectedProvider!,
                CreatedAt = DateTime.Now,
                Status = $"Failed: {ex.Message}"
            });
        }
        finally
        {
            isCreating = false;
        }
    }
}
```

- [ ] **Step 2: Update Home.razor**

Replace `src/RegMailNet.Ui/Components/Pages/Home.razor` with:

```razor
@page "/"
@inject Services.IAccountHistoryService HistoryService
@inject Services.ISettingsService SettingsService
@using RegMailNet.Ui.Models

@if (isLoading)
{
    <div class="flex items-center justify-center py-20">
        <p class="text-muted-foreground">Loading...</p>
    </div>
}
else
{
    <div class="max-w-4xl space-y-6">
        <div>
            <h1 class="text-3xl font-bold tracking-tight text-foreground">Dashboard</h1>
            <p class="text-muted-foreground mt-1">Overview of your email account creation.</p>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
            <BbCard>
                <BbCardHeader>
                    <BbCardDescription>Total Created</BbCardDescription>
                    <BbCardTitle Class="text-2xl">@entries.Count(e => e.Status == "Success")</BbCardTitle>
                </BbCardHeader>
            </BbCard>
            <BbCard>
                <BbCardHeader>
                    <BbCardDescription>Outlook</BbCardDescription>
                    <BbCardTitle Class="text-2xl">@entries.Count(e => e.Provider == "Outlook" && e.Status == "Success")</BbCardTitle>
                </BbCardHeader>
            </BbCard>
            <BbCard>
                <BbCardHeader>
                    <BbCardDescription>Gmail</BbCardDescription>
                    <BbCardTitle Class="text-2xl">@entries.Count(e => e.Provider == "Gmail" && e.Status == "Success")</BbCardTitle>
                </BbCardHeader>
            </BbCard>
            <BbCard>
                <BbCardHeader>
                    <BbCardDescription>Yahoo</BbCardDescription>
                    <BbCardTitle Class="text-2xl">@entries.Count(e => e.Provider == "Yahoo" && e.Status == "Success")</BbCardTitle>
                </BbCardHeader>
            </BbCard>
        </div>

        <BbCard>
            <BbCardHeader>
                <BbCardTitle>Service Status</BbCardTitle>
                <BbCardDescription>API key configuration status.</BbCardDescription>
            </BbCardHeader>
            <BbCardContent>
                <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
                    <div class="flex items-center gap-2 text-sm">
                        <span class="w-2 h-2 rounded-full @(string.IsNullOrEmpty(settings.CapsolverKey) ? "bg-destructive" : "bg-green-500")"></span>
                        Capsolver — @(string.IsNullOrEmpty(settings.CapsolverKey) ? "No API Key" : "Connected")
                    </div>
                    <div class="flex items-center gap-2 text-sm">
                        <span class="w-2 h-2 rounded-full @(string.IsNullOrEmpty(settings.NopechaKey) ? "bg-destructive" : "bg-green-500")"></span>
                        Nopecha — @(string.IsNullOrEmpty(settings.NopechaKey) ? "No API Key" : "Connected")
                    </div>
                    <div class="flex items-center gap-2 text-sm">
                        <span class="w-2 h-2 rounded-full @(string.IsNullOrEmpty(settings.SmsPoolToken) ? "bg-destructive" : "bg-green-500")"></span>
                        SmsPool — @(string.IsNullOrEmpty(settings.SmsPoolToken) ? "No API Key" : "Connected")
                    </div>
                    <div class="flex items-center gap-2 text-sm">
                        <span class="w-2 h-2 rounded-full @(string.IsNullOrEmpty(settings.FiveSimToken) ? "bg-destructive" : "bg-green-500")"></span>
                        5Sim — @(string.IsNullOrEmpty(settings.FiveSimToken) ? "No API Key" : "Connected")
                    </div>
                    <div class="flex items-center gap-2 text-sm">
                        <span class="w-2 h-2 rounded-full @(string.IsNullOrEmpty(settings.GetsmsCodeToken) ? "bg-destructive" : "bg-green-500")"></span>
                        GetsmsCode — @(string.IsNullOrEmpty(settings.GetsmsCodeToken) ? "No API Key" : "Connected")
                    </div>
                </div>
            </BbCardContent>
        </BbCard>

        <BbCard>
            <BbCardHeader>
                <BbCardTitle>Recent Activity</BbCardTitle>
            </BbCardHeader>
            <BbCardContent>
                @if (!entries.Any())
                {
                    <p class="text-sm text-muted-foreground text-center py-8">
                        No accounts created yet. Go to <a href="create" class="text-primary hover:underline">Create Account</a> to get started.
                    </p>
                }
                else
                {
                    <div class="space-y-2">
                        @foreach (var entry in entries.Take(5))
                        {
                            <div class="flex items-center justify-between text-sm py-2 border-b border-border last:border-0">
                                <div>
                                    <span class="font-medium">@entry.Email</span>
                                    <span class="text-muted-foreground ml-2">@entry.Provider</span>
                                </div>
                                <div class="flex items-center gap-3">
                                    <span class="text-muted-foreground">@entry.CreatedAt.ToString("HH:mm:ss")</span>
                                    <span class="@(entry.Status == "Success" ? "text-green-600" : "text-destructive")">@entry.Status</span>
                                </div>
                            </div>
                        }
                    </div>
                }
            </BbCardContent>
        </BbCard>
    </div>
}

@code {
    private bool isLoading = true;
    private IReadOnlyList<AccountHistoryEntry> entries = [];
    private AppSettings settings = new();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            settings = await SettingsService.GetSettingsAsync();
            entries = await HistoryService.GetHistoryAsync();
        }
        finally
        {
            isLoading = false;
        }
    }
}
```

- [ ] **Step 3: Update History.razor**

Replace `src/RegMailNet.Ui/Components/Pages/History.razor` with:

```razor
@page "/history"
@inject Services.IAccountHistoryService HistoryService
@using RegMailNet.Ui.Models

@if (isLoading)
{
    <div class="flex items-center justify-center py-20">
        <p class="text-muted-foreground">Loading...</p>
    </div>
}
else
{
    <div class="max-w-4xl space-y-6">
        <div>
            <h1 class="text-3xl font-bold tracking-tight text-foreground">History</h1>
            <p class="text-muted-foreground mt-1">All created email accounts.</p>
        </div>

        <BbCard>
            <BbCardHeader>
                <div class="flex items-center justify-between">
                    <BbCardTitle>Accounts</BbCardTitle>
                    <select @bind="filterProvider" class="p-1.5 border border-border rounded-md bg-background text-foreground text-sm">
                        <option value="">All Providers</option>
                        <option value="Outlook">Outlook</option>
                        <option value="Gmail">Gmail</option>
                        <option value="Yahoo">Yahoo</option>
                    </select>
                </div>
            </BbCardHeader>
            <BbCardContent>
                @if (!FilteredEntries.Any())
                {
                    <p class="text-sm text-muted-foreground text-center py-8">No accounts in history.</p>
                }
                else
                {
                    <div class="overflow-x-auto">
                        <table class="w-full text-sm">
                            <thead>
                                <tr class="border-b border-border text-left text-muted-foreground">
                                    <th class="py-2 pr-4">Email</th>
                                    <th class="py-2 pr-4">Password</th>
                                    <th class="py-2 pr-4">Provider</th>
                                    <th class="py-2 pr-4">Created</th>
                                    <th class="py-2">Status</th>
                                </tr>
                            </thead>
                            <tbody>
                                @foreach (var entry in FilteredEntries)
                                {
                                    <tr class="border-b border-border last:border-0">
                                        <td class="py-2 pr-4 font-medium">@entry.Email</td>
                                        <td class="py-2 pr-4 font-mono text-xs">@entry.Password</td>
                                        <td class="py-2 pr-4">@entry.Provider</td>
                                        <td class="py-2 pr-4 text-muted-foreground">@entry.CreatedAt.ToString("yyyy-MM-dd HH:mm")</td>
                                        <td class="py-2">
                                            <span class="@(entry.Status == "Success" ? "text-green-600" : "text-destructive")">
                                                @entry.Status
                                            </span>
                                        </td>
                                    </tr>
                                }
                            </tbody>
                        </table>
                    </div>
                }
            </BbCardContent>
        </BbCard>
    </div>
}

@code {
    private bool isLoading = true;
    private IReadOnlyList<AccountHistoryEntry> entries = [];
    private string filterProvider = "";

    private IEnumerable<AccountHistoryEntry> FilteredEntries =>
        string.IsNullOrEmpty(filterProvider)
            ? entries
            : entries.Where(e => e.Provider == filterProvider);

    protected override async Task OnInitializedAsync()
    {
        try
        {
            entries = await HistoryService.GetHistoryAsync();
        }
        finally
        {
            isLoading = false;
        }
    }
}
```

- [ ] **Step 4: Update Proxies.razor**

Replace `src/RegMailNet.Ui/Components/Pages/Proxies.razor` with:

```razor
@page "/proxies"
@inject Services.ISettingsService SettingsService
@using RegMailNet.Ui.Models

@if (isLoading)
{
    <div class="flex items-center justify-center py-20">
        <p class="text-muted-foreground">Loading...</p>
    </div>
}
else
{
    <div class="max-w-2xl space-y-6">
        <div>
            <h1 class="text-3xl font-bold tracking-tight text-foreground">Proxies</h1>
            <p class="text-muted-foreground mt-1">Manage proxy servers for account creation.</p>
        </div>

        <BbCard>
            <BbCardHeader>
                <BbCardTitle>Add Proxy</BbCardTitle>
                <BbCardDescription>Format: http://user:pass@host:port</BbCardDescription>
            </BbCardHeader>
            <BbCardContent>
                <div class="flex gap-2">
                    <input @bind="newProxy" placeholder="http://user:pass@host:port"
                           class="flex-1 p-2 border border-border rounded-md bg-background text-foreground text-sm" />
                    <BbButton @onclick="AddProxy" Disabled="@string.IsNullOrWhiteSpace(newProxy)">Add</BbButton>
                </div>
            </BbCardContent>
        </BbCard>

        <BbCard>
            <BbCardHeader>
                <BbCardTitle>Configured Proxies</BbCardTitle>
                <BbCardDescription>@settings.Proxies.Count proxy(ies) configured</BbCardDescription>
            </BbCardHeader>
            <BbCardContent>
                @if (!settings.Proxies.Any())
                {
                    <p class="text-sm text-muted-foreground text-center py-4">No proxies configured. Add one above.</p>
                }
                else
                {
                    <div class="space-y-2">
                        @for (int i = 0; i < settings.Proxies.Count; i++)
                        {
                            var proxy = settings.Proxies[i];
                            var index = i;
                            <div class="flex items-center justify-between p-2 border border-border rounded-md">
                                <span class="text-sm font-mono">@proxy</span>
                                <BbButton Variant="ButtonVariant.Destructive" Size="ButtonSize.Small"
                                          @onclick="@(() => RemoveProxy(index))">
                                    Remove
                                </BbButton>
                            </div>
                        }
                    </div>
                }
            </BbCardContent>
        </BbCard>
    </div>
}

@code {
    private bool isLoading = true;
    private AppSettings settings = new();
    private string newProxy = "";

    protected override async Task OnInitializedAsync()
    {
        try
        {
            settings = await SettingsService.GetSettingsAsync();
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task AddProxy()
    {
        if (string.IsNullOrWhiteSpace(newProxy)) return;
        settings.Proxies.Add(newProxy.Trim());
        await SettingsService.SaveSettingsAsync(settings);
        newProxy = "";
    }

    private async Task RemoveProxy(int index)
    {
        settings.Proxies.RemoveAt(index);
        await SettingsService.SaveSettingsAsync(settings);
    }
}
```

- [ ] **Step 5: Update Settings.razor**

Replace `src/RegMailNet.Ui/Components/Pages/Settings.razor` with:

```razor
@page "/settings"
@inject Services.ISettingsService SettingsService
@using RegMailNet.Ui.Models

@if (isLoading)
{
    <div class="flex items-center justify-center py-20">
        <p class="text-muted-foreground">Loading...</p>
    </div>
}
else
{
    <div class="max-w-2xl space-y-6">
        <div>
            <h1 class="text-3xl font-bold tracking-tight text-foreground">Settings</h1>
            <p class="text-muted-foreground mt-1">Configure API keys and defaults.</p>
        </div>

        <BbCard>
            <BbCardHeader>
                <BbCardTitle>Browser</BbCardTitle>
                <BbCardDescription>Default browser for account creation.</BbCardDescription>
            </BbCardHeader>
            <BbCardContent>
                <select @bind="settings.Browser" class="w-full p-2 border border-border rounded-md bg-background text-foreground text-sm">
                    <option value="firefox">Firefox</option>
                    <option value="chrome">Chrome</option>
                    <option value="undetected-chrome">Undetected Chrome</option>
                </select>
            </BbCardContent>
        </BbCard>

        <BbCard>
            <BbCardHeader>
                <BbCardTitle>Captcha Services</BbCardTitle>
                <BbCardDescription>API keys for captcha solving services.</BbCardDescription>
            </BbCardHeader>
            <BbCardContent class="space-y-4">
                <div>
                    <label class="text-sm text-muted-foreground mb-1 block">Capsolver API Key</label>
                    <input @bind="settings.CapsolverKey" type="password" placeholder="Enter key..."
                           class="w-full p-2 border border-border rounded-md bg-background text-foreground text-sm" />
                </div>
                <div>
                    <label class="text-sm text-muted-foreground mb-1 block">Nopecha API Key</label>
                    <input @bind="settings.NopechaKey" type="password" placeholder="Enter key..."
                           class="w-full p-2 border border-border rounded-md bg-background text-foreground text-sm" />
                </div>
            </BbCardContent>
        </BbCard>

        <BbCard>
            <BbCardHeader>
                <BbCardTitle>SMS Services</BbCardTitle>
                <BbCardDescription>API keys for phone verification services.</BbCardDescription>
            </BbCardHeader>
            <BbCardContent class="space-y-4">
                <div>
                    <label class="text-sm text-muted-foreground mb-1 block">Default SMS Service</label>
                    <select @bind="settings.DefaultSmsService" class="w-full p-2 border border-border rounded-md bg-background text-foreground text-sm">
                        <option value="smspool">SmsPool</option>
                        <option value="5sim">5Sim</option>
                        <option value="getsmscode">GetsmsCode</option>
                    </select>
                </div>
                <div>
                    <label class="text-sm text-muted-foreground mb-1 block">SmsPool Token</label>
                    <input @bind="settings.SmsPoolToken" type="password" placeholder="Enter token..."
                           class="w-full p-2 border border-border rounded-md bg-background text-foreground text-sm" />
                </div>
                <div>
                    <label class="text-sm text-muted-foreground mb-1 block">5Sim Token</label>
                    <input @bind="settings.FiveSimToken" type="password" placeholder="Enter token..."
                           class="w-full p-2 border border-border rounded-md bg-background text-foreground text-sm" />
                </div>
                <div>
                    <label class="text-sm text-muted-foreground mb-1 block">GetsmsCode Username</label>
                    <input @bind="settings.GetsmsCodeUser" placeholder="Enter username..."
                           class="w-full p-2 border border-border rounded-md bg-background text-foreground text-sm" />
                </div>
                <div>
                    <label class="text-sm text-muted-foreground mb-1 block">GetsmsCode Token</label>
                    <input @bind="settings.GetsmsCodeToken" type="password" placeholder="Enter token..."
                           class="w-full p-2 border border-border rounded-md bg-background text-foreground text-sm" />
                </div>
            </BbCardContent>
        </BbCard>

        <div class="flex justify-end">
            <BbButton Size="ButtonSize.Large" @onclick="Save">Save Settings</BbButton>
        </div>

        @if (showSaved)
        {
            <BbAlert Variant="AlertVariant.Success">
                <BbAlertDescription>Settings saved successfully.</BbAlertDescription>
            </BbAlert>
        }
    </div>
}

@code {
    private bool isLoading = true;
    private bool showSaved;
    private AppSettings settings = new();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            settings = await SettingsService.GetSettingsAsync();
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task Save()
    {
        await SettingsService.SaveSettingsAsync(settings);
        showSaved = true;
        await Task.Delay(2000);
        showSaved = false;
    }
}
```

- [ ] **Step 6: Commit**

```bash
git add src/RegMailNet.Ui/Components/Pages/
git commit -m "feat(ui): update all pages to use API-backed services"
```

---

## Task 9: Solution — Update slnx and Verify Build

**Files:**
- Modify: `RegMailNet.slnx`
- Modify: `src/RegMailNet.Ui/Components/_Imports.razor`

- [ ] **Step 1: Update solution file to include API project**

Replace `RegMailNet.slnx` with:

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/RegMailNet/RegMailNet.csproj" />
    <Project Path="src/RegMailNet.Ui/RegMailNet.Ui.csproj" />
    <Project Path="src/RegMailNet.Api/RegMailNet.Api.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/RegMailNet.Tests/RegMailNet.Tests.csproj" />
  </Folder>
</Solution>
```

- [ ] **Step 2: Update _Imports.razor**

Replace `src/RegMailNet.Ui/Components/_Imports.razor` with:

```razor
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using RegMailNet.Ui
@using RegMailNet.Ui.Components
@using RegMailNet.Ui.Components.Layout
@using RegMailNet.Ui.Models
@using BlazorBlueprint.Components
@using BlazorBlueprint.Icons.Lucide.Components
@using BlazorBlueprint.Icons.Lucide.Data
```

Changes: Added `@using RegMailNet.Ui.Models`, removed unused namespaces.

- [ ] **Step 3: Build the UI project**

Run: `dotnet build src/RegMailNet.Ui/RegMailNet.Ui.csproj`
Expected: Build succeeds with no errors.

- [ ] **Step 4: Build the API project**

Run: `dotnet build src/RegMailNet.Api/RegMailNet.Api.csproj`
Expected: Build succeeds with no errors.

- [ ] **Step 5: Commit**

```bash
git add RegMailNet.slnx src/RegMailNet.Ui/Components/_Imports.razor
git commit -m "chore: update solution file and imports for WASM migration"
```
