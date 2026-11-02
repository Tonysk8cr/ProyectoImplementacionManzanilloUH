# PBI-01: CRUD visual de propietarios (sin base de datos)

Aplicación web ASP.NET Core MVC (.NET 8) independiente. Guarda los datos en memoria:
al apagar la aplicación se pierden. No modifica ningún archivo del resto del proyecto.

## Ejecutar

```bash
dotnet run --project PBI01-Propietarios --launch-profile https
```

Abrir https://localhost:7100 (o http://localhost:5100).

## Estructura

- `Models/Propietario.cs`: datos y validaciones de campos obligatorios.
- `Services/`: `IPropietarioRepository` y su versión en memoria (se cambia por la BD más adelante).
- `Controllers/PropietariosController.cs`: crear, listar, ver, editar y eliminar.
- `Views/Propietarios/`: pantallas del CRUD.
