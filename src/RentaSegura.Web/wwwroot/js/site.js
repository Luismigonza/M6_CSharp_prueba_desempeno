// ============================================================================
//  site.js  —  Interacciones del cliente.
//
//  1. Toggle de favoritos (AJAX, sin recargar).
//  2. Marcar notificaciones como leídas al abrir la campana.
// ============================================================================

(function () {

    // ── Antiforgery token ──────────────────────────────────────────────────
    function getToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
    }

    async function post(url, body) {
        return fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': getToken()
            },
            body: body ? new URLSearchParams(body).toString() : ''
        });
    }

    // ── Toggle de favoritos ────────────────────────────────────────────────
    document.addEventListener('click', async function (e) {
        const btn = e.target.closest('.btn-fav');
        if (!btn) return;
        e.preventDefault();

        const id   = btn.dataset.id;
        const icon = btn.querySelector('i');

        const res = await post('/Favorites/Toggle', { propertyId: id });
        if (res.status === 401) { window.location.href = '/Account/Login'; return; }
        if (!res.ok) return;

        const data = await res.json();
        icon.className = data.isFavorite
            ? 'bi bi-heart-fill text-danger'
            : 'bi bi-heart';
    });

    // ── Marcar todas las notificaciones como leídas ────────────────────────
    // Se activa al hacer clic en "Marcar todas leídas" en el dropdown.
    document.getElementById('markAllRead')?.addEventListener('click', async function () {
        const res = await post('/Notifications/MarkAllRead');
        if (!res.ok) return;

        // Ocultar el badge y el botón sin recargar.
        document.getElementById('notifBadge')?.remove();
        this.remove();

        // Quitar la clase visual de "nuevo" de todos los items.
        document.querySelectorAll('.badge.bg-primary').forEach(b => b.remove());
    });

    // ── Marcar como leídas al abrir el dropdown de la campana ─────────────
    // Experiencia: al ver las notificaciones, ya se marcan como leídas.
    document.getElementById('bellBtn')?.addEventListener('click', async function () {
        const badge = document.getElementById('notifBadge');
        if (!badge) return;  // ya todo está leído

        await post('/Notifications/MarkAllRead');
        badge.remove();
        document.getElementById('markAllRead')?.remove();
        document.querySelectorAll('.badge.bg-primary').forEach(b => b.remove());
    });

})();