namespace RegMailNet.SmsServices;

public interface ISmsService
{
    Task<PhoneResult> GetPhoneAsync(bool sendPrefix = false, CancellationToken cancellationToken = default);
    Task<string> GetCodeAsync(string phoneOrOrderId, CancellationToken cancellationToken = default);
}

public record PhoneResult(string PhoneNumber, string? OrderId = null);

public class SmsServiceApiException : Exception
{
    public SmsServiceApiException(string message) : base(message) { }
    public SmsServiceApiException(string message, Exception inner) : base(message, inner) { }
}
