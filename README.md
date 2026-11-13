# Plataforma Centralizada de Servicios Veterinarios (VetPlatform)

**Universidad Hispanoamericana · Escuela de Ingeniería Informática · INFO-117: Desarrollo de Aplicaciones**
Profesor: Ing. Edgar Alberto Ugalde Saborio · Estado: 🚧 en desarrollo académico

Esta guía permite clonar, configurar, ejecutar y probar el proyecto desde cero. Si es tu primera vez, síguela en orden.

## Contenido
1. [Introducción](#1-introducción)
2. [Requisitos previos](#2-requisitos-previos)
3. [Obtener el proyecto](#3-obtener-el-proyecto)
4. [Configurar la conexión con Somee](#4-configurar-la-conexión-con-somee)
5. [Inicialización de la base de datos](#5-inicialización-de-la-base-de-datos)
6. [Ejecutar el proyecto](#6-ejecutar-el-proyecto)
7. [Verificar funcionalidades (CRUD)](#7-verificar-funcionalidades-crud)
8. [Ejecutar las pruebas automatizadas](#8-ejecutar-las-pruebas-automatizadas)
9. [Solución de problemas](#9-solución-de-problemas)
10. [Referencia técnica](#10-referencia-técnica)

---

## 1. Introducción

### Descripción
La plataforma mejora la gestión de la información del cuidado de mascotas mediante un **expediente clínico digital unificado**. Centraliza los registros médicos, facilita la continuidad del historial entre centros veterinarios y permite un acceso rápido y controlado en emergencias.

### Funcionalidades disponibles en la web
| Funcionalidad | Quién la usa | Dónde |
|---|---|---|
| Registro e inicio de sesión | Propietario | `/Account/Register`, `/Account/Login` |
| Perfil de propietario (crear, ver, editar, eliminar) | Propietario | Menú **Mi perfil** (`/OwnerProfile`) |
| Registrar mascotas y ver su ficha e historial | Propietario | Menú **Mis mascotas** (`/Pets`) |
| Lista de pacientes, historial cronológico y detalle de cada consulta | Veterinario regente | Menú **Mis pacientes** (`/Pets/Patients`) |
| Estado de la aplicación y de la base de datos | Cualquiera | `/health` |

El backend también expone endpoints JSON (`/api/...`) para clínicas, consultas, recetas, emergencias, marketplace y agenda.

### Tecnologías
- **.NET 8** · ASP.NET Core MVC (vistas Razor, autenticación por cookie)
- **Entity Framework Core 8** con **SQL Server**: LocalDB en local y **Somee** (MS SQL 2025 Express) como base compartida
- FluentValidation, BCrypt (contraseñas), JWT
- Pruebas: xUnit, FluentAssertions, `WebApplicationFactory`, SQLite en memoria

### Arquitectura (monolito modular, Clean Architecture)
```
src/VetPlatform.Domain           Entidades, enums, excepciones, interfaces de repositorio
src/VetPlatform.Application      Commands, DTOs, Result<T>, interfaces de servicio, validadores
src/VetPlatform.Infrastructure   EF Core (AppDbContext, migraciones), repositorios, servicios, seguridad
src/VetPlatform.API              Proyecto que se ejecuta: controladores MVC/API, vistas, Program.cs
tests/VetPlatform.Application.Tests   Pruebas de servicios (SQLite en memoria u opcionalmente SQL Server)
tests/VetPlatform.Web.Tests           Pruebas de integración web (pipeline MVC completo)
database/                        Script del esquema, datos de prueba y guía de administración de Somee
docs/                            Diagrama de la base de datos
```

### Integrantes
- Miguel Ángel Arias Duran · [winflip27](https://github.com/winflip27)
- Alois Johann Buschor Obando · [ajbuschor](https://github.com/ajbuschor)
- Dilan Alexis Durán Díaz · [ddurandiaz3d](https://github.com/ddurandiaz3d)
- Yudi Mariel Fallas Calderón · [yfallas02](https://github.com/yfallas02)
- Santiago Fonseca Chinchilla · [Siggy1604](https://github.com/Siggy1604)
- Mavio Sebastián Ramírez Segnini · [Mavio2006](https://github.com/Mavio2006)
- Daniel Sánchez Azofeifa · [danisanchez23](https://github.com/danisanchez23)
- Sebastián Vega Castillo · [Sebas10CR](https://github.com/Sebas10CR)
- Anthony Emanuel Villalobos Hidalgo · [Tonysk8cr](https://github.com/Tonysk8cr)

---

## 2. Requisitos previos

| Herramienta | Versión | Cómo comprobarla |
|---|---|---|
| .NET SDK | **8.0** (puede convivir con SDKs más nuevos) | `dotnet --list-sdks` → debe aparecer `8.0.x` |
| Visual Studio 2022 o posterior (recomendado), con la carga de trabajo **ASP.NET y desarrollo web** | — | Incluye SQL Server **LocalDB** |
| SQL Server LocalDB | Incluido con Visual Studio | `sqllocaldb info` → `MSSQLLocalDB` |
| Git | Cualquiera reciente | `git --version` |
| Herramienta EF Core | **8.0.8** | `dotnet ef --version` |

Instalar la herramienta EF Core (una sola vez):
```powershell
dotnet tool install --global dotnet-ef --version 8.0.8
```

**Para trabajar con Somee necesitas además:**
- El archivo `.txt` con las credenciales y el connection string que Anthony comparte por el **grupo privado de WhatsApp** (ver sección 4).
- Conexión a internet. El servidor es `<base>.mssql.somee.com`, puerto 1433 por defecto.
- Opcional, para revisar los datos: acceso al panel web de Somee (somee.com) o SSMS / Azure Data Studio.

---

## 3. Obtener el proyecto

```powershell
git clone https://github.com/Tonysk8cr/ProyectoImplementacionManzanilloUH.git
cd ProyectoImplementacionManzanilloUH
dotnet restore
dotnet build
```
`dotnet build` debe terminar en **`0 Error(s)`**.

**En Visual Studio:** *File → Open → Project/Solution* → `VetPlatform.sln`. El proyecto que se ejecuta es **`VetPlatform.API`**: márcalo como *Startup Project* si no lo está.

> Todos los comandos de esta guía se ejecutan desde la **carpeta raíz** del repositorio (la que contiene `VetPlatform.sln`).

---

## 4. Configurar la conexión con Somee

### 4.1 Cómo lee la conexión la aplicación
La aplicación lee una sola clave: **`ConnectionStrings:DefaultConnection`** (en `src/VetPlatform.Infrastructure/DependencyInjection.cs`). Si está vacía, la app **no arranca** y el error dice qué falta. El valor se toma de esta lista; cada fuente reemplaza a las anteriores:

| Orden | Fuente | ¿En git? | Contenido |
|---|---|---|---|
| 1 | `src/VetPlatform.API/appsettings.json` | Sí | Vacío a propósito |
| 2 | `src/VetPlatform.API/appsettings.Development.json` | Sí | LocalDB + clave JWT solo para desarrollo |
| 3 | **User Secrets** (solo en Development) | **No**: vive en tu perfil de Windows | **Aquí va el connection string de Somee** |
| 4 | Variable de entorno `ConnectionStrings__DefaultConnection` | No | Alternativa puntual |

Los perfiles **`https`** y **`http`** de Visual Studio (y `dotnet run`) usan el entorno **Development**. Por eso, con User Secrets configurados, la app se conecta a Somee; sin ellos, usa tu LocalDB.

### 4.2 Paso a paso con el archivo de WhatsApp
1. **Descarga** el archivo `.txt` que Anthony envía por el grupo privado de WhatsApp.
2. **Guárdalo fuera del repositorio**, por ejemplo en `Documentos\VetPlatform\somee-credenciales.txt`. No lo pongas dentro de la carpeta del proyecto. Si por error queda dentro, `.gitignore` lo protege si se llama `*credenciales*.txt` o `somee*.txt`.
3. **Ábrelo** y ubica la línea del **connection string** (empieza con `workstation id=` o `data source=`). El archivo también trae el usuario y la contraseña del *login* SQL y, si se incluyó, la contraseña de las cuentas de prueba.
4. **Configura el valor en User Secrets.** Usa una de estas dos formas:
   - **Terminal** (desde la raíz del repo; usa comillas simples para que PowerShell no interprete el `$` de la contraseña):
     ```powershell
     dotnet user-secrets set "ConnectionStrings:DefaultConnection" '<pega aquí el connection string del .txt>' --project src/VetPlatform.API
     dotnet user-secrets list --project src/VetPlatform.API
     ```
     `list` debe mostrar la clave `ConnectionStrings:DefaultConnection`.
   - **Visual Studio:** clic derecho en `VetPlatform.API` → **Manage User Secrets**. Se abre `secrets.json`; pega esto con el valor del `.txt` y guarda:
     ```json
     {
       "ConnectionStrings:DefaultConnection": "<connection string del .txt>"
     }
     ```
   Los secretos se guardan en `%APPDATA%\Microsoft\UserSecrets\0275680b-6b12-47c3-b5a9-d97af5b6885c\secrets.json`, fuera del repositorio.
5. **Verifica la conexión:** ejecuta la app (sección 6) y abre `/health`. Debe responder `{"status":"ok","database":"ok"}`.

Para volver a usar LocalDB: `dotnet user-secrets clear --project src/VetPlatform.API`.

### 4.3 Reglas de seguridad
- El `.txt` es **solo para configurar tu entorno local**. No lo subas a git ni lo reenvíes fuera del grupo privado.
- **Nunca** pegues el connection string en `appsettings.json`, `appsettings.Development.json` ni en ningún archivo versionado.
- No muestres el `.txt` ni `secrets.json` en capturas, videos ni issues.
- La base de Somee es **compartida por todo el equipo**: lo que hagas queda visible para los demás.

---

## 5. Inicialización de la base de datos

### Somee (base compartida)
La base de Somee **ya la inicializa el administrador** (Anthony) con `database/somee-schema.sql`. **No necesitas ejecutar migraciones para empezar.** Para comprobar el estado:
- **Rápido:** `/health` → `"database":"ok"` confirma la conexión.
- **Esquema al día** (con tus User Secrets configurados):
  ```powershell
  dotnet ef migrations list -p src/VetPlatform.Infrastructure -s src/VetPlatform.API
  ```
  Ninguna migración debe aparecer como `(Pending)`. Si alguna lo está, avisa a Anthony antes de aplicarla. `dotnet ef database update` solo aplica las migraciones que faltan y no borra datos, pero conviene que una sola persona actualice la base compartida.
- **Panel de Somee → SQL Query:** `SELECT name FROM sys.tables ORDER BY name;` → **24** tablas.

La administración de Somee (crear la base, ejecutar el esquema, cargar datos de prueba, publicar el sitio) está en [`database/SOMEE.md`](database/SOMEE.md).

### LocalDB (opcional, para trabajar sin afectar a los demás)
Sin User Secrets configurados:
```powershell
dotnet ef database update -p src/VetPlatform.Infrastructure -s src/VetPlatform.API
sqlcmd -S "(localdb)\MSSQLLocalDB" -d VetPlatform -C -I -b -f 65001 -i database/seed-test-data.sql
```
El segundo comando (opcional) carga los datos de prueba. Es idempotente: no duplica registros.

### Datos de prueba
`database/seed-test-data.sql` crea 8 cuentas `@vetdemo.test` (propietarios, veterinarios regentes, urgencias y administradores), una clínica, 4 mascotas, 6 consultas, 3 recetas y 1 adenda. La contraseña **no está en el repositorio**: pídela en el grupo privado o búscala en el `.txt`.

---

## 6. Ejecutar el proyecto

Solo hay un proyecto ejecutable: **`VetPlatform.API`** (web y API en el mismo proceso).

**Visual Studio:** elige el perfil **`https`** en el menú junto a ▶ y presiona **F5**.

**Terminal:**
```powershell
dotnet run --project src/VetPlatform.API --launch-profile https
```

| Perfil | URL | Entorno | Base de datos |
|---|---|---|---|
| `https` | https://localhost:7221 (y http://localhost:5295) | Development | Somee si configuraste User Secrets; si no, LocalDB |
| `http` | http://localhost:5295 | Development | Igual que `https` |
| `somee` | http://localhost:5299 | Production | Lee `appsettings.Production.json` (solo para administración y publicación) |

**Comprobar que inició bien:**
1. La consola muestra `Now listening on: https://localhost:7221` y `Hosting environment: Development`.
2. `https://localhost:7221/health` → `{"status":"ok","database":"ok"}`.
3. `https://localhost:7221` muestra la portada "Plataforma Centralizada de Servicios Veterinarios".

---

## 7. Verificar funcionalidades (CRUD)

> En Somee usa un correo y una cédula **únicos** (por ejemplo, con tus iniciales): la base es compartida y no admite cédulas repetidas.

### Crear
1. **Registrarse** → completa nombre, correo, contraseña (mínimo 8 caracteres, con una mayúscula y un número), cédula (5–20 caracteres), teléfono (mínimo 8 dígitos) y dirección (opcional). Al enviar, aparece *"Cuenta creada correctamente. Ahora puedes iniciar sesión."*
2. **Iniciar sesión** con esa cuenta → se abre **Mis mascotas**.
3. **Registrar mascota** → nombre y especie (obligatorios), raza y fecha de nacimiento (opcionales). Se abre su ficha con el mensaje *"Mascota "…" registrada correctamente."*

### Consultar
- **Mis mascotas** lista tus mascotas; **Ver detalle** muestra la ficha, la edad, el código QR y el historial clínico.
- **Mi perfil** muestra tu cédula, teléfono y dirección.
- **Veterinario:** inicia sesión con `dra.solis@vetdemo.test` (datos de prueba) → **Mis pacientes** → Firulais → historial ordenado de la más reciente a la más antigua → **Ver detalle** de una consulta.

### Actualizar
- **Mi perfil → Editar** → cambia el teléfono → *"Tus datos se actualizaron correctamente."*

### Eliminar
- **Mi perfil → Eliminar** → confirma → *"Tus datos de propietario fueron eliminados."* La eliminación es **lógica**: el registro queda con `IsDeleted = 1` y la app te lleva a crear el perfil de nuevo.
- Las consultas, recetas y adendas **no se editan ni se eliminan, por diseño** (expediente clínico inmutable). Las correcciones se registran como adendas.

### Comprobar que se guardó en Somee
En el panel de Somee → **SQL Query** (cambia el correo por el tuyo):
```sql
SELECT u.Email, u.FullName, p.IdentificationNumber, p.Phone, p.IsDeleted
FROM Users u LEFT JOIN OwnerProfiles p ON p.UserId = u.Id
WHERE u.Email = 'tu.correo@ejemplo.com';

SELECT pt.Name, pt.Species, pt.CreatedAt
FROM Pets pt JOIN Users u ON u.Id = pt.OwnerId
WHERE u.Email = 'tu.correo@ejemplo.com';
```
Después de cada paso, el resultado debe reflejar el cambio: perfil creado, teléfono actualizado, `IsDeleted = 1`, mascota registrada.

---

## 8. Ejecutar las pruebas automatizadas

```powershell
dotnet test
```
Las pruebas **no usan Somee**: las de servicios corren en SQLite en memoria y las web levantan la app con SQLite.

| Proyecto | Tipo | Resultado esperado |
|---|---|---|
| `VetPlatform.Application.Tests` | Unitarias / de servicio (37) | `Passed! - Failed: 0, Passed: 37` |
| `VetPlatform.Web.Tests` | Integración web (20) | `Failed: 4, Passed: 16`: ver la nota |

> **Nota:** 4 pruebas web antiguas (`Sprint1FlowTests` ×3 y `Pbi05PruebasTests.Consulta_de_mascota_inexistente...`) fallan desde que PBI-01 hizo obligatorias la cédula y el teléfono: su helper de registro no envía esos campos. No es un problema de tu entorno.

Filtrar por grupo:
```powershell
dotnet test --filter "FullyQualifiedName~ClinicalHistory"
dotnet test --filter "FullyQualifiedName~DatabaseIntegrationTests"
```
Con un filtro, el proyecto que no tiene pruebas coincidentes muestra *"No test matches the given testcase filter"*. Es normal.

**Contra SQL Server real (LocalDB):** cada prueba crea una base temporal, aplica la migración real y la borra al terminar.
```powershell
$env:VETPLATFORM_TEST_SQLSERVER = 'Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True'
dotnet test tests/VetPlatform.Application.Tests
Remove-Item Env:VETPLATFORM_TEST_SQLSERVER
```
> No apuntes esta variable a Somee: las pruebas crean y borran bases, y el usuario de Somee no tiene ese permiso.

Cómo interpretar los resultados: `Passed!` = todo bien. `Failed!` lista cada prueba con `[FAIL]` y su `Error Message`. Si falla `Migrations_snapshot_matches_the_current_model`, alguien cambió una entidad sin crear su migración (ver sección 9).

---

## 9. Solución de problemas

| Síntoma | Causa probable | Solución |
|---|---|---|
| `/health` responde 503 `"database":"unreachable"` | La app no llega a la base | Revisa la consola o la ventana *Output*: el mensaje exacto está ahí |
| `Login failed for user '...'` | Contraseña incorrecta o cambiada; se usó la contraseña de la cuenta de Somee en lugar de la del *login* SQL | Copia de nuevo el connection string del `.txt` más reciente y repite la sección 4.2 |
| `A network-related or instance-specific error` / no se encuentra el servidor | Connection string mal copiado (servidor incompleto), sin internet o puerto 1433 bloqueado | Verifica `data source=...mssql.somee.com` y prueba otra red |
| `Keyword not supported` / `Format of the initialization string does not conform` | Connection string cortado, con saltos de línea o comillas sobrantes | Pégalo en una sola línea, entre comillas simples en PowerShell |
| La contraseña con `$` llega cortada | PowerShell interpretó `$...` como variable (comillas dobles) | Usa comillas simples `'...'` o *Manage User Secrets* |
| Al iniciar: `Falta ConnectionStrings:DefaultConnection` | Entorno Production sin configuración, o variable de entorno vacía | Usa el perfil `https`/`http`; para Production crea `appsettings.Production.json` desde la plantilla |
| Al iniciar: `Jwt:Key debe tener al menos 32 caracteres` | Falta la clave JWT (Production) | Agrega `Jwt:Key` en `appsettings.Production.json` (ver `database/SOMEE.md`) |
| La app usa LocalDB y no Somee | No hay User Secrets en tu equipo | `dotnet user-secrets list --project src/VetPlatform.API` y repite la sección 4.2 |
| `Cannot open database "VetPlatform"` (LocalDB) | No se creó la base local | `dotnet ef database update -p src/VetPlatform.Infrastructure -s src/VetPlatform.API` |
| `Invalid object name 'Users'` | El esquema no existe en esa base | En Somee avisa a Anthony; en LocalDB ejecuta el `dotnet ef database update` de arriba |
| `dotnet ef` no se reconoce | Falta la herramienta EF | `dotnet tool install --global dotnet-ef --version 8.0.8` y reabre la terminal |
| `The current .NET SDK does not support targeting .NET 8.0` | Falta el SDK 8 | Instala el .NET 8 SDK |
| Error `MSB3027`/`MSB3021` *file is locked by VetPlatform.API* al compilar | La app sigue ejecutándose | Detenla (Shift+F5 en Visual Studio) y vuelve a compilar |
| Falla `Migrations_snapshot_matches_the_current_model` | Se cambió una entidad sin migración | `dotnet ef migrations add <Nombre> -p src/VetPlatform.Infrastructure -s src/VetPlatform.API -o Data/Migrations` y coordina la actualización de Somee |
| Error 1934 `QUOTED_IDENTIFIER` al insertar a mano | Tablas con índices filtrados | Empieza el script con `SET QUOTED_IDENTIFIER ON;` (`sqlcmd -I`) |
| Acentos raros (`SolÃ­s`) después de usar `sqlcmd` | El archivo se leyó como ANSI | Agrega `-f 65001` |
| Las 4 pruebas web de Sprint 1 fallan | Problema conocido (sección 8) | No requiere acción de tu parte |

Si nada de esto lo resuelve, reporta en el grupo: qué comando ejecutaste, el mensaje de error completo **sin credenciales** y la salida de `dotnet --list-sdks`.

---

## 10. Referencia técnica

### Configuración
| Clave | Variable de entorno | Descripción |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | Conexión a SQL Server (obligatoria) |
| `Jwt:Key` | `Jwt__Key` | Secreto HMAC, mínimo 32 caracteres (obligatorio) |
| `Jwt:Issuer` / `Jwt:Audience` / `Jwt:ExpiryMinutes` | `Jwt__Issuer`… | Datos del token |
| `EmergencyAccess:DefaultExpiryMinutes` | `EmergencyAccess__DefaultExpiryMinutes` | Duración del acceso urgente (120) |
| `Database:ApplyMigrationsOnStartup` | `Database__ApplyMigrationsOnStartup` | Aplica migraciones al iniciar (false) |

Plantilla para Production: `src/VetPlatform.API/appsettings.Production.example.json`. Cópiala como `appsettings.Production.json`, que está ignorado por git.

### Documentación relacionada
- [`proyecto_de_implementaci_n_sprint_1.md`](proyecto_de_implementaci_n_sprint_1.md): enunciado del Sprint 1 del curso.

### Decisiones de diseño
- **Denegación por defecto:** toda petición exige autenticación; la autorización por recurso (dueño, regente activo, acceso urgente vigente) está en `ClinicalAccessGuard`.
- **Inmutabilidad clínica:** consultas, recetas, adendas, atenciones de emergencia y auditoría no se modifican; `AppDbContext` lo impide. Las correcciones son adendas.
- **Regente único:** índice único filtrado (`[IsActive] = 1`) más revocación transaccional del anterior.
- **Stock y agenda:** tokens de concurrencia optimista (`Version`) y transacciones explícitas.
- **Auditoría:** solo metadatos, nunca contenido clínico; los 401 y 403 se auditan automáticamente.
- **Primer administrador:** los roles `PlatformAdmin` y `ClinicAdmin` no se pueden auto-registrar. Se asignan en la base con `SET QUOTED_IDENTIFIER ON; INSERT INTO [UserRoleAssignments] ([Id],[UserId],[Role],[OrganizationId],[GrantedAt],[RevokedAt],[IsDeleted]) VALUES (NEWID(), '<userId>', 'PlatformAdmin', NULL, SYSDATETIMEOFFSET(), NULL, 0);`
