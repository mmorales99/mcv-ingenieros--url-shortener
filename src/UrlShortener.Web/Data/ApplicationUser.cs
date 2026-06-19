using Microsoft.AspNetCore.Identity;

namespace UrlShortener.Web.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public bool ForcePasswordChange { get; set; }
}
