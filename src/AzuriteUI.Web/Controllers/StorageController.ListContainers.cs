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

public partial class StorageController : ODataController
{
    /// <summary>
    /// Searches for containers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In this case, an OData v4 query is accepted with the following options:
    /// </para>
    /// <para>
    /// - <c>$count</c> is used to return a count of entities within the search parameters within the <see cref="PagedResponse{T}"/> response.
    /// - <c>$filter</c> is used to restrict the entities to be sent.
    /// - <c>$orderby</c> is used for ordering the entities to be sent.
    /// - <c>$select</c> is used to select which properties of the entities are sent.
    /// - <c>$skip</c> is used to skip some entities
    /// - <c>$top</c> is used to limit the number of entities returned.
    /// </para>
    /// </remarks>
    /// <param name="count">The OData <c>$count</c> query option.</param>
    /// <param name="filter">The OData <c>$filter</c> query option.</param>
    /// <param name="orderby">The OData <c>$orderby</c> query option.</param>
    /// <param name="select">The OData <c>$select</c> query option.</param>
    /// <param name="skip">The OData <c>$skip</c> query option.</param>
    /// <param name="top">The OData <c>$top</c> query option.</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>An <see cref="OkObjectResult"/> response object with the paged response.</returns>
    [HttpGet]
    [EndpointName("ListContainers")]
    [EndpointDescription("Lists all containers in the storage account, with OData v4 query support.")]
    [ProducesResponseType<PagedResponse<ContainerDTO>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public virtual async Task<IActionResult> ListContainersAsync(
        [FromQuery(Name = "$count")] bool? count = null,
        [FromQuery(Name = "$filter")] string? filter = null,
        [FromQuery(Name = "$orderby")] string? orderby = null,
        [FromQuery(Name = "$select")] string? select = null,
        [FromQuery(Name = "$skip")] int? skip = null,
        [FromQuery(Name = "$top")] int? top = null,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("ListContainersAsync(query: {QueryString})", Request.QueryString.Value);

        BuildServiceProvider(Request);
        ODataValidationSettings validationSettings = new() { MaxTop = ODataMaxTop };
        ODataQuerySettings querySettings = new() { PageSize = ODataPageSize, EnsureStableOrdering = true };
        ODataQueryContext queryContext = new(EdmModel, typeof(ContainerDTO), new ODataPath());
        ODataQueryOptions<ContainerDTO> queryOptions = new(queryContext, Request);

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
        IQueryable<ContainerDTO> dataset = Repository.Containers.AsQueryable();
        int totalCount = await dataset.CountAsync(cancellationToken).ConfigureAwait(false);

        // Apply the filter to the dataset.
        IQueryable<ContainerDTO> filteredDataset = dataset.ApplyODataFilter(queryOptions.Filter, querySettings);
        int filteredCount = await filteredDataset.CountAsync(cancellationToken).ConfigureAwait(false);

        // Now apply orderby, skip, and top options to the dataset
        IQueryable<ContainerDTO> orderedDataset = filteredDataset
            .ApplyODataOrderBy(queryOptions.OrderBy, querySettings)
            .ApplyODataPaging(queryOptions, querySettings);

        // Create the paged response.
        var resultSet = await orderedDataset.ToListAsync(cancellationToken).ConfigureAwait(false);
        PagedResponse<object> response = CreatePagedResponse(queryOptions, resultSet.ApplyODataSelect(queryOptions.SelectExpand, querySettings), totalCount, filteredCount);
        Logger.LogInformation("ListContainersAsync() returning {Count} items (Total: {Total}, Filtered: {Filtered})", resultSet.Count, totalCount, filteredCount);
        return Ok(response);
    }    
}