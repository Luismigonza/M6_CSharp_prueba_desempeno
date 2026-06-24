using Microsoft.EntityFrameworkCore;
using RentaSegura.Web.Domain;
using RentaSegura.Web.Infrastructure;

namespace RentaSegura.Web.Services;

public sealed class NotificationService
{
    private readonly AppDbContext _db;
    private readonly IEmailSender _email;

    public NotificationService(AppDbContext db, IEmailSender email)
    {
        _db   = db;
        _email = email;
    }

    /// <summary>Crea la notificación in-app y envía el correo al destinatario.</summary>
    public async Task NotifyAsync(string userId, string email, string title, string body,
        CancellationToken ct = default)
    {
        _db.Notifications.Add(new Notification
        {
            RecipientUserId = userId, Title = title, Body = body
        });
        await _db.SaveChangesAsync(ct);
        await _email.SendAsync(email, title, $"<p>{body}</p><p>- Equipo RentaSegura</p>", ct);
    }

    /// <summary>Marca todas las notificaciones de un usuario como leídas.</summary>
    public async Task MarkAllReadAsync(string userId, CancellationToken ct = default)
    {
        await _db.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }

    /// <summary>Marca una notificación específica como leída.</summary>
    public async Task MarkReadAsync(string userId, Guid notificationId, CancellationToken ct = default)
    {
        await _db.Notifications
            .Where(n => n.Id == notificationId && n.RecipientUserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }
}