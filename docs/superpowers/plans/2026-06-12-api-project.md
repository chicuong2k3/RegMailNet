# RegMailNet API Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a FastEndpoints-based ASP.NET Core API project exposing RegMailNet account creation as HTTP endpoints.

**Architecture:** Thin API layer over the existing `RegMailNetManager` facade. Each endpoint maps a POST request to the corresponding manager method. FastEndpoints REPR pattern (Request-Endpoint-Response) with FluentValidation for input validation.

**Tech Stack:** ASP.NET Core 9, FastEndpoints, FluentValidation, Swagger/OpenAPI

---

## File Structure

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `src/RegMailNet.Api/RegMailNet.Api.csproj` | Project file with dependencies |
| Create | `src/RegMailNet.Api/Program.cs` | Host setup, DI, middleware pipeline |
| Create | `src/RegMailNet.Api/appsettings.json` | Default configuration |
| Create | `src/RegMailNet.Api/Requests/CreateAccountRequest.cs` | Shared request DTO |
| Create | `src/RegMailNet.Api/Responses/AccountCreatedResponse.cs` | Shared response DTO |
| Create | `src/RegMailNet.Api/Endpoints/CreateGmailAccount.cs` | POST /api/accounts/gmail |
| Create | `src/RegMailNet.Api/Endpoints/CreateOutlookAccount.cs` | POST /api/accounts/outlook |
| Create | `src/RegMailNet.Api/Endpoints/CreateYahooAccount.cs` | POST /api/accounts/yahoo |
| Create | `src/RegMailNet.Api/Endpoints/HealthCheck.cs` | GET /api/health |

---

### Task 1: Create project file and directory structure

**Files:**
- Create: `src/RegMailNet.Api/RegMailNet.Api.csproj`

- [ ] **Step 1: Create the project file**

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>RegMailNet.Api</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FastEndpoints" Version="5.*" />
    <PackageReference Include="FastEndpoints.Swagger" Version="5.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\RegMailNet\RegMailNet.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create empty directory placeholders**

Create these empty directories (FastEndpoints discovers endpoints by scanning assemblies, so the directories just need to exist):

```
src/RegMailNet.Api/Endpoints/
src/RegMailNet.Api/Requests/
src/RegMailNet.Api/Responses/
```

- [ ] **Step 3: Commit**

```bash
git add src/RegMailNet.Api/RegMailNet.Api.csproj
git commit -m "feat(api): scaffold API project with FastEndpoints"
```

---

### Task 2: Create request and response DTOs

**Files:**
- Create: `src/RegMailNet.Api/Requests/CreateAccountRequest.cs`
- Create: `src/RegMailNet.Api/Responses/AccountCreatedResponse.cs`

- [ ] **Step 1: Create the request DTO**

```csharp
namespace RegMailNet.Api.Requests;

public sealed class CreateAccountRequest
{
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Proxy { get; init; }
    public bool UseProxy { get; init; } = true;
}
```

- [ ] **Step 2: Create the response DTO**

```csharp
namespace RegMailNet.Api.Responses;

public sealed class AccountCreatedResponse
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
```

- [ ] **Step 3: Commit**

```bash
git add src/RegMailNet.Api/Requests/ src/RegMailNet.Api/Responses/
git commit -m "feat(api): add request and response DTOs"
```

---

### Task 3: Create the three account creation endpoints

**Files:**
- Create: `src/RegMailNet.Api/Endpoints/CreateGmailAccount.cs`
- Create: `src/RegMailNet.Api/Endpoints/CreateOutlookAccount.cs`
- Create: `src/RegMailNet.Api/Endpoints/CreateYahooAccount.cs`

- [ ] **Step 1: Create Gmail endpoint**

