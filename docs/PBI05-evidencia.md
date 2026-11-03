# PBI-05: pruebas básicas del flujo Sprint 1

## Alcance
Registro de propietario, inicio de sesión, registro y consulta de mascota mediante
HTTP, vistas Razor, cookies y antiforgery reales. Base SQLite de pruebas aislada;
no requiere PostgreSQL ni la cadena de Azure.

## Casos y resultados
Ejecución local: `dotnet test VetPlatform.sln -c Release --nologo -v quiet`.
Resultado: **30 aprobadas, 0 fallidas** (22 Application y 8 Web).

| Caso | Prueba automatizada | Resultado esperado y observado |
| --- | --- | --- |
| Registro satisfactorio | `Registro_login_y_lista_de_mascotas_flujo_completo` | Registro redirige a login; credenciales permiten entrar a Mis mascotas. Aprobado. |
| Registro inválido | `Registro_sin_nombre_muestra_error_y_no_crea_cuenta` | Error de nombre obligatorio visible, conserva correo; no permite iniciar sesión ni acceder a mascotas. Aprobado. |
| Registro y consulta satisfactorios | `Registrar_mascota_luego_se_ve_en_la_lista_y_en_su_detalle` | Alta redirige al detalle; lista y detalle presentan nombre, especie, historial y etiqueta de edad. Aprobado. |
| Mascota inválida | `Mascota_con_datos_invalidos_vuelve_al_formulario_con_errores` | Muestra errores de nombre y especie obligatorios. Aprobado. |
| Consulta inexistente | `Consulta_de_mascota_inexistente_muestra_error_comprensible` | Muestra «Mascota no encontrada.» en resumen de errores. Aprobado. |

## Hallazgo corregido
La consulta de una mascota inexistente devolvía la lista sin mostrar el error del
servicio. Se agregó el resumen de validación a `Views/Pets/Index.cshtml`.

## Revisión independiente de QA
- [ ] Ejecutar el comando y confirmar los 30 resultados.
- [ ] Revisar los escenarios y sus aserciones en `tests/VetPlatform.Web.Tests/`.
- [ ] Verificar visualmente registro y validaciones.
- [ ] Aprobar o solicitar cambios en el PR antes de fusionar.

Esta evidencia automatizada no sustituye la aprobación independiente de QA ni
verifica la conexión o migración en Azure, todavía pendiente de configuración.
