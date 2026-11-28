# PBI-05: Gestión de citas veterinarias (Sprint 003, ítem 22)

## Uso

Desde **Gestión de citas** en el menú, el personal autorizado selecciona una clínica y consulta su agenda. Por defecto se muestra el día actual en Costa Rica (UTC-6). Puede filtrar por fecha y estado; al dejar la fecha vacía se consultan todos los días.

El listado incluye fecha, hora, mascota, cliente, veterinario, motivo, estado y acciones. **Ver detalle** muestra la información de la cita. Las acciones están disponibles tanto en el listado como en el detalle:

| Estado actual | Acciones disponibles |
| --- | --- |
| Reservada, antes de su hora | Cancelar cita |
| Reservada, desde su hora de inicio | Cancelar cita, marcar completada, marcar ausente |
| Cancelada, completada o ausente | Solo consulta |

Las acciones son POST con token antifalsificación. El servidor vuelve a comprobar permisos y estado; ocultar un botón no es el control de acceso. Tras guardar, se redirige al detalle y las consultas posteriores muestran el estado persistido.

## Acceso y persistencia

- El administrador de plataforma puede consultar todas las clínicas.
- Un administrador de clínica accede a las clínicas que representa, aquellas con una asignación activa del rol `ClinicAdmin` para esa organización o aquellas donde es miembro activo.
- Los veterinarios con roles existentes acceden a las clínicas que representan o donde son miembros activos.
- Propietarios, usuarios sin sesión y personal ajeno a la clínica no pueden consultar ni gestionar sus citas. La pertenencia y las asignaciones organizacionales se consultan en la base de datos.

El servicio usa `AppDbContext` y la conexión SQL Server existente (`ConnectionStrings:DefaultConnection`). No usa listas de ejemplo ni una base alternativa en producción. Listado, detalle, roles, pertenencia, cambios de estado, liberación de horarios, supresión de recordatorios pendientes y auditoría se consultan o persisten mediante Entity Framework.

Los cambios de estado y su auditoría se guardan en una transacción. El estado de la cita y la versión del horario actúan como tokens de concurrencia para rechazar actualizaciones obsoletas. Cancelar libera el horario; completar o marcar ausente conserva su ocupación. El endpoint de cancelación del propietario tampoco puede cancelar posteriormente una cita finalizada.

## SQL Server / Somee

La rama actual usa `UseSqlServer`. Se corrigieron los tipos y metadatos de las migraciones de recordatorios y motivo que todavía apuntaban a PostgreSQL. La propiedad `Reason` vuelve a tener el límite de 500 caracteres que corresponde al esquema.

Para una base administrada por EF Migrations, aplicar las migraciones con el procedimiento de despliegue del proyecto. Para una base Somee inicializada mediante el script SQL original, el administrador puede revisar y ejecutar `database/PBI05_Citas.sql` si faltan las columnas de motivo o recordatorios. Ese script es aditivo e idempotente; no crea citas ni datos de prueba y no modifica el historial de migraciones de EF. No combinar ambos mecanismos sin revisar el historial de la base.

La conexión real de Somee no está en los archivos de configuración disponibles en este repositorio: `appsettings.json` tiene la conexión vacía y Development usa LocalDB. El README indica que la cadena compartida se obtiene por el canal privado y se configura fuera del control de versiones. Por tanto, las pruebas locales no acreditan una conexión al servidor compartido. Falta configurar esa cadena y comprobar `/health` y el módulo con una cuenta de personal de la clínica para completar la validación en Somee.

## QA

1. Iniciar sesión como personal de una clínica. Abrir **Gestión de citas** y comprobar las columnas requeridas.
2. Filtrar por fecha y estado. Verificar que los horarios posteriores a las 18:00 de Costa Rica pertenecen al día local correcto, aunque su fecha UTC sea la siguiente.
3. Abrir un detalle y comprobar mascota, cliente, clínica, veterinario, horario, motivo y estado.
4. Cancelar una reservada. Verificar el nuevo estado en detalle y listado, horario liberado y recordatorios pendientes cancelados.
5. En una reservada cuya hora ya comenzó, marcar completada o ausente. Verificar persistencia y ausencia de nuevas acciones.
6. Intentar acceder por URL o POST a una cita de otra clínica, como propietario y sin sesión. Comprobar el rechazo sin cambios en datos.
7. Abrir la misma cita en dos sesiones. Modificarla en una e intentar una acción desde la otra; la segunda debe rechazarse sin sobrescribir el estado guardado.

Las pruebas incluyen servicios con SQLite y SQL Server LocalDB, migraciones reales de SQL Server, filtros, acceso, membresía revocada, concurrencia, notificaciones, auditoría y vistas MVC con cookies y antifalsificación. Los datos de SQL Server de prueba usan bases temporales independientes y nunca Somee.
