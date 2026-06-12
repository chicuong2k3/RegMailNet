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

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5002")
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
            keys["capsolver"] = settingsService.Settings.CapsolverKey;
        if (!string.IsNullOrEmpty(settingsService.Settings.NopechaKey))
            keys["nopecha"] = settingsService.Settings.NopechaKey;
        return keys;
    }

    Dictionary<string, Dictionary<string, string>> BuildSmsKeys()
    {
        var keys = new Dictionary<string, Dictionary<string, string>>();
        if (!string.IsNullOrEmpty(settingsService.Settings.SmsPoolToken))
            keys["smspool"] = new() { ["token"] = settingsService.Settings.SmsPoolToken };
        if (!string.IsNullOrEmpty(settingsService.Settings.FiveSimToken))
            keys["5sim"] = new() { ["token"] = settingsService.Settings.FiveSimToken };
        if (!string.IsNullOrEmpty(settingsService.Settings.GetsmsCodeToken))
            keys["getsmscode"] = new()
            {
                ["user"] = settingsService.Settings.GetsmsCodeUser,
                ["token"] = settingsService.Settings.GetsmsCodeToken
            };
        return keys;
    }

    var options = Options.Create(new RegMailNetOptions
    {
        CaptchaServicesSupported = new List<string> { "capsolver", "nopecha" },
        DefaultCaptchaService = "capsolver",
        SmsServicesSupported = new List<string> { "getsmscode", "smspool", "5sim" },
        DefaultSmsService = settingsService.Settings.DefaultSmsService,
        SupportedSolversByEmail = new List<CaptchaSolverMapping>
        {
            new() { EmailService = "outlook", Solvers = new List<string> { "capsolver", "nopecha" } },
            new() { EmailService = "yahoo", Solvers = new List<string> { "capsolver", "nopecha" } }
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
