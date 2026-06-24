# RentaSegura 

Plataforma web de rentas cortas desarrollada como prueba técnica Riwi — ASP.NET C#.  
Conecta **huéspedes** (exploran y reservan) con **anfitriones** (publican inmuebles y miden su rentabilidad), resolviendo dos brechas del mercado: fricción y fraude en la reserva, y gestión "a ciegas" del propietario.

---

## Requisitos previos

**Opción A — Docker (recomendada)**
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado y corriendo (ícono de ballena en verde).
- No se necesita instalar .NET ni PostgreSQL.

**Opción B — Ejecución local sin Docker**
- [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9.0)
- PostgreSQL 16 corriendo en `localhost:5432`
- Ajustar `ConnectionStrings:Default` en `appsettings.json`

---

## Levantar el proyecto con Docker

Desde la **raíz del proyecto** (donde está `docker-compose.yml`):

```bash
docker compose up --build
```

La primera vez tarda 2-3 minutos mientras descarga las imágenes base de .NET y PostgreSQL.  
Espera hasta ver en los logs:

```
app-1  | Now listening on: http://[::]:8080
app-1  | Application started.
```

| Servicio | URL |
|----------|-----|
| Aplicación | http://localhost:8080 |
| Bandeja de correos (MailHog) | http://localhost:8025 |
| Health check | http://localhost:8080/health |

Para detener:
```bash
docker compose down
```

Para borrar también la base de datos y empezar desde cero:
```bash
docker compose down -v
```

> **Nota:** si al arrancar ves un error de conexión a la base de datos, espera unos segundos. El `DbInitializer` reintenta la conexión automáticamente hasta 10 veces con 3 segundos entre intentos mientras PostgreSQL termina de inicializarse.

---

## Credenciales de prueba

Las siguientes cuentas se crean automáticamente al arrancar junto con 3 inmuebles demo en Medellín y Rionegro:

| Rol | Correo | Contraseña |
|-----|--------|-----------|
| Anfitrión | `anfitrion@rentasegura.local` | `Anfitrion123*` |
| Huésped | `huesped@rentasegura.local` | `Huesped123*` |

También puedes registrar cuentas nuevas desde la aplicación eligiendo el rol.

---

## Estructura del proyecto

```
RentaSegura/
├── docker-compose.yml                    # Orquesta app + PostgreSQL + MailHog
├── RentaSegura.sln
│
├── src/RentaSegura.Web/                  # Aplicación principal (.NET 9 MVC)
│   ├── Domain/                           # Entidades y reglas de negocio puras
│   │   ├── ApplicationUser.cs            # Usuario + roles (Huésped / Anfitrión)
│   │   ├── Property.cs                   # Inmueble (implementa IAuditable)
│   │   ├── Reservation.cs                # Reserva: horarios fijos + CanCancel()
│   │   ├── Favorite.cs                   # Wishlist del usuario
│   │   ├── KycVerification.cs            # Resultado de validación de identidad
│   │   ├── Notification.cs               # Notificación in-app
│   │   ├── IAuditable.cs                 # Contrato de auditoría automática
│   │   └── Enums.cs                      # ReservationStatus, KycStatus
│   │
│   ├── Infrastructure/
│   │   ├── AppDbContext.cs               # DbContext (Identity + dominio)
│   │   ├── DbInitializer.cs              # Esquema + constraint + datos demo
│   │   ├── Interceptors/
│   │   │   └── AuditInterceptor.cs       # Timestamps y usuario en SaveChanges
│   │   ├── HealthChecks/
│   │   │   └── VaultHealthCheck.cs       # Verifica acceso a la bóveda
│   │   └── Security/
│   │       └── DocumentVault.cs          # Cifrado AES-GCM + borrado seguro
│   │
│   ├── Services/                         # Lógica de aplicación
│   │   ├── PropertyService.cs            # Catálogo (filtro ciudad) y CRUD
│   │   ├── ReservationService.cs         # Crear y cancelar reservas
│   │   ├── FavoriteService.cs            # Alternar y listar favoritos
│   │   ├── DashboardService.cs           # KPIs y métricas del anfitrión
│   │   ├── ReportService.cs              # Generación de Excel (.xlsx)
│   │   ├── NotificationService.cs        # In-app + correo + marcar leídas
│   │   ├── EmailSender.cs                # Canal SMTP intercambiable
│   │   ├── IdentityVerification.cs       # KYC con IA (interfaz + stub)
│   │   └── ProfileService.cs             # Nombre y contraseña del usuario
│   │
│   ├── Controllers/
│   │   ├── HomeController.cs             # Catálogo público y detalle
│   │   ├── AccountController.cs          # Registro, login, logout
│   │   ├── ReservationsController.cs     # Crear y cancelar reservas
│   │   ├── FavoritesController.cs        # Wishlist (AJAX)
│   │   ├── KycController.cs              # Subida y procesamiento de documento
│   │   ├── PropertiesController.cs       # CRUD inmuebles (anfitrión)
│   │   ├── DashboardController.cs        # Panel y exportación Excel
│   │   ├── NotificationsController.cs    # Marcar leídas (AJAX)
│   │   └── ProfileController.cs          # Perfil del usuario
│   │
│   ├── Models/ViewModels.cs              # DTOs de presentación
│   ├── Views/                            # Razor Views por controlador
│   │   ├── Home/         Index, Details, Error
│   │   ├── Account/      Login, Register
│   │   ├── Reservations/ Index (con cancelación)
│   │   ├── Favorites/    Index
│   │   ├── Kyc/          Index (con preview de imagen)
│   │   ├── Properties/   Index, Form
│   │   ├── Dashboard/    Index (con Chart.js)
│   │   ├── Profile/      Index
│   │   └── Shared/       _Layout (navbar + campana notificaciones)
│   │
│   ├── Program.cs                        # Composición: DI, pipeline, rate limiting
│   ├── appsettings.json                  # Conexión, SMTP, bóveda de documentos
│   └── wwwroot/
│       ├── css/site.css                  # Diseño moderno con variables CSS
│       └── js/site.js                    # Favoritos AJAX + notificaciones leídas
│
└── tests/RentaSegura.UnitTests/          # Pruebas unitarias xUnit
    └── ReservationTests.cs               # Horarios, precio, cancelación
```

