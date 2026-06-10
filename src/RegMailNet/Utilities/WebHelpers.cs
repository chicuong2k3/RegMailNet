using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace RegMailNet.Utilities;

public static class WebHelpers
{
    public static void SafeClick(IWebElement element)
    {
        try
        {
            element.Click();
        }
        catch (WebDriverException)
        {
            // Fall back handled by caller if needed
        }
    }

    public static void WaitAndClick(IWebDriver driver, By by, int timeoutSeconds = 10)
    {
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
        var element = wait.Until(d =>
        {
            try
            {
                var el = d.FindElement(by);
                return el.Displayed && el.Enabled ? el : null;
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        });
        SafeClick(element!);
    }

    public static void SetInputValue(IWebDriver driver, By selector, string value)
    {
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        var element = wait.Until(d => d.FindElement(selector));
        element.SendKeys(value);
    }

    public static void TypeInto(IWebDriver driver, By locator, string value)
    {
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        var element = wait.Until(d =>
        {
            try
            {
                var el = d.FindElement(locator);
                return el.Displayed && el.Enabled ? el : null;
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        });

        var js = (IJavaScriptExecutor)driver;
        js.ExecuteScript("arguments[0].scrollIntoView({block:'center'});", element);

        try
        {
            element.Click();
        }
        catch (ElementClickInterceptedException)
        {
            js.ExecuteScript("arguments[0].focus();", element);
        }

        element.SendKeys(Keys.Control + "a");
        element.SendKeys(Keys.Delete);
        element.SendKeys(value.ToString());
    }

    public static void ActionChainClick(IWebDriver driver, IWebElement element)
    {
        try
        {
            new Actions(driver)
                .MoveToElement(element)
                .Pause(TimeSpan.FromMilliseconds(50))
                .Click()
                .Perform();
        }
        catch (ElementClickInterceptedException)
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", element);
        }
    }
}
