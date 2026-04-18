using ContentAPI.Models.Common;

namespace ContentAPI.Extensions;

public static class PaginationExtensions
{
    public static PagedResponse<TResponse> ToPagedResponse<TEntity, TResponse>(
        this IEnumerable<TEntity> source,
        int page,
        int pageSize,
        Func<TEntity, TResponse> mapFunction)
    {
        var totalCount = source.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(mapFunction)
            .ToList();

        var meta = new PaginationMeta(
            page,
            pageSize,
            totalPages,
            totalCount,
            page < totalPages,
            page > 1
        );

        return new PagedResponse<TResponse>(items, meta);
    }
}