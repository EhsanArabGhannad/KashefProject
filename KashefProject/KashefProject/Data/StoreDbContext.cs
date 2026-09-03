using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KashefProject.Data;

public sealed class StoreDbContext(DbContextOptions<StoreDbContext> options) : IdentityDbContext(options)
{
    public DbSet<CatalogCategory> Categories => Set<CatalogCategory>();
    public DbSet<CatalogProduct> Products => Set<CatalogProduct>();
    public DbSet<CatalogProductImage> ProductImages => Set<CatalogProductImage>();
    public DbSet<ShoppingCart> ShoppingCarts => Set<ShoppingCart>();
    public DbSet<ShoppingCartItem> ShoppingCartItems => Set<ShoppingCartItem>();
    public DbSet<StoreOrder> Orders => Set<StoreOrder>();
    public DbSet<StoreOrderLine> OrderLines => Set<StoreOrderLine>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ShoppingCart>().HasIndex(cart => cart.OwnerHash).IsUnique();
        builder.Entity<ShoppingCartItem>().HasIndex(item => new { item.ShoppingCartId, item.ProductId }).IsUnique();
        builder.Entity<ShoppingCartItem>().ToTable(table => table.HasCheckConstraint("CK_CartItem_Quantity", "Quantity BETWEEN 1 AND 20"));
        builder.Entity<StoreOrder>().HasIndex(order => order.Reference).IsUnique();
        builder.Entity<StoreOrder>().HasIndex(order => order.CheckoutKey).IsUnique();
        builder.Entity<StoreOrder>().Property(order => order.Status).HasConversion<string>().HasMaxLength(24);
        builder.Entity<StoreOrderLine>().ToTable(table =>
        {
            table.HasCheckConstraint("CK_OrderLine_Quantity", "Quantity BETWEEN 1 AND 20");
            table.HasCheckConstraint("CK_OrderLine_Price", "UnitPriceCents > 0");
        });

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
