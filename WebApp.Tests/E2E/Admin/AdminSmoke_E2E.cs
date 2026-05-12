using AwesomeAssertions;
using Microsoft.Playwright;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.E2E.Admin;

[Collection("E2E")]
[Trait("Category", "E2E")]
public sealed class AdminSmoke_E2E : IAsyncLifetime
{
    private readonly PlaywrightWebAppFactory _factory;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public AdminSmoke_E2E(PlaywrightWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AnonymousUserCannotOpenAdminDashboard()
    {
        await WithPageAsync(async page =>
        {
            await page.GotoAsync($"{_factory.RootUri}/Admin");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/Account/Login");
            page.Url.Should().Contain("ReturnUrl=%2FAdmin");
        });
    }

    [Fact]
    public async Task NonAdminUserCannotOpenAdminDashboard()
    {
        await WithPageAsync(async page =>
        {
            await PlaywrightLoginHelper.LoginAsync(
                page,
                _factory.RootUri,
                TestUsers.CompanyAOwnerEmail,
                TestUsers.Password);

            await page.GotoAsync($"{_factory.RootUri}/Admin");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var bodyText = await page.Locator("body").InnerTextAsync();

            page.Url.Should().Contain("/access-denied");
            bodyText.Should().Contain("Access denied");
        });
    }

    [Fact]
    public async Task SystemAdminCanNavigateAdminDashboardUsersCompaniesLookupsAndTickets()
    {
        await WithPageAsync(async page =>
        {
            await PlaywrightLoginHelper.LoginAsync(
                page,
                _factory.RootUri,
                TestUsers.SystemAdminEmail,
                TestUsers.Password);

            await page.GotoAsync($"{_factory.RootUri}/Admin");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var dashboardText = await page.Locator("body").InnerTextAsync();

            page.Url.Should().Contain("/Admin");
            dashboardText.Should().Contain("Platform Admin");
            dashboardText.Should().Contain("Recent users");

            await page.Locator(".admin-nav a[href='/Admin/Users']").ClickAsync();
            await page.FillAsync("#Search_SearchText", TestUsers.CompanyAOwnerEmail);
            await page.ClickAsync("form.admin-search button[type=submit]");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            (await page.Locator("body").InnerTextAsync()).Should().Contain(TestUsers.CompanyAOwnerEmail);

            await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Details" }).First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var userDetailsText = await page.Locator("body").InnerTextAsync();
            userDetailsText.Should().Contain(TestUsers.CompanyAOwnerEmail);
            userDetailsText.Should().Contain("Company memberships");

            await page.Locator(".admin-nav a[href='/Admin/Companies']").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            (await page.Locator("body").InnerTextAsync()).Should().Contain(TestTenants.CompanyAName);

            await page.Locator(".admin-nav a[href='/Admin/Lookups']").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            (await page.Locator("body").InnerTextAsync()).Should().Contain("APARTMENT_BUILDING");

            await page.Locator(".admin-nav a[href='/Admin/Tickets']").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var ticketsText = await page.Locator("body").InnerTextAsync();
            ticketsText.Should().Contain("T-A-0001");
            ticketsText.Should().Contain(TestTenants.CompanyAName);
        });
    }

    public async Task InitializeAsync()
    {
        _ = _factory.Services;
        _playwright = await Playwright.CreateAsync();

        var endpoint = Environment.GetEnvironmentVariable("PLAYWRIGHT_WS_ENDPOINT");
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            endpoint = "ws://127.0.0.1:3000/";
        }

        _browser = await _playwright.Chromium.ConnectAsync(endpoint);
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();
    }

    private async Task WithPageAsync(Func<IPage, Task> test)
    {
        _browser.Should().NotBeNull("InitializeAsync should connect to the Playwright server");
        var context = await _browser!.NewContextAsync();
        try
        {
            var page = await context.NewPageAsync();
            await test(page);
        }
        finally
        {
            await context.CloseAsync();
        }
    }
}
