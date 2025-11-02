using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Wrapper;

/// <summary>
/// A set of extension methods that makes working with OData easier.
/// </summary>
public static class ODataExtensions
{
    /// <summary>
    /// Applies the <c>$filter</c> OData query option to the provided query.
    /// </summary>
    /// <typeparam name="T">The type of entity being queried.</typeparam>
    /// <param name="query">The current <see cref="IQueryable{T}"/> representing the query.</param>
    /// <param name="filterQueryOption">The filter query option to apply.</param>
    /// <param name="settings">The query settings being used.</param>
    /// <returns>A modified <see cref="IQueryable{T}"/> representing the filtered data.</returns>
    internal static IQueryable<T> ApplyODataFilter<T>(this IQueryable<T> query, FilterQueryOption? filterQueryOption, ODataQuerySettings settings)
        => (IQueryable<T>)(filterQueryOption?.ApplyTo(query, settings) ?? query);

    /// <summary>
    /// Applies the <c>$orderBy</c> OData query option to the provided query for blobs.
    /// </summary>
    /// <typeparam name="T">The type of entity being queried.</typeparam>
    /// <param name="query">The current <see cref="IQueryable{T}"/> representing the query.</param>
    /// <param name="orderingQueryOption">The ordering query option to apply.</param>
    /// <param name="settings">The query settings being used.</param>
    /// <returns>A modified <see cref="IQueryable{T}"/> representing the ordered data.</returns>
    internal static IQueryable<T> ApplyODataOrderBy<T>(this IQueryable<T> query, OrderByQueryOption? orderingQueryOption, ODataQuerySettings settings) where T : IBaseDTO
        => orderingQueryOption?.ApplyTo(query, settings).ThenBy(e => e.Name) ?? query.OrderBy(e => e.Name);

    /// <summary>
    /// Applies the <c>$skip</c> and <c>$top</c> OData query options to the provided query.
    /// </summary>
    /// <typeparam name="T">The type of entity being queried.</typeparam>
    /// <param name="query">The current <see cref="IQueryable{T}"/> representing the query.</param>
    /// <param name="options">The query options to apply.</param>
    /// <param name="settings">The query settings being used.</param>
    /// <returns>A modified <see cref="IQueryable{T}"/> representing the paged data.</returns>
    internal static IQueryable<T> ApplyODataPaging<T>(this IQueryable<T> query, ODataQueryOptions<T> options, ODataQuerySettings settings)
    {
        int takeValue = Math.Min(options.Top?.Value ?? int.MaxValue, settings.PageSize ?? 100);
        int skipValue = Math.Max(options.Skip?.Value ?? 0, 0);
        return query.Skip(skipValue).Take(takeValue);
    }

    /// <summary>
    /// Applies the <c>$select</c> OData query option to the provided query.
    /// </summary>
    /// <typeparam name="T">The type of entity being queried.</typeparam>
    /// <param name="dataset">The datset to apply the <c>$select</c> option to.</param>
    /// <param name="queryOption">The <see cref="SelectExpandQueryOption"/> to apply.</param>
    /// <param name="settings">The query settings being used.</param>
    /// <returns>The resulting dataset after property selection.</returns>
    internal static IEnumerable<object> ApplyODataSelect<T>(this IList<T> dataset, SelectExpandQueryOption? queryOption, ODataQuerySettings settings)
    {
        if (dataset.Count == 0 || queryOption is null)
        {
            return dataset.Cast<object>().AsEnumerable();
        }

        return queryOption.ApplyTo(dataset.AsQueryable(), settings).Cast<object>().ToList().Select(x => ((ISelectExpandWrapper)x).ToDictionary());
    }
}
