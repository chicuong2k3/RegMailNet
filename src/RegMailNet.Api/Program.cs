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
    var regMailNetOptions = Microsoft.Extensions.Options.Options.Create(
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
        options: regMailNetOptions);
});

builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();

var app = builder.Build();

app.UseFastEndpoints();
app.UseSwaggerGen();

app.Run();
