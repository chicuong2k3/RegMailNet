namespace RegMailNet.EmailProviders;

public class AccountCreationException : Exception
{
    public AccountCreationException(string message) : base(message) { }
    public AccountCreationException(string message, Exception inner) : base(message, inner) { }
}
