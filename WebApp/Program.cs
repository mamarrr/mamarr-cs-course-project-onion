using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using App.DAL.EF;
using App.DAL.EF.Seeding;
using App.Domain.Identity;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Swashbuckle.AspNetCore.SwaggerGen;
using WebApp;
using WebApp.Helpers;
using WebApp.Extensions;
using WebApp.Middleware;
using WebApp.Services.Identity;
using WebApp.UI.Breadcrumbs;
using WebApp.UI.Chrome;
using WebApp.UI.Culture;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.UserMenu;
using WebApp.UI.Workspace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// used for older style [Column(TypeName = "jsonb")] for LangStr
#pragma warning disable CS0618 // Type or member is obsolete
//NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
#pragma warning restore CS0618 // Type or member is obsolete


builder.Services.AddAppDalEf(connectionString);

// using Microsoft.AspNetCore.DataProtection;
builder.Services
    .AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>();

builder.Services.AddIdentity<AppUser, AppRole>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/access-denied";
});

builder.Services.AddAppBll();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IIdentityAccountService, IdentityAccountService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAppChromeBuilder, AppChromeBuilder>();
builder.Services.AddScoped<IWorkspaceResolver, WorkspaceResolver>();
builder.Services.AddScoped<IBreadcrumbBuilder, BreadcrumbBuilder>();
builder.Services.AddScoped<INavigationBuilder, NavigationBuilder>();
builder.Services.AddScoped<ICurrentPortalContextResolver, CurrentPortalContextResolver>();
builder.Services.AddScoped<ICultureOptionsBuilder, CultureOptionsBuilder>();
builder.Services.AddScoped<IUserMenuBuilder, UserMenuBuilder>();
builder.Services.AddApiMappers();

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); // => remove default claims
builder.Services
    .AddAuthentication()
    .AddCookie(options =>
    {
        options.SlidingExpiration = true;
        options.AccessDeniedPath = "/access-denied";
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
    .AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        options.ViewLocationExpanders.Add(new PortalFeatureViewLocationExpander());
    });

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
    app.UseExceptionHandler("/error");
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

app.MapControllers();

app.MapControllerRoute(
        name: "management_dashboard",
        pattern: "m/{companySlug}",
        defaults: new { area = "Portal", controller = "Dashboard", action = "Index" })
    .WithStaticAssets();

app.MapControllerRoute(
        name: "customer_dashboard",
        pattern: "m/{companySlug}/customers/{customerSlug}",
        defaults: new { area = "Portal", controller = "CustomerDashboard", action = "Index" })
    .WithStaticAssets();

app.MapControllerRoute(
        name: "public_root",
        pattern: "",
        defaults: new { area = "Public", controller = "Onboarding", action = "Index" })
    .WithStaticAssets();

app.MapControllerRoute(
        name: "public_onboarding",
        pattern: "onboarding",
        defaults: new { area = "Public", controller = "Onboarding", action = "Index" })
    .WithStaticAssets();

app.MapControllerRoute(
        name: "public_login",
        pattern: "login",
        defaults: new { area = "Public", controller = "Onboarding", action = "Login" })
    .WithStaticAssets();

app.MapControllerRoute(
        name: "public_register",
        pattern: "register",
        defaults: new { area = "Public", controller = "Onboarding", action = "Register" })
    .WithStaticAssets();

app.MapControllerRoute(
        name: "public_logout",
        pattern: "logout",
        defaults: new { area = "Public", controller = "Onboarding", action = "Logout" })
    .WithStaticAssets();

app.MapControllerRoute(
        name: "public_set_language",
        pattern: "set-language",
        defaults: new { area = "Public", controller = "Home", action = "SetLanguage" })
    .WithStaticAssets();

app.MapControllerRoute(
        name: "public_set_context",
        pattern: "set-context",
        defaults: new { area = "Public", controller = "Onboarding", action = "SetContext" })
    .WithStaticAssets();

app.MapControllerRoute(
        name: "public_new_management_company",
        pattern: "onboarding/new-management-company",
        defaults: new { area = "Public", controller = "Onboarding", action = "NewManagementCompany" })
    .WithStaticAssets();

app.MapControllerRoute(
        name: "public_join_management_company",
        pattern: "onboarding/join-management-company",
        defaults: new { area = "Public", controller = "Onboarding", action = "JoinManagementCompany" })
    .WithStaticAssets();

app.MapControllerRoute(
        name: "public_resident_access",
        pattern: "onboarding/resident-access",
        defaults: new { area = "Public", controller = "Onboarding", action = "ResidentAccess" })
    .WithStaticAssets();

app.MapControllerRoute(
        name: "public_privacy",
        pattern: "privacy",
        defaults: new { area = "Public", controller = "Home", action = "Privacy" })
    .WithStaticAssets();

app.MapControllerRoute(
        name: "public_access_denied",
        pattern: "access-denied",
        defaults: new { area = "Public", controller = "Home", action = "AccessDenied" })
    .WithStaticAssets();

app.MapControllerRoute(
        name: "public_error",
        pattern: "error",
        defaults: new { area = "Public", controller = "Home", action = "Error" })
    .WithStaticAssets();

app.MapControllerRoute(
        name: "public_legacy_onboarding",
        pattern: "Onboarding/{action=Index}/{id?}",
        defaults: new { area = "Public", controller = "Onboarding" })
    .WithStaticAssets();

app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
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

// needed for integration testing with WebApplicationFactory
// ReSharper disable once ClassNeverInstantiated.Global
public partial class Program
{
}

