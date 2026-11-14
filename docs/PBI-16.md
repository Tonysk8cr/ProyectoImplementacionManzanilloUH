# PBI-16: Agendamiento rápido de citas

El propietario con sesión iniciada puede entrar a **Agendar Cita** desde la portada o el menú. La pantalla permite elegir una de sus mascotas, el día y el motivo (Consulta, Vacunas, Revisión o texto libre de hasta 500 caracteres). **Ver horarios disponibles** muestra las horas libres con clínica y veterinario; **Confirmar Cita** guarda la reserva y abre un resumen con mascota, fecha, hora, motivo y clínica.

El diseño usa las vistas adaptables existentes, sin instalar una aplicación móvil. Los horarios se muestran en hora de Costa Rica (UTC-6), incluso si el servidor usa otra zona horaria. El filtrado considera el día local, incluidos horarios que caen al día siguiente en UTC.

Solo se muestran horarios futuros, sin reserva activa, de clínicas activas y veterinarios con membresía vigente. Al confirmar se revalida la mascota, el horario y las reglas del servicio de reservas existente. Si otro usuario ocupó el turno, se informa el problema y se permite elegir otro, conservando mascota, fecha y motivo. Se mantienen la transacción, la protección frente a concurrencia y las notificaciones del PBI-17.

Sin mascotas se ofrece registrarlas. Sin horarios se invita a elegir otro día. Los formularios requieren sesión de propietario y token antifalsificación. El resumen solo puede consultarlo el propietario de la cita. Se usa redirección después de confirmar para que actualizar la página no repita la reserva.

## Preparación

Aplicar las migraciones antes de ejecutar en una base existente:

```powershell
dotnet ef database update --project src/VetPlatform.Infrastructure --startup-project src/VetPlatform.API
```

`AppointmentReason` incorpora el motivo a las citas. Las reservas anteriores reciben el valor `Consulta veterinaria`. La clínica debe haber publicado disponibilidades mediante el servicio existente; no se generan horarios ficticios.

## Verificación de aceptación

1. Iniciar sesión como propietario y pulsar **Agendar Cita** en la portada. Comprobar que solo aparezcan sus mascotas.
2. Elegir mascota, fecha y motivo; consultar horarios y comprobar hora, clínica y veterinario.
3. Elegir un turno y confirmar. Comprobar el mensaje **Cita guardada correctamente** y su resumen. Actualizar el resumen no debe crear otra reserva.
4. Abrir el mismo turno en dos sesiones. Después de reservarlo en una, confirmar en la otra: debe indicarse que ya no está disponible.
5. Comprobar el estado vacío sin mascotas o sin horarios, y el acceso restringido al resumen de otro propietario.

Las pruebas web cubren navegación, sesión, estado sin mascotas, selección por día local, confirmación persistida, recordatorio asociado, turno ocupado, envío repetido, mascota ajena, horario inválido y antifalsificación. El objetivo de completar la reserva en menos de dos minutos requiere una medición de usabilidad con usuarios y disponibilidad real de la clínica.
