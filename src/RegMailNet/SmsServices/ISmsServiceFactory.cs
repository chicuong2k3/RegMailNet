namespace RegMailNet.SmsServices;

public interface ISmsServiceFactory
{
    ISmsService Create(Dictionary<string, string> smsData, string emailProvider);
}
