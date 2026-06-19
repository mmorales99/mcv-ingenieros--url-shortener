using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using UrlShortener.Web.Data;
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

    [Fact]
    public async Task CreateEmployeeAsync_SetsForcedPasswordChangeAndEnablesLockout()
    {
        using var factory = new UrlShortenerWebApplicationFactory();
        await factory.SeedAdminAsync();

        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<AccountProvisioningService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var result = await service.CreateEmployeeAsync("employee@mcv.local", "Employee123!");
        var employee = await userManager.FindByEmailAsync("employee@mcv.local");

        Assert.True(result.Succeeded);
        Assert.NotNull(employee);
        Assert.True(employee!.ForcePasswordChange);
        Assert.True(employee.LockoutEnabled);
    }
}
