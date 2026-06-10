using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RegMailNet.Configuration;
using RegMailNet.EmailProviders;
using RegMailNet.SmsServices;
using RegMailNet.Utilities;
using RegMailNet.WebDriver;
using OpenQA.Selenium;

namespace RegMailNet;

public class RegMailNetManager
{
    private readonly IWebDriverFactory _webDriverFactory;
    private readonly ISmsServiceFactory _smsServiceFactory;
    private readonly IFreeProxyService? _freeProxyService;
    private readonly OutlookProvider _outlookProvider;
    private readonly GmailProvider _gmailProvider;
    private readonly YahooProvider _yahooProvider;
    private readonly DataGenerator _dataGenerator;
    private readonly RegMailNetOptions _options;
    private readonly ILogger<RegMailNetManager> _logger;

    private readonly string _browser;
    private readonly Dictionary<string, string> _captchaKeys;
    private readonly Dictionary<string, Dictionary<string, string>> _smsKeys;
    private readonly List<string>? _proxies;
    private readonly bool _autoProxy;

    public RegMailNetManager(
        string browser,
        Dictionary<string, string>? captchaKeys = null,
        Dictionary<string, Dictionary<string, string>>? smsKeys = null,
        List<string>? proxies = null,
        bool autoProxy = false,
        IWebDriverFactory? webDriverFactory = null,
        ISmsServiceFactory? smsServiceFactory = null,
        IFreeProxyService? freeProxyService = null,
        OutlookProvider? outlookProvider = null,
        GmailProvider? gmailProvider = null,
        YahooProvider? yahooProvider = null,
        DataGenerator? dataGenerator = null,
        IOptions<RegMailNetOptions>? options = null,
        ILogger<RegMailNetManager>? logger = null)
    {
        _options = options?.Value ?? new RegMailNetOptions();
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<RegMailNetManager>.Instance;

        if (!_options.SupportedBrowsers.Contains(browser))
            throw new ArgumentException($"Unsupported browser '{browser}'. Supported browsers are: {string.Join(", ", _options.SupportedBrowsers)}");

        _browser = browser;
        _captchaKeys = captchaKeys ?? new();
        _smsKeys = smsKeys ?? new();
        _proxies = proxies;
        _autoProxy = autoProxy;

        _webDriverFactory = webDriverFactory ?? new WebDriverFactory(new ProxyAuthExtensionBuilder(), Microsoft.Extensions.Logging.Abstractions.NullLogger<WebDriverFactory>.Instance);
        _smsServiceFactory = smsServiceFactory ?? throw new ArgumentNullException(nameof(smsServiceFactory));
        _freeProxyService = freeProxyService;
        _outlookProvider = outlookProvider ?? new OutlookProvider(Microsoft.Extensions.Logging.Abstractions.NullLogger<OutlookProvider>.Instance);
        _gmailProvider = gmailProvider ?? new GmailProvider(_smsServiceFactory, Microsoft.Extensions.Logging.Abstractions.NullLogger<GmailProvider>.Instance);
        _yahooProvider = yahooProvider ?? new YahooProvider(_smsServiceFactory, Microsoft.Extensions.Logging.Abstractions.NullLogger<YahooProvider>.Instance);
        _dataGenerator = dataGenerator ?? new DataGenerator();
    }

    public AccountCreationResult CreateOutlookAccount(
        string username = "",
        string password = "",
        string firstName = "",
        string lastName = "",
        string country = "",
        string birthdate = "",
        bool hotmail = false,
        bool useProxy = true)
    {
        var captchaKey = GetCaptchaKey("outlook");
        var proxy = useProxy ? GetProxy() : null;

        var driver = _webDriverFactory.CreateDriver(_browser, captchaExtension: true, proxy: proxy, captchaKey: captchaKey);

        var info = _dataGenerator.GenerateMissingInfo(username, password, firstName, lastName, country, birthdate);
        var bd = _dataGenerator.GetBirthdate(info.Birthdate);

        return _outlookProvider.CreateAccount(driver, info.Username, info.Password, info.FirstName, info.LastName, info.Country, bd.Month, bd.Day, bd.Year, hotmail);
    }

