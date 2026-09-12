using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
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
    public DbSet<OrderNotification> OrderNotifications => Set<OrderNotification>();
    public DbSet<ContactInquiry> ContactInquiries => Set<ContactInquiry>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ShoppingCart>().HasIndex(cart => cart.OwnerHash).IsUnique();
        builder.Entity<ShoppingCartItem>().HasIndex(item => new { item.ShoppingCartId, item.ProductId }).IsUnique();
        builder.Entity<ShoppingCartItem>().ToTable(table => table.HasCheckConstraint("CK_CartItem_Quantity", "Quantity BETWEEN 1 AND 20"));
        builder.Entity<StoreOrder>().HasIndex(order => order.Reference).IsUnique();
        builder.Entity<StoreOrder>().HasIndex(order => new { order.CustomerUserId, order.CreatedUtc });
        builder.Entity<StoreOrder>().HasOne<IdentityUser>().WithMany()
            .HasForeignKey(order => order.CustomerUserId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<StoreOrder>().HasIndex(order => order.CheckoutKey).IsUnique();
        builder.Entity<StoreOrder>().HasIndex(order => order.StripeCheckoutSessionId).IsUnique();
        builder.Entity<StoreOrder>().HasIndex(order => order.StripePaymentIntentId).IsUnique();
        builder.Entity<StoreOrder>().Property(order => order.Status).HasConversion<string>().HasMaxLength(24);
        builder.Entity<StoreOrder>().Property(order => order.FulfillmentStatus).HasConversion<string>().HasMaxLength(24);
        builder.Entity<OrderNotification>().Property(notification => notification.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Entity<OrderNotification>().Property(notification => notification.Status).HasConversion<string>().HasMaxLength(24);
        builder.Entity<OrderNotification>().HasIndex(notification => new { notification.StoreOrderId, notification.Kind }).IsUnique();
        builder.Entity<OrderNotification>().HasIndex(notification => new { notification.Status, notification.NextAttemptUtc });
        builder.Entity<ContactInquiry>().HasIndex(inquiry => inquiry.Reference).IsUnique();
        builder.Entity<ContactInquiry>().Property(inquiry => inquiry.Status).HasConversion<string>().HasMaxLength(24);
        builder.Entity<ContactInquiry>().Property(inquiry => inquiry.NotificationStatus).HasConversion<string>().HasMaxLength(24);
        builder.Entity<ContactInquiry>().HasIndex(inquiry => new { inquiry.NotificationStatus, inquiry.NextAttemptUtc });
        builder.Entity<ContactInquiry>().HasIndex(inquiry => new { inquiry.Status, inquiry.CreatedUtc });
        builder.Entity<CustomerProfile>().HasIndex(profile => profile.UserId).IsUnique();
        builder.Entity<CustomerProfile>().HasOne<IdentityUser>().WithOne()
            .HasForeignKey<CustomerProfile>(profile => profile.UserId).OnDelete(DeleteBehavior.Cascade);
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
