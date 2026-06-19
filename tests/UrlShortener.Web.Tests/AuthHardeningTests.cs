using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Web.Data;
using UrlShortener.Web.Tests.Infrastructure;

namespace UrlShortener.Web.Tests;

public sealed class AuthHardeningTests
{
    [Fact]
    public async Task EmployeeUser_IsLockedOutAfterConfiguredFailedAttempts()
    {
        using var factory = new UrlShortenerWebApplicationFactory();
        await factory.SeedAdminAsync();
        var seededEmployee = await factory.SeedEmployeeAsync();

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var employee = await userManager.FindByIdAsync(seededEmployee.Id);

        Assert.NotNull(employee);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await userManager.AccessFailedAsync(employee!);
        }

        var reloadedUser = await userManager.FindByIdAsync(seededEmployee.Id);

        Assert.NotNull(reloadedUser);
        Assert.True(await userManager.IsLockedOutAsync(reloadedUser!));
    }
}
