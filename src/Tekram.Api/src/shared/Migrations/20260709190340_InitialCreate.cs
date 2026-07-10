using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tekram.Api.src.shared.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Required extensions for UUID generation, text search, and case-insensitive email
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm");

            migrationBuilder.EnsureSchema(
                name: "orders");

            migrationBuilder.EnsureSchema(
                name: "restaurants");

            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "coupons",
                schema: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "text", nullable: false),
                    discount_type = table.Column<string>(type: "text", nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    min_subtotal_usd = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0m),
                    max_uses = table.Column<int>(type: "integer", nullable: true),
                    uses_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coupons", x => x.id);
                    table.CheckConstraint("CK_coupons_discount_type", "discount_type IN ('percent','fixed')");
                });

            migrationBuilder.CreateTable(
                name: "restaurants",
                schema: "restaurants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    cuisine = table.Column<string>(type: "text", nullable: false),
                    rating = table.Column<decimal>(type: "numeric(2,1)", nullable: false, defaultValue: 0.0m),
                    price_tier = table.Column<int>(type: "integer", nullable: false),
                    avg_prep_minutes = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "active"),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_restaurants", x => x.id);
                    table.CheckConstraint("CK_restaurants_status", "status IN ('active','inactive')");
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "citext", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false, defaultValue: "customer"),
                    email_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    phone_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.CheckConstraint("CK_users_role", "role IN ('customer','driver','merchant','admin')");
                });

            migrationBuilder.CreateTable(
                name: "menu_categories",
                schema: "restaurants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    restaurant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_categories", x => x.id);
                    table.ForeignKey(
                        name: "FK_menu_categories_restaurants_restaurant_id",
                        column: x => x.restaurant_id,
                        principalSchema: "restaurants",
                        principalTable: "restaurants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                schema: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    restaurant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coupon_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    delivery_address = table.Column<string>(type: "text", nullable: false),
                    payment_method = table.Column<string>(type: "text", nullable: false, defaultValue: "COD"),
                    subtotal_usd = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    delivery_fee_usd = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0m),
                    small_order_surcharge_usd = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0m),
                    discount_usd = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0m),
                    total_usd = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                    table.CheckConstraint("CK_orders_payment_method", "payment_method IN ('COD','WALLET')");
                    table.CheckConstraint("CK_orders_status", "status IN ('pending','confirmed','preparing','out_for_delivery','delivered','cancelled')");
                    table.ForeignKey(
                        name: "FK_orders_coupons_coupon_id",
                        column: x => x.coupon_id,
                        principalSchema: "orders",
                        principalTable: "coupons",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_orders_restaurants_restaurant_id",
                        column: x => x.restaurant_id,
                        principalSchema: "restaurants",
                        principalTable: "restaurants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_orders_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "otp_codes",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "text", nullable: false),
                    code_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_otp_codes", x => x.id);
                    table.CheckConstraint("CK_otp_codes_channel", "channel IN ('email','phone')");
                    table.ForeignKey(
                        name: "FK_otp_codes_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_items",
                schema: "restaurants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    restaurant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    price_usd = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    stock_count = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_menu_items_menu_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "restaurants",
                        principalTable: "menu_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_menu_items_restaurants_restaurant_id",
                        column: x => x.restaurant_id,
                        principalSchema: "restaurants",
                        principalTable: "restaurants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_item_customization_groups",
                schema: "restaurants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    max_selections = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_item_customization_groups", x => x.id);
                    table.ForeignKey(
                        name: "FK_menu_item_customization_groups_menu_items_menu_item_id",
                        column: x => x.menu_item_id,
                        principalSchema: "restaurants",
                        principalTable: "menu_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                schema: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price_usd = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    customizations = table.Column<string>(type: "jsonb", nullable: true),
                    line_total_usd = table.Column<decimal>(type: "numeric(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_items_menu_items_menu_item_id",
                        column: x => x.menu_item_id,
                        principalSchema: "restaurants",
                        principalTable: "menu_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_items_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "orders",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_item_customization_options",
                schema: "restaurants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    price_modifier_usd = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_item_customization_options", x => x.id);
                    table.ForeignKey(
                        name: "FK_menu_item_customization_options_menu_item_customization_gro~",
                        column: x => x.group_id,
                        principalSchema: "restaurants",
                        principalTable: "menu_item_customization_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_coupons_code",
                schema: "orders",
                table: "coupons",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_menu_categories_restaurant_id_display_order",
                schema: "restaurants",
                table: "menu_categories",
                columns: new[] { "restaurant_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_menu_item_customization_groups_menu_item_id",
                schema: "restaurants",
                table: "menu_item_customization_groups",
                column: "menu_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_menu_item_customization_options_group_id",
                schema: "restaurants",
                table: "menu_item_customization_options",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_menu_items_category_id",
                schema: "restaurants",
                table: "menu_items",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_menu_items_restaurant_id",
                schema: "restaurants",
                table: "menu_items",
                column: "restaurant_id",
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_menu_item_id",
                schema: "orders",
                table: "order_items",
                column: "menu_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_order_id",
                schema: "orders",
                table: "order_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_coupon_id",
                schema: "orders",
                table: "orders",
                column: "coupon_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_restaurant_id_status",
                schema: "orders",
                table: "orders",
                columns: new[] { "restaurant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_user_id_created_at",
                schema: "orders",
                table: "orders",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_otp_codes_user_id_channel_created_at",
                schema: "auth",
                table: "otp_codes",
                columns: new[] { "user_id", "channel", "created_at" },
                descending: new[] { false, false, true },
                filter: "\"consumed_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_restaurants_status_cuisine",
                schema: "restaurants",
                table: "restaurants",
                columns: new[] { "status", "cuisine" },
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                schema: "auth",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_phone",
                schema: "auth",
                table: "users",
                column: "phone",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "menu_item_customization_options",
                schema: "restaurants");

            migrationBuilder.DropTable(
                name: "order_items",
                schema: "orders");

            migrationBuilder.DropTable(
                name: "otp_codes",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "menu_item_customization_groups",
                schema: "restaurants");

            migrationBuilder.DropTable(
                name: "orders",
                schema: "orders");

            migrationBuilder.DropTable(
                name: "menu_items",
                schema: "restaurants");

            migrationBuilder.DropTable(
                name: "coupons",
                schema: "orders");

            migrationBuilder.DropTable(
                name: "users",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "menu_categories",
                schema: "restaurants");

            migrationBuilder.DropTable(
                name: "restaurants",
                schema: "restaurants");
        }
    }
}
