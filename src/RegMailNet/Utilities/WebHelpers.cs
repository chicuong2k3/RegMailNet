using Microsoft.Playwright;

namespace RegMailNet.Utilities;

/// <summary>
/// Reusable Playwright interaction helpers with auto-wait built in.
/// Uses Playwright's built-in auto-wait via Locator API.
/// </summary>
public static class WebHelpers
{
    /// <summary>
    /// Click an element identified by a CSS selector, waiting for it to be visible and enabled.
    /// </summary>
    public static async Task ClickAsync(IPage page, string selector, int timeoutSeconds = 5)
    {
        await page.Locator(selector).ClickAsync(new LocatorClickOptions
        {
            Timeout = timeoutSeconds * 1000,
        });
    }

    /// <summary>
    /// Fill a text input identified by a CSS selector, clearing it first.
    /// </summary>
    public static async Task FillAsync(IPage page, string selector, string value, int timeoutSeconds = 5)
    {
        await page.Locator(selector).FillAsync(value, new LocatorFillOptions
        {
            Timeout = timeoutSeconds * 1000,
        });
    }

    /// <summary>
    /// Wait for an element to appear, then click it.
    /// Tries multiple selectors in order; clicks the first one found.
    /// </summary>
    public static async Task WaitAndClickAnyAsync(IPage page, string[] selectors, int timeoutSeconds = 5)
    {
        foreach (var selector in selectors)
        {
            try
            {
                var locator = page.Locator(selector);
                await locator.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeoutSeconds * 1000,
                });
                await locator.ClickAsync();
                return;
            }
            catch (TimeoutException)
            {
                continue;
            }
        }
        throw new TimeoutException($"None of the selectors matched within {timeoutSeconds}s: {string.Join(", ", selectors)}");
    }

    /// <summary>
    /// Select an option from a dropdown by its value attribute.
    /// </summary>
    public static async Task SelectOptionAsync(IPage page, string selector, string value, int timeoutSeconds = 5)
    {
        await page.Locator(selector).SelectOptionAsync(new SelectOptionValue { Value = value },
            new LocatorSelectOptionOptions { Timeout = timeoutSeconds * 1000 });
    }

    /// <summary>
    /// Select an option from a dropdown by its label (visible text).
    /// </summary>
    public static async Task SelectByTextAsync(IPage page, string selector, string label, int timeoutSeconds = 5)
    {
        await page.Locator(selector).SelectOptionAsync(new SelectOptionValue { Label = label },
            new LocatorSelectOptionOptions { Timeout = timeoutSeconds * 1000 });
    }

    /// <summary>
    /// Select an option from a dropdown by its index.
    /// </summary>
    public static async Task SelectByIndexAsync(IPage page, string selector, int index, int timeoutSeconds = 5)
    {
        // Get all option elements, then select by the value at the given index
        var options = await page.Locator($"{selector} option").AllAsync();
        if (index < 0 || index >= options.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} out of range (0-{options.Count - 1})");

        var value = await options[index].GetAttributeAsync("value");
        await page.Locator(selector).SelectOptionAsync(new SelectOptionValue { Value = value },
            new LocatorSelectOptionOptions { Timeout = timeoutSeconds * 1000 });
    }

    /// <summary>
    /// Type text into an element, clearing it first with keyboard shortcuts.
    /// Scrolls the element into view before typing.
    /// </summary>
    public static async Task TypeIntoAsync(IPage page, string selector, string value, int timeoutSeconds = 5)
    {
        var locator = page.Locator(selector);
        await locator.ScrollIntoViewIfNeededAsync(new LocatorScrollIntoViewIfNeededOptions
        {
            Timeout = timeoutSeconds * 1000,
        });
        await locator.ClickAsync(new LocatorClickOptions { Timeout = timeoutSeconds * 1000 });
        await page.Keyboard.PressAsync("Control+a");
        await page.Keyboard.PressAsync("Backspace");
        await locator.FillAsync(value, new LocatorFillOptions { Timeout = timeoutSeconds * 1000 });
    }

    /// <summary>
    /// Check if an element matching the selector exists on the page.
    /// Returns false if not found within the timeout.
    /// </summary>
    public static async Task<bool> ElementExistsAsync(IPage page, string selector, int timeoutSeconds = 3)
    {
        try
        {
            await page.Locator(selector).WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Attached,
                Timeout = timeoutSeconds * 1000,
            });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    /// <summary>
    /// Wait for navigation to a URL containing the specified string.
    /// </summary>
    public static async Task WaitForUrlContainsAsync(IPage page, string urlPart, int timeoutSeconds = 30)
    {
        await page.WaitForURLAsync($"**/*{urlPart}**", new PageWaitForURLOptions
        {
            Timeout = timeoutSeconds * 1000,
        });
    }

    /// <summary>
    /// Get the current page URL.
    /// </summary>
    public static string GetUrl(IPage page) => page.Url;
}
