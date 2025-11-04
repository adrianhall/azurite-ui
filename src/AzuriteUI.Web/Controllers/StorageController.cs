using AzuriteUI.Web.Controllers.Models;
using AzuriteUI.Web.Extensions;
using AzuriteUI.Web.Services.Repositories;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System.Diagnostics.CodeAnalysis;
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
    [NonAction]
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

    /// <summary>
    /// Determines if the request has met the preconditions based on the entity's ETag and Last-Modified values.
    /// </summary>
    /// <param name="entity">The entity being referenced for a conditional request.</param>
    /// <returns>The status code to return, or null to continue with the request.</returns>
    [NonAction]
    [SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Readability")]
    internal int? GetResponseForConditionalRequest(IBaseDTO entity)
    {
        RequestHeaders headers = Request.GetTypedHeaders();
        bool isFetch = Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase)
            || Request.Method.Equals("HEAD", StringComparison.OrdinalIgnoreCase);

        if (headers.IfMatch.Count > 0 && !headers.IfMatch.Any(e => e.Matches(entity.ETag)))
        {
            return StatusCodes.Status412PreconditionFailed;
        }

        if (headers.IfMatch.Count == 0 && headers.IfUnmodifiedSince.HasValue && headers.IfUnmodifiedSince.Value <= entity.LastModified)
        {
            return StatusCodes.Status412PreconditionFailed;
        }

        if (headers.IfNoneMatch.Count > 0 && headers.IfNoneMatch.Any(e => e.Matches(entity.ETag)))
        {
            return isFetch ? StatusCodes.Status304NotModified : StatusCodes.Status412PreconditionFailed;
        }

        if (headers.IfNoneMatch.Count == 0 && headers.IfModifiedSince.HasValue && headers.IfModifiedSince.Value > entity.LastModified)
        {
            return isFetch ? StatusCodes.Status304NotModified : StatusCodes.Status412PreconditionFailed;
        }

        return null;
    }

    /// <summary>
    /// Returns a conditional response based on the given status code.
    /// </summary>
    /// <param name="statusCode">The status code to return.</param>
    /// <param name="entity">The entity to include in the response.</param>
    /// <returns>The conditional response.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the status code is not a conditional response.</exception>
    [NonAction]
    internal IActionResult ConditionalResponse(int statusCode, IBaseDTO entity)
    {
        return statusCode switch
        {
            StatusCodes.Status304NotModified => StatusCode(StatusCodes.Status304NotModified),
            StatusCodes.Status412PreconditionFailed => StatusCode(StatusCodes.Status412PreconditionFailed, entity),
            _ => throw new InvalidOperationException("Invalid status code for conditional response.")
        };
    }  
}