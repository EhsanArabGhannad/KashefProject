using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KashefProject.Data;

public sealed class StoreDbContext(DbContextOptions<StoreDbContext> options) : IdentityDbContext(options)
{
    public DbSet<CatalogCategory> Categories => Set<CatalogCategory>();
    public DbSet<CatalogProduct> Products => Set<CatalogProduct>();
    public DbSet<CatalogProductImage> ProductImages => Set<CatalogProductImage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<CatalogCategory>()
            .HasIndex(category => category.Slug)
            .IsUnique();

        builder.Entity<CatalogProduct>()
            .HasIndex(product => product.Slug)
            .IsUnique();

        builder.Entity<CatalogProduct>()
            .HasIndex(product => new { product.IsPublished, product.DisplayOrder });

        builder.Entity<CatalogProduct>()
            .HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CatalogProductImage>()
            .HasIndex(image => new { image.ProductId, image.SortOrder });
    }
}
