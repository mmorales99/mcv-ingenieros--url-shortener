namespace UrlShortener.Web.Services;

public enum ShortLinkResolutionStatus
{
    Redirect = 1,
    Expired = 2,
    NotFound = 3
}
