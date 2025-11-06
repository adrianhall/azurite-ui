using AzuriteUI.Web.Controllers.Models;
using AzuriteUI.Web.Extensions;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData;
using Microsoft.OData.UriParser;
using System.Net.Mime;

namespace AzuriteUI.Web.Controllers;

public partial class UploadsController : ODataController
{
    /// <summary>
    /// <para>
    /// Lists all active upload sessions with OData v4 query support.
    /// The GET method is used to retrieve resource representation. The resource is never modified.
    /// Supports the following OData query options:
    /// </para>
    /// <para>
    /// - <c>$count</c> is used to return a count of entities within the search parameters within the <see cref="PagedResponse{T}"/> response.
    /// - <c>$filter</c> is used to restrict the entities to be sent.
    /// - <c>$orderby</c> is used for ordering the entities to be sent.
    /// - <c>$select</c> is used to select which properties of the entities are sent.
    /// - <c>$skip</c> is used to skip some entities.
    /// - <c>$top</c> is used to limit the number of entities returned.
    /// </para>
    /// </summary>
    /// <remarks>
    /// We include the query parameters to drive the OpenAPI documentation, but they are not actually used in the code.
    /// </remarks>
    /// <param name="count">The OData <c>$count</c> query option.</param>
    /// <param name="filter">The OData <c>$filter</c> query option.</param>
    /// <param name="orderby">The OData <c>$orderby</c> query option.</param>
    /// <param name="select">The OData <c>$select</c> query option.</param>
    /// <param name="skip">The OData <c>$skip</c> query option.</param>
    /// <param name="top">The OData <c>$top</c> query option.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An <see cref="OkObjectResult"/> response object with the paged response.</returns>
    [HttpGet]
    [EndpointName("ListUploads")]
    [EndpointDescription("Lists all active upload sessions, with OData v4 query support.")]
    [ProducesResponseType<PagedResponse<UploadDTO>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public virtual async Task<IActionResult> ListUploadsAsync(
        [FromQuery(Name = "$count")] bool? count = null,
        [FromQuery(Name = "$filter")] string? filter = null,
        [FromQuery(Name = "$orderby")] string? orderby = null,
        [FromQuery(Name = "$select")] string? select = null,
        [FromQuery(Name = "$skip")] int? skip = null,
        [FromQuery(Name = "$top")] int? top = null,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("ListUploadsAsync(query: {QueryString})", Request.QueryString.Value);

        StorageController.BuildServiceProvider(Request);
        ODataValidationSettings validationSettings = new() { MaxTop = ODataMaxTop };
        ODataQuerySettings querySettings = new() { PageSize = ODataPageSize, EnsureStableOrdering = true };
        ODataQueryContext queryContext = new(EdmModel, typeof(UploadDTO), new ODataPath());
        ODataQueryOptions<UploadDTO> queryOptions = new(queryContext, Request);

        try
        {
            queryOptions.Validate(validationSettings);
        }
        catch (ODataException validationException)
        {
            Logger.LogWarning("Error when validating query: {Message}", validationException.Message);
            return BadRequest(validationException.Message);
        }

        // Determine the dataset to be queried.
        IQueryable<UploadDTO> dataset = Repository.Uploads.AsQueryable();
        int totalCount = await dataset.CountAsync(cancellationToken).ConfigureAwait(false);

        // Apply the filter to the dataset.
        IQueryable<UploadDTO> filteredDataset = dataset.ApplyODataFilter(queryOptions.Filter, querySettings);
        int filteredCount = await filteredDataset.CountAsync(cancellationToken).ConfigureAwait(false);

        // Now apply orderby, skip, and top options to the dataset
        IQueryable<UploadDTO> orderedDataset = filteredDataset
            .ApplyUploadOrderBy(queryOptions.OrderBy, querySettings)
            .ApplyODataPaging(queryOptions, querySettings);

        // Create the paged response.
        var resultSet = await orderedDataset.ToListAsync(cancellationToken).ConfigureAwait(false);
        PagedResponse<object> response = StorageController.CreatePagedResponse(
            queryOptions,
            resultSet.ApplyODataSelect(queryOptions.SelectExpand, querySettings),
            totalCount,
            filteredCount);

        Logger.LogInformation("ListUploadsAsync() returning {Count} items (Total: {Total}, Filtered: {Filtered})",
            resultSet.Count, totalCount, filteredCount);

        return Ok(response);
    }
}
