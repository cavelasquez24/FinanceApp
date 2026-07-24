using System.Security.Claims;
using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.DTOs.Dashboard;
using FinanceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/v1/dashboard/current")]
[Authorize]
public class CurrentDashboardController : ControllerBase
{
    private readonly ICurrentDashboardService _service;

    public CurrentDashboardController(ICurrentDashboardService service)
    {
        _service = service;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value!);

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _service.GetAsync(GetUserId(), cancellationToken);
        return Ok(ApiResponse<CurrentDashboardDto>.Ok(result));
    }
}
