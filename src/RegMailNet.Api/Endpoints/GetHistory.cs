using System.Text.Json.Serialization;
using FastEndpoints;
using RegMailNet.Api.Services;

namespace RegMailNet.Api.Endpoints;

public sealed class GetHistory : EndpointWithoutRequest<HistoryResponse>
{
    public override void Configure()
    {
        Get("/api/history");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var service = Resolve<EfHistoryService>();
        var entries = await service.LoadAsync();
        await SendOkAsync(new HistoryResponse { Entries = entries }, ct);
    }
}

public sealed class HistoryResponse
{
    [JsonPropertyName("entries")]
    public List<Models.AccountHistoryEntry> Entries { get; set; } = [];
}