**Stack:** ASP.NET Core 9 MVC · PostgreSQL 16 · Entity Framework Core 9 · ASP.NET Core Identity · ClosedXML · Docker Compose · Chart.js

---

## Arquitectura y cómo se abordaron los problemas técnicos

La organización sigue un **monolito por capas** con responsabilidades bien separadas: `Domain` (entidades y reglas puras, sin dependencias externas), `Infrastructure` (EF, seguridad, health checks), `Services` (lógica de aplicación, orquesta el dominio) y `Controllers + Views` (MVC, solo coordinan el flujo HTTP). Las capas internas no conocen las externas.

### 1. Prevención de double-booking

Validar el solapamiento solo en C# tiene una **condición de carrera**: dos reservas simultáneas pasan la validación al mismo tiempo e insertan ambas. La garantía real se delega a PostgreSQL con un `EXCLUDE` constraint sobre un rango de fechas:

```sql
ALTER TABLE "Reservations"
  ADD CONSTRAINT ck_no_double_booking
  EXCLUDE USING gist (
    "PropertyId" WITH =,
    daterange("CheckInDate", "CheckOutDate", '[)') WITH &&
  )
  WHERE ("Status" <> 'Cancelled');
```

El constraint se crea de forma idempotente al arrancar (en `DbInitializer`). La BD rechaza atómicamente cualquier par de reservas solapadas del mismo inmueble. En la aplicación se hace un pre-chequeo adicional para dar un mensaje claro al usuario, y se captura el `SqlState 23P01` (exclusion_violation) para el caso de carrera real.

### 2. Horarios estándar fijos (14:00 / 12:00)

Son política de la plataforma, no un dato que ingrese el usuario. Se modelan como constantes en la entidad `Reservation`:

```csharp
public static readonly TimeOnly StandardCheckIn  = new(14, 0);
public static readonly TimeOnly StandardCheckOut = new(12, 0);
```

Se exponen como `CheckInDateTime` / `CheckOutDateTime` (propiedades de solo lectura calculadas). Es imposible crear una reserva con horarios distintos porque el valor nunca pasa por un input del usuario.

### 3. Validación de identidad con IA (KYC)

El flujo está detrás de la interfaz `IIdentityVerificationService`:

1. El usuario sube la foto de su cédula.
2. Se cifra y guarda en la bóveda (AES-GCM).
3. La IA extrae nombres, apellidos, número de documento y fecha de nacimiento.
4. Se emite un veredicto (aprobado / rechazado).
5. El documento se **elimina de forma segura** inmediatamente.
6. En la BD solo quedan los datos extraídos, el veredicto y la marca de borrado.

