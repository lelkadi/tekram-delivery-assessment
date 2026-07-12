namespace Tekram.Api.src.orders.Application.Handlers;

using FluentValidation;
using Tekram.Api.src.auth.Application.Interfaces;
using Tekram.Api.src.orders.Application.DTOs;
using Tekram.Api.src.orders.Application.Interfaces;
using Tekram.Api.src.orders.Application.Validators;
using Tekram.Api.src.orders.Domain;
using Tekram.Api.src.shared;

public sealed class PlaceOrderHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly IMenuPricingReader _menuPricingReader;
    private readonly PlaceOrderRequestValidator _validator;
    private readonly IUserRepository _userRepository;

    public PlaceOrderHandler(
        IOrderRepository orderRepository,
        ICouponRepository couponRepository,
        IMenuPricingReader menuPricingReader,
        PlaceOrderRequestValidator validator,
        IUserRepository userRepository)
    {
        _orderRepository = orderRepository;
        _couponRepository = couponRepository;
        _menuPricingReader = menuPricingReader;
        _validator = validator;
        _userRepository = userRepository;
    }

    public async Task<OrderResponse> HandleAsync(Guid userId, PlaceOrderRequest request, CancellationToken ct)
    {
        // Validate
        await _validator.ValidateAndThrowAsync(request, ct);

        // Check user verification
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null || !user.EmailVerified || !user.PhoneVerified)
            throw new DomainException(403, ErrorCodes.VerificationRequired, "User must be verified to place an order.");

        // Calculate subtotal from items
        decimal subtotal = 0m;
        var orderItems = new List<OrderItem>();

        foreach (var itemReq in request.Items)
        {
            var menuItem = await _menuPricingReader.GetItemForPricingAsync(itemReq.MenuItemId, ct);
            if (menuItem == null)
                throw new DomainException(409, ErrorCodes.ItemUnavailable, $"Menu item {itemReq.MenuItemId} is not available.");

            if (menuItem.StockCount.HasValue && menuItem.StockCount < itemReq.Quantity)
                throw new DomainException(409, ErrorCodes.ItemUnavailable, $"Insufficient stock for '{menuItem.Name}'.");

            decimal unitPrice = menuItem.PriceUsd;

            // Resolve customization pricing
            if (itemReq.CustomizationChoices is { Count: > 0 })
            {
                foreach (var choice in itemReq.CustomizationChoices)
                {
                    var option = await _menuPricingReader.GetOptionAsync(choice.OptionId, ct);
                    if (option != null)
                        unitPrice += option.PriceModifierUsd;
                }
            }

            var lineTotal = Math.Round(unitPrice * itemReq.Quantity, 2);
            subtotal += lineTotal;

            orderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                MenuItemId = itemReq.MenuItemId,
                Quantity = itemReq.Quantity,
                UnitPriceUsd = unitPrice,
                LineTotalUsd = lineTotal
            });
        }

        subtotal = Math.Round(subtotal, 2);

        // Apply coupon
        decimal discount = 0m;
        Guid? couponId = null;
        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var coupon = await _couponRepository.GetByCodeAsync(request.CouponCode, ct);
            if (coupon == null)
                throw new DomainException(422, ErrorCodes.InvalidCoupon, "Invalid or inactive coupon code.");

            if (coupon.ValidUntil < DateTime.UtcNow)
                throw new DomainException(422, ErrorCodes.InvalidCoupon, "Coupon has expired.");

            if (coupon.ValidFrom > DateTime.UtcNow)
                throw new DomainException(422, ErrorCodes.InvalidCoupon, "Coupon is not yet valid.");

            if (coupon.MaxUses.HasValue && coupon.UsesCount >= coupon.MaxUses.Value)
                throw new DomainException(422, ErrorCodes.InvalidCoupon, "Coupon usage limit reached.");

            if (subtotal < coupon.MinSubtotalUsd)
                throw new DomainException(422, ErrorCodes.InvalidCoupon, $"Minimum subtotal of ${coupon.MinSubtotalUsd:F2} required for this coupon.");

            discount = OrderPricingPolicy.ApplyCoupon(subtotal, coupon);
            couponId = coupon.Id;

            await _couponRepository.IncrementUsageAsync(coupon, ct);
        }

        // Delivery fee & surcharge
        var deliveryFee = OrderPricingPolicy.BaseDeliveryFeeUsd;
        var surcharge = OrderPricingPolicy.CalculateSmallOrderSurcharge(subtotal);
        var total = OrderPricingPolicy.CalculateTotal(subtotal, deliveryFee, surcharge, discount);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RestaurantId = request.RestaurantId,
            CouponId = couponId,
            Status = "pending",
            DeliveryAddress = request.DeliveryAddress,
            PaymentMethod = request.PaymentMethod,
            SubtotalUsd = subtotal,
            DeliveryFeeUsd = deliveryFee,
            SmallOrderSurchargeUsd = surcharge,
            DiscountUsd = discount,
            TotalUsd = total,
            OrderItems = orderItems
        };

        await _orderRepository.AddAsync(order, ct);

        return new OrderResponse(
            BookingId: order.Id,
            Status: order.Status,
            Totals: new OrderTotalsResponse(
                SubtotalUsd: order.SubtotalUsd,
                DeliveryFeeUsd: order.DeliveryFeeUsd,
                SmallOrderSurchargeUsd: order.SmallOrderSurchargeUsd,
                DiscountUsd: order.DiscountUsd,
                TotalUsd: order.TotalUsd
            ),
            CreatedAt: order.CreatedAt
        );
    }
}
