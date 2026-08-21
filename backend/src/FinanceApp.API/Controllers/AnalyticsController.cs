using System.Security.Claims;
using FinanceApp.Application.DTOs.Analytics;
using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/v1/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _service;

    public AnalyticsController(IAnalyticsService service) => _service = service;

    private Guid GetUserId() => Guid.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value!);

    [HttpGet("net-worth-timeline")]
    public async Task<IActionResult> GetNetWorthTimeline(
        [FromQuery] int months = 12, CancellationToken cancellationToken = default)
    {
        var result = await _service.GetNetWorthTimelineAsync(GetUserId(), months, cancellationToken);
        return Ok(ApiResponse<NetWorthTimelineDto>.Ok(result));
    }

    [HttpGet("health-score")]
    public async Task<IActionResult> GetHealthScore(
        [FromQuery] int? month = null,
        [FromQuery] int? year = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var result = await _service.GetFinancialHealthScoreAsync(
            GetUserId(), month ?? now.Month, year ?? now.Year, cancellationToken);
        return Ok(ApiResponse<FinancialHealthScoreDto>.Ok(result));
    }

    [HttpGet("expense-intelligence")]
    public async Task<IActionResult> GetExpenseIntelligence(
        [FromQuery] int? month = null,
        [FromQuery] int? year = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var result = await _service.GetExpenseIntelligenceAsync(
            GetUserId(), month ?? now.Month, year ?? now.Year, cancellationToken);
        return Ok(ApiResponse<ExpenseIntelligenceDto>.Ok(result));
    }

    [HttpGet("debt-projection")]
    public async Task<IActionResult> GetDebtProjection(CancellationToken cancellationToken = default)
    {
        var result = await _service.GetDebtProjectionAsync(GetUserId(), cancellationToken);
        return Ok(ApiResponse<DebtProjectionDto>.Ok(result));
    }

    [HttpGet("savings-goals-eta")]
    public async Task<IActionResult> GetSavingsGoalsEta(CancellationToken cancellationToken = default)
    {
        var result = await _service.GetSavingsGoalEtaAsync(GetUserId(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SavingsGoalEtaDto>>.Ok(result));
    }

    [HttpGet("year-over-year")]
    public async Task<IActionResult> GetYearOverYear(
        [FromQuery] int? year = null, CancellationToken cancellationToken = default)
    {
        var result = await _service.GetYearOverYearAsync(
            GetUserId(), year ?? DateTime.UtcNow.Year, cancellationToken);
        return Ok(ApiResponse<YearOverYearDto>.Ok(result));
    }

    [HttpGet("budget-vs-actual")]
    public async Task<IActionResult> GetBudgetVsActual(
        [FromQuery] int months = 6, CancellationToken cancellationToken = default)
    {
        var result = await _service.GetBudgetVsActualHistoryAsync(GetUserId(), months, cancellationToken);
        return Ok(ApiResponse<BudgetVsActualHistoryDto>.Ok(result));
    }
}
