using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;
using RegMailNet;
using RegMailNet.Api.Configuration;
using RegMailNet.Api.Data;
using RegMailNet.Api.Services;
using RegMailNet.Browser;
using RegMailNet.Configuration;
using RegMailNet.EmailProviders;
using RegMailNet.SmsServices;
using RegMailNet.Utilities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRegMailNet(builder.Configuration);
builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection(ApiOptions.SectionName));

// EF Core + PostgreSQL — must register before singleton that uses it
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<EfSettingsService>();
builder.Services.AddScoped<EfHistoryService>();

builder.Services.AddSingleton(sp =>
{
    var regMailNetOptions = Microsoft.Extensions.Options.Options.Create(
        builder.Configuration.GetSection(RegMailNetOptions.SectionName).Get<RegMailNetOptions>() ?? new());

    var apiConfig = builder.Configuration.GetSection(ApiOptions.SectionName).Get<ApiOptions>() ?? new();

    // Load settings from database via a scoped DbContext
    using var scope = sp.CreateScope();
    var settingsService = scope.ServiceProvider.GetRequiredService<EfSettingsService>();
    var dbSettings = settingsService.LoadAsync().GetAwaiter().GetResult();

    Dictionary<string, string> BuildCaptchaKeys()
    {
        var keys = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(dbSettings.CapsolverKey))
            keys[CaptchaSolver.Capsolver.ToValue()] = dbSettings.CapsolverKey;
        if (!string.IsNullOrEmpty(dbSettings.NopechaKey))
            keys[CaptchaSolver.Nopecha.ToValue()] = dbSettings.NopechaKey;
        return keys;
    }

    Dictionary<string, Dictionary<string, string>> BuildSmsKeys()
    {
        var keys = new Dictionary<string, Dictionary<string, string>>();
        if (!string.IsNullOrEmpty(dbSettings.SmsPoolToken))
            keys[SmsService.SmsPool.ToValue()] = new() { ["token"] = dbSettings.SmsPoolToken };
        if (!string.IsNullOrEmpty(dbSettings.FiveSimToken))
            keys[SmsService.FiveSim.ToValue()] = new() { ["token"] = dbSettings.FiveSimToken };
        if (!string.IsNullOrEmpty(dbSettings.GetsmsCodeToken))
            keys[SmsService.GetsmsCode.ToValue()] = new()
            {
                ["user"] = dbSettings.GetsmsCodeUser,
                ["token"] = dbSettings.GetsmsCodeToken
            };
        return keys;
    }

    return new RegMailNetManager(
        captchaKeysProvider: BuildCaptchaKeys,
        smsKeysProvider: BuildSmsKeys,
        proxies: dbSettings.Proxies.Count > 0 ? dbSettings.Proxies : apiConfig.Proxies.Count > 0 ? apiConfig.Proxies : null,
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

builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:5001", "http://localhost:5003", "http://localhost:5000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();
app.UseFastEndpoints();
app.UseSwaggerGen();

// Apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
