using FastEndpoints;
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
                Provider = "gmail",
                CreatedAt = DateTime.UtcNow
            }, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create Gmail account");
            await SendAsync(new AccountCreatedResponse
            {
                Provider = "gmail",
                CreatedAt = DateTime.UtcNow
            }, 500, ct);
        }
    }
}
