namespace Tekram.Api.src.shared;

public record PaginationResponse(
    int CurrentPage,
    int Limit,
    int TotalItems,
    int TotalPages
);

public static class PaginationExtensions
{
    public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> query, int page, int limit)
    {
        return query.Skip((page - 1) * limit).Take(limit);
    }

    public static PaginationResponse ToPaginationResponse(int totalItems, int page, int limit)
    {
        return new PaginationResponse(
            CurrentPage: page,
            Limit: limit,
            TotalItems: totalItems,
            TotalPages: (int)Math.Ceiling(totalItems / (double)limit)
        );
    }
}