```csharp
using FastEndpoints;
using RegMailNet.Api.Requests;
using RegMailNet.Api.Responses;

namespace RegMailNet.Api.Endpoints;

public sealed class CreateGmailAccount : Endpoint<CreateAccountRequest, AccountCreatedResponse>
{
    public override void Configure()
    {
        Post("/api/accounts/gmail");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateAccountRequest req, CancellationToken ct)
    {
        var manager = Resolve<RegMailNet.RegMailNetManager>();

        var result = await manager.CreateGmailAccountAsync(
            username: req.Username ?? "",
            password: req.Password ?? "",
            firstName: req.FirstName ?? "",
            lastName: req.LastName ?? "",
            useProxy: req.UseProxy,
            cancellationToken: ct);

        await SendOkAsync(new AccountCreatedResponse
        {
            Email = result.Email,
            Password = result.Password,
            Provider = "gmail",
            CreatedAt = DateTime.UtcNow
        }, ct);
    }
}
```

- [ ] **Step 2: Create Outlook endpoint**

```csharp
using FastEndpoints;
using RegMailNet.Api.Requests;
using RegMailNet.Api.Responses;

namespace RegMailNet.Api.Endpoints;

public sealed class CreateOutlookAccount : Endpoint<CreateAccountRequest, AccountCreatedResponse>
{
    public override void Configure()
    {
        Post("/api/accounts/outlook");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateAccountRequest req, CancellationToken ct)
    {
        var manager = Resolve<RegMailNet.RegMailNetManager>();

        var result = await manager.CreateOutlookAccountAsync(
            username: req.Username ?? "",
            password: req.Password ?? "",
            firstName: req.FirstName ?? "",
            lastName: req.LastName ?? "",
            useProxy: req.UseProxy,
            cancellationToken: ct);

        await SendOkAsync(new AccountCreatedResponse
        {
            Email = result.Email,
            Password = result.Password,
            Provider = "outlook",
            CreatedAt = DateTime.UtcNow
        }, ct);
    }
}
```

- [ ] **Step 3: Create Yahoo endpoint**

```csharp
using FastEndpoints;
using RegMailNet.Api.Requests;
using RegMailNet.Api.Responses;

namespace RegMailNet.Api.Endpoints;

public sealed class CreateYahooAccount : Endpoint<CreateAccountRequest, AccountCreatedResponse>
{
    public override void Configure()
    {
        Post("/api/accounts/yahoo");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateAccountRequest req, CancellationToken ct)
    {
        var manager = Resolve<RegMailNet.RegMailNetManager>();

        var result = await manager.CreateYahooAccountAsync(
            username: req.Username ?? "",
            password: req.Password ?? "",
            firstName: req.FirstName ?? "",
            lastName: req.LastName ?? "",
            useProxy: req.UseProxy,
            cancellationToken: ct);

        await SendOkAsync(new AccountCreatedResponse
        {
            Email = result.Email,
            Password = result.Password,
            Provider = "yahoo",
            CreatedAt = DateTime.UtcNow
        }, ct);
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add src/RegMailNet.Api/Endpoints/
git commit -m "feat(api): add account creation endpoints"
```

---

### Task 4: Create health check endpoint

**Files:**
- Create: `src/RegMailNet.Api/Endpoints/HealthCheck.cs`

- [ ] **Step 1: Create the endpoint**

```csharp
using FastEndpoints;

namespace RegMailNet.Api.Endpoints;

public sealed class HealthCheck : EndpointWithoutRequest<HealthCheckResponse>
{
    public override void Configure()
    {
        Get("/api/health");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendOkAsync(new HealthCheckResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow
        }, ct);
    }
}

public sealed class HealthCheckResponse
{
    public string Status { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/RegMailNet.Api/Endpoints/HealthCheck.cs
git commit -m "feat(api): add health check endpoint"
```

---

### Task 5: Create Program.cs and appsettings.json

**Files:**
- Create: `src/RegMailNet.Api/Program.cs`
- Create: `src/RegMailNet.Api/appsettings.json`

- [ ] **Step 1: Create appsettings.json**

