using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Web.Services;
using UrlShortener.Web.Tests.Infrastructure;

namespace UrlShortener.Web.Tests;

public sealed class AccountProvisioningServiceTests
{
    [Fact]
    public async Task CreateFirstAdminAsync_AllowsOnlyOneInitialAdministrator()
    {
        using var factory = new UrlShortenerWebApplicationFactory();

        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<AccountProvisioningService>();

        var firstResult = await service.CreateFirstAdminAsync("admin@mcv.local", "Admin123!");
        var secondResult = await service.CreateFirstAdminAsync("another-admin@mcv.local", "Admin123!");

        Assert.True(firstResult.Succeeded);
        Assert.False(secondResult.Succeeded);
        Assert.Contains(secondResult.Errors, error => error.Description.Contains("no longer available", StringComparison.OrdinalIgnoreCase));
    }
}
