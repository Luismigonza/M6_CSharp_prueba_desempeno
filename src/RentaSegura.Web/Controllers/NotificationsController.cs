using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentaSegura.Web.Services;

namespace RentaSegura.Web.Controllers;

[Authorize]
public sealed class NotificationsController : Controller
{
    private readonly NotificationService _notifications;
    public NotificationsController(NotificationService notifications) => _notifications = notifications;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // POST /Notifications/MarkAllRead
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notifications.MarkAllReadAsync(UserId);
        return Json(new { ok = true });
    }

    // POST /Notifications/MarkRead/{id}
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        await _notifications.MarkReadAsync(UserId, id);
        return Json(new { ok = true });
    }
}