using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RegMailNet.Browser;
using RegMailNet.Configuration;
using RegMailNet.EmailProviders;
using RegMailNet.SmsServices;
using RegMailNet.Utilities;

namespace RegMailNet;

public class RegMailNetManager
{
    private readonly IBrowserFactory _browserFactory;
    private readonly ISmsServiceFactory _smsServiceFactory;
    private readonly IFreeProxyService? _freeProxyService;
    private readonly OutlookProvider _outlookProvider;
    private readonly GmailProvider _gmailProvider;
    private readonly YahooProvider _yahooProvider;
    private readonly DataGenerator _dataGenerator;
    private readonly RegMailNetOptions _options;
    private readonly ILogger<RegMailNetManager> _logger;

    private readonly Func<Dictionary<string, string>>? _captchaKeysProvider;
    private readonly Func<Dictionary<string, Dictionary<string, string>>>? _smsKeysProvider;
    private readonly Dictionary<string, string> _captchaKeys;
    private readonly Dictionary<string, Dictionary<string, string>> _smsKeys;
    private readonly List<string>? _proxies;
    private readonly bool _autoProxy;
    private readonly bool _headless;

    public RegMailNetManager(
        Dictionary<string, string>? captchaKeys = null,
        Dictionary<string, Dictionary<string, string>>? smsKeys = null,
        List<string>? proxies = null,
        bool autoProxy = false,
        bool headless = true,
        IBrowserFactory? browserFactory = null,
        ISmsServiceFactory? smsServiceFactory = null,
        IFreeProxyService? freeProxyService = null,
        OutlookProvider? outlookProvider = null,
        GmailProvider? gmailProvider = null,
        YahooProvider? yahooProvider = null,
        DataGenerator? dataGenerator = null,
        IOptions<RegMailNetOptions>? options = null,
        ILogger<RegMailNetManager>? logger = null,
        Func<Dictionary<string, string>>? captchaKeysProvider = null,
        Func<Dictionary<string, Dictionary<string, string>>>? smsKeysProvider = null)
    {
        _options = options?.Value ?? new RegMailNetOptions();
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<RegMailNetManager>.Instance;

        _captchaKeysProvider = captchaKeysProvider;
        _smsKeysProvider = smsKeysProvider;
        _captchaKeys = captchaKeys ?? new();
        _smsKeys = smsKeys ?? new();
        _proxies = proxies;
        _autoProxy = autoProxy;
        _headless = headless;

        _browserFactory = browserFactory ?? throw new ArgumentNullException(nameof(browserFactory));
        _smsServiceFactory = smsServiceFactory ?? throw new ArgumentNullException(nameof(smsServiceFactory));
        _freeProxyService = freeProxyService;
        _outlookProvider = outlookProvider ?? new OutlookProvider(Microsoft.Extensions.Logging.Abstractions.NullLogger<OutlookProvider>.Instance);
        _gmailProvider = gmailProvider ?? new GmailProvider(_smsServiceFactory, Microsoft.Extensions.Logging.Abstractions.NullLogger<GmailProvider>.Instance);
        _yahooProvider = yahooProvider ?? new YahooProvider(_smsServiceFactory, Microsoft.Extensions.Logging.Abstractions.NullLogger<YahooProvider>.Instance);
        _dataGenerator = dataGenerator ?? new DataGenerator();
    }

    public async Task<AccountCreationResult> CreateOutlookAccountAsync(
        string username = "",
        string password = "",
        string firstName = "",
        string lastName = "",
        string country = "",
        string birthdate = "",
        bool hotmail = false,
        bool useProxy = true,
        CancellationToken cancellationToken = default)
    {
        var captchaKey = GetCaptchaKey("outlook");
        var proxy = useProxy ? await GetProxyAsync(cancellationToken) : null;

        await using var browserPage = await _browserFactory.CreatePageAsync(
            headless: _headless,
            proxy: proxy,
            humanize: true);

        var info = _dataGenerator.GenerateMissingInfo(username, password, firstName, lastName, country, birthdate);
        var bd = _dataGenerator.GetBirthdate(info.Birthdate);

        return await _outlookProvider.CreateAccountAsync(
            browserPage.Page, info.Username, info.Password, info.FirstName, info.LastName,
            info.Country, bd.Month, bd.Day, bd.Year, hotmail, cancellationToken);
    }

