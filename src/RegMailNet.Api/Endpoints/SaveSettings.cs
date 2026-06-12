using FastEndpoints;
using RegMailNet.Api.Requests;
using RegMailNet.Api.Services;

namespace RegMailNet.Api.Endpoints;

public sealed class SaveSettings : Endpoint<SaveSettingsRequest>
{
    public override void Configure()
    {
        Put("/api/settings");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SaveSettingsRequest req, CancellationToken ct)
    {
        var service = Resolve<EfSettingsService>();
        await service.SaveAsync(req.Settings);
        await SendOkAsync(ct);
    }
}
