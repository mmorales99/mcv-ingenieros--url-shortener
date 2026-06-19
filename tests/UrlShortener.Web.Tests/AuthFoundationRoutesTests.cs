using System.Net;
using UrlShortener.Web.Tests.Infrastructure;

namespace UrlShortener.Web.Tests;

public sealed class AuthFoundationRoutesTests
{
    [Fact]
    public async Task SetupAdminPage_WhenNoAdminExists_ShowsInitialSetup()
    {
        using var factory = new UrlShortenerWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/setup/admin");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Initial admin setup", content);
    }

    [Fact]
    public async Task SetupAdminPage_WhenAdminExists_RedirectsToLogin()
    {
        using var factory = new UrlShortenerWebApplicationFactory();
        await factory.SeedAdminAsync();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/setup/admin");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("http://localhost/Account/Login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task RegisterPage_ShowsSelfServiceDisabledMessage()
    {
        using var factory = new UrlShortenerWebApplicationFactory();
        using var client = factory.CreateClient();

        var content = await client.GetStringAsync("/Account/Register");

        Assert.Contains("Self-service registration is disabled", content);
    }

    [Fact]
    public async Task Dashboard_WhenAnonymous_RedirectsToLogin()
    {
        using var factory = new UrlShortenerWebApplicationFactory();
        await factory.SeedAdminAsync();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/dashboard");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("http://localhost/Account/Login", response.Headers.Location?.OriginalString);
    }
}
