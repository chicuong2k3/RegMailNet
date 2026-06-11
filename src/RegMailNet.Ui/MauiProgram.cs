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

            // Build RegMailNet configuration from settings
            var captchaKeys = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(settingsService.Settings.CapsolverKey))
                captchaKeys["capsolver"] = settingsService.Settings.CapsolverKey;
            if (!string.IsNullOrEmpty(settingsService.Settings.NopechaKey))
                captchaKeys["nopecha"] = settingsService.Settings.NopechaKey;

            var smsKeys = new Dictionary<string, Dictionary<string, string>>();
            if (!string.IsNullOrEmpty(settingsService.Settings.SmsPoolToken))
                smsKeys["smspool"] = new() { ["token"] = settingsService.Settings.SmsPoolToken };
            if (!string.IsNullOrEmpty(settingsService.Settings.FiveSimToken))
                smsKeys["5sim"] = new() { ["token"] = settingsService.Settings.FiveSimToken };
            if (!string.IsNullOrEmpty(settingsService.Settings.GetsmsCodeToken))
                smsKeys["getsmscode"] = new()
                {
                    ["user"] = settingsService.Settings.GetsmsCodeUser,
                    ["token"] = settingsService.Settings.GetsmsCodeToken
                };

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
                    captchaKeys: captchaKeys,
                    smsKeys: smsKeys,
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

            return builder.Build();
        }
    }
}