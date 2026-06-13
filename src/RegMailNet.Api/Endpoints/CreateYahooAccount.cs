using FastEndpoints;
using RegMailNet;
using RegMailNet.Api.Responses;

namespace RegMailNet.Api.Endpoints;

public sealed class CreateYahooAccount : EndpointWithoutRequest<AccountCreatedResponse>
{
    public override void Configure()
    {
        Post("/api/accounts/yahoo");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var manager = Resolve<RegMailNet.RegMailNetManager>();

        try
        {
            var result = await manager.CreateYahooAccountAsync(
                useProxy: true,
                cancellationToken: ct);

            await SendOkAsync(new AccountCreatedResponse
            {
                Email = result.Email,
                Password = result.Password,
                Provider = EmailProvider.Yahoo.ToValue(),
                CreatedAt = DateTime.UtcNow
            }, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create Yahoo account");
            await SendAsync(new AccountCreatedResponse
            {
                Provider = EmailProvider.Yahoo.ToValue(),
                CreatedAt = DateTime.UtcNow
            }, 500, ct);
        }
    }
}
