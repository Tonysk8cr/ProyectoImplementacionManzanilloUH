using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetPlatform.API.Middleware;
using VetPlatform.Application;
using VetPlatform.Infrastructure;
using VetPlatform.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// MVC: controladores con vistas Razor (Model-View-Controller).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration); // DbContext, repositorios, servicios, autenticación por cookie, políticas
builder.Services.AddControllersWithViews(options =>
    // Protege todas las peticiones POST/PUT/DELETE contra falsificación de solicitudes.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

var app = builder.Build();

// Manejo global de excepciones (va primero para "envolver" el resto del pipeline):
// /api → ProblemDetails JSON (ExceptionHandlingMiddleware); vistas MVC → página de error HTML.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Crea/actualiza el esquema de la base de datos al arrancar (configurable por appsettings).
if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<AuditMiddleware>(); // Audita respuestas 401/403 (solo ruta, nunca cuerpos).
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Health check para Somee / balanceadores: incluye si la app llega a la base de datos (sin exponer detalles).
app.MapGet("/health", async (AppDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "ok", database = "ok" })
        : Results.Json(new { status = "degraded", database = "unreachable" }, statusCode: StatusCodes.Status503ServiceUnavailable))
    .AllowAnonymous();

app.Run();

public partial class Program { }
