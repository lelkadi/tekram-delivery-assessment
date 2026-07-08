namespace Tekram.Api.src.shared;

using Microsoft.EntityFrameworkCore;
using Tekram.Api.src.auth.Domain;
using Tekram.Api.src.orders.Domain;
using Tekram.Api.src.restaurants.Domain;

public class TekramDbContext : DbContext
{
    public TekramDbContext(DbContextOptions<TekramDbContext> options) : base(options)
    {
    }

    // ---- auth.* ----
    public DbSet<User> Users => Set<User>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    // ---- restaurants.* ----
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<CustomizationGroup> CustomizationGroups => Set<CustomizationGroup>();
    public DbSet<CustomizationOption> CustomizationOptions => Set<CustomizationOption>();

    // ---- orders.* ----
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Coupon> Coupons => Set<Coupon>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // =========================== auth ===========================

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users", "auth");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.Phone).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
            entity.Property(e => e.EmailVerified).HasDefaultValue(false);
            entity.Property(e => e.PhoneVerified).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Phone).IsUnique();
        });

        modelBuilder.Entity<OtpCode>(entity =>
        {
            entity.ToTable("otp_codes", "auth");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Channel).IsRequired().HasMaxLength(10);
            entity.Property(e => e.CodeHash).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.Channel, e.CreatedAt })
                  .HasFilter("consumed_at IS NULL")
                  .IsDescending(false, false, true);
        });

        // =========================== restaurants ===========================

        modelBuilder.Entity<Restaurant>(entity =>
        {
            entity.ToTable("restaurants", "restaurants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Cuisine).IsRequired();
            entity.Property(e => e.Rating).HasColumnType("numeric(2,1)").HasDefaultValue(0.0m);
            entity.Property(e => e.PriceTier).IsRequired();
            entity.Property(e => e.AvgPrepMinutes).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("active");
            entity.Property(e => e.Latitude).HasColumnType("numeric(9,6)").IsRequired();
            entity.Property(e => e.Longitude).HasColumnType("numeric(9,6)").IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.HasIndex(e => new { e.Status, e.Cuisine }).HasFilter("deleted_at IS NULL");
        });

        modelBuilder.Entity<MenuCategory>(entity =>
        {
            entity.ToTable("menu_categories", "restaurants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.HasOne<Restaurant>().WithMany().HasForeignKey(e => e.RestaurantId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.RestaurantId, e.DisplayOrder });
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.ToTable("menu_items", "restaurants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.PriceUsd).HasColumnType("numeric(10,2)").IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.HasOne<MenuCategory>().WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Restaurant>().WithMany().HasForeignKey(e => e.RestaurantId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.RestaurantId).HasFilter("deleted_at IS NULL");
        });

        modelBuilder.Entity<CustomizationGroup>(entity =>
        {
            entity.ToTable("menu_item_customization_groups", "restaurants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.IsRequired).HasDefaultValue(false);
            entity.Property(e => e.MaxSelections).HasDefaultValue(1);
            entity.HasOne<MenuItem>().WithMany().HasForeignKey(e => e.MenuItemId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomizationOption>(entity =>
        {
            entity.ToTable("menu_item_customization_options", "restaurants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.PriceModifierUsd).HasColumnType("numeric(10,2)").HasDefaultValue(0m);
            entity.HasOne<CustomizationGroup>().WithMany().HasForeignKey(e => e.GroupId).OnDelete(DeleteBehavior.Cascade);
        });

        // =========================== orders ===========================

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.ToTable("coupons", "orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired();
            entity.Property(e => e.DiscountType).IsRequired().HasMaxLength(10);
            entity.Property(e => e.DiscountValue).HasColumnType("numeric(10,2)").IsRequired();
            entity.Property(e => e.MinSubtotalUsd).HasColumnType("numeric(10,2)").HasDefaultValue(0m);
            entity.Property(e => e.UsesCount).HasDefaultValue(0);
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders", "orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(30).HasDefaultValue("pending");
            entity.Property(e => e.DeliveryAddress).IsRequired();
            entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(10).HasDefaultValue("COD");
            entity.Property(e => e.SubtotalUsd).HasColumnType("numeric(10,2)").IsRequired();
            entity.Property(e => e.DeliveryFeeUsd).HasColumnType("numeric(10,2)").HasDefaultValue(0m);
            entity.Property(e => e.SmallOrderSurchargeUsd).HasColumnType("numeric(10,2)").HasDefaultValue(0m);
            entity.Property(e => e.DiscountUsd).HasColumnType("numeric(10,2)").HasDefaultValue(0m);
            entity.Property(e => e.TotalUsd).HasColumnType("numeric(10,2)").IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId);
            entity.HasOne<Restaurant>().WithMany().HasForeignKey(e => e.RestaurantId);
            entity.HasOne<Coupon>().WithMany().HasForeignKey(e => e.CouponId).IsRequired(false);
            entity.HasIndex(e => new { e.UserId, e.CreatedAt }).IsDescending(false, true);
            entity.HasIndex(e => new { e.RestaurantId, e.Status });
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items", "orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.UnitPriceUsd).HasColumnType("numeric(10,2)").IsRequired();
            entity.Property(e => e.Customizations).HasColumnType("jsonb");
            entity.Property(e => e.LineTotalUsd).HasColumnType("numeric(10,2)").IsRequired();
            entity.HasOne<Order>().WithMany(e => e.OrderItems).HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<MenuItem>().WithMany().HasForeignKey(e => e.MenuItemId);
            entity.HasIndex(e => e.OrderId);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries()
                     .Where(e => e.State == EntityState.Modified && e.Properties.Any(p => p.Metadata.Name == "UpdatedAt")))
        {
            entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
