namespace RegMailNet.WebDriver;

public interface IProxyAuthExtensionBuilder
{
    string BuildExtension(string host, int port, string username, string password);
}

public class ProxyAuthExtensionBuilder : IProxyAuthExtensionBuilder
{
    public string BuildExtension(string host, int port, string username, string password)
    {
        var backgroundJs = GenerateBackgroundJs(host, port, username, password);
        var folderPath = Path.Combine(Path.GetTempPath(), "ninjemail_proxy_ext_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, "background.js");
        File.WriteAllText(filePath, backgroundJs);

        return folderPath;
    }

    private static string GenerateBackgroundJs(string host, int port, string username, string password)
    {
        return $@"
        var config = {{
                mode: ""fixed_servers"",
                rules: {{
                singleProxy: {{
                    scheme: ""http"",
                    host: ""{host}"",
                    port: parseInt({port})
                }},
                bypassList: [""localhost""]
                }}
            }};

        chrome.proxy.settings.set({{value: config, scope: ""regular""}}, function() {{}});

        function callbackFn(details) {{
            return {{
                authCredentials: {{
                    username: ""{username}"",
                    password: ""{password}""
                }}
            }};
        }}

        chrome.webRequest.onAuthRequired.addListener(
                    callbackFn,
                    {{urls: [""<all_urls>""]}},
                    ['blocking']
        );";
    }
}
