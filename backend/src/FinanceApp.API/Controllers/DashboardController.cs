using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.DTOs.Dashboard;
using FinanceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value!);

    /// <summary>
    /// Resumen principal: ingresos, gastos, ahorro y patrimonio del mes.
    /// </summary>
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(
        [FromQuery] int? month,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        // Si no se envían mes y año, usamos el mes actual
        var targetMonth = month ?? DateTime.Today.Month;
        var targetYear = year ?? DateTime.Today.Year;

        var result = await _dashboardService.GetOverviewAsync(
            GetUserId(), targetMonth, targetYear, cancellationToken);
        return Ok(ApiResponse<DashboardOverviewDto>.Ok(result));
    }

    /// <summary>
    /// Evolución de los últimos N meses para el gráfico de líneas.
    /// </summary>
    [HttpGet("monthly-trend")]
    public async Task<IActionResult> GetMonthlyTrend(
        [FromQuery] int months = 12,
        CancellationToken cancellationToken = default)
    {
        // Limitamos entre 3 y 24 meses
        months = Math.Clamp(months, 3, 24);

        var result = await _dashboardService.GetMonthlyTrendAsync(
            GetUserId(), months, cancellationToken);
        return Ok(ApiResponse<MonthlyTrendDto>.Ok(result));
    }

    /// <summary>
    /// Gastos agrupados por categoría para el gráfico de torta.
    /// </summary>
    [HttpGet("expenses-by-category")]
    public async Task<IActionResult> GetExpensesByCategory(
        [FromQuery] int? month,
        [FromQuery] int? year,
        CancellationToken cancellationToken = default)
    {
        var targetMonth = month ?? DateTime.Today.Month;
        var targetYear = year ?? DateTime.Today.Year;

        var result = await _dashboardService.GetExpensesByCategoryAsync(
            GetUserId(), targetMonth, targetYear, cancellationToken);
        return Ok(ApiResponse<ExpenseByCategoryChartDto>.Ok(result));
    }

    /// <summary>
    /// v2.0.1 sección 5 — separa flujo de caja (consumo real) de
    /// construcción de patrimonio (ahorro/inversión/pago de deuda).
    /// </summary>
    [HttpGet("cashflow-statement")]
    public async Task<IActionResult> GetCashFlowStatement(
        [FromQuery] int? month,
        [FromQuery] int? year,
        CancellationToken cancellationToken = default)
    {
        var targetMonth = month ?? DateTime.Today.Month;
        var targetYear = year ?? DateTime.Today.Year;

        var result = await _dashboardService.GetCashFlowStatementAsync(
            GetUserId(), targetMonth, targetYear, cancellationToken);
        return Ok(ApiResponse<CashFlowStatementDto>.Ok(result));
    }
}
