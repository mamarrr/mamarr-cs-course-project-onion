using Microsoft.Playwright;

namespace WebApp.Tests.Helpers;

public static class PlaywrightLoginHelper
{
    public static async Task LoginAsync(IPage page, string rootUri, string email, string password)
    {
        await page.GotoAsync($"{rootUri}/Identity/Account/Login");
        await page.FillAsync("#Input_Email", email);
        await page.FillAsync("#Input_Password", password);
        await page.ClickAsync("#login-submit");

        await page.WaitForFunctionAsync(
            "() => !window.location.pathname.toLowerCase().includes('/identity/account/login') " +
            "|| document.querySelector('.text-danger:not(:empty), .validation-summary-errors') !== null",
            options: new PageWaitForFunctionOptions { Timeout = 15_000 });

        if (page.Url.Contains("/Identity/Account/Login", StringComparison.OrdinalIgnoreCase))
        {
            var errors = await page.Locator(".text-danger, .validation-summary-errors").AllTextContentsAsync();
            throw new InvalidOperationException(
                $"Login failed for {email}. URL={page.Url}. Errors={string.Join(" | ", errors.Where(e => !string.IsNullOrWhiteSpace(e)))}");
        }
    }

    public static async Task RegisterAsync(IPage page, string rootUri, string email, string password)
    {
        await page.GotoAsync($"{rootUri}/Identity/Account/Register");
        await page.FillAsync("#Input_Email", email);
        await page.FillAsync("#Input_Password", password);
        await page.FillAsync("#Input_ConfirmPassword", password);
        await page.ClickAsync("#registerSubmit");

        await page.WaitForFunctionAsync(
            "() => !window.location.pathname.toLowerCase().includes('/identity/account/register') " +
            "|| document.querySelector('.text-danger:not(:empty), .validation-summary-errors') !== null",
            options: new PageWaitForFunctionOptions { Timeout = 15_000 });

        if (page.Url.Contains("/Identity/Account/Register", StringComparison.OrdinalIgnoreCase))
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
        await page.GotoAsync($"{rootUri}/Home/SetLanguage?culture={culture}&returnUrl=%2F");
        await page.WaitForURLAsync($"{rootUri}/");
    }
}
