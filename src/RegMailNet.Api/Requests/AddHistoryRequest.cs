using RegMailNet.Api.Models;

namespace RegMailNet.Api.Requests;

public sealed class AddHistoryRequest
{
    public AccountHistoryEntry Entry { get; init; } = new();
}
