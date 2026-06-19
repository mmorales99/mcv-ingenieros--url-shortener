using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Web.Data;

namespace UrlShortener.Web.Services;

public sealed partial class ShortLinkService(ApplicationDbContext dbContext)
{
    public async Task<ShortLinkCreationResult> CreateAsync(
        string ownerUserId,
        ShortLinkCreationInput input,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var alias = input.Alias.Trim();
        var normalizedAlias = NormalizeAlias(alias);
        var destinationUrl = input.DestinationUrl.Trim();
        var expiresAtUtc = NormalizeExpiration(input.ExpiresOnUtc);

        ValidateAlias(alias, errors);
        ValidateDestinationUrl(destinationUrl, errors);
        ValidateExpiration(expiresAtUtc, errors);

        if (errors.Count > 0)
        {
            return ShortLinkCreationResult.Failure(errors);
        }

        var ownerExists = await dbContext.Users.AnyAsync(user => user.Id == ownerUserId, cancellationToken);
        if (!ownerExists)
        {
            return ShortLinkCreationResult.Failure(["The link owner could not be found."]);
        }

        var aliasExists = await dbContext.ShortLinks.AnyAsync(
            shortLink => shortLink.NormalizedAlias == normalizedAlias,
            cancellationToken);

        if (aliasExists)
        {
            return ShortLinkCreationResult.Failure(["That alias is already in use."]);
        }

        var shortLink = new ShortLink
        {
            ApplicationUserId = ownerUserId,
            Alias = alias,
            NormalizedAlias = normalizedAlias,
            DestinationUrl = destinationUrl,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            IsActive = true
        };

        dbContext.ShortLinks.Add(shortLink);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ShortLinkCreationResult.Success(shortLink);
    }

    public string NormalizeAlias(string alias) => alias.Trim().ToLowerInvariant();

    public async Task<ShortLinkResolutionResult> ResolveAsync(string alias, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            return ShortLinkResolutionResult.NotFound();
        }

        var normalizedAlias = NormalizeAlias(alias);
        var shortLink = await dbContext.ShortLinks
            .AsNoTracking()
            .SingleOrDefaultAsync(link => link.NormalizedAlias == normalizedAlias, cancellationToken);

        if (shortLink is null || !shortLink.IsActive)
        {
            return ShortLinkResolutionResult.NotFound();
        }

        if (shortLink.ExpiresAtUtc.HasValue && shortLink.ExpiresAtUtc.Value <= DateTimeOffset.UtcNow)
        {
            return ShortLinkResolutionResult.Expired(shortLink);
        }

        return ShortLinkResolutionResult.Redirect(shortLink);
    }

    public async Task<IReadOnlyList<ShortLink>> GetOwnedLinksAsync(string ownerUserId, CancellationToken cancellationToken = default)
    {
        var links = await dbContext.ShortLinks
            .AsNoTracking()
            .Where(link => link.ApplicationUserId == ownerUserId)
            .ToListAsync(cancellationToken);

        return links
            .OrderByDescending(link => link.CreatedAtUtc)
            .ToList();
    }

    public async Task<ShortLinkOperationResult> UpdateAsync(
        string ownerUserId,
        Guid shortLinkId,
        ShortLinkUpdateInput input,
        CancellationToken cancellationToken = default)
    {
        var shortLink = await dbContext.ShortLinks
            .SingleOrDefaultAsync(
                link => link.Id == shortLinkId && link.ApplicationUserId == ownerUserId,
                cancellationToken);

        if (shortLink is null)
        {
            return ShortLinkOperationResult.Forbidden("The requested short link could not be found for this user.");
        }

        var errors = new List<string>();
        var destinationUrl = input.DestinationUrl.Trim();
        var expiresAtUtc = NormalizeExpiration(input.ExpiresOnUtc);

        ValidateDestinationUrl(destinationUrl, errors);
        ValidateExpiration(expiresAtUtc, errors);

        if (errors.Count > 0)
        {
            return ShortLinkOperationResult.ValidationFailure(errors);
        }

        shortLink.DestinationUrl = destinationUrl;
        shortLink.ExpiresAtUtc = expiresAtUtc;
        shortLink.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ShortLinkOperationResult.Success(shortLink);
    }

    public async Task<ShortLinkOperationResult> DeactivateAsync(
        string ownerUserId,
        Guid shortLinkId,
        CancellationToken cancellationToken = default)
    {
        var shortLink = await dbContext.ShortLinks
            .SingleOrDefaultAsync(
                link => link.Id == shortLinkId && link.ApplicationUserId == ownerUserId,
                cancellationToken);

        if (shortLink is null)
        {
            return ShortLinkOperationResult.Forbidden("The requested short link could not be found for this user.");
        }

        shortLink.IsActive = false;
        shortLink.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ShortLinkOperationResult.Success(shortLink);
    }

    private static DateTimeOffset? NormalizeExpiration(DateTime? expiresOnUtcDate)
    {
        if (!expiresOnUtcDate.HasValue)
        {
            return null;
        }

        var utcDate = DateTime.SpecifyKind(expiresOnUtcDate.Value.Date, DateTimeKind.Utc);
        return new DateTimeOffset(utcDate.AddDays(1).AddTicks(-1), TimeSpan.Zero);
    }

    private static void ValidateExpiration(DateTimeOffset? expiresAtUtc, ICollection<string> errors)
    {
        if (expiresAtUtc.HasValue && expiresAtUtc.Value <= DateTimeOffset.UtcNow)
        {
            errors.Add("Expiration date must be in the future.");
        }
    }

    private static void ValidateAlias(string alias, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            errors.Add("Alias is required.");
            return;
        }

        if (alias.Length > 100)
        {
            errors.Add("Alias must be 100 characters or fewer.");
        }

        if (!AliasPattern().IsMatch(alias))
        {
            errors.Add("Alias may contain only letters, numbers, and hyphens.");
        }
    }

    private static void ValidateDestinationUrl(string destinationUrl, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(destinationUrl))
        {
            errors.Add("Destination URL is required.");
            return;
        }

        if (!Uri.TryCreate(destinationUrl, UriKind.Absolute, out var uri))
        {
            errors.Add("Destination URL must be a valid absolute URL.");
            return;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Destination URL must use http or https.");
        }

        if (uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Destination URL cannot target localhost or loopback addresses.");
        }

        if (IPAddress.TryParse(uri.Host, out var ipAddress) && IsPrivateOrReserved(ipAddress))
        {
            errors.Add("Destination URL cannot target a private or reserved IP address.");
        }
    }

    private static bool IsPrivateOrReserved(IPAddress ipAddress)
    {
        if (IPAddress.IsLoopback(ipAddress))
        {
            return true;
        }

        if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = ipAddress.GetAddressBytes();
            return ipAddress.IsIPv6LinkLocal
                || ipAddress.IsIPv6SiteLocal
                || ipAddress.Equals(IPAddress.IPv6Loopback)
                || (bytes[0] & 0xFE) == 0xFC;
        }

        if (ipAddress.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytesV4 = ipAddress.GetAddressBytes();
        return bytesV4[0] == 10
            || bytesV4[0] == 127
            || bytesV4[0] == 0
            || (bytesV4[0] == 169 && bytesV4[1] == 254)
            || (bytesV4[0] == 172 && bytesV4[1] is >= 16 and <= 31)
            || (bytesV4[0] == 192 && bytesV4[1] == 168);
    }

    [GeneratedRegex("^[A-Za-z0-9-]+$")]
    private static partial Regex AliasPattern();
}
