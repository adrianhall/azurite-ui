using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using AzuriteUI.Web.Extensions;

namespace AzuriteUI.Web.Filters;

/// <summary>
/// Result filter that automatically adds HTTP headers (ETag, Last-Modified, Link)
/// for responses containing IBaseDTO objects.
/// </summary>
/// <remarks>
/// This filter only processes single DTO responses. Collections (PagedResponse, IEnumerable)
/// are skipped since they don't have a single ETag or Last-Modified value.
/// </remarks>
/// <param name="logger">The logger instance.</param>
/// <param name="linkGenerator">The link generator for creating URLs.</param>
public class DtoHeaderFilter(LinkGenerator linkGenerator, ILogger<DtoHeaderFilter> logger) : IResultFilter
{
    /// <summary>
    /// Called before the action result is executed.
    /// </summary>
    /// <param name="context">The result executing context.</param>
    public void OnResultExecuting(ResultExecutingContext context)
    {
        // Only process ObjectResult responses
        if (context.Result is not ObjectResult objectResult || objectResult.Value is not IBaseDTO dto)
        {
            return;
        }

        logger.LogDebug("Adding DTO conditional and link headers for endpoint response {endpoint}", context.HttpContext.GetEndpoint()?.DisplayName);
        AddETagHeader(context.HttpContext, dto);
        AddLastModifiedHeader(context.HttpContext, dto);
        AddLinkHeader(context.HttpContext, dto);
    }

    /// <summary>
    /// Called after the action result is executed.
    /// </summary>
    /// <param name="context">The result executed context.</param>
    public void OnResultExecuted(ResultExecutedContext context)
    {
        // No-op - we only need OnResultExecuting
    }

    /// <summary>
    /// Adds an ETag header based on the DTO's ETag property.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="dto">The DTO being processed.</param>
    internal void AddETagHeader(HttpContext context, IBaseDTO dto)
    {
        if (!string.IsNullOrEmpty(dto.ETag))
        {
            // Construct the ETag header value (quoted per RFC 7232)
            var etag = $"\"{dto.ETag.Dequote()}\""; // Defensive: ensure ETag is quoted only once
            context.Response.Headers.ETag = etag;
        }
        else
        {
            logger.LogWarning("DTO ETag is null or empty; skipping ETag header");
        }
    }

    /// <summary>
    /// Adds a Last-Modified header based on the DTO's LastModified property.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="dto">The DTO being processed.</param>
    internal void AddLastModifiedHeader(HttpContext context, IBaseDTO dto)
    {
        if (dto.LastModified != DateTimeOffset.MinValue)
        {
            // Format the Last-Modified header value in RFC 1123 format
            var lastModified = dto.LastModified.ToString("R");
            context.Response.Headers.LastModified = lastModified;
        }
        else
        {
            logger.LogWarning("DTO LastModified is MinValue; skipping Last-Modified header");
        }
    }

    /// <summary>
    /// Adds a Link header with a self-referential URL for the DTO.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="dto">The DTO being processed.</param>
    internal void AddLinkHeader(HttpContext context, IBaseDTO dto)
    {
        // TODO: Add Link header as format Link: <url>; rel="self"
        string? endpointName = GetEndpointName(dto);
        if (string.IsNullOrEmpty(endpointName))
        {
            logger.LogWarning("Could not determine endpoint name for DTO Link header; skipping Link header");
            return;
        }


        string? selfLink = linkGenerator.GetUriByName(
            context,
            endpointName,
            new RouteValueDictionary(context.Request.RouteValues),
            scheme: context.Request.Scheme,
            host: context.Request.Host
        );
        if (string.IsNullOrEmpty(selfLink))
        {
            logger.LogWarning("Could not generate self link URL for DTO Link header; skipping Link header");
            return;
        }

        var linkHeader = $"<{selfLink}>; rel=\"self\"";
        context.Response.Headers.Link = linkHeader;
    }

    /// <summary>
    /// Determines the endpoint name for generating the self link based on the DTO type.
    /// </summary>
    /// <param name="dto">The DTO being processed.</param>
    /// <returns>The endpoint name for the Get operation.</returns>
    internal static string? GetEndpointName(IBaseDTO dto)
    {
        // Infer the endpoint name based on the DTO type
        return dto switch
        {
            ContainerDTO => "GetContainerByName",
            BlobDTO => "GetBlobByName",
            _ => null,
        };
    }
}
