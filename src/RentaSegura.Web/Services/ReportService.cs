using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using RentaSegura.Web.Domain;
using RentaSegura.Web.Infrastructure;

namespace RentaSegura.Web.Services;

public sealed class ReportService
{
    private readonly AppDbContext _db;
    public ReportService(AppDbContext db) => _db = db;

    public async Task<byte[]> GenerateOwnerReportAsync(
        string ownerId, Guid? propertyId, CancellationToken ct = default)
    {
        var propertyIds = await _db.Properties.AsNoTracking()
            .Where(p => p.OwnerId == ownerId && (propertyId == null || p.Id == propertyId))
            .Select(p => p.Id)
            .ToListAsync(ct);

        var rows = await (
            from r in _db.Reservations.AsNoTracking()
            join p in _db.Properties.AsNoTracking() on r.PropertyId equals p.Id
            join u in _db.Users.AsNoTracking() on r.GuestUserId equals u.Id
            where propertyIds.Contains(r.PropertyId) && r.Status != ReservationStatus.Cancelled
            orderby r.CheckInDate
            select new
            {
                p.Title, p.City, p.PricePerNight,
                r.CheckInDate, r.CheckOutDate, r.Nights, r.PricePaid,
                GuestName  = u.FullName,
                GuestEmail = u.Email
            }).ToListAsync(ct);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Reservas");

        // Encabezados
        string[] headers =
        {
            "Inmueble", "Ciudad", "Llegada", "Salida",
            "Noches", "Precio / noche", "Total pagado",
            "Huésped", "Correo"
        };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var headerRange = ws.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1f2937");
        headerRange.Style.Font.FontColor       = XLColor.White;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Datos
        var row = 2;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.Title;
            ws.Cell(row, 2).Value = r.City;
            ws.Cell(row, 3).Value = r.CheckInDate.ToString("yyyy-MM-dd");
            ws.Cell(row, 4).Value = r.CheckOutDate.ToString("yyyy-MM-dd");
            ws.Cell(row, 5).Value = r.Nights;

            ws.Cell(row, 6).Value = r.PricePerNight;       // precio unitario por noche
            ws.Cell(row, 6).Style.NumberFormat.Format = "$ #,##0";

            ws.Cell(row, 7).Value = r.PricePaid;           // noches × precio = total
            ws.Cell(row, 7).Style.NumberFormat.Format = "$ #,##0";

            ws.Cell(row, 8).Value = r.GuestName  ?? string.Empty;
            ws.Cell(row, 9).Value = r.GuestEmail ?? string.Empty;

            // Alternar color de fila para legibilidad
            if (row % 2 == 0)
                ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#f9fafb");

            row++;
        }

        // Fila de totales
        if (rows.Count > 0)
        {
            var totalRow = ws.Range(row, 1, row, headers.Length);
            totalRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#fef3c7");
            totalRow.Style.Font.Bold = true;

            ws.Cell(row, 4).Value = "TOTAL";
            ws.Cell(row, 5).FormulaA1 = $"SUM(E2:E{row - 1})";            // total noches
            ws.Cell(row, 7).FormulaA1 = $"SUM(G2:G{row - 1})";            // total ingresos
            ws.Cell(row, 7).Style.NumberFormat.Format = "$ #,##0";
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}