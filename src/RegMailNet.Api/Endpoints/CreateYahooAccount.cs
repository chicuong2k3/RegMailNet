using FastEndpoints;
using RegMailNet.Api.Requests;
using RegMailNet.Api.Responses;

namespace RegMailNet.Api.Endpoints;

public sealed class CreateYahooAccount : Endpoint<CreateAccountRequest, AccountCreatedResponse>
{
    public override void Configure()
    {
        Post("/api/accounts/yahoo");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateAccountRequest req, CancellationToken ct)
    {
        var manager = Resolve<RegMailNet.RegMailNetManager>();

        try
        {
            var result = await manager.CreateYahooAccountAsync(
                username: req.Username ?? "",
                password: req.Password ?? "",
                firstName: req.FirstName ?? "",
                lastName: req.LastName ?? "",
                useProxy: req.UseProxy,
                cancellationToken: ct);

            await SendOkAsync(new AccountCreatedResponse
            {
                Email = result.Email,
                Password = result.Password,
                Provider = "yahoo",
                CreatedAt = DateTime.UtcNow
            }, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create Yahoo account");
            await SendAsync(new AccountCreatedResponse
            {
                Provider = "yahoo",
                CreatedAt = DateTime.UtcNow
            }, 500, ct);
        }
    }
}
