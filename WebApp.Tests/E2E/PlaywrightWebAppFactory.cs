using App.DAL.EF;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WebApp.Tests.E2E;

public class PlaywrightWebAppFactory : CustomWebApplicationFactory
{
    private IHost? _kestrelHost;

    public string RootUri => "http://host.docker.internal:5065";

    public PlaywrightWebAppFactory()
    {
        // Force eager host creation so Kestrel is listening before the test runs.
        _ = Services;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var testHost = builder.Build();

        builder.ConfigureWebHost(webHost =>
        {
            webHost.UseKestrel();
            webHost.UseUrls("http://0.0.0.0:5065");
        });

        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        testHost.Start();

        // Schema only. E2E tests are self-contained and create their own users via UI register
        // (cookie auth needs users created through the Identity Razor Pages flow).
        using var scope = _kestrelHost.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        return testHost;
    }

    protected override void Dispose(bool disposing)
    {
        _kestrelHost?.Dispose();
        base.Dispose(disposing);
    }
}