La implementación incluida (`StubIdentityVerificationService`) es determinista y sin claves externas para que la demo funcione sin configuración adicional. En producción se reemplaza por Azure AI Document Intelligence, AWS Textract o Google Document AI cambiando **una sola línea** en `Program.cs`, sin tocar el flujo ni el controlador.

### 4. Privacidad: cifrado AES-GCM + borrado seguro

El documento de identidad nunca se almacena en claro ni persiste en la base de datos. `AesDocumentVault` usa AES-GCM (cifrado autenticado con nonce aleatorio por archivo). El borrado seguro sobrescribe el archivo con datos aleatorios antes de eliminarlo, evitando recuperación forense. La postura de privacidad por diseño es no conservar el documento más tiempo del estrictamente necesario.

### 5. Autenticación diferida

El catálogo y el detalle son `[AllowAnonymous]`. El login se solicita **únicamente** al reservar o guardar un favorito permanente, con `returnUrl` para no perder el contexto. Reduce la fricción inicial y la tasa de rebote, manteniendo al usuario explorando más tiempo antes de comprometerse a crear una cuenta.

### 6. Notificaciones omnicanal

`NotificationService` despacha en cada evento clave (reserva confirmada, veredicto KYC, cancelación) una notificación in-app persistente visible en la campana del navbar con badge de no leídas, y un correo SMTP. Al abrir la campana, las notificaciones se marcan como leídas automáticamente vía AJAX sin recargar la página. El canal de correo es intercambiable (`IEmailSender`); en desarrollo apunta a MailHog.

### 7. Cancelación con política en el dominio

La regla de las 48 horas de antelación mínima vive en `Reservation.CanCancel()`, no en el controlador:

```csharp
public (bool Allowed, string? Reason) CanCancel()
{
    if (Status == ReservationStatus.Cancelled)  return (false, "La reserva ya está cancelada.");
    if (Status == ReservationStatus.Completed)  return (false, "No se puede cancelar una reserva completada.");
    var horas = (CheckInDateTime - DateTime.UtcNow).TotalHours;
    if (horas < 48) return (false, "Solo se puede cancelar con al menos 48 horas de antelación.");
    return (true, null);
}
```

Esto garantiza que la política no pueda ser evadida por ninguna vía de entrada: otro controlador, un job, una API futura. La regla de negocio pertenece al dominio.

### 8. Auditoría automática con interceptor de EF

`AuditInterceptor` implementa `SaveChangesInterceptor` y rellena `CreatedAtUtc`, `UpdatedAtUtc` y `LastModifiedBy` automáticamente en cada entidad que implemente `IAuditable`. La auditoría es transversal: actúa a nivel de `SaveChanges` y es imposible olvidarla en una operación nueva porque ningún servicio necesita recordarla.

### 9. Rate limiting y seguridad

- **Rate limiting** (`AddRateLimiter`): 5 requests/minuto por IP en login, registro y subida de documentos KYC. 20 requests/minuto en endpoints AJAX (favoritos, notificaciones). Si se supera el límite la app responde 429 con mensaje en español.
- **Bloqueo de cuenta**: 5 intentos fallidos consecutivos bloquean la cuenta 5 minutos (`lockoutOnFailure: true` + `options.Lockout`). Protege contra fuerza bruta sin necesidad de CAPTCHA.
- **Headers HTTP de seguridad**: `X-Frame-Options: SAMEORIGIN`, `X-Content-Type-Options: nosniff`, `Referrer-Policy` y `Permissions-Policy` aplicados globalmente como middleware antes de cualquier controlador.
- **Errores de Identity en español**: los mensajes de error de registro (correo duplicado, contraseña débil, etc.) se traducen y deduiplican para que el usuario nunca vea mensajes en inglés ni mensajes repetidos.

### 10. Health checks

Expuestos en `/health` como JSON con el estado de dos componentes: la base de datos PostgreSQL y la bóveda de documentos. Cualquier orquestador (Kubernetes, Docker Swarm, AWS ECS) puede consultar este endpoint para verificar si el servicio está sano antes de enrutar tráfico o después de un despliegue.

### 11. Dashboard y reportes Excel

