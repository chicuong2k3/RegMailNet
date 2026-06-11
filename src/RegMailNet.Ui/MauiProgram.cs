using BlazorBlueprint.Components;
using Microsoft.Extensions.Logging;
using RegMailNet.Configuration;
using RegMailNet.EmailProviders;
using RegMailNet.SmsServices;
using RegMailNet.Utilities;
using RegMailNet.Browser;
using RegMailNet.Ui.Services;

namespace RegMailNet.Ui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddBlazorBlueprintComponents();

            // Load settings
            var settingsService = new SettingsService();
            settingsService.Load();
            builder.Services.AddSingleton(settingsService);

            // Lazy providers that read from SettingsService at call time
            // so keys saved after startup are picked up immediately.
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

            var options = Microsoft.Extensions.Options.Options.Create(new RegMailNetOptions
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

            // Register RegMailNetManager
            builder.Services.AddSingleton(sp =>
            {
                var browserFactory = sp.GetRequiredService<IBrowserFactory>();
                var smsFactory = sp.GetRequiredService<ISmsServiceFactory>();
                return new RegMailNetManager(
                    browserFactory: browserFactory,
                    captchaKeysProvider: BuildCaptchaKeys,
                    smsKeysProvider: BuildSmsKeys,
                    proxies: settingsService.Settings.Proxies,
                    smsServiceFactory: smsFactory,
                    options: options);
            });

            // Register RegMailNet support services
            builder.Services.AddSingleton<DataGenerator>();
            builder.Services.AddSingleton<ISmsServiceFactory, SmsServiceFactory>();
            builder.Services.AddSingleton<OutlookProvider>();
            builder.Services.AddSingleton<GmailProvider>();
            builder.Services.AddSingleton<YahooProvider>();
            builder.Services.AddSingleton<IBrowserFactory, CamoufoxBrowserFactory>();

            // Register UI services
            builder.Services.AddSingleton<AccountHistoryService>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Ensure camoufox Python package is installed at startup
            _ = Task.Run(async () =>
            {
                try
                {
                    var browserFactory = app.Services.GetRequiredService<IBrowserFactory>();
                    await browserFactory.EnsureInstalledAsync();
                }
                catch (Exception ex)
                {
                    var logger = app.Services.GetService<ILogger<MauiApp>>();
                    logger?.LogError(ex, "Failed to ensure camoufox is installed");
                }
            });

            return app;
        }
    }
}