```json
{
  "RegMailNet": {
    "CaptchaServicesSupported": [ "capsolver", "nopecha" ],
    "DefaultCaptchaService": "capsolver",
    "SmsServicesSupported": [ "smspool", "fivesim", "getsmscode" ],
    "DefaultSmsService": "smspool",
    "SupportedSolversByEmail": [
      { "EmailService": "outlook", "Solvers": [ "capsolver", "nopecha" ] },
      { "EmailService": "yahoo", "Solvers": [ "capsolver", "nopecha" ] }
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

- [ ] **Step 2: Create Program.cs**

```csharp
using FastEndpoints;
using FastEndpoints.Swagger;
using RegMailNet.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRegMailNet(builder.Configuration);
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();

var app = builder.Build();

app.UseFastEndpoints();
app.UseSwaggerGen();

app.Run();
```

- [ ] **Step 3: Verify the project builds**

```bash
dotnet build src/RegMailNet.Api/RegMailNet.Api.csproj
```

Expected: Build succeeds with no errors.

- [ ] **Step 4: Commit**

```bash
git add src/RegMailNet.Api/Program.cs src/RegMailNet.Api/appsettings.json
git commit -m "feat(api): add Program.cs with FastEndpoints setup and Swagger"
```

---

### Task 6: Wire up RegMailNetManager in DI

**Files:**
- Modify: `src/RegMailNet.Api/Program.cs`

The `RegMailNetManager` is currently constructed manually (not registered in `AddRegMailNet()`). We need to register it in the API's DI so endpoints can resolve it. Add a factory registration after `AddRegMailNet()`.

- [ ] **Step 1: Update Program.cs to register RegMailNetManager**

Replace the contents of `src/RegMailNet.Api/Program.cs` with:

```csharp
using FastEndpoints;
using FastEndpoints.Swagger;
using RegMailNet;
using RegMailNet.Configuration;
using RegMailNet.EmailProviders;
using RegMailNet.SmsServices;
using RegMailNet.Utilities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRegMailNet(builder.Configuration);

builder.Services.AddSingleton(sp =>
{
    var options = Microsoft.Extensions.Options.Options.Create(
        builder.Configuration.GetSection(RegMailNetOptions.SectionName).Get<RegMailNetOptions>() ?? new());

    return new RegMailNetManager(
        captchaKeys: new Dictionary<string, string>(),
        smsKeys: new Dictionary<string, Dictionary<string, string>>(),
        proxies: null,
        autoProxy: false,
        headless: true,
        browserFactory: sp.GetRequiredService<Browser.IBrowserFactory>(),
        smsServiceFactory: sp.GetRequiredService<ISmsServiceFactory>(),
        freeProxyService: sp.GetRequiredService<IFreeProxyService>(),
        outlookProvider: sp.GetRequiredService<OutlookProvider>(),
        gmailProvider: sp.GetRequiredService<GmailProvider>(),
        yahooProvider: sp.GetRequiredService<YahooProvider>(),
        dataGenerator: sp.GetRequiredService<DataGenerator>(),
        options: options);
});

builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();

var app = builder.Build();

app.UseFastEndpoints();
app.UseSwaggerGen();

app.Run();
```

- [ ] **Step 2: Verify build**

```bash
dotnet build src/RegMailNet.Api/RegMailNet.Api.csproj
```

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/RegMailNet.Api/Program.cs
git commit -m "feat(api): register RegMailNetManager in DI container"
```

---

### Task 7: Add API configuration section for keys and proxies

**Files:**
- Create: `src/RegMailNet.Api/Configuration/ApiOptions.cs`
- Modify: `src/RegMailNet.Api/Program.cs`
- Modify: `src/RegMailNet.Api/appsettings.json`

- [ ] **Step 1: Create ApiOptions to hold API-level config**

```csharp
namespace RegMailNet.Api.Configuration;

public sealed class ApiOptions
{
    public const string SectionName = "Api";

    public Dictionary<string, string> CaptchaKeys { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> SmsKeys { get; set; } = new();
    public List<string> Proxies { get; set; } = new();
    public bool AutoProxy { get; set; }
    public bool Headless { get; set; } = true;
}
```

