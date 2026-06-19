using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Web.Data;

namespace UrlShortener.Web.Services;

public sealed class AccountProvisioningService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager)
{
    public async Task EnsureRolesAsync()
    {
        if (!await roleManager.RoleExistsAsync(ApplicationRoleNames.Admin))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(ApplicationRoleNames.Admin));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Unable to create required role '{ApplicationRoleNames.Admin}'.");
            }
        }
    }

    public Task<bool> HasAnyAdminAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.UserRoles
            .Join(
                dbContext.Roles,
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => role.Name)
            .AnyAsync(roleName => roleName == ApplicationRoleNames.Admin, cancellationToken);
    }

    public async Task<IdentityResult> CreateFirstAdminAsync(string email, string password)
    {
        await EnsureRolesAsync();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        if (await HasAnyAdminAsync())
        {
            return IdentityResult.Failed(new IdentityError
            {
                Description = "Initial admin setup is no longer available."
            });
        }

        var result = await CreateUserAsync(email, password, ApplicationRoleNames.Admin, forcePasswordChange: false);
        if (result.Succeeded)
        {
            await transaction.CommitAsync();
        }

        return result;
    }

    public async Task<IdentityResult> CreateEmployeeAsync(string email, string password)
    {
        await EnsureRolesAsync();
        return await CreateUserAsync(email, password, forcePasswordChange: true);
    }

    private async Task<IdentityResult> CreateUserAsync(string email, string password, string? role = null, bool forcePasswordChange = false)
    {
        var normalizedEmail = email.Trim();
        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = true,
            ForcePasswordChange = forcePasswordChange,
            LockoutEnabled = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            return createResult;
        }

        if (role is null)
        {
            return createResult;
        }

        var roleResult = await userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return roleResult;
        }

        return createResult;
    }
}
