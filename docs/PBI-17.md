# PBI-17: Recordatorios para mascotas

Al reservar una cita se programa un recordatorio para su dueño, 24 horas antes del horario reservado. `BookAppointmentCommand` acepta `reason`, con valor predeterminado `Consulta veterinaria`. El mensaje incluye nombre de la mascota, motivo, fecha, hora y desplazamiento UTC.

Los cuidados se programan mediante `POST /api/care-schedules`, con JSON:

```json
{
  "petId": "UUID de la mascota",
  "reason": "Desparasitación",
  "dateTime": "2026-12-01T09:00:00-06:00"
}
```

Solo el dueño puede programar o cancelar cuidados para su mascota. Se exige un motivo de hasta 500 caracteres y una fecha futura. La fecha se almacena en UTC; el mensaje indica la zona horaria de la hora mostrada.

- `GET /api/reminders`: consulta los recordatorios del usuario autenticado y sus estados.
- `DELETE /api/care-schedules/{id}`: cancela un cuidado pendiente usando el identificador devuelto al crearlo.
- `DELETE /api/appointments/{id}`: cancela una cita y sus recordatorios pendientes; libera el horario. Puede hacerlo su dueño, veterinario asignado o administrador de plataforma.

Estos endpoints usan la autenticación y protección antifalsificación existentes del proyecto.

El despachador revisa la cola cada 30 segundos. Los eventos creados con menos de 24 horas de anticipación son elegibles inmediatamente; los eventos vencidos se descartan. Antes de cada intento se comprueba que la cita siga reservada. Las notificaciones enviadas o canceladas no vuelven a seleccionarse; las fallidas conservan el límite existente de cinco intentos.

El proveedor actual es `LoggingNotificationSender`: registra el envío en el log, sin correo, SMS ni push real. Para entrega externa se debe implementar y registrar `INotificationSender`. La consulta autenticada permite leer el contenido completo de los recordatorios.

Aplicar la migración `PetReminders` con el mecanismo habitual de despliegue (`dotnet ef database update --project src/VetPlatform.Infrastructure --startup-project src/VetPlatform.API`) antes de usar la funcionalidad. No se requiere cambiar datos existentes.
