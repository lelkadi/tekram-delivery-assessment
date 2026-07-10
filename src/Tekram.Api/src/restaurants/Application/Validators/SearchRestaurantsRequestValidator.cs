namespace Tekram.Api.src.restaurants.Application.Validators;

using FluentValidation;
using Tekram.Api.src.restaurants.Application.DTOs;

public class SearchRestaurantsRequestValidator : AbstractValidator<SearchRestaurantsRequest>
{
    public SearchRestaurantsRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.Limit).InclusiveBetween(1, 50);
        RuleFor(x => x.PriceTier).InclusiveBetween(1, 4).When(x => x.PriceTier.HasValue);
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.Cuisine).MaximumLength(100);
    }
}
