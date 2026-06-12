using BlazorBlueprint.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
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
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IAccountHistoryService, AccountHistoryService>();
builder.Services.AddScoped<IAccountCreationService, AccountCreationService>();

await builder.Build().RunAsync();
