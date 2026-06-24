using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentaSegura.Web.Domain;
using RentaSegura.Web.Services;

namespace RentaSegura.Web.Controllers;

[Authorize(Roles = Roles.Anfitrion)]
[Route("Owner/Dashboard")]
public sealed class DashboardController : Controller
{
    private readonly DashboardService _dashboard;
    private readonly ReportService _reports;

    public DashboardController(DashboardService dashboard, ReportService reports)
    {
        _dashboard = dashboard;
        _reports = reports;
    }

    private string OwnerId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    
    [HttpGet("")]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to)
    {
        // Por defecto: últimos 6 meses.
        var toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = from ?? toDate.AddMonths(-6);

        ViewBag.From = fromDate;
        ViewBag.To = toDate;

        var metrics = await _dashboard.GetMetricsAsync(OwnerId, fromDate, toDate);
        return View(metrics);
    }

    // Encargadp de descargar el exel en .xlsx
    [HttpGet("Export")]
    public async Task<IActionResult> Export(Guid? propertyId)
    {
        var bytes = await _reports.GenerateOwnerReportAsync(OwnerId, propertyId);
        var fileName = $"reporte-rentasegura-{DateTime.UtcNow:yyyyMMdd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