- [ ] **Step 2: Update Program.cs to use ApiOptions for RegMailNetManager**

```csharp
using FastEndpoints;
using FastEndpoints.Swagger;
using RegMailNet;
using RegMailNet.Api.Configuration;
using RegMailNet.Configuration;
using RegMailNet.EmailProviders;
using RegMailNet.SmsServices;
using RegMailNet.Utilities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRegMailNet(builder.Configuration);
builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection(ApiOptions.SectionName));

builder.Services.AddSingleton(sp =>
{
    var apiOpts = Microsoft.Extensions.Options.Options.Create(
        builder.Configuration.GetSection(RegMailNetOptions.SectionName).Get<RegMailNetOptions>() ?? new());

    var apiConfig = builder.Configuration.GetSection(ApiOptions.SectionName).Get<ApiOptions>() ?? new();

    return new RegMailNetManager(
        captchaKeys: apiConfig.CaptchaKeys,
        smsKeys: apiConfig.SmsKeys,
        proxies: apiConfig.Proxies.Count > 0 ? apiConfig.Proxies : null,
        autoProxy: apiConfig.AutoProxy,
        headless: apiConfig.Headless,
        browserFactory: sp.GetRequiredService<Browser.IBrowserFactory>(),
        smsServiceFactory: sp.GetRequiredService<ISmsServiceFactory>(),
        freeProxyService: sp.GetRequiredService<IFreeProxyService>(),
        outlookProvider: sp.GetRequiredService<OutlookProvider>(),
        gmailProvider: sp.GetRequiredService<GmailProvider>(),
        yahooProvider: sp.GetRequiredService<YahooProvider>(),
        dataGenerator: sp.GetRequiredService<DataGenerator>(),
        options: apiOpts);
});

builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();

var app = builder.Build();

app.UseFastEndpoints();
app.UseSwaggerGen();

app.Run();
```

- [ ] **Step 3: Update appsettings.json with Api section**

```json
{
  "RegMailNet": {
    "CaptchaServicesSupported": [ "capsolver", "nopecha" ],
    "DefaultCaptchaService": "capsolver",
    "SmsServicesSupported": [ "smspool", "fivesim", "getsmscode" ],
    "DefaultSmsService": "smspool",
    "SupportedSolversByEmail": [
      { "EmailService": "outlook", "Solvers": [ "capsolver", "nopecha" ] },
      { "EmailService": "yahoo", "Solvers": [ "capsolver", "nopecha" ] }
    ]
  },
  "Api": {
    "Headless": true,
    "AutoProxy": false,
    "CaptchaKeys": {},
    "SmsKeys": {},
    "Proxies": []
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

- [ ] **Step 4: Verify build**

```bash
dotnet build src/RegMailNet.Api/RegMailNet.Api.csproj
```

Expected: Build succeeds.

- [ ] **Step 5: Commit**

```bash
git add src/RegMailNet.Api/
git commit -m "feat(api): add ApiOptions for runtime config (keys, proxies, headless)"
```

---

### Task 8: Final build verification and smoke test

- [ ] **Step 1: Full solution build**

```bash
dotnet build src/RegMailNet.Api/RegMailNet.Api.csproj
```

Expected: Build succeeds with no errors.

- [ ] **Step 2: Verify the API starts**

```bash
dotnet run --project src/RegMailNet.Api
```

Expected: Application starts, listens on `http://localhost:5000`. Swagger UI available at `http://localhost:5000/swagger`.

- [ ] **Step 3: Test health endpoint**

```bash
curl http://localhost:5000/api/health
```

Expected: `{"status":"Healthy","timestamp":"..."}`

- [ ] **Step 4: Final commit (if any fixes were needed)**

```bash
git add -A
git commit -m "fix(api): address build/startup issues from smoke test"
```
