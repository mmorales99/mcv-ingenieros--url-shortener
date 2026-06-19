using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Web.Data;
using UrlShortener.Web.Services;

namespace UrlShortener.Web.Tests.Infrastructure;

public sealed class UrlShortenerWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"url-shortener-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={databasePath};Cache=Shared"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite($"Data Source={databasePath};Cache=Shared"));
        });
    }

    public async Task SeedAdminAsync(string email = "admin@mcv.local", string password = "Admin123!")
    {
        using var scope = Services.CreateScope();
        var provisioningService = scope.ServiceProvider.GetRequiredService<AccountProvisioningService>();
        var result = await provisioningService.CreateFirstAdminAsync(email, password);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }
    }

    public new void Dispose()
    {
        base.Dispose();

        if (File.Exists(databasePath))
        {
            try
            {
                File.Delete(databasePath);
            }
            catch (IOException)
            {
            }
        }
    }
}