    public AccountCreationResult CreateGmailAccount(
        string username = "",
        string password = "",
        string firstName = "",
        string lastName = "",
        string birthdate = "",
        bool useProxy = true)
    {
        var proxy = useProxy ? GetProxy() : null;
        var driver = _webDriverFactory.CreateDriver(_browser, proxy: proxy);

        var info = _dataGenerator.GenerateMissingInfo(username, password, firstName, lastName, "", birthdate);
        var bd = _dataGenerator.GetBirthdate(info.Birthdate);

        var smsKey = GetSmsKey();

        return _gmailProvider.CreateAccount(driver, _smsServiceFactory, smsKey, info.Username, info.Password, info.FirstName, info.LastName, bd.Month, bd.Day, bd.Year);
    }

    public AccountCreationResult CreateYahooAccount(
        string username = "",
        string password = "",
        string firstName = "",
        string lastName = "",
        string birthdate = "",
        bool useProxy = true)
    {
        var captchaKey = GetCaptchaKey("yahoo");
        var proxy = useProxy ? GetProxy() : null;

        var driver = _webDriverFactory.CreateDriver(_browser, captchaExtension: true, proxy: proxy, captchaKey: captchaKey);

        var smsKey = GetSmsKey();

        var info = _dataGenerator.GenerateMissingInfo(username, password, firstName, lastName, "", birthdate);
        var bd = _dataGenerator.GetBirthdate(info.Birthdate);

        return _yahooProvider.CreateAccount(driver, _smsServiceFactory, smsKey, info.Username, info.Password, info.FirstName, info.LastName, bd.Month, bd.Day, bd.Year);
    }

    private string? GetProxy()
    {
        if (_proxies?.Count > 0)
            return _proxies[Random.Shared.Next(_proxies.Count)];

        if (_autoProxy && _freeProxyService != null)
        {
            _logger.LogInformation("Getting Free Proxy..");
            var proxy = _freeProxyService.GetProxyAsync().GetAwaiter().GetResult();
            if (proxy != null)
                return proxy;

            _logger.LogInformation("There are no free proxies available.");
        }

        return null;
    }

    private CaptchaKeyInfo GetCaptchaKey(string emailProvider)
    {
        var mapping = _options.SupportedSolversByEmail
            .FirstOrDefault(m => m.EmailService.Equals(emailProvider, StringComparison.OrdinalIgnoreCase));

        if (mapping != null)
        {
            foreach (var solver in mapping.Solvers)
            {
                if (_captchaKeys.TryGetValue(solver, out var key))
                    return new CaptchaKeyInfo(solver, key);
            }
        }

        _logger.LogInformation("Supported captcha solving services for {Provider} are: {Solvers}",
            emailProvider, mapping != null ? string.Join(", ", mapping.Solvers) : "none");
        throw new ArgumentException($"No captcha key provided for email provider: {emailProvider}");
    }

    private Dictionary<string, string> GetSmsKey()
    {
        if (_smsKeys.Count == 0)
            throw new ArgumentException("No SMS API keys provided for SMS verification.");

        if (_smsKeys.TryGetValue(_options.DefaultSmsService, out var defaultData))
            return new Dictionary<string, string> { ["name"] = _options.DefaultSmsService, ["data"] = SerializeData(defaultData) };

        var randomKey = _smsKeys.Keys.ElementAt(Random.Shared.Next(_smsKeys.Count));
        return new Dictionary<string, string> { ["name"] = randomKey, ["data"] = SerializeData(_smsKeys[randomKey]) };
    }

    private static string SerializeData(Dictionary<string, string> data)
    {
        return string.Join(",", data.Select(kv => $"{kv.Key}={kv.Value}"));
    }
}
