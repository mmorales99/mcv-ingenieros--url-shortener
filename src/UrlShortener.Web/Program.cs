using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Web.Components;
using UrlShortener.Web.Components.Account;
using UrlShortener.Web.Data;
using UrlShortener.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<AccountProvisioningService>();
builder.Services.AddScoped<ShortLinkService>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddAuthorization();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var provisioningService = scope.ServiceProvider.GetRequiredService<AccountProvisioningService>();

    await dbContext.Database.MigrateAsync();
    await provisioningService.EnsureRolesAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var excludedPaths = new[]
        {
            "/Account/Logout",
            "/Account/Login",
            "/Account/Manage/ChangePassword",
            "/_framework"
        };

        if (!excludedPaths.Any(excludedPath => path.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase)))
        {
            var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.GetUserAsync(context.User);

            if (user?.ForcePasswordChange == true)
            {
                context.Response.Redirect("/Account/Manage/ChangePassword");
                return;
            }
        }
    }

    await next();
});

app.MapGet("/r/{alias}", async (string alias, ShortLinkService shortLinkService, CancellationToken cancellationToken) =>
{
    var result = await shortLinkService.ResolveAsync(alias, cancellationToken);

    return result.Status switch
    {
        ShortLinkResolutionStatus.Redirect => Results.Redirect(result.ShortLink!.DestinationUrl, permanent: false),
        ShortLinkResolutionStatus.Expired => Results.Content(
            """
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <title>Link expired</title>
            </head>
            <body>
                <main>
                    <h1>Link not available</h1>
                    <p>This short link has expired and is no longer available.</p>
                </main>
            </body>
            </html>
            """,
            "text/html",
            System.Text.Encoding.UTF8,
            StatusCodes.Status410Gone),
        _ => Results.Content(
            """
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <title>Link not found</title>
            </head>
            <body>
                <main>
                    <h1>Link not found</h1>
                    <p>The requested short link does not exist.</p>
                </main>
            </body>
            </html>
            """,
            "text/html",
            System.Text.Encoding.UTF8,
            StatusCodes.Status404NotFound)
    };
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();

public partial class Program;
