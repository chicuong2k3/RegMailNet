using RegMailNet.Api.Models;

namespace RegMailNet.Api.Requests;

public sealed class SaveSettingsRequest
{
    public AppSettings Settings { get; init; } = new();
}
