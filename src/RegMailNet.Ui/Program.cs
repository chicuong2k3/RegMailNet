using BlazorBlueprint.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Options;
using RegMailNet;
using RegMailNet.Configuration;
using RegMailNet.EmailProviders;
using RegMailNet.SmsServices;
using RegMailNet.Utilities;
using RegMailNet.Browser;
using RegMailNet.Ui.Components;
using RegMailNet.Ui.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
builder.Services.AddScoped(sp =>
{
    var client = new HttpClient();
    if (!string.IsNullOrEmpty(apiBaseUrl))
        client.BaseAddress = new Uri(apiBaseUrl);
    return client;
});

builder.Services.AddBlazorBlueprintComponents();

// Register UI services (concrete types used by @inject in razor pages)
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<AccountHistoryService>();

// Register interfaces forwarding to the same concrete instances
builder.Services.AddScoped<ISettingsService>(sp => sp.GetRequiredService<SettingsService>());
builder.Services.AddScoped<IAccountHistoryService>(sp => sp.GetRequiredService<AccountHistoryService>());
builder.Services.AddScoped<IAccountCreationService, AccountCreationService>();

// Register RegMailNet support services
builder.Services.AddScoped<DataGenerator>();
builder.Services.AddScoped<ISmsServiceFactory, SmsServiceFactory>();
builder.Services.AddScoped<OutlookProvider>();
builder.Services.AddScoped<GmailProvider>();
builder.Services.AddScoped<YahooProvider>();
builder.Services.AddScoped<IBrowserFactory, CamoufoxBrowserFactory>();

// Register RegMailNetManager with lazy key providers
builder.Services.AddScoped(sp =>
{
    var settingsService = sp.GetRequiredService<SettingsService>();
    var browserFactory = sp.GetRequiredService<IBrowserFactory>();
    var smsFactory = sp.GetRequiredService<ISmsServiceFactory>();

    Dictionary<string, string> BuildCaptchaKeys()
    {
        var keys = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(settingsService.Settings.CapsolverKey))
            keys[CaptchaSolver.Capsolver.ToValue()] = settingsService.Settings.CapsolverKey;
        if (!string.IsNullOrEmpty(settingsService.Settings.NopechaKey))
            keys[CaptchaSolver.Nopecha.ToValue()] = settingsService.Settings.NopechaKey;
        return keys;
    }

    Dictionary<string, Dictionary<string, string>> BuildSmsKeys()
    {
        var keys = new Dictionary<string, Dictionary<string, string>>();
        if (!string.IsNullOrEmpty(settingsService.Settings.SmsPoolToken))
            keys[SmsService.SmsPool.ToValue()] = new() { ["token"] = settingsService.Settings.SmsPoolToken };
        if (!string.IsNullOrEmpty(settingsService.Settings.FiveSimToken))
            keys[SmsService.FiveSim.ToValue()] = new() { ["token"] = settingsService.Settings.FiveSimToken };
        if (!string.IsNullOrEmpty(settingsService.Settings.GetsmsCodeToken))
            keys[SmsService.GetsmsCode.ToValue()] = new()
            {
                ["user"] = settingsService.Settings.GetsmsCodeUser,
                ["token"] = settingsService.Settings.GetsmsCodeToken
            };
        return keys;
    }

    var options = Options.Create(new RegMailNetOptions
    {
        CaptchaServicesSupported = new List<string> { CaptchaSolver.Capsolver.ToValue(), CaptchaSolver.Nopecha.ToValue() },
        DefaultCaptchaService = CaptchaSolver.Capsolver.ToValue(),
        SmsServicesSupported = new List<string> { SmsService.GetsmsCode.ToValue(), SmsService.SmsPool.ToValue(), SmsService.FiveSim.ToValue() },
        DefaultSmsService = settingsService.Settings.DefaultSmsService,
        SupportedSolversByEmail = new List<CaptchaSolverMapping>
        {
            new() { EmailService = EmailProvider.Outlook.ToValue(), Solvers = new List<string> { CaptchaSolver.Capsolver.ToValue(), CaptchaSolver.Nopecha.ToValue() } },
            new() { EmailService = EmailProvider.Yahoo.ToValue(), Solvers = new List<string> { CaptchaSolver.Capsolver.ToValue(), CaptchaSolver.Nopecha.ToValue() } }
        }
    });

    return new RegMailNetManager(
        browserFactory: browserFactory,
        captchaKeysProvider: BuildCaptchaKeys,
        smsKeysProvider: BuildSmsKeys,
        proxies: settingsService.Settings.Proxies,
        smsServiceFactory: smsFactory,
        options: options);
});

await builder.Build().RunAsync();
