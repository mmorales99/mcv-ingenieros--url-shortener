using UrlShortener.Web.Data;

namespace UrlShortener.Web.Services;

public sealed class ShortLinkCreationResult
{
    private ShortLinkCreationResult(bool succeeded, IReadOnlyList<string> errors, ShortLink? shortLink)
    {
        Succeeded = succeeded;
        Errors = errors;
        ShortLink = shortLink;
    }

    public bool Succeeded { get; }
    public IReadOnlyList<string> Errors { get; }
    public ShortLink? ShortLink { get; }

    public static ShortLinkCreationResult Success(ShortLink shortLink) => new(true, [], shortLink);

    public static ShortLinkCreationResult Failure(IReadOnlyList<string> errors) => new(false, errors, null);
}
