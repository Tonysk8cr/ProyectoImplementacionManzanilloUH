# Diagrama de la base de datos

Generado a partir de la migración real (`src/VetPlatform.Infrastructure/Data/Migrations/*_InitialCreate.cs`). GitHub y Visual Studio Code (con la extensión de Mermaid) lo muestran como imagen.

## Cómo leerlo
- **Línea continua (`||--o{`)**: **llave foránea real** en SQL Server. La base impide registros huérfanos. Hay 8.
- **Línea punteada (`||..o{`)**: **referencia lógica**, una columna `Guid` que apunta a otra tabla pero **sin restricción en la base**. La integridad la controlan los servicios de la aplicación.
- `||--o{` se lee "uno a muchos" (por ejemplo, un expediente tiene muchas consultas); `||--||` se lee "uno a uno".
- Todas las tablas tienen `Id` (uniqueidentifier) e `IsDeleted` (eliminación lógica); en el diagrama no se repiten.

## Núcleo clínico e identidad
```mermaid
erDiagram
    Users ||--o{ UserRoleAssignments : "UserId (FK)"
    Users ||..o| OwnerProfiles : "UserId"
    Users ||..o{ Pets : "OwnerId"
    Pets ||--|| ClinicalRecords : "PetId (FK)"
    ClinicalRecords ||--o{ Consultations : "ClinicalRecordId (FK)"
    Consultations ||--o{ Addenda : "ConsultationId (FK)"
    Consultations ||--o{ Prescriptions : "ConsultationId (FK)"
    Prescriptions ||--o{ PrescriptionAddenda : "PrescriptionId (FK)"
    Pets ||..o{ AttendingVetRelationships : "PetId"
    Users ||..o{ AttendingVetRelationships : "VetUserId"
    Users ||..o{ Consultations : "VetUserId"

    Users {
        string Email UK
        string PasswordHash
        string FullName
        bool IsVerified
        bool IsSuspended
    }
    UserRoleAssignments {
        guid UserId FK
        string Role
        guid OrganizationId
        datetimeoffset RevokedAt
    }
    OwnerProfiles {
        guid UserId
        string IdentificationNumber UK
        string Phone
        string Address
    }
    Pets {
        guid OwnerId
        string Name
        string Species
        string Breed
        date BirthDate
        string QrCode UK
    }
    ClinicalRecords {
        guid PetId FK
    }
    AttendingVetRelationships {
        guid PetId
        guid VetUserId
        guid ClinicId
        bool IsActive
    }
    Consultations {
        guid ClinicalRecordId FK
        guid VetUserId
        datetimeoffset AttendanceDate
        datetimeoffset RegistrationDate
        string Notes
        string Status
    }
    Prescriptions {
        guid ConsultationId FK
        string Medication
        string Dosage
        int DurationDays
    }
```

## Clínicas, emergencias y agenda
```mermaid
erDiagram
    Clinics ||--o{ ClinicMembers : "ClinicId (FK)"
    Users ||..o{ ClinicMembers : "UserId"
    Users ||..o{ Clinics : "RepresentativeUserId"
    Clinics ||..o{ AttendingVetRelationships : "ClinicId"
    Pets ||..o{ EmergencyAccesses : "PetId"
    EmergencyAccesses ||..o{ EmergencyAttendances : "EmergencyAccessId"
    ClinicalRecords ||..o{ EmergencyAttendances : "ClinicalRecordId"
    Clinics ||..o{ AvailabilitySlots : "ClinicId"
    AvailabilitySlots ||..o| Appointments : "SlotId"
    Pets ||..o{ Appointments : "PetId"
```

## Marketplace y sistema
```mermaid
erDiagram
    Users ||..o{ Stores : "OperatorUserId"
    Stores ||..o{ Products : "StoreId"
    Products ||..o{ InventoryMovements : "ProductId"
    Stores ||..o{ Orders : "StoreId"
    Users ||..o{ Orders : "BuyerUserId"
    Orders ||--o{ OrderItems : "OrderId (FK)"
    Products ||..o{ OrderItems : "ProductId"
    Users ||..o{ Notifications : "RecipientUserId"
    Users ||..o{ AuditLogs : "ActorUserId"
```

## Reglas que el diagrama no muestra
- **Un solo regente activo por mascota:** índice único filtrado `[IsActive] = 1` en `AttendingVetRelationships.PetId`.
- **Una sola cita activa por horario:** índice único filtrado `[Status] = 'Booked'` en `Appointments.SlotId`.
- **Cédula y perfil únicos entre los no eliminados:** índices filtrados `[IsDeleted] = 0` en `OwnerProfiles`.
- **Registros inmutables:** `Consultations`, `Prescriptions`, `Addenda`, `PrescriptionAddenda`, `EmergencyAttendances` y `AuditLogs` no admiten `UPDATE` desde la aplicación.
- **Concurrencia:** `Products.Version` y `AvailabilitySlots.Version` son tokens de concurrencia optimista.
