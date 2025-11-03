using AzuriteUI.Web.Controllers.Models;
using AzuriteUI.Web.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System.Globalization;

namespace AzuriteUI.Web.Controllers;

/// <summary>
/// The controller that manages all the endpoints under <c>/api/containers</c>, which is basically
/// the ones that deal with storage.
/// </summary>
/// <param name="repository">The storage repository to use for data access.</param>
/// <param name="odataModel">The <see cref="IEdmModel"/> to use for OData operations.</param>
/// <param name="logger">The logger to use for diagnostics and reporting.</param>
[ApiController]
[Route("api/containers")]
public partial class StorageController(
    IStorageRepository repository,
    IEdmModel odataModel,
    ILogger<StorageController> logger
) : ODataController
{
    /// <summary>
    /// The storage repository to use for data access.
    /// </summary>
    public IStorageRepository Repository => repository;

    /// <summary>
    /// The EDM model for OData.
    /// </summary>
    public IEdmModel EdmModel => odataModel;

    /// <summary>
    /// The logger for diagnostics and reporting.
    /// </summary>
    public ILogger Logger => logger;

    /// <summary>
    /// The default page size for OData queries.
    /// </summary>
    public const int ODataPageSize = 25;

    /// <summary>
    /// The maximum allowed value for $top in OData queries.
    /// </summary>
    public const int ODataMaxTop = 250;

    /// <summary>
    /// The name of the query parameter for skipping ahead.
    /// </summary>
    public const string SkipParameterName = "$skip";

    /// <summary>
    /// The name of the query parameter for taking some entities.
    /// </summary>
    public const string TopParameterName = "$top";

    /// <summary>
    /// Creates a new <see cref="IServiceProvider"/> for the request to handle OData queries.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/> currently being processed.</param>
    /// <returns>An <see cref="IServiceProvider"/> for the request pipeline with OData support.</returns>
    [NonAction]
    internal static IServiceProvider BuildServiceProvider(HttpRequest request)
    {
        IServiceCollection services = new ServiceCollection();

        services.AddSingleton(_ => new DefaultQueryConfigurations
        {
            EnableCount = true,
            EnableFilter = true,
            EnableOrderBy = true,
            EnableSelect = true
        });

        services
            .AddScoped(_ => new ODataQuerySettings { EnsureStableOrdering = true })
            .AddSingleton<ODataUriResolver>(_ => new UnqualifiedODataUriResolver { EnableCaseInsensitive = true })
            .AddScoped<ODataUriParserSettings>();

        IServiceProvider provider = services.BuildServiceProvider();
        request.ODataFeature().Services = provider;
        return provider;
    }

    /// <summary>
    /// Creates a <see cref="PagedResponse{T}"/> object from the results of a query.
    /// </summary>
    /// <param name="queryOptions">The OData query options to use in constructing the result.</param>
    /// <param name="results">The set of results.</param>
    /// <param name="totalCount">The total count of items in the result set, without filtering.</param>
    /// <param name="filteredCount">The total count of items in the result set, with filtering.</param>
    /// <returns>A <see cref="PagedResponse{T}"/> object.</returns>
    [NonAction]
    internal static PagedResponse<object> CreatePagedResponse(ODataQueryOptions queryOptions, IEnumerable<object>? results, int totalCount, int filteredCount)
    {
        int resultCount = results?.Count() ?? 0;
        int originalSkip = queryOptions.Skip?.Value ?? 0;
        int originalTop = queryOptions.Top?.Value ?? ODataPageSize;

        var nextParams = CalculateNextLinkParameters(originalSkip, originalTop, resultCount, filteredCount);
        var prevParams = CalculatePrevLinkParameters(originalSkip, originalTop);

        return new()
        {
            Items = results ?? [],
            FilteredCount = filteredCount,
            TotalCount = totalCount,
            NextLink = nextParams.HasValue ? CreateLink(queryOptions.Request, nextParams.Value.skip, nextParams.Value.top) : null,
            PrevLink = prevParams.HasValue ? CreateLink(queryOptions.Request, prevParams.Value.skip, prevParams.Value.top) : null
        };
    }

    /// <summary>
    /// Creates a link with the given skip and top parameters.
    /// </summary>
    /// <param name="request">The original request.</param>
    /// <param name="skip">The new value of the skip parameter.</param>
    /// <param name="top">The new value of the top parameter.</param>
    /// <returns>The query string with the updated parameters.</returns>
    internal static string CreateLink(HttpRequest request, int skip = 0, int top = 0)
    {
        Dictionary<string, StringValues> query = QueryHelpers.ParseNullableQuery(request.QueryString.Value) ?? [];

        // Update or remove skip parameter
        if (skip > 0)
        {
            query[SkipParameterName] = skip.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            query.Remove(SkipParameterName);
        }

        // Update or remove top parameter
        if (top > 0)
        {
            query[TopParameterName] = top.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            query.Remove(TopParameterName);
        }

        return QueryHelpers.AddQueryString(string.Empty, query).TrimStart('?');
    }

    /// <summary>
    /// Calculates the skip and top parameters for the next page link.
    /// </summary>
    /// <param name="currentSkip">The current skip value.</param>
    /// <param name="currentTop">The current top value.</param>
    /// <param name="resultCount">The number of results on the current page.</param>
    /// <param name="filteredCount">The total count of filtered results.</param>
    /// <returns>A tuple of (skip, top) for the next page, or null if there is no next page.</returns>
    [NonAction]
    internal static (int skip, int top)? CalculateNextLinkParameters(
        int currentSkip,
        int currentTop,
        int resultCount,
        int filteredCount)
    {
        int nextSkip = currentSkip + resultCount;
        return nextSkip < filteredCount
            ? (nextSkip, currentTop)
            : null;
    }

    /// <summary>
    /// Calculates the skip and top parameters for the previous page link.
    /// </summary>
    /// <param name="currentSkip">The current skip value.</param>
    /// <param name="currentTop">The current top value.</param>
    /// <returns>A tuple of (skip, top) for the previous page, or null if there is no previous page.</returns>
    [NonAction]
    internal static (int skip, int top)? CalculatePrevLinkParameters(
        int currentSkip,
        int currentTop)
    {
        return currentSkip > 0
            ? (Math.Max(0, currentSkip - currentTop), currentTop)
            : null;
    }    
}