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
            entity.ToTable("users", "auth", t => t.HasCheckConstraint("CK_users_role", "role IN ('customer','driver','merchant','admin')"));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Email).HasColumnName("email").HasColumnType("citext").IsRequired();
            entity.Property(e => e.Phone).HasColumnName("phone").IsRequired();
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(e => e.Role).HasColumnName("role").HasColumnType("text").IsRequired().HasDefaultValue("customer");
            entity.Property(e => e.EmailVerified).HasColumnName("email_verified").HasDefaultValue(false);
            entity.Property(e => e.PhoneVerified).HasColumnName("phone_verified").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Phone).IsUnique();
        });

        modelBuilder.Entity<OtpCode>(entity =>
        {
            entity.ToTable("otp_codes", "auth", t => t.HasCheckConstraint("CK_otp_codes_channel", "channel IN ('email','phone')"));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.Channel).HasColumnName("channel").HasColumnType("text").IsRequired();
            entity.Property(e => e.CodeHash).HasColumnName("code_hash").IsRequired();
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at").IsRequired();
            entity.Property(e => e.ConsumedAt).HasColumnName("consumed_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.Channel, e.CreatedAt })
                  .HasFilter("\"consumed_at\" IS NULL")
                  .IsDescending(false, false, true);
        });

        // =========================== restaurants ===========================

        modelBuilder.Entity<Restaurant>(entity =>
        {
            entity.ToTable("restaurants", "restaurants", t => t.HasCheckConstraint("CK_restaurants_status", "status IN ('active','inactive')"));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Cuisine).HasColumnName("cuisine").IsRequired();
            entity.Property(e => e.Rating).HasColumnName("rating").HasColumnType("numeric(2,1)").HasDefaultValue(0.0m);
            entity.Property(e => e.PriceTier).HasColumnName("price_tier").IsRequired();
            entity.Property(e => e.AvgPrepMinutes).HasColumnName("avg_prep_minutes").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasColumnType("text").IsRequired().HasDefaultValue("active");
            entity.Property(e => e.Latitude).HasColumnName("latitude").HasColumnType("numeric(9,6)").IsRequired();
            entity.Property(e => e.Longitude).HasColumnName("longitude").HasColumnType("numeric(9,6)").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.HasIndex(e => new { e.Status, e.Cuisine }).HasFilter("\"deleted_at\" IS NULL");
        });

        modelBuilder.Entity<MenuCategory>(entity =>
        {
            entity.ToTable("menu_categories", "restaurants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.RestaurantId).HasColumnName("restaurant_id").IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.DisplayOrder).HasColumnName("display_order").HasDefaultValue(0);
            entity.HasOne<Restaurant>().WithMany().HasForeignKey(e => e.RestaurantId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.RestaurantId, e.DisplayOrder });
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.ToTable("menu_items", "restaurants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CategoryId).HasColumnName("category_id").IsRequired();
            entity.Property(e => e.RestaurantId).HasColumnName("restaurant_id").IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.PriceUsd).HasColumnName("price_usd").HasColumnType("numeric(10,2)").IsRequired();
            entity.Property(e => e.StockCount).HasColumnName("stock_count");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.HasOne<MenuCategory>().WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Restaurant>().WithMany().HasForeignKey(e => e.RestaurantId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.RestaurantId).HasFilter("\"deleted_at\" IS NULL");
        });

        modelBuilder.Entity<CustomizationGroup>(entity =>
        {
            entity.ToTable("menu_item_customization_groups", "restaurants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.MenuItemId).HasColumnName("menu_item_id").IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.IsRequired).HasColumnName("is_required").HasDefaultValue(false);
            entity.Property(e => e.MaxSelections).HasColumnName("max_selections").HasDefaultValue(1);
            entity.HasOne<MenuItem>().WithMany().HasForeignKey(e => e.MenuItemId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomizationOption>(entity =>
        {
            entity.ToTable("menu_item_customization_options", "restaurants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.GroupId).HasColumnName("group_id").IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.PriceModifierUsd).HasColumnName("price_modifier_usd").HasColumnType("numeric(10,2)").HasDefaultValue(0m);
            entity.HasOne<CustomizationGroup>().WithMany().HasForeignKey(e => e.GroupId).OnDelete(DeleteBehavior.Cascade);
        });

        // =========================== orders ===========================

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.ToTable("coupons", "orders", t => t.HasCheckConstraint("CK_coupons_discount_type", "discount_type IN ('percent','fixed')"));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Code).HasColumnName("code").IsRequired();
            entity.Property(e => e.DiscountType).HasColumnName("discount_type").HasColumnType("text").IsRequired();
            entity.Property(e => e.DiscountValue).HasColumnName("discount_value").HasColumnType("numeric(10,2)").IsRequired();
            entity.Property(e => e.MinSubtotalUsd).HasColumnName("min_subtotal_usd").HasColumnType("numeric(10,2)").HasDefaultValue(0m);
            entity.Property(e => e.MaxUses).HasColumnName("max_uses");
            entity.Property(e => e.UsesCount).HasColumnName("uses_count").HasDefaultValue(0);
            entity.Property(e => e.ValidFrom).HasColumnName("valid_from").IsRequired();
            entity.Property(e => e.ValidUntil).HasColumnName("valid_until").IsRequired();
            entity.Property(e => e.Active).HasColumnName("active").HasDefaultValue(true);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders", "orders", t =>
            {
                t.HasCheckConstraint("CK_orders_status", "status IN ('pending','confirmed','preparing','out_for_delivery','delivered','cancelled')");
                t.HasCheckConstraint("CK_orders_payment_method", "payment_method IN ('COD','WALLET')");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.RestaurantId).HasColumnName("restaurant_id").IsRequired();
            entity.Property(e => e.CouponId).HasColumnName("coupon_id");
            entity.Property(e => e.Status).HasColumnName("status").HasColumnType("text").IsRequired().HasDefaultValue("pending");
            entity.Property(e => e.DeliveryAddress).HasColumnName("delivery_address").IsRequired();
            entity.Property(e => e.PaymentMethod).HasColumnName("payment_method").HasColumnType("text").IsRequired().HasDefaultValue("COD");
            entity.Property(e => e.SubtotalUsd).HasColumnName("subtotal_usd").HasColumnType("numeric(10,2)").IsRequired();
            entity.Property(e => e.DeliveryFeeUsd).HasColumnName("delivery_fee_usd").HasColumnType("numeric(10,2)").HasDefaultValue(0m);
            entity.Property(e => e.SmallOrderSurchargeUsd).HasColumnName("small_order_surcharge_usd").HasColumnType("numeric(10,2)").HasDefaultValue(0m);
            entity.Property(e => e.DiscountUsd).HasColumnName("discount_usd").HasColumnType("numeric(10,2)").HasDefaultValue(0m);
            entity.Property(e => e.TotalUsd).HasColumnName("total_usd").HasColumnType("numeric(10,2)").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
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
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.OrderId).HasColumnName("order_id").IsRequired();
            entity.Property(e => e.MenuItemId).HasColumnName("menu_item_id").IsRequired();
            entity.Property(e => e.Quantity).HasColumnName("quantity").IsRequired();
            entity.Property(e => e.UnitPriceUsd).HasColumnName("unit_price_usd").HasColumnType("numeric(10,2)").IsRequired();
            entity.Property(e => e.Customizations).HasColumnName("customizations").HasColumnType("jsonb");
            entity.Property(e => e.LineTotalUsd).HasColumnName("line_total_usd").HasColumnType("numeric(10,2)").IsRequired();
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
