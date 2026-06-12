namespace RegMailNet.Api.Requests;

public sealed class CreateAccountRequest
{
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool UseProxy { get; init; } = true;
}
