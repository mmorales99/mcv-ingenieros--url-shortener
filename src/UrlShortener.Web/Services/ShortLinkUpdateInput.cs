namespace UrlShortener.Web.Services;

public sealed class ShortLinkUpdateInput
{
    public string DestinationUrl { get; set; } = string.Empty;
    public DateTime? ExpiresOnUtc { get; set; }
}
