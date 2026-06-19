using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace UrlShortener.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<ShortLink> ShortLinks => Set<ShortLink>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ShortLink>(entity =>
        {
            entity.HasKey(shortLink => shortLink.Id);
            entity.Property(shortLink => shortLink.Alias)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(shortLink => shortLink.NormalizedAlias)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(shortLink => shortLink.DestinationUrl)
                .HasMaxLength(2048)
                .IsRequired();
            entity.Property(shortLink => shortLink.CreatedAtUtc)
                .IsRequired();
            entity.Property(shortLink => shortLink.UpdatedAtUtc)
                .IsRequired();
            entity.HasIndex(shortLink => shortLink.NormalizedAlias)
                .IsUnique();
            entity.HasOne(shortLink => shortLink.ApplicationUser)
                .WithMany(user => user.ShortLinks)
                .HasForeignKey(shortLink => shortLink.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
