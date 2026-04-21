using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using App.BLL.CustomerWorkspace.Access;
using App.BLL.CustomerWorkspace.Customers;
using App.BLL.CustomerWorkspace.Profiles;
using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.LeaseAssignments;
using App.BLL.ManagementCompany.Membership;
using App.BLL.ManagementCompany.Profiles;
using App.BLL.Onboarding;
using App.BLL.Onboarding.Account;
using App.BLL.Onboarding.Api;
using App.BLL.Onboarding.CompanyJoinRequests;
using App.BLL.Onboarding.ContextSelection;
using App.BLL.Onboarding.WorkspaceCatalog;
using App.BLL.PropertyWorkspace.Profiles;
using App.BLL.PropertyWorkspace.Properties;
using App.BLL.ResidentWorkspace.Access;
using App.BLL.ResidentWorkspace.Profiles;
using App.BLL.ResidentWorkspace.Residents;
using App.BLL.UnitWorkspace.Access;
using App.BLL.UnitWorkspace.Profiles;
using App.BLL.UnitWorkspace.Units;
using App.BLL.UnitWorkspace.Workspace;
using App.DAL.EF;
using App.DAL.EF.Seeding;
using App.Domain.Identity;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Swashbuckle.AspNetCore.SwaggerGen;
using WebApp;
using WebApp.ApiControllers.Shared;
using WebApp.Middleware;
using WebApp.Services.ManagementLayout;
using WebApp.Services.SharedLayout;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// used for older style [Column(TypeName = "jsonb")] for LangStr
#pragma warning disable CS0618 // Type or member is obsolete
//NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
#pragma warning restore CS0618 // Type or member is obsolete


builder.Services
    .AddDbContext<AppDbContext>(options => options
        .UseNpgsql(
            connectionString,
            o => { o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery); }
        )
        .ConfigureWarnings(w =>
            w.Throw(RelationalEventId.MultipleCollectionIncludeWarning)
        )
        .EnableDetailedErrors()
        .EnableSensitiveDataLogging()
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution)
    );


builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// using Microsoft.AspNetCore.DataProtection;
builder.Services
    .AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>();

builder.Services.AddIdentity<AppUser, AppRole>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Home/AccessDenied";
});

builder.Services.AddScoped<IAccountOnboardingService, AccountOnboardingService>();
builder.Services.AddScoped<IWorkspaceRedirectService, WorkspaceRedirectService>();
builder.Services.AddScoped<IApiOnboardingContextService, ApiWorkspaceContextService>();
builder.Services.AddScoped<IApiOnboardingRouteContextMapper, ApiOnboardingRouteContextMapper>();
builder.Services.AddScoped<IUserWorkspaceCatalogService, UserWorkspaceCatalogService>();
builder.Services.AddScoped<ICompanyJoinRequestService, CompanyJoinRequestService>();
builder.Services.AddScoped<IManagementUserAdminService, ManagementUserAdminService>();
builder.Services.AddScoped<IManagementCustomersService, ManagementCustomersService>();
builder.Services.AddScoped<IManagementCustomerAccessService, ManagementCustomersService>();
builder.Services.AddScoped<IManagementCustomerService, ManagementCustomersService>();
builder.Services.AddScoped<IManagementCustomerPropertyService, ManagementCustomersService>();
builder.Services.AddScoped<IManagementResidentAccessService, ManagementResidentAccessService>();
builder.Services.AddScoped<IManagementResidentService, ManagementResidentService>();
builder.Services.AddScoped<IManagementPropertyUnitService, ManagementPropertyUnitService>();
builder.Services.AddScoped<IManagementUnitDashboardService, ManagementPropertyUnitService>();
builder.Services.AddScoped<IManagementLeaseService, ManagementLeaseService>();
builder.Services.AddScoped<IManagementLeaseSearchService, ManagementLeaseSearchService>();
builder.Services.AddScoped<IManagementCompanyProfileService, ManagementCompanyProfileService>();
builder.Services.AddScoped<IManagementCustomerProfileService, ManagementCustomerProfileService>();
builder.Services.AddScoped<IManagementPropertyProfileService, ManagementPropertyProfileService>();
builder.Services.AddScoped<IManagementUnitProfileService, ManagementUnitProfileService>();
builder.Services.AddScoped<IManagementResidentProfileService, ManagementResidentProfileService>();
builder.Services.AddScoped<IWorkspaceLayoutContextProvider, WorkspaceLayoutContextProvider>();
builder.Services.AddScoped<IManagementLayoutViewModelProvider, ManagementLayoutViewModelProvider>();

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); // => remove default claims
builder.Services
    .AddAuthentication()
    .AddCookie(options =>
    {
        options.SlidingExpiration = true;
        options.AccessDeniedPath = "/Home/AccessDenied";
    })
    .AddJwtBearer(cfg =>
    {
        cfg.RequireHttpsMetadata = false; // TODO: set to true in production!
        cfg.SaveToken = true;
        cfg.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidAudience = builder.Configuration["JWT:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]!)),
            ClockSkew = TimeSpan.Zero // remove delay of token when expire
        };
    });



