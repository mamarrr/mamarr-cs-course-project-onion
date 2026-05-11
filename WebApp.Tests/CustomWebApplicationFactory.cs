using App.DAL.EF;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebApp.Tests.Helpers;

namespace WebApp.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestPolicyScheme = "TestPolicy";

    // A shared-cache in-memory SQLite DB named per factory instance. The keep-alive
    // connection keeps the DB alive for the lifetime of this factory; every DbContext
    // opens its own connection to the same DB.
    private readonly string _dbName = $"test-{Guid.NewGuid():N}";
    private SqliteConnection? _keepAliveConnection;

    private string ConnectionString => $"DataSource=file:{_dbName}?mode=memory&cache=shared";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Skip Program.cs's SetupAppData (MigrateDatabase/SeedIdentity/SeedData):
        // we manage schema with EnsureCreated and seed via DataSeeder below.
        builder.UseSetting("DataInitialization:DropDatabase", "false");
        builder.UseSetting("DataInitialization:MigrateDatabase", "false");
        builder.UseSetting("DataInitialization:SeedIdentity", "false");
        builder.UseSetting("DataInitialization:SeedData", "false");

        builder.ConfigureServices(services =>
        {
            // find DbContext options
            var descriptor = services
                .SingleOrDefault(d => d.ServiceType ==
                                      typeof(IDbContextOptionsConfiguration<AppDbContext>));

            // if found - remove
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // find DbContext itself
            var descriptorDbContext = services
                .SingleOrDefault(d => d.ServiceType ==
                                      typeof(App.DAL.EF.AppDbContext));

            // if found - remove
            if (descriptorDbContext != null)
            {
                services.Remove(descriptorDbContext);
            }

            // Open a long-lived connection so the named in-memory DB stays alive.
            if (_keepAliveConnection == null)
            {
                _keepAliveConnection = new SqliteConnection(ConnectionString);
                _keepAliveConnection.Open();
            }

            // add new DbContext with SQLite shared in-memory database.
            // Match Program.cs production tracking behavior — without it, BaseRepository's
            // RemoveAsync (FindAsync then Remove with a fresh-mapped Domain entity) conflicts
            // with the already-tracked entity from Find.
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(ConnectionString);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
                // ignore that we don't have migration for sqlite
                // original db was postgres
                options.ConfigureWarnings(w =>
                    w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            });

            // Asp.Versioning's default rejects unversioned requests with 400. The unversioned
            // ListItemsController route ("api/[controller]") needs to opt into the default version
            // so tests can hit it directly. This only affects the test host.
            services.Configure<ApiVersioningOptions>(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
            });

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.AuthenticationScheme,
                    _ => { })
                .AddPolicyScheme(TestPolicyScheme, TestPolicyScheme, options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        if (context.Request.Headers.ContainsKey(TestAuthHandler.UserIdHeader))
                        {
                            return TestAuthHandler.AuthenticationScheme;
                        }

                        var authorization = context.Request.Headers.Authorization.ToString();
                        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            return JwtBearerDefaults.AuthenticationScheme;
                        }

                        return IdentityConstants.ApplicationScheme;
                    };
                });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestPolicyScheme;
                options.DefaultChallengeScheme = TestPolicyScheme;
                options.DefaultForbidScheme = TestPolicyScheme;
            });
        });

        // Run schema + seed once after the host is fully built so UserManager/RoleManager are available.
        builder.UseDefaultServiceProvider((_, _) => { });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        EnsureCreatedAndSeed(host.Services);
        return host;
    }

    protected void EnsureCreatedAndSeed(IServiceProvider rootServices)
    {
        using var scope = rootServices.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var logger = sp.GetRequiredService<ILogger<CustomWebApplicationFactory>>();

        // Use EnsureCreated instead of Migrate: migrations are Postgres-specific
        // (uuid, varchar), so they can't be applied to SQLite. EnsureCreated builds
        // the schema directly from the model.
        db.Database.EnsureCreated();

        try
        {
            DataSeeder.SeedData(rootServices);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred seeding the " +
                                "database with test data. Error: {Message}", ex.Message);
            throw;
        }
    }

    public HttpClient CreateClientNoRedirect()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public HttpClient CreateAuthenticatedMvcClient(TestUser user)
    {
        var client = CreateClientNoRedirect();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, user.Id.ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.EmailHeader, user.Email);
        if (user.IsSystemAdmin)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "SystemAdmin");
        }
        else
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "User");
        }

        return client;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _keepAliveConnection?.Dispose();
            _keepAliveConnection = null;
        }
        base.Dispose(disposing);
    }
}
