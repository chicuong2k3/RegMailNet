using FastEndpoints;

namespace RegMailNet.Api.Endpoints;

public sealed class HealthCheck : EndpointWithoutRequest<HealthCheckResponse>
{
    public override void Configure()
    {
        Get("/api/health");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendOkAsync(new HealthCheckResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow
        }, ct);
    }
}

public sealed class HealthCheckResponse
{
    public string Status { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
