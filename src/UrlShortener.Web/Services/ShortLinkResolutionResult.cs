using UrlShortener.Web.Data;

namespace UrlShortener.Web.Services;

public sealed class ShortLinkResolutionResult
{
    private ShortLinkResolutionResult(ShortLinkResolutionStatus status, ShortLink? shortLink)
    {
        Status = status;
        ShortLink = shortLink;
    }

    public ShortLinkResolutionStatus Status { get; }
    public ShortLink? ShortLink { get; }

    public static ShortLinkResolutionResult Redirect(ShortLink shortLink) => new(ShortLinkResolutionStatus.Redirect, shortLink);
    public static ShortLinkResolutionResult Expired(ShortLink shortLink) => new(ShortLinkResolutionStatus.Expired, shortLink);
    public static ShortLinkResolutionResult NotFound() => new(ShortLinkResolutionStatus.NotFound, null);
}
