using FastEndpoints;
using RegMailNet;
using RegMailNet.Api.Requests;
using RegMailNet.Api.Responses;

namespace RegMailNet.Api.Endpoints;

public sealed class CreateGmailAccount : Endpoint<CreateAccountRequest, AccountCreatedResponse>
{
    public override void Configure()
    {
        Post("/api/accounts/gmail");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateAccountRequest req, CancellationToken ct)
    {
        var manager = Resolve<RegMailNet.RegMailNetManager>();

        try
        {
            var result = await manager.CreateGmailAccountAsync(
                useProxy: true,
                cancellationToken: ct);

            await SendOkAsync(new AccountCreatedResponse
            {
                Email = result.Email,
                Password = result.Password,
                Provider = EmailProvider.Gmail.ToValue(),
                CreatedAt = DateTime.UtcNow
            }, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create Gmail account");
            await SendAsync(new AccountCreatedResponse
            {
                Provider = EmailProvider.Gmail.ToValue(),
                CreatedAt = DateTime.UtcNow
            }, 500, ct);
        }
    }
}
