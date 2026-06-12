using FastEndpoints;
using RegMailNet;
using RegMailNet.Api.Requests;
using RegMailNet.Api.Responses;

namespace RegMailNet.Api.Endpoints;

public sealed class CreateOutlookAccount : Endpoint<CreateAccountRequest, AccountCreatedResponse>
{
    public override void Configure()
    {
        Post("/api/accounts/outlook");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateAccountRequest req, CancellationToken ct)
    {
        var manager = Resolve<RegMailNet.RegMailNetManager>();

        try
        {
            var result = await manager.CreateOutlookAccountAsync(
                useProxy: req.UseProxy,
                cancellationToken: ct);

            await SendOkAsync(new AccountCreatedResponse
            {
                Email = result.Email,
                Password = result.Password,
                Provider = EmailProvider.Outlook.ToValue(),
                CreatedAt = DateTime.UtcNow
            }, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create Outlook account");
            await SendAsync(new AccountCreatedResponse
            {
                Provider = EmailProvider.Outlook.ToValue(),
                CreatedAt = DateTime.UtcNow
            }, 500, ct);
        }
    }
}