    public async Task<AccountCreationResult> CreateGmailAccountAsync(
        string username = "",
        string password = "",
        string firstName = "",
        string lastName = "",
        string birthdate = "",
        bool useProxy = true,
        CancellationToken cancellationToken = default)
    {
        var proxy = useProxy ? await GetProxyAsync(cancellationToken) : null;

        await using var browserPage = await _browserFactory.CreatePageAsync(
            headless: _headless,
            proxy: proxy,
            humanize: true);

        var info = _dataGenerator.GenerateMissingInfo(username, password, firstName, lastName, "", birthdate);
        var bd = _dataGenerator.GetBirthdate(info.Birthdate);

        var smsKey = GetSmsKey();

        return await _gmailProvider.CreateAccountAsync(
            browserPage.Page, _smsServiceFactory, smsKey,
            info.Username, info.Password, info.FirstName, info.LastName,
            bd.Month, bd.Day, bd.Year, cancellationToken);
    }

    public async Task<AccountCreationResult> CreateYahooAccountAsync(
        string username = "",
        string password = "",
        string firstName = "",
        string lastName = "",
        string birthdate = "",
        bool useProxy = true,
        CancellationToken cancellationToken = default)
    {
        var captchaKey = GetCaptchaKey("yahoo");
        var proxy = useProxy ? await GetProxyAsync(cancellationToken) : null;

        await using var browserPage = await _browserFactory.CreatePageAsync(
            headless: _headless,
            proxy: proxy,
            humanize: true);

        var smsKey = GetSmsKey();

        var info = _dataGenerator.GenerateMissingInfo(username, password, firstName, lastName, "", birthdate);
        var bd = _dataGenerator.GetBirthdate(info.Birthdate);

        return await _yahooProvider.CreateAccountAsync(
            browserPage.Page, _smsServiceFactory, smsKey,
            info.Username, info.Password, info.FirstName, info.LastName,
            bd.Month, bd.Day, bd.Year, cancellationToken);
    }

    private async Task<string?> GetProxyAsync(CancellationToken cancellationToken = default)
    {
        if (_proxies?.Count > 0)
            return _proxies[Random.Shared.Next(_proxies.Count)];

        if (_autoProxy && _freeProxyService != null)
        {
            _logger.LogInformation("Getting Free Proxy..");
            var proxy = await _freeProxyService.GetProxyAsync(cancellationToken: cancellationToken);
            if (proxy != null)
                return proxy;

            _logger.LogInformation("There are no free proxies available.");
        }

        return null;
    }

    private CaptchaKeyInfo GetCaptchaKey(string emailProvider)
    {
        var captchaKeys = _captchaKeysProvider?.Invoke() ?? _captchaKeys;

        var mapping = _options.SupportedSolversByEmail
            .FirstOrDefault(m => m.EmailService.Equals(emailProvider, StringComparison.OrdinalIgnoreCase));

        if (mapping != null)
        {
            foreach (var solver in mapping.Solvers)
            {
                if (captchaKeys.TryGetValue(solver, out var key))
                    return new CaptchaKeyInfo(solver, key);
            }
        }

        _logger.LogInformation("Supported captcha solving services for {Provider} are: {Solvers}",
            emailProvider, mapping != null ? string.Join(", ", mapping.Solvers) : "none");
        throw new ArgumentException($"No captcha key provided for email provider: {emailProvider}");
    }

    private Dictionary<string, string> GetSmsKey()
    {
        var smsKeys = _smsKeysProvider?.Invoke() ?? _smsKeys;

        if (smsKeys.Count == 0)
            throw new ArgumentException("No SMS API keys provided for SMS verification.");

        if (smsKeys.TryGetValue(_options.DefaultSmsService, out var defaultData))
            return new Dictionary<string, string> { ["name"] = _options.DefaultSmsService, ["data"] = SerializeData(defaultData) };

        var randomKey = smsKeys.Keys.ElementAt(Random.Shared.Next(smsKeys.Count));
        return new Dictionary<string, string> { ["name"] = randomKey, ["data"] = SerializeData(smsKeys[randomKey]) };
    }

    private static string SerializeData(Dictionary<string, string> data)
    {
        return string.Join(",", data.Select(kv => $"{kv.Key}={kv.Value}"));
    }
}
