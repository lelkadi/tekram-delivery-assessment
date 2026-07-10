namespace Tekram.Api.src.orders.Application.Validators;

using FluentValidation;
using Tekram.Api.src.orders.Application.DTOs;

public class PlaceOrderRequestValidator : AbstractValidator<PlaceOrderRequest>
{
    private static readonly HashSet<string> ValidPaymentMethods = new() { "COD", "WALLET" };

    public PlaceOrderRequestValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one item is required.");
        RuleFor(x => x.DeliveryAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .Must(m => ValidPaymentMethods.Contains(m))
            .WithMessage("Payment method must be 'COD' or 'WALLET'.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.MenuItemId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}
