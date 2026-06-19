namespace UrlShortener.Web.Services;

public sealed class ShortLinkCreationInput
{
    public string DestinationUrl { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public DateTime? ExpiresOnUtc { get; set; }
}
