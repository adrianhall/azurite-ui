using AzuriteUI.Web.Services.Azurite.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace AzuriteUI.Web.IntegrationTests.Helpers;

[ApiController]
[Route("api/test/exceptions")]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test controller methods")]
[ExcludeFromCodeCoverage(Justification = "Test controller")]
public class TestExceptionController : ControllerBase
{
    [HttpGet("not-found")]
    public IActionResult GetNotFound()
    {
        throw new ResourceNotFoundException("The requested resource was not found.")
        {
            ResourceName = "test-resource"
        };
    }

    [HttpGet("conflict")]
    public IActionResult GetConflict()
    {
        throw new ResourceExistsException("The resource already exists.")
        {
            ResourceName = "test-resource"
        };
    }

    [HttpGet("bad-request")]
    public IActionResult GetBadRequest()
    {
        throw new AzuriteServiceException("The request is invalid.")
        {
            StatusCode = 400
        };
    }

    [HttpGet("range-not-satisfiable")]
    public IActionResult GetRangeNotSatisfiable()
    {
        throw new RangeNotSatisfiableException("The requested range is not satisfiable.")
        {
            StatusCode = 416
        };
    }

    [HttpGet("service-unavailable")]
    public IActionResult GetServiceUnavailable()
    {
        throw new AzuriteServiceException("The service is currently unavailable.")
        {
            StatusCode = 503
        };
    }

    [HttpGet("bad-gateway")]
    public IActionResult GetBadGateway()
    {
        throw new AzuriteServiceException("Bad gateway error occurred.")
        {
            StatusCode = 502
        };
    }
}