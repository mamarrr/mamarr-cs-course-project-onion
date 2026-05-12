using Microsoft.Playwright;

namespace WebApp.Tests.Helpers;

public static class PlaywrightLoginHelper
{
    public static async Task LoginAsync(IPage page, string rootUri, string email, string password)
    {
        await page.GotoAsync($"{rootUri}/login");
        await page.FillAsync("#Email", email);
        await page.FillAsync("#Password", password);
        await page.ClickAsync("form button[type=submit]");

        await page.WaitForFunctionAsync(
            "() => !window.location.pathname.toLowerCase().includes('/login') " +
            "|| document.querySelector('.text-danger:not(:empty), .validation-summary-errors') !== null",
            options: new PageWaitForFunctionOptions { Timeout = 15_000 });

        if (page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase))
        {
            var errors = await page.Locator(".text-danger, .validation-summary-errors").AllTextContentsAsync();
            throw new InvalidOperationException(
                $"Login failed for {email}. URL={page.Url}. Errors={string.Join(" | ", errors.Where(e => !string.IsNullOrWhiteSpace(e)))}");
        }
    }

    public static async Task RegisterAsync(IPage page, string rootUri, string email, string password)
    {
        await page.GotoAsync($"{rootUri}/register");
        await page.FillAsync("#Email", email);
        await page.FillAsync("#Password", password);
        await page.FillAsync("#FirstName", "E2E");
        await page.FillAsync("#LastName", "User");
        await page.ClickAsync("form button[type=submit]");

        await page.WaitForFunctionAsync(
            "() => !window.location.pathname.toLowerCase().includes('/register') " +
            "|| document.querySelector('.text-danger:not(:empty), .validation-summary-errors') !== null",
            options: new PageWaitForFunctionOptions { Timeout = 15_000 });

        if (page.Url.Contains("/register", StringComparison.OrdinalIgnoreCase))
        {
            var errors = await page.Locator(".text-danger, .validation-summary-errors").AllTextContentsAsync();
            throw new InvalidOperationException(
                $"Registration failed for {email}. URL={page.Url}. Errors={string.Join(" | ", errors.Where(e => !string.IsNullOrWhiteSpace(e)))}");
        }
    }

    public static async Task LogoutAsync(IPage page, string rootUri)
    {
        await page.GotoAsync($"{rootUri}/");
        // Identity UI exposes a logout form button in the nav. Click it.
        var logout = page.Locator("form[action*='Logout'] button[type=submit]");
        if (await logout.CountAsync() > 0)
        {
            await logout.First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }

    public static async Task SetCultureAsync(IPage page, string rootUri, string culture)
    {
        // Use the SetLanguage endpoint. It plants the AspNetCore.Culture cookie and
        // local-redirects to the supplied returnUrl (we send the caller back to Home).
        await page.GotoAsync($"{rootUri}/set-language?culture={culture}&returnUrl=%2F");
        await page.WaitForURLAsync($"{rootUri}/");
    }
}
