using FastEndpoints;
using RegMailNet.Api.Requests;
using RegMailNet.Api.Services;

namespace RegMailNet.Api.Endpoints;

public sealed class AddHistory : Endpoint<AddHistoryRequest>
{
    public override void Configure()
    {
        Post("/api/history");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AddHistoryRequest req, CancellationToken ct)
    {
        var service = Resolve<EfHistoryService>();
        await service.AddAsync(req.Entry);
        await SendOkAsync(ct);
    }
}
