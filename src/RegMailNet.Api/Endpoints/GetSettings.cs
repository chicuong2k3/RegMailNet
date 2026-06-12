using FastEndpoints;
using RegMailNet.Api.Models;
using RegMailNet.Api.Services;

namespace RegMailNet.Api.Endpoints;

public sealed class GetSettings : EndpointWithoutRequest<AppSettings>
{
    public override void Configure()
    {
        Get("/api/settings");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var service = Resolve<EfSettingsService>();
        var settings = await service.LoadAsync();
        await SendOkAsync(settings, ct);
    }
}
