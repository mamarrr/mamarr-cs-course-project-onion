using Microsoft.AspNetCore.Hosting;
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

        EnsureCreatedAndSeed(_kestrelHost.Services);

        return testHost;
    }

    protected override void Dispose(bool disposing)
    {
        _kestrelHost?.Dispose();
        base.Dispose(disposing);
    }
}
