using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Identity.Web;
using Serilog;
using Serilog.Formatting.Compact;
using TheL10inator.Api.Authentication;
using TheL10inator.Api.Endpoints;
using TheL10inator.Api.Middleware;
using TheL10inator.Domain.Interfaces;
using TheL10inator.Domain.Models;
using TheL10inator.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();

    if (context.HostingEnvironment.IsDevelopment())
    {
        configuration.WriteTo.Console();
    }
    else
    {
        configuration.WriteTo.Console(new CompactJsonFormatter());
    }
});

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();

var connectionString = builder.Configuration.GetConnectionString("Sql") ?? string.Empty;
var firstAdminEmail = builder.Configuration["Administration:FirstAdminEmail"];
var useDevBypass = builder.Configuration.GetValue<bool>("Authentication:UseDevBypass");

builder.Services.AddTheL10inatorInfrastructure(connectionString, firstAdminEmail);
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

// Authentication — JWT bearer (Entra ID) in production, DevBypass locally.
if (useDevBypass)
{
    builder.Services
        .AddAuthentication(DevBypassAuthHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, DevBypassAuthHandler>(
            DevBypassAuthHandler.SchemeName,
            _ => { });
}
else
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
}

builder.Services.AddAuthorization(options =>
{
    var defaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.DefaultPolicy = defaultPolicy;
    options.FallbackPolicy = defaultPolicy;

    options.AddPolicy("Admin", policy => policy.RequireAssertion(context =>
        context.Resource is HttpContext httpContext
            && httpContext.Items.TryGetValue(CurrentUserAccessor.CurrentUserItemKey, out var value)
            && value is CurrentUser user
            && user.Role == TeamRole.Admin));
});

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

var healthChecks = builder.Services.AddHealthChecks();
if (!string.IsNullOrWhiteSpace(connectionString))
{
    healthChecks.AddSqlServer(connectionString, name: "database");
}

var app = builder.Build();

if (useDevBypass)
{
    app.Logger.LogWarning(
        "Authentication dev bypass is ENABLED — all requests will authenticate as the caller identified by the {Header} header. This must NOT be used in staging or production.",
        DevBypassAuthHandler.EmailHeaderName);
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UserResolutionMiddleware>();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
}).AllowAnonymous();
app.MapHealthChecks("/health/ready").AllowAnonymous();

app.MapUserEndpoints();

// Run the first-admin seed before accepting traffic so the app never starts without
// at least one Admin team member in the database. Skip when no SQL connection string is
// configured — that case is reserved for WebApplicationFactory tests that stand up
// their own seed flow against a Testcontainers database.
if (!string.IsNullOrWhiteSpace(connectionString))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<IAdminSeeder>();
    await seeder.SeedIfMissingAsync(CancellationToken.None);
}

app.Run();

public partial class Program;
