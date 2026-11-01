# PROYECTO DE IMPLEMENTACIÓN
## SPRINT 1: Nuestro primer incremento

- **Modalidad:** Grupal con responsabilidad individual
- **Duración académica:** Semana 07 + Semana 08
  - **Semana 07:** Planning + inicio de ejecución
  - **Semana 08:** Ejecución + Refinamiento
  - **Semana 09:** Review + Retrospective
- **Trabajo:** Parcialmente en clase + horas independientes
- **Evaluación:** Evidencia acumulativa del Proyecto Práctico; no agregaría otra nota independiente.

---

## Principio de carga

Este primer Sprint debe ser deliberadamente pequeño.

Los estudiantes cursan otras materias y pueden tener responsabilidades laborales. Además, todavía están aprendiendo a utilizar Scrum y Azure DevOps. No conviene evaluar Sprint 1 por volumen de código.

El reto es conseguir por primera vez un incremento pequeño, demostrable y trazable.

> **Nota:** Microsoft Learn recomienda que los Backlog Items sean unidades manejables que puedan completarse dentro de un Sprint, mientras Features y Epics pueden extenderse durante varios Sprints.  
> *Referencia:* [Microsoft Learn - Define Features and Epics](https://learn.microsoft.com/en-us/azure/devops/boards/backlogs/define-features-epics?view=azure-devops)

---

## Backlog base propuesto para Sprint 1

### Sprint Goal
> Construir un primer flujo funcional que permita registrar y consultar la información básica de una mascota asociada a su propietario, dejando evidencia completa del trabajo mediante Azure DevOps.

*No recomiendo incorporar todavía emergencias 911, recetas, marketplace, clínicas afiliadas ni cambio de veterinario regente. Esos módulos aumentan demasiado las dependencias para un primer Sprint.*

### Backlog

| ID | Product Backlog Item | Criterio mínimo de aceptación | Prioridad | Esfuerzo sugerido |
| :--- | :--- | :--- | :---: | :---: |
| **PBI-01** | Como propietario, quiero registrar mis datos básicos para identificarme dentro de la solución | Permite registrar datos mínimos definidos por el equipo; valida campos obligatorios | Alta | 3 |
| **PBI-02** | Como propietario, quiero registrar una mascota para mantener su información básica | Nombre, especie, raza u otra clasificación, fecha/edad y datos básicos definidos | Alta | 3 |
| **PBI-03** | Como propietario, quiero asociar mi mascota con mi perfil para reconocer que forma parte de mis mascotas | La mascota queda vinculada al propietario correcto | Alta | 2 |
| **PBI-04** | Como propietario, quiero consultar la información básica de mi mascota para verificar los datos registrados | Presenta información almacenada de manera comprensible | Alta | 2 |
| **PBI-05** | Como equipo, queremos contar con una prueba básica del flujo desarrollado para comprobar que el registro y consulta funcionan | Evidencia al menos un escenario satisfactorio y uno de validación/error | Alta | 2 |

**Carga orientativa:** 12 puntos relativos.

*Los 12 puntos no deben convertirse todavía en una meta fija de velocidad. Al ser Sprint 1, el equipo no tiene datos históricos suficientes para conocer su velocidad. Servirán únicamente como primera estimación que luego podrán comparar con lo realmente conseguido.*

---

## Tasks sugeridas

No convertiría el Sprint en 30 tareas. Para cada PBI utilicen únicamente las tareas necesarias. Como referencia:

| PBI | Tasks posibles |
| :--- | :--- |
| **PBI-01** | Definir datos mínimos / interfaz básica / persistencia / validación |
| **PBI-02** | Definir datos mascota / interfaz / persistencia / validación |
| **PBI-03** | Crear relación propietario-mascota / comprobar asociación |
| **PBI-04** | Construir consulta / mostrar información / validar resultado |
| **PBI-05** | Diseñar casos básicos / ejecutar / registrar resultados |

*Cada equipo podrá adaptar la descomposición según su arquitectura y tecnologías.*

Microsoft distingue precisamente entre **Product Backlog Items**, que representan unidades de valor, y **Tasks**, que permiten representar el trabajo necesario para conseguirlas. ([Microsoft Learn](https://learn.microsoft.com/en-us/azure/devops/boards/backlogs/define-features-epics))

---

## Definition of Done inicial

Para Sprint 1 propongo una Definition of Done sencilla. Un PBI solamente podrá considerarse terminado cuando:

1. Cumpla sus criterios de aceptación.
2. El código correspondiente esté integrado en la versión del equipo.
3. No presente errores críticos conocidos en el flujo demostrado.
4. Se haya realizado al menos la prueba definida.
5. Azure DevOps refleje correctamente su estado.
6. Exista evidencia suficiente para demostrar el resultado.
7. Otro integrante del equipo pueda explicar qué se construyó.

*No añadiría todavía procesos sofisticados de CI/CD, cobertura obligatoria, análisis estático complejo o automatización extensa. Eso puede incorporarse progresivamente.*

---

## Primer reto Scrum

Este Sprint no busca demostrar cuánto pueden programar. **Busca demostrar cómo pueden trabajar.**

El desafío consiste en construir un incremento pequeño y funcional mientras el equipo aprende a:  
$$\text{planificar} \rightarrow \text{distribuir} \rightarrow \text{desarrollar} \rightarrow \text{probar} \rightarrow \text{comunicar} \rightarrow \text{actualizar} \rightarrow \text{revisar} \rightarrow \text{mejorar}$$

### Pasos de ejecución

* **Paso 1 - Sprint Planning:** Revisen el Product Backlog. Analicen el Sprint Goal y confirmen qué PBIs pueden completar responsablemente. No incorporen trabajo solamente porque "sería bueno tenerlo".
* **Paso 2 - Descomposición:** Dividan los PBIs seleccionados en Tasks suficientemente pequeñas para dar seguimiento al trabajo.
* **Paso 3 - Responsabilidad:** Cada Task deberá tener responsable, estado, relación con su PBI y estimación cuando corresponda. Todos los integrantes deben tener una contribución identificable.
* **Paso 4 - Ejecución:** Durante el Sprint actualicen estados, documenten bloqueos, registren decisiones relevantes, mantengan comunicación, no oculten trabajo pendiente y no marquen como terminado algo que todavía no cumple la Definition of Done.
* **Paso 5 - Gestionar cambios:** Si aparece un cambio, no lo implementen silenciosamente. Regístrenlo, analicen su impacto y decidan qué hacer. El cambio también es una evidencia de aprendizaje.
* **Paso 6 - Probar:** Antes del Review comprueben los criterios de aceptación.
* **Paso 7 - Sprint Review:** Demuestren software funcionando. No realicen una presentación extensa sobre lo que pretendían construir; muestren lo que realmente construyeron.
* **Paso 8 - Retrospective:** Respondan como equipo:
  * ¿Qué funcionó?
  * ¿Qué nos dificultó avanzar?
  * ¿Qué debemos dejar de hacer?
  * ¿Qué debemos comenzar a hacer?
  * ¿Qué mantendremos?
  * ¿Cuál será una acción concreta de mejora para Sprint 2?

---

## ENTREGABLE DEL SPRINT

En Moodle se entregará un **único enlace** a la carpeta institucional de Drive del equipo.

**Estructura requerida:**  
`/ProyectoImplementacion/Sprint01/`

Debe contener:
1. **`01-Sprint Goal y alcance`**: Documento breve.
2. **`02 - Azure DevOps`**: Documento con enlaces a:
   * Proyecto
   * Product Backlog
   * Sprint Backlog
   * Board
   * PBIs relevantes
3. **`03-Incremento`**: Enlace al repositorio, versión ejecutable, ambiente o evidencia que corresponda a la tecnología utilizada.
4. **`04- Pruebas`**: Evidencia mínima de las pruebas realizadas.
5. **`05-Sprint Review`**: Resumen de lo comprometido, terminado, no terminado, cambios y observaciones.
6. **`06- Retrospective`**: Conclusiones y acción de mejora seleccionada.
7. **`07- Evidencia individual`**: Tabla estructurada de la siguiente manera:

| Integrante | PBIs/Tasks | Aporte | Evidencia Azure DevOps |
| :--- | :--- | :--- | :--- |
| | | | |

### Regla de evidencia
Drive será el punto de entrada de la entrega, no un repositorio de duplicados. Cuando la evidencia original exista en Azure DevOps, Git u otra plataforma utilizada por el proyecto, incorporen el enlace directo.

El docente debe poder partir del enlace entregado en Moodle y reconstruir fácilmente:  
$$\text{qué hicieron} \rightarrow \text{quién lo hizo} \rightarrow \text{dónde está} \rightarrow \text{cómo se comprobó}$$

---

## Recursos recomendados (Microsoft Learn)

Usaría principalmente Microsoft Learn, no blogs ni tutoriales de terceros:

* [Administrar proyectos mediante Backlogs en Azure Boards](https://learn.microsoft.com/en-us/azure/devops/boards/backlogs/backlogs-overview?view=azure-devops): Explica Product Backlog, prioridades, PBIs, Features, Epics y planificación.
* [Crear y administrar el Product Backlog](https://learn.microsoft.com/en-us/azure/devops/boards/backlogs/backlogs-overview?view=azure-devops): Útil para revisar el procedimiento dentro de Azure Boards.
* [Organizar el Backlog y relacionar elementos](https://learn.microsoft.com/en-us/azure/devops/boards/backlogs/organize-backlog?view=azure-devops): Muestra cómo organizar jerarquías y relacionar elementos.
* [Definir Features y Epics](https://learn.microsoft.com/en-us/azure/devops/boards/backlogs/define-features-epics): Especialmente útil para comprender la jerarquía Scrum $\text{Epic} \rightarrow \text{Feature} \rightarrow \text{PBI} \rightarrow \text{Task}$.
* [Consultar historial y trazabilidad de Work Items](https://learn.microsoft.com/en-us/azure/devops/boards/queries/history-and-auditing?view=azure-devops): Recurso clave para el laboratorio de gestión del cambio.
* [Establecer la capacidad del Sprint](https://learn.microsoft.com/es-es/azure/devops/boards/sprints/set-capacity?view=azure-devops): Lectura recomendada (no obligatoria en el primer Sprint) para entender la disponibilidad real del equipo.

---

## Reflexión

> El éxito de Sprint 1 no consiste en terminar muchas funcionalidades. Consiste en que el equipo descubra cuánto puede comprometer responsablemente, cómo trabajar en conjunto y qué necesita mejorar para el siguiente Sprint.