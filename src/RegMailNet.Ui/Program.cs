using BlazorBlueprint.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RegMailNet.Ui;
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
