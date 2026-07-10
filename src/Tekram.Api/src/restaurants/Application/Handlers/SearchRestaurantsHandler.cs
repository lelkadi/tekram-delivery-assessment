namespace Tekram.Api.src.restaurants.Application.Handlers;

using FluentValidation;
using Tekram.Api.src.restaurants.Application.DTOs;
using Tekram.Api.src.restaurants.Application.Interfaces;
using Tekram.Api.src.shared;

public class SearchRestaurantsHandler
{
    private readonly IRestaurantRepository _repository;
    private readonly IValidator<SearchRestaurantsRequest> _validator;

    public SearchRestaurantsHandler(
        IRestaurantRepository repository,
        IValidator<SearchRestaurantsRequest> validator)
    {
        _repository = repository;
        _validator = validator;
    }

    public async Task<RestaurantListResponse> HandleAsync(SearchRestaurantsRequest request,
        CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        var (items, totalCount) = await _repository.SearchAsync(
            request.Search, request.Cuisine, request.PriceTier,
            request.Page, request.Limit, ct);

        var data = items.Select(r => new RestaurantListItem(
            Id: r.Id,
            Name: r.Name,
            Description: r.Description,
            Cuisine: r.Cuisine,
            Rating: r.Rating,
            AveragePrepTimeMinutes: r.AvgPrepMinutes,
            PriceTier: r.PriceTier,
            Latitude: r.Latitude,
            Longitude: r.Longitude,
            Status: r.Status
        )).ToList();

        return new RestaurantListResponse(
            Data: data,
            Pagination: PaginationExtensions.ToPaginationResponse(totalCount, request.Page, request.Limit)
        );
    }
}
