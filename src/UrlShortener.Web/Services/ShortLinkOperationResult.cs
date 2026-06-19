using UrlShortener.Web.Data;

namespace UrlShortener.Web.Services;

public sealed class ShortLinkOperationResult
{
    private ShortLinkOperationResult(bool succeeded, bool forbidden, IReadOnlyList<string> errors, ShortLink? shortLink)
    {
        Succeeded = succeeded;
        IsForbidden = forbidden;
        Errors = errors;
        ShortLink = shortLink;
    }

    public bool Succeeded { get; }
    public bool IsForbidden { get; }
    public IReadOnlyList<string> Errors { get; }
    public ShortLink? ShortLink { get; }

    public static ShortLinkOperationResult Success(ShortLink shortLink) => new(true, false, [], shortLink);

    public static ShortLinkOperationResult ValidationFailure(IReadOnlyList<string> errors) => new(false, false, errors, null);

    public static ShortLinkOperationResult Forbidden(string message) => new(false, true, [message], null);
}
