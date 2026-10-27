# VetPlatform — MVP (.NET 8)

Monolito modular con Clean Architecture para la plataforma veterinaria: Identidad y Auditoría, Expediente Clínico, Clínicas y Regencia, Emergencias, Marketplace y Agenda.

## Estructura
```
src/VetPlatform.Domain          Entidades, enums, excepciones, interfaces de repositorio
src/VetPlatform.Application     Commands, DTOs, Result<T>, interfaces de servicio, validadores (FluentValidation)
src/VetPlatform.Infrastructure  EF Core (PostgreSQL), repositorios, servicios, JWT/BCrypt, políticas, migraciones
src/VetPlatform.API             Controllers, middleware (ProblemDetails, auditoría de 401/403), Program.cs
tests/VetPlatform.Application.Tests  xUnit + FluentAssertions sobre servicios reales con SQLite en memoria
```

## Prerrequisitos
- .NET SDK 8.0
- PostgreSQL 14+
- Herramienta EF Core: `dotnet tool install --global dotnet-ef --version 8.0.8`

## Configuración (variables clave)
`src/VetPlatform.API/appsettings.json` o variables de entorno (`__` como separador):

| Clave | Variable de entorno | Descripción |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | Cadena de conexión PostgreSQL |
| `Jwt:Key` | `Jwt__Key` | Secreto HMAC (mín. 32 caracteres). **Cámbielo en producción** |
| `Jwt:Issuer` / `Jwt:Audience` | `Jwt__Issuer` / `Jwt__Audience` | Emisor/audiencia del token |
| `Jwt:ExpiryMinutes` | `Jwt__ExpiryMinutes` | Vigencia del token |
| `EmergencyAccess:DefaultExpiryMinutes` | `EmergencyAccess__DefaultExpiryMinutes` | Duración del acceso urgente (por defecto 120) |
| `Database:ApplyMigrationsOnStartup` | `Database__ApplyMigrationsOnStartup` | Aplica migraciones al iniciar (false por defecto) |

## Migraciones
La migración inicial `InitialCreate` ya está incluida en `src/VetPlatform.Infrastructure/Data/Migrations`.
```bash
dotnet ef database update -p src/VetPlatform.Infrastructure -s src/VetPlatform.API
# nueva migración tras cambios de modelo:
dotnet ef migrations add <Nombre> -p src/VetPlatform.Infrastructure -s src/VetPlatform.API -o Data/Migrations
```

## Ejecutar
```bash
dotnet restore
dotnet run --project src/VetPlatform.API
```
Swagger (entorno Development): `https://localhost:<puerto>/swagger`. Health check: `GET /health`.

Primer administrador: los roles `PlatformAdmin`/`ClinicAdmin` no pueden auto-registrarse. Registre un usuario y asígnele el rol directamente en BD:
```sql
INSERT INTO "UserRoleAssignments" ("Id","UserId","Role","OrganizationId","GrantedAt","RevokedAt","IsDeleted")
VALUES (gen_random_uuid(), '<userId>', 'PlatformAdmin', NULL, now(), NULL, false);
```
Los profesionales (veterinarios, tiendas) quedan sin verificar hasta `POST /api/auth/verify/{userId}` (admin). El acceso urgente exige `EmergencyVet` verificado.

## Pruebas
```bash
dotnet test
```
Cubren: consulta y adenda (inmutabilidad), denegación auditada, regente único, recetas; acceso urgente, expiración, atención y notificaciones (envío desacoplado); reserva atómica de stock, sobreventa concurrente, token de versión, idempotencia, liberación/consumo de reservas; reserva de citas y conflicto de disponibilidad.

## Decisiones de diseño
- **Denegación por defecto**: `FallbackPolicy` exige autenticación; políticas por rol y verificación de recurso (dueño, regente activo, acceso urgente vigente) en los servicios vía `ClinicalAccessGuard`.
- **Inmutabilidad clínica**: `Consultation`, `Prescription`, adendas, `EmergencyAttendance` y `AuditLog` son `IImmutableRecord`; `AppDbContext` rechaza modificaciones. Las correcciones son adendas.
- **Regente único**: índice único filtrado (`IsActive = TRUE`) + revocación transaccional del anterior.
- **Acceso urgente**: temporal, justificado, re-validado (`IsValidAt`) en cada operación; al expirar se persiste `Expired`.
- **Stock**: `Product.Version` como token de concurrencia optimista + transacción explícita; todo-o-nada por pedido, reintentos ante conflicto. `StockOnHand`/`ReservedStock` separan stock físico y reservado.
- **Idempotencia**: índice único (`BuyerUserId`, `IdempotencyKey`); cabecera `Idempotency-Key` o campo en el body.
- **Agenda**: disponibilidad validada al confirmar (transacción + `Version` en el slot + índice único filtrado por cita activa).
- **Notificaciones**: se guardan como `Pending` en el mismo `SaveChanges` del acto; `NotificationDispatcherHostedService` las envía y reintenta (`RetryPendingNotificationsAsync`, máx. 5 intentos). `INotificationSender` por defecto solo registra en log.
- **Auditoría**: solo metadatos (actor, acción, recurso, resultado, organización); sin contenido clínico. `AuditMiddleware` registra 401/403 automáticamente.
- **QR opaco**: `Pet.QrCode` es un GUID aleatorio independiente del Id interno.
