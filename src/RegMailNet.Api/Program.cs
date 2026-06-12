using FastEndpoints;
using FastEndpoints.Swagger;
using RegMailNet;
using RegMailNet.Api.Configuration;
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

builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();

var app = builder.Build();

app.UseFastEndpoints();
app.UseSwaggerGen();

app.Run();
