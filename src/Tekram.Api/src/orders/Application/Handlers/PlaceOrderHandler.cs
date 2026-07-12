namespace Tekram.Api.src.orders.Application.Handlers;

using System.Linq;
using System.Text.Json;
using FluentValidation;
using Tekram.Api.src.auth.Application.Interfaces;
using Tekram.Api.src.orders.Application.DTOs;
using Tekram.Api.src.orders.Application.Interfaces;
using Tekram.Api.src.orders.Domain;
using Tekram.Api.src.shared;

public class PlaceOrderHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly IMenuPricingReader _menuPricingReader;
    private readonly IValidator<PlaceOrderRequest> _validator;

    public PlaceOrderHandler(
        IUserRepository userRepository,
        IOrderRepository orderRepository,
        ICouponRepository couponRepository,
        IMenuPricingReader menuPricingReader,
        IValidator<PlaceOrderRequest> validator)
    {
        _userRepository = userRepository;
        _orderRepository = orderRepository;
        _couponRepository = couponRepository;
        _menuPricingReader = menuPricingReader;
        _validator = validator;
    }

    public async Task<OrderResponse> HandleAsync(Guid userId, PlaceOrderRequest request,
        CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        // Verification gate
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            throw new DomainException(404, ErrorCodes.InvalidCredentials, "User not found.");

        if (!user.EmailVerified || !user.PhoneVerified)
            throw new DomainException(403, ErrorCodes.VerificationRequired,
                "Both email and phone must be verified before placing an order.");

        // Price parity verification — re-fetch every item from the DB
        var orderItems = new List<(OrderItemRequest requestItem, decimal unitPrice, string? customizations)>();
        decimal subtotalUsd = 0;

        foreach (var item in request.Items)
        {
            var menuItem = await _menuPricingReader.GetItemForPricingAsync(item.MenuItemId, ct);
            if (menuItem is null || menuItem.DeletedAt.HasValue)
                throw new DomainException(409, ErrorCodes.ItemUnavailable,
                    $"Item {item.MenuItemId} is not available.");

            // Stock validation
            if (menuItem.StockCount.HasValue && menuItem.StockCount.Value < item.Quantity)
                throw new DomainException(409, ErrorCodes.ItemUnavailable,
                    $"Item '{menuItem.Name}' has insufficient stock.");

            var unitPrice = menuItem.PriceUsd;
            decimal customizationMarkup = 0;
            List<object>? customizationSnapshots = null;

            if (item.CustomizationChoices?.Count > 0)
            {
                // Validate that each claimed customization group belongs to this menu item
                var groups = await _menuPricingReader.GetCustomizationGroupsAsync(item.MenuItemId, ct);
                var validGroupIds = groups.Select(g => g.Id).ToHashSet();

                customizationSnapshots = new List<object>();
                foreach (var choice in item.CustomizationChoices)
                {
                    if (!validGroupIds.Contains(choice.GroupId))
                        throw new DomainException(422, ErrorCodes.InvalidCustomization,
                            $"Customization group {choice.GroupId} is not valid for this menu item.");

                    var option = await _menuPricingReader.GetOptionAsync(choice.OptionId, ct);
                    if (option is null)
                        throw new DomainException(409, ErrorCodes.ItemUnavailable,
                            $"Customization option {choice.OptionId} is not available.");

                    // Verify the option actually belongs to the claimed group
                    if (option.GroupId != choice.GroupId)
                        throw new DomainException(422, ErrorCodes.InvalidCustomization,
                            $"Option '{option.Name}' does not belong to group {choice.GroupId}.");

                    customizationMarkup += option.PriceModifierUsd;

                    customizationSnapshots.Add(new
                    {
                        group_id = choice.GroupId,
                        option_id = choice.OptionId,
                        option_name = option.Name,
                        price_modifier_usd = option.PriceModifierUsd
                    });
                }
            }

            var effectiveUnitPrice = unitPrice + customizationMarkup;
            var lineTotal = effectiveUnitPrice * item.Quantity;
            subtotalUsd += lineTotal;

            orderItems.Add((item, effectiveUnitPrice,
                customizationSnapshots is { Count: > 0 }
                    ? JsonSerializer.Serialize(customizationSnapshots)
                    : null));
        }

        subtotalUsd = Math.Round(subtotalUsd, 2);

        // Delivery fee
        var deliveryFeeUsd = OrderPricingPolicy.CalculateDeliveryFee(0, 0);

        // Small order surcharge
        var surchargeUsd = OrderPricingPolicy.CalculateSmallOrderSurcharge(subtotalUsd);

        // Coupon
        decimal discountUsd = 0;
        Guid? couponId = null;

        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var coupon = await _couponRepository.GetByCodeAsync(request.CouponCode, ct);

            if (coupon is null || !coupon.Active)
                throw new DomainException(422, ErrorCodes.InvalidCoupon,
                    "Invalid or inactive coupon code.");

            var now = DateTime.UtcNow;
            if (now < coupon.ValidFrom || now > coupon.ValidUntil)
                throw new DomainException(422, ErrorCodes.InvalidCoupon,
                    "Coupon is expired or not yet valid.");

            if (coupon.MaxUses.HasValue && coupon.UsesCount >= coupon.MaxUses.Value)
                throw new DomainException(422, ErrorCodes.InvalidCoupon,
                    "Coupon usage limit has been reached.");

            discountUsd = OrderPricingPolicy.ApplyCoupon(subtotalUsd, coupon);

            if (discountUsd <= 0)
                throw new DomainException(422, ErrorCodes.InvalidCoupon,
                    "Coupon does not apply to this order.");

            couponId = coupon.Id;
            coupon.UsesCount++;
        }

        var totalUsd = OrderPricingPolicy.CalculateTotal(subtotalUsd, deliveryFeeUsd, surchargeUsd,
            discountUsd);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RestaurantId = request.RestaurantId,
            CouponId = couponId,
            Status = "pending",
            DeliveryAddress = request.DeliveryAddress,
            PaymentMethod = request.PaymentMethod,
            SubtotalUsd = subtotalUsd,
            DeliveryFeeUsd = deliveryFeeUsd,
            SmallOrderSurchargeUsd = surchargeUsd,
            DiscountUsd = discountUsd,
            TotalUsd = totalUsd,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            OrderItems = orderItems.Select(oi => new OrderItem
            {
                Id = Guid.NewGuid(),
                MenuItemId = oi.requestItem.MenuItemId,
                Quantity = oi.requestItem.Quantity,
                UnitPriceUsd = oi.unitPrice,
                Customizations = oi.customizations,
                LineTotalUsd = oi.unitPrice * oi.requestItem.Quantity
            }).ToList()
        };

        await _orderRepository.AddAsync(order, ct);

        return new OrderResponse(
            BookingId: order.Id,
            Status: order.Status,
            Totals: new OrderTotalsResponse(
                SubtotalUsd: subtotalUsd,
                DeliveryFeeUsd: deliveryFeeUsd,
                SmallOrderSurchargeUsd: surchargeUsd,
                DiscountUsd: discountUsd,
                TotalUsd: totalUsd
            ),
            CreatedAt: order.CreatedAt
        );
    }
}
