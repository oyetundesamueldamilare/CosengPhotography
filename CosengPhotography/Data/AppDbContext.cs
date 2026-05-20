using CosengPhotography.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CosengPhotography.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Gallery> Galleries { get; set; } // Pluralized for consistency
        public DbSet<Photo> Photos { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // CRITICAL: This configures the Identity tables

            // 1. Configure the Gallery-Photo relationship
            builder.Entity<Gallery>()
                .HasMany(g => g.Photos)
                .WithOne(p => p.Gallery)
                .HasForeignKey(p => p.GalleryId)
                .OnDelete(DeleteBehavior.Cascade);
            // Logic: If an admin deletes a gallery, all photos should be removed too.

            // 2. Index the Gallery ID (Guid) for faster lookups
            builder.Entity<Gallery>()
                .HasIndex(g => g.Id);
        }
    }
}