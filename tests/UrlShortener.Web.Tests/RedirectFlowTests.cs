using System.Net;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Web.Data;
using UrlShortener.Web.Tests.Infrastructure;

namespace UrlShortener.Web.Tests;

public sealed class RedirectFlowTests
{
    [Fact]
    public async Task RedirectRoute_WithActiveAlias_RedirectsToDestination()
    {
        using var factory = new UrlShortenerWebApplicationFactory();
        await factory.SeedAdminAsync();
        var employee = await factory.SeedEmployeeAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.ShortLinks.Add(new ShortLink
            {
                ApplicationUserId = employee.Id,
                Alias = "ProjectDocs",
                NormalizedAlias = "projectdocs",
                DestinationUrl = "https://example.com/project-docs",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
                IsActive = true
            });
            await dbContext.SaveChangesAsync();
        }

        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/r/projectdocs");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("https://example.com/project-docs", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task RedirectRoute_WithCaseVariantAlias_ResolvesCaseInsensitively()
    {
        using var factory = new UrlShortenerWebApplicationFactory();
        await factory.SeedAdminAsync();
        var employee = await factory.SeedEmployeeAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.ShortLinks.Add(new ShortLink
            {
                ApplicationUserId = employee.Id,
                Alias = "Team-Portal",
                NormalizedAlias = "team-portal",
                DestinationUrl = "https://example.com/team",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
                IsActive = true
            });
            await dbContext.SaveChangesAsync();
        }

        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/r/TEAM-PORTAL");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("https://example.com/team", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task RedirectRoute_WithExpiredAlias_ReturnsExpiredPage()
    {
        using var factory = new UrlShortenerWebApplicationFactory();
        await factory.SeedAdminAsync();
        var employee = await factory.SeedEmployeeAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.ShortLinks.Add(new ShortLink
            {
                ApplicationUserId = employee.Id,
                Alias = "Old-Link",
                NormalizedAlias = "old-link",
                DestinationUrl = "https://example.com/old",
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
                CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
                UpdatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
                IsActive = true
            });
            await dbContext.SaveChangesAsync();
        }

        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/r/old-link");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        Assert.Contains("Link not available", content);
    }

    [Fact]
    public async Task RedirectRoute_WithMissingAlias_ReturnsNotFoundPage()
    {
        using var factory = new UrlShortenerWebApplicationFactory();
        await factory.SeedAdminAsync();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/r/missing-link");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Link not found", content);
    }
}
