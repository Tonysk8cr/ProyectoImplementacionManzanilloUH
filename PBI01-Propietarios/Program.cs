using PBI01.Propietarios.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC con vistas Razor (igual que el resto del proyecto).
builder.Services.AddControllersWithViews();

// Sin base de datos: los propietarios viven en memoria mientras la app está encendida.
// Cuando exista la BD, solo se reemplaza esta clase por una que use el backend real.
builder.Services.AddSingleton<IPropietarioRepository, PropietarioRepositoryEnMemoria>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Propietarios}/{action=Index}/{id?}");

app.Run();