var supportedCultures = builder.Configuration
    .GetSection("SupportedCultures")
    .GetChildren()
    .Select(x => new CultureInfo(x.Value!))
    .ToArray();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    // datetime and currency support
    options.SupportedCultures = supportedCultures;
    // UI translated strings
    options.SupportedUICultures = supportedCultures;
    // if nothing is found, use this
    options.DefaultRequestCulture = new RequestCulture("en", "en");
    options.SetDefaultCulture("en");

    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        // Order is important, it's in which order they will be evaluated
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider()
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsAllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("X-Version", "X-Version-Created-At");
    });
});


var apiVersioningBuilder = builder.Services.AddApiVersioning(options =>
{
    options.ReportApiVersions = true;
    // in case of no explicit version
    options.DefaultApiVersion = new ApiVersion(1, 0);
});

apiVersioningBuilder.AddApiExplorer(options =>
{
    // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
    // note: the specified format code will format the version as "'v'major[.minor][-status]"
    options.GroupNameFormat = "'v'VVV";

    // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
    // can also be used to control the format of the API version in route templates
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen();

builder.Services
    .AddControllersWithViews();

// ==============================================
var app = builder.Build();
// ============================================== PIPELINE ===============================
SetupAppData(app, app.Environment, app.Configuration);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRequestLocalization(options: app.Services
    .GetService<IOptions<RequestLocalizationOptions>>()!.Value);

app.UseCors("CorsAllowAll");

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.UseOnboardingContextGuard();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    foreach (var description in provider.ApiVersionDescriptions)
    {
        options.SwaggerEndpoint(
            $"/swagger/{description.GroupName}/swagger.json",
            description.GroupName.ToUpperInvariant()
        );
    }
    // serve from root
    // options.RoutePrefix = string.Empty;
});


app.MapStaticAssets();

app.MapControllerRoute(
        name: "management_dashboard",
        pattern: "m/{companySlug}",
        defaults: new { area = "Management", controller = "Dashboard", action = "Index" })
    .WithStaticAssets();

app.MapControllerRoute(
        name: "customer_dashboard",
        pattern: "m/{companySlug}/c/{customerSlug}",
        defaults: new { area = "Customer", controller = "CustomerDashboard", action = "Index" })
    .WithStaticAssets();

app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Onboarding}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

return;

static void SetupAppData(IApplicationBuilder app, IWebHostEnvironment env, IConfiguration configuration)
{
    using var serviceScope = ((IApplicationBuilder)app).ApplicationServices
        .GetRequiredService<IServiceScopeFactory>()
        .CreateScope();
    var logger = serviceScope.ServiceProvider.GetRequiredService<ILogger<IApplicationBuilder>>();

    using var context = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();

    WaitDbConnection(context, logger);

    using var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    using var roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

    if (configuration.GetValue<bool>("DataInitialization:DropDatabase"))
    {
        logger.LogWarning("DropDatabase");
        AppDataInit.DeleteDatabase(context);
    }

    if (configuration.GetValue<bool>("DataInitialization:MigrateDatabase"))
    {
        logger.LogInformation("MigrateDatabase");
        AppDataInit.MigrateDatabase(context);
    }

    if (configuration.GetValue<bool>("DataInitialization:SeedIdentity"))
    {
        logger.LogInformation("SeedIdentity");
        AppDataInit.SeedIdentity(userManager, roleManager);
    }

    if (configuration.GetValue<bool>("DataInitialization:SeedData"))
    {
        logger.LogInformation("SeedData");
        AppDataInit.SeedAppData(context);
    }
}

static void WaitDbConnection(AppDbContext ctx, ILogger<IApplicationBuilder> logger)
{
    while (true)
    {
        try
        {
            ctx.Database.OpenConnection();
            ctx.Database.CloseConnection();
            return;
        }
        catch (Npgsql.PostgresException e)
        {
            logger.LogWarning("Checked postgres db connection. Got: {}", e.Message);

            if (e.Message.Contains("does not exist"))
            {
                logger.LogWarning("Applying migration, probably db is not there (but server is)");
                return;
            }

            logger.LogWarning("Waiting for db connection. Sleep 1 sec");
            System.Threading.Thread.Sleep(1000);
        }
    }
}

