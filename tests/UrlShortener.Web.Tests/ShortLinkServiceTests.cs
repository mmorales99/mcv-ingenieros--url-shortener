using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Web.Services;
using UrlShortener.Web.Tests.Infrastructure;

namespace UrlShortener.Web.Tests;

public sealed class ShortLinkServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidInput_PersistsShortLink()
    {
        using var factory = new UrlShortenerWebApplicationFactory();
        await factory.SeedAdminAsync();
        var employee = await factory.SeedEmployeeAsync();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<ShortLinkService>();

        var result = await service.CreateAsync(employee.Id, new ShortLinkCreationInput
        {
            DestinationUrl = "https://example.com/docs",
            Alias = "Project-Docs",
            ExpiresOnUtc = DateTime.UtcNow.Date.AddDays(7)
        });

        Assert.True(result.Succeeded);
        Assert.NotNull(result.ShortLink);
        Assert.Equal("Project-Docs", result.ShortLink!.Alias);
        Assert.Equal("project-docs", result.ShortLink.NormalizedAlias);
        Assert.Equal(1, await dbContext.ShortLinks.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateAliasIgnoringCase_Fails()
    {
        using var factory = new UrlShortenerWebApplicationFactory();
        await factory.SeedAdminAsync();
        var employee = await factory.SeedEmployeeAsync();

        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ShortLinkService>();

        var firstResult = await service.CreateAsync(employee.Id, new ShortLinkCreationInput
        {
            DestinationUrl = "https://example.com/one",
            Alias = "Docs-Link"
        });

        var secondResult = await service.CreateAsync(employee.Id, new ShortLinkCreationInput
        {
            DestinationUrl = "https://example.com/two",
            Alias = "docs-link"
        });

        Assert.True(firstResult.Succeeded);
        Assert.False(secondResult.Succeeded);
        Assert.Contains(secondResult.Errors, error => error.Contains("already in use", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateAsync_WithPrivateIpDestination_Fails()
    {
        using var factory = new UrlShortenerWebApplicationFactory();
        await factory.SeedAdminAsync();
        var employee = await factory.SeedEmployeeAsync();

        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ShortLinkService>();

        var result = await service.CreateAsync(employee.Id, new ShortLinkCreationInput
        {
            DestinationUrl = "http://192.168.1.20/internal",
            Alias = "internal-dashboard"
        });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("private or reserved IP", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetOwnedLinksAsync_ReturnsOnlyOwnedLinks()
    {
        using var factory = new UrlShortenerWebApplicationFactory();
        await factory.SeedAdminAsync();
        var employeeOne = await factory.SeedEmployeeAsync("employee-one@mcv.local", "Employee123!");
        var employeeTwo = await factory.SeedEmployeeAsync("employee-two@mcv.local", "Employee123!");

        using (var setupScope = factory.Services.CreateScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
            dbContext.ShortLinks.AddRange(
                new Data.ShortLink
                {
                    ApplicationUserId = employeeOne.Id,
                    Alias = "mine",
                    NormalizedAlias = "mine",
                    DestinationUrl = "https://example.com/mine",
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    UpdatedAtUtc = DateTimeOffset.UtcNow
                },
                new Data.ShortLink
                {
                    ApplicationUserId = employeeTwo.Id,
                    Alias = "theirs",
                    NormalizedAlias = "theirs",
                    DestinationUrl = "https://example.com/theirs",
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    UpdatedAtUtc = DateTimeOffset.UtcNow
                });
            await dbContext.SaveChangesAsync();
        }

        using var actionScope = factory.Services.CreateScope();
        var service = actionScope.ServiceProvider.GetRequiredService<ShortLinkService>();

        var links = await service.GetOwnedLinksAsync(employeeOne.Id);

        Assert.Single(links);
        Assert.Equal("mine", links[0].Alias);
    }

    [Fact]
    public async Task UpdateAsync_WhenOwnerMatches_UpdatesLink()
    {
        using var factory = new UrlShortenerWebApplicationFactory();
        await factory.SeedAdminAsync();
        var employee = await factory.SeedEmployeeAsync();
        Guid shortLinkId;

        using (var setupScope = factory.Services.CreateScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
            var shortLink = new Data.ShortLink
            {
                ApplicationUserId = employee.Id,
                Alias = "docs",
                NormalizedAlias = "docs",
                DestinationUrl = "https://example.com/docs",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };
            dbContext.ShortLinks.Add(shortLink);
            await dbContext.SaveChangesAsync();
            shortLinkId = shortLink.Id;
        }

        using var actionScope = factory.Services.CreateScope();
        var service = actionScope.ServiceProvider.GetRequiredService<ShortLinkService>();

        var result = await service.UpdateAsync(employee.Id, shortLinkId, new ShortLinkUpdateInput
        {
            DestinationUrl = "https://example.com/docs-v2",
            ExpiresOnUtc = DateTime.UtcNow.Date.AddDays(5)
        });

        Assert.True(result.Succeeded);
        Assert.Equal("https://example.com/docs-v2", result.ShortLink!.DestinationUrl);
        Assert.NotNull(result.ShortLink.ExpiresAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_WhenOwnerDoesNotMatch_IsForbidden()
    {
        using var factory = new UrlShortenerWebApplicationFactory();
        await factory.SeedAdminAsync();
        var owner = await factory.SeedEmployeeAsync("owner@mcv.local", "Employee123!");
        var nonOwner = await factory.SeedEmployeeAsync("other@mcv.local", "Employee123!");
        Guid shortLinkId;

        using (var setupScope = factory.Services.CreateScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
            var shortLink = new Data.ShortLink
            {
                ApplicationUserId = owner.Id,
                Alias = "secure-link",
                NormalizedAlias = "secure-link",
                DestinationUrl = "https://example.com/secure",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };
            dbContext.ShortLinks.Add(shortLink);
            await dbContext.SaveChangesAsync();
            shortLinkId = shortLink.Id;
        }

        using var actionScope = factory.Services.CreateScope();
        var service = actionScope.ServiceProvider.GetRequiredService<ShortLinkService>();

        var result = await service.UpdateAsync(nonOwner.Id, shortLinkId, new ShortLinkUpdateInput
        {
            DestinationUrl = "https://example.com/hijacked"
        });

        Assert.False(result.Succeeded);
        Assert.True(result.IsForbidden);
    }

    [Fact]
    public async Task DeactivateAsync_WhenOwnerMatches_StopsFutureResolution()
    {
        using var factory = new UrlShortenerWebApplicationFactory();
        await factory.SeedAdminAsync();
        var employee = await factory.SeedEmployeeAsync();
        Guid shortLinkId;

        using (var setupScope = factory.Services.CreateScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
            var shortLink = new Data.ShortLink
            {
                ApplicationUserId = employee.Id,
                Alias = "remove-me",
                NormalizedAlias = "remove-me",
                DestinationUrl = "https://example.com/remove",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };
            dbContext.ShortLinks.Add(shortLink);
            await dbContext.SaveChangesAsync();
            shortLinkId = shortLink.Id;
        }

        using var actionScope = factory.Services.CreateScope();
        var service = actionScope.ServiceProvider.GetRequiredService<ShortLinkService>();

        var deactivateResult = await service.DeactivateAsync(employee.Id, shortLinkId);
        var resolutionResult = await service.ResolveAsync("remove-me");

        Assert.True(deactivateResult.Succeeded);
        Assert.Equal(ShortLinkResolutionStatus.NotFound, resolutionResult.Status);
    }
}