`DashboardService` calcula sobre un **rango de fechas seleccionable**: ingresos totales, número de reservas, noches reservadas, tasa de ocupación (noches reservadas / noches disponibles del portafolio) y serie mensual para la gráfica (Chart.js). El reporte Excel generado con **ClosedXML** (licencia MIT) incluye: inmueble, ciudad, fechas, noches, precio por noche, total pagado, huésped y correo. Tiene filas alternas para legibilidad y fila de totales con suma de noches e ingresos. Se puede exportar para todo el portafolio o filtrado por un inmueble específico.

---

## Flujo de prueba completo

### Como huésped

1. Abre http://localhost:8080 — explora el catálogo sin cuenta.
2. Filtra por ciudad (ej. *Medellín*); pulsa la **X** para borrar el filtro y ver todos.
3. Intenta reservar o dar favorito → la app pide login solo en ese momento (**autenticación diferida**).
4. Inicia sesión con `huesped@rentasegura.local` / `Huesped123*` o crea una cuenta nueva.
5. Ve a **Mi identidad** → sube cualquier foto (mínimo 2 KB, puedes usar cualquier imagen desde el móvil). Verás una vista previa antes de enviarla.
6. El sistema verifica la identidad, aprueba el documento y muestra la fecha en que fue eliminado de forma segura.
7. Entra al detalle de un inmueble → elige fechas de llegada y salida → el **precio total aparece dinámicamente** sin recargar.
8. Pulsa **Confirmar reserva** → llega una notificación en la campana (badge rojo) y un correo visible en http://localhost:8025.
9. Intenta reservar el **mismo inmueble en fechas solapadas** → el sistema lo bloquea (double-booking).
10. Ve a **Mis reservas** → cada reserva muestra la imagen del inmueble, fechas en formato largo en español y horarios 2:00 PM / 12:00 PM.
11. Pulsa **Ver inmueble** para volver al detalle, o **Cancelar** para cancelar (requiere 48 h de antelación; si no aplica, muestra el motivo).
12. Abre la campana del navbar → las notificaciones se marcan como leídas automáticamente y el badge desaparece.
13. Ve a **Mi perfil** → edita el nombre o cambia la contraseña.

### Como anfitrión

1. Inicia sesión con `anfitrion@rentasegura.local` / `Anfitrion123*`.
2. **Mis inmuebles** → edita uno de los inmuebles demo o crea uno nuevo con título, descripción, ciudad, dirección, tarifa, habitaciones, capacidad e imagen.
3. **Dashboard** → cambia el rango de fechas con los campos *Desde* / *Hasta* y pulsa *Aplicar*. Revisa los 4 KPIs y la gráfica de barras de ingresos por mes.
4. Pulsa **Exportar portafolio (Excel)** → descarga el `.xlsx` con todas las reservas.
5. En la tabla de desglose por inmueble, pulsa el botón Excel de una fila específica → reporte filtrado solo para ese inmueble.

### Seguridad

- Intenta acceder a http://localhost:8080/Owner/Dashboard sin sesión → redirige al login.
- Intenta acceder al dashboard de otro anfitrión → deniega el acceso.
- Ingresa la contraseña incorrecta 6 veces → la cuenta se bloquea con mensaje en español.
- Abre http://localhost:8080/health → JSON con estado de PostgreSQL y bóveda.

---

## Decisiones técnicas clave

| Decisión | Alternativa descartada | Razón |
|----------|----------------------|-------|
| `EXCLUDE` constraint en BD para double-booking | Solo validar en C# | La validación en app tiene condición de carrera bajo concurrencia real |
| `CanCancel()` en el dominio | Validar en el controlador | La política puede ser evadida por otras vías de entrada si está en el controlador |
| `AuditInterceptor` de EF | Timestamps en cada servicio | Imposible olvidarlo en operaciones nuevas; no contamina los servicios |
| `EnsureCreated` en lugar de migraciones | `dotnet ef migrations` | Arranque de un comando sin pasos manuales para el MVP |
| ClosedXML para Excel | EPPlus | EPPlus v5+ requiere licencia comercial; ClosedXML es MIT |
| AES-GCM para cifrado de documentos | Sin cifrado o AES-CBC | AES-GCM es cifrado autenticado; cualquier modificación del archivo se detecta |
| Stub de KYC detrás de interfaz | Implementación directa | Cambiar de proveedor de IA en producción no toca el flujo |
| ASP.NET Core Identity + cookies | JWT | El flujo MVC server-rendered funciona naturalmente con cookies; no hay cliente SPA |
| Rate limiting a nivel de middleware | Validar en controladores | El middleware corta el request antes de llegar a la BD, más eficiente |
