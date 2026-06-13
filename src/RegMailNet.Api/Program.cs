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

// EF Core + PostgreSQL for account history
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<EfHistoryService>();

builder.Services.AddSingleton(sp =>
{
    var regMailNetOptions = Microsoft.Extensions.Options.Options.Create(
        builder.Configuration.GetSection(RegMailNetOptions.SectionName).Get<RegMailNetOptions>() ?? new());

    var apiConfig = builder.Configuration.GetSection(ApiOptions.SectionName).Get<ApiOptions>() ?? new();

    // Build captcha keys from environment variables and config
    Dictionary<string, string> BuildCaptchaKeys()
    {
        var keys = new Dictionary<string, string>();

        // From environment variables
        var nopechaKey = Environment.GetEnvironmentVariable("NOPECHA_API_KEY");
        var capsolverKey = Environment.GetEnvironmentVariable("CAPSOLVER_API_KEY");

        if (!string.IsNullOrEmpty(nopechaKey))
            keys[CaptchaSolver.Nopecha.ToValue()] = nopechaKey;
        if (!string.IsNullOrEmpty(capsolverKey))
            keys[CaptchaSolver.Capsolver.ToValue()] = capsolverKey;

        // From config (fallback)
        foreach (var (solver, key) in apiConfig.CaptchaKeys)
        {
            if (!keys.ContainsKey(solver) && !string.IsNullOrEmpty(key))
                keys[solver] = key;
        }

        return keys;
    }

    // Build SMS keys from environment variables and config
    Dictionary<string, Dictionary<string, string>> BuildSmsKeys()
    {
        var keys = new Dictionary<string, Dictionary<string, string>>();

        // From environment variables
        var smspoolToken = Environment.GetEnvironmentVariable("SMSPOOL_TOKEN");
        var fiveSimToken = Environment.GetEnvironmentVariable("5SIM_TOKEN");
        var getsmsCodeUser = Environment.GetEnvironmentVariable("GETSMS_USER");
        var getsmsCodeToken = Environment.GetEnvironmentVariable("GETSMS_TOKEN");

        if (!string.IsNullOrEmpty(smspoolToken))
            keys[SmsService.SmsPool.ToValue()] = new() { ["token"] = smspoolToken };
        if (!string.IsNullOrEmpty(fiveSimToken))
            keys[SmsService.FiveSim.ToValue()] = new() { ["token"] = fiveSimToken };
        if (!string.IsNullOrEmpty(getsmsCodeToken))
            keys[SmsService.GetsmsCode.ToValue()] = new()
            {
                ["user"] = getsmsCodeUser ?? string.Empty,
                ["token"] = getsmsCodeToken
            };

        return keys;
    }

    // Build proxies from environment variables and config
    var proxies = new List<string>();
    var envProxy = Environment.GetEnvironmentVariable("PROXY_URL");
    if (!string.IsNullOrEmpty(envProxy))
        proxies.Add(envProxy);
    proxies.AddRange(apiConfig.Proxies);

    return new RegMailNetManager(
        captchaKeysProvider: BuildCaptchaKeys,
        smsKeysProvider: BuildSmsKeys,
        proxies: proxies.Count > 0 ? proxies : null,
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
