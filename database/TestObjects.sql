-- =====================================================================
-- VetPlatform - DATOS DE PRUEBA (SQL Server / Somee)
-- Idempotente: crea cada cuenta solo si su correo no existe y cada
-- registro solo si no existe. Se puede ejecutar varias veces.
-- Ejecutar DESPUES de database/somee-schema.sql.
-- Contrasena de las cuentas: ver database/CUENTAS_PRUEBA.local.md
-- (archivo local, ignorado por git). Hash BCrypt factor 11.
-- =====================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @now datetimeoffset = SYSDATETIMEOFFSET();
DECLARE @hash nvarchar(200) = N'$2a$11$ua.izg7kfSHoWryiOJ1ibub7wrY.Hv8UN83jL2JOVMV8yQ3P68XIi';

-- ---------- Cuentas (solo si el correo no existe) ----------
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Email] = N'admin@vetdemo.test')
    INSERT INTO [Users] ([Id],[Email],[PasswordHash],[FullName],[IsVerified],[IsSuspended],[CreatedAt],[UpdatedAt],[IsDeleted])
    VALUES (NEWID(), N'admin@vetdemo.test', @hash, N'Administración VetPlatform', 1, 0, @now, @now, 0);
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Email] = N'clinica@vetdemo.test')
    INSERT INTO [Users] ([Id],[Email],[PasswordHash],[FullName],[IsVerified],[IsSuspended],[CreatedAt],[UpdatedAt],[IsDeleted])
    VALUES (NEWID(), N'clinica@vetdemo.test', @hash, N'Gerencia Clínica Manzanillo', 1, 0, @now, @now, 0);
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Email] = N'dra.solis@vetdemo.test')
    INSERT INTO [Users] ([Id],[Email],[PasswordHash],[FullName],[IsVerified],[IsSuspended],[CreatedAt],[UpdatedAt],[IsDeleted])
    VALUES (NEWID(), N'dra.solis@vetdemo.test', @hash, N'Dra. Ana Solís', 1, 0, @now, @now, 0);
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Email] = N'dr.mora@vetdemo.test')
    INSERT INTO [Users] ([Id],[Email],[PasswordHash],[FullName],[IsVerified],[IsSuspended],[CreatedAt],[UpdatedAt],[IsDeleted])
    VALUES (NEWID(), N'dr.mora@vetdemo.test', @hash, N'Dr. Luis Mora', 1, 0, @now, @now, 0);
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Email] = N'urgencias@vetdemo.test')
    INSERT INTO [Users] ([Id],[Email],[PasswordHash],[FullName],[IsVerified],[IsSuspended],[CreatedAt],[UpdatedAt],[IsDeleted])
    VALUES (NEWID(), N'urgencias@vetdemo.test', @hash, N'Dr. Pablo Jiménez (Urgencias)', 1, 0, @now, @now, 0);
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Email] = N'maria.lopez@vetdemo.test')
    INSERT INTO [Users] ([Id],[Email],[PasswordHash],[FullName],[IsVerified],[IsSuspended],[CreatedAt],[UpdatedAt],[IsDeleted])
    VALUES (NEWID(), N'maria.lopez@vetdemo.test', @hash, N'María López', 1, 0, @now, @now, 0);
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Email] = N'carlos.rojas@vetdemo.test')
    INSERT INTO [Users] ([Id],[Email],[PasswordHash],[FullName],[IsVerified],[IsSuspended],[CreatedAt],[UpdatedAt],[IsDeleted])
    VALUES (NEWID(), N'carlos.rojas@vetdemo.test', @hash, N'Carlos Rojas', 1, 0, @now, @now, 0);
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Email] = N'ana.vargas@vetdemo.test')
    INSERT INTO [Users] ([Id],[Email],[PasswordHash],[FullName],[IsVerified],[IsSuspended],[CreatedAt],[UpdatedAt],[IsDeleted])
    VALUES (NEWID(), N'ana.vargas@vetdemo.test', @hash, N'Ana Vargas', 1, 0, @now, @now, 0);

DECLARE @admin_u uniqueidentifier = (SELECT [Id] FROM [Users] WHERE [Email] = N'admin@vetdemo.test');
DECLARE @clinica_u uniqueidentifier = (SELECT [Id] FROM [Users] WHERE [Email] = N'clinica@vetdemo.test');
DECLARE @dra_solis_u uniqueidentifier = (SELECT [Id] FROM [Users] WHERE [Email] = N'dra.solis@vetdemo.test');
DECLARE @dr_mora_u uniqueidentifier = (SELECT [Id] FROM [Users] WHERE [Email] = N'dr.mora@vetdemo.test');
DECLARE @urgencias_u uniqueidentifier = (SELECT [Id] FROM [Users] WHERE [Email] = N'urgencias@vetdemo.test');
DECLARE @maria_lopez_u uniqueidentifier = (SELECT [Id] FROM [Users] WHERE [Email] = N'maria.lopez@vetdemo.test');
DECLARE @carlos_rojas_u uniqueidentifier = (SELECT [Id] FROM [Users] WHERE [Email] = N'carlos.rojas@vetdemo.test');
DECLARE @ana_vargas_u uniqueidentifier = (SELECT [Id] FROM [Users] WHERE [Email] = N'ana.vargas@vetdemo.test');

-- ---------- Clinica ----------
DECLARE @clinic uniqueidentifier = (SELECT TOP 1 [Id] FROM [Clinics] WHERE [Name] = N'Clínica Veterinaria Manzanillo');
IF @clinic IS NULL
BEGIN
    SET @clinic = NEWID();
    INSERT INTO [Clinics] ([Id],[Name],[Status],[RepresentativeUserId],[Address],[Phone],[CreatedAt],[IsDeleted])
    VALUES (@clinic, N'Clínica Veterinaria Manzanillo', 0, @clinica_u, N'San José, Costa Rica', N'2222-3333', @now, 0);
END

-- ---------- Roles (solo si faltan) ----------
IF NOT EXISTS (SELECT 1 FROM [UserRoleAssignments] WHERE [UserId] = @admin_u AND [Role] = N'PlatformAdmin' AND [RevokedAt] IS NULL)
    INSERT INTO [UserRoleAssignments] ([Id],[UserId],[Role],[OrganizationId],[GrantedAt],[RevokedAt],[IsDeleted])
    VALUES (NEWID(), @admin_u, N'PlatformAdmin', NULL, @now, NULL, 0);
IF NOT EXISTS (SELECT 1 FROM [UserRoleAssignments] WHERE [UserId] = @clinica_u AND [Role] = N'ClinicAdmin' AND [RevokedAt] IS NULL)
    INSERT INTO [UserRoleAssignments] ([Id],[UserId],[Role],[OrganizationId],[GrantedAt],[RevokedAt],[IsDeleted])
    VALUES (NEWID(), @clinica_u, N'ClinicAdmin', @clinic, @now, NULL, 0);
IF NOT EXISTS (SELECT 1 FROM [UserRoleAssignments] WHERE [UserId] = @dra_solis_u AND [Role] = N'AttendingVet' AND [RevokedAt] IS NULL)
    INSERT INTO [UserRoleAssignments] ([Id],[UserId],[Role],[OrganizationId],[GrantedAt],[RevokedAt],[IsDeleted])
    VALUES (NEWID(), @dra_solis_u, N'AttendingVet', NULL, @now, NULL, 0);
IF NOT EXISTS (SELECT 1 FROM [UserRoleAssignments] WHERE [UserId] = @dr_mora_u AND [Role] = N'AttendingVet' AND [RevokedAt] IS NULL)
    INSERT INTO [UserRoleAssignments] ([Id],[UserId],[Role],[OrganizationId],[GrantedAt],[RevokedAt],[IsDeleted])
    VALUES (NEWID(), @dr_mora_u, N'AttendingVet', NULL, @now, NULL, 0);
IF NOT EXISTS (SELECT 1 FROM [UserRoleAssignments] WHERE [UserId] = @urgencias_u AND [Role] = N'EmergencyVet' AND [RevokedAt] IS NULL)
    INSERT INTO [UserRoleAssignments] ([Id],[UserId],[Role],[OrganizationId],[GrantedAt],[RevokedAt],[IsDeleted])
    VALUES (NEWID(), @urgencias_u, N'EmergencyVet', NULL, @now, NULL, 0);
IF NOT EXISTS (SELECT 1 FROM [UserRoleAssignments] WHERE [UserId] = @maria_lopez_u AND [Role] = N'PetOwner' AND [RevokedAt] IS NULL)
    INSERT INTO [UserRoleAssignments] ([Id],[UserId],[Role],[OrganizationId],[GrantedAt],[RevokedAt],[IsDeleted])
    VALUES (NEWID(), @maria_lopez_u, N'PetOwner', NULL, @now, NULL, 0);
IF NOT EXISTS (SELECT 1 FROM [UserRoleAssignments] WHERE [UserId] = @carlos_rojas_u AND [Role] = N'PetOwner' AND [RevokedAt] IS NULL)
    INSERT INTO [UserRoleAssignments] ([Id],[UserId],[Role],[OrganizationId],[GrantedAt],[RevokedAt],[IsDeleted])
    VALUES (NEWID(), @carlos_rojas_u, N'PetOwner', NULL, @now, NULL, 0);
IF NOT EXISTS (SELECT 1 FROM [UserRoleAssignments] WHERE [UserId] = @ana_vargas_u AND [Role] = N'PetOwner' AND [RevokedAt] IS NULL)
    INSERT INTO [UserRoleAssignments] ([Id],[UserId],[Role],[OrganizationId],[GrantedAt],[RevokedAt],[IsDeleted])
    VALUES (NEWID(), @ana_vargas_u, N'PetOwner', NULL, @now, NULL, 0);

-- ---------- Miembros de la clinica ----------
IF NOT EXISTS (SELECT 1 FROM [ClinicMembers] WHERE [ClinicId] = @clinic AND [UserId] = @dra_solis_u AND [LeftAt] IS NULL)
    INSERT INTO [ClinicMembers] ([Id],[ClinicId],[UserId],[JoinedAt],[LeftAt],[IsDeleted]) VALUES (NEWID(), @clinic, @dra_solis_u, @now, NULL, 0);
IF NOT EXISTS (SELECT 1 FROM [ClinicMembers] WHERE [ClinicId] = @clinic AND [UserId] = @dr_mora_u AND [LeftAt] IS NULL)
    INSERT INTO [ClinicMembers] ([Id],[ClinicId],[UserId],[JoinedAt],[LeftAt],[IsDeleted]) VALUES (NEWID(), @clinic, @dr_mora_u, @now, NULL, 0);
IF NOT EXISTS (SELECT 1 FROM [ClinicMembers] WHERE [ClinicId] = @clinic AND [UserId] = @urgencias_u AND [LeftAt] IS NULL)
    INSERT INTO [ClinicMembers] ([Id],[ClinicId],[UserId],[JoinedAt],[LeftAt],[IsDeleted]) VALUES (NEWID(), @clinic, @urgencias_u, @now, NULL, 0);

-- ---------- Perfiles de propietario ----------
IF NOT EXISTS (SELECT 1 FROM [OwnerProfiles] WHERE [UserId] = @maria_lopez_u AND [IsDeleted] = 0)
   AND NOT EXISTS (SELECT 1 FROM [OwnerProfiles] WHERE [IdentificationNumber] = N'101110111' AND [IsDeleted] = 0)
    INSERT INTO [OwnerProfiles] ([Id],[UserId],[IdentificationNumber],[Phone],[Address],[CreatedAt],[UpdatedAt],[IsDeleted])
    VALUES (NEWID(), @maria_lopez_u, N'101110111', N'8811-2233', N'Heredia, Costa Rica', @now, @now, 0);
IF NOT EXISTS (SELECT 1 FROM [OwnerProfiles] WHERE [UserId] = @carlos_rojas_u AND [IsDeleted] = 0)
   AND NOT EXISTS (SELECT 1 FROM [OwnerProfiles] WHERE [IdentificationNumber] = N'202220222' AND [IsDeleted] = 0)
    INSERT INTO [OwnerProfiles] ([Id],[UserId],[IdentificationNumber],[Phone],[Address],[CreatedAt],[UpdatedAt],[IsDeleted])
    VALUES (NEWID(), @carlos_rojas_u, N'202220222', N'8822-3344', N'Alajuela, Costa Rica', @now, @now, 0);
IF NOT EXISTS (SELECT 1 FROM [OwnerProfiles] WHERE [UserId] = @ana_vargas_u AND [IsDeleted] = 0)
   AND NOT EXISTS (SELECT 1 FROM [OwnerProfiles] WHERE [IdentificationNumber] = N'303330333' AND [IsDeleted] = 0)
    INSERT INTO [OwnerProfiles] ([Id],[UserId],[IdentificationNumber],[Phone],[Address],[CreatedAt],[UpdatedAt],[IsDeleted])
    VALUES (NEWID(), @ana_vargas_u, N'303330333', N'8833-4455', NULL, @now, @now, 0);

-- ---------- Mascotas, expedientes y veterinario regente ----------
DECLARE @firulais uniqueidentifier = (SELECT TOP 1 [Id] FROM [Pets] WHERE [OwnerId] = @maria_lopez_u AND [Name] = N'Firulais');
IF @firulais IS NULL
BEGIN
    SET @firulais = NEWID();
    INSERT INTO [Pets] ([Id],[OwnerId],[Name],[Species],[Breed],[BirthDate],[Status],[QrCode],[CreatedAt],[IsDeleted])
    VALUES (@firulais, @maria_lopez_u, N'Firulais', N'Perro', N'Mestizo', '2020-03-15', N'Active', LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), '-', '')), @now, 0);
END
IF NOT EXISTS (SELECT 1 FROM [ClinicalRecords] WHERE [PetId] = @firulais)
    INSERT INTO [ClinicalRecords] ([Id],[PetId],[CreatedAt],[IsDeleted]) VALUES (NEWID(), @firulais, @now, 0);
DECLARE @firulais_rec uniqueidentifier = (SELECT TOP 1 [Id] FROM [ClinicalRecords] WHERE [PetId] = @firulais);
IF NOT EXISTS (SELECT 1 FROM [AttendingVetRelationships] WHERE [PetId] = @firulais AND [IsActive] = 1)
    INSERT INTO [AttendingVetRelationships] ([Id],[PetId],[VetUserId],[ClinicId],[EstablishedAt],[RevokedAt],[IsActive],[IsDeleted])
    VALUES (NEWID(), @firulais, @dra_solis_u, @clinic, @now, NULL, 1, 0);
DECLARE @luna uniqueidentifier = (SELECT TOP 1 [Id] FROM [Pets] WHERE [OwnerId] = @maria_lopez_u AND [Name] = N'Luna');
IF @luna IS NULL
BEGIN
    SET @luna = NEWID();
    INSERT INTO [Pets] ([Id],[OwnerId],[Name],[Species],[Breed],[BirthDate],[Status],[QrCode],[CreatedAt],[IsDeleted])
    VALUES (@luna, @maria_lopez_u, N'Luna', N'Gato', N'Siamés', '2022-07-01', N'Active', LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), '-', '')), @now, 0);
END
IF NOT EXISTS (SELECT 1 FROM [ClinicalRecords] WHERE [PetId] = @luna)
    INSERT INTO [ClinicalRecords] ([Id],[PetId],[CreatedAt],[IsDeleted]) VALUES (NEWID(), @luna, @now, 0);
DECLARE @luna_rec uniqueidentifier = (SELECT TOP 1 [Id] FROM [ClinicalRecords] WHERE [PetId] = @luna);
IF NOT EXISTS (SELECT 1 FROM [AttendingVetRelationships] WHERE [PetId] = @luna AND [IsActive] = 1)
    INSERT INTO [AttendingVetRelationships] ([Id],[PetId],[VetUserId],[ClinicId],[EstablishedAt],[RevokedAt],[IsActive],[IsDeleted])
    VALUES (NEWID(), @luna, @dra_solis_u, @clinic, @now, NULL, 1, 0);
DECLARE @rocky uniqueidentifier = (SELECT TOP 1 [Id] FROM [Pets] WHERE [OwnerId] = @carlos_rojas_u AND [Name] = N'Rocky');
IF @rocky IS NULL
BEGIN
    SET @rocky = NEWID();
    INSERT INTO [Pets] ([Id],[OwnerId],[Name],[Species],[Breed],[BirthDate],[Status],[QrCode],[CreatedAt],[IsDeleted])
    VALUES (@rocky, @carlos_rojas_u, N'Rocky', N'Perro', N'Labrador', '2019-11-20', N'Active', LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), '-', '')), @now, 0);
END
IF NOT EXISTS (SELECT 1 FROM [ClinicalRecords] WHERE [PetId] = @rocky)
    INSERT INTO [ClinicalRecords] ([Id],[PetId],[CreatedAt],[IsDeleted]) VALUES (NEWID(), @rocky, @now, 0);
DECLARE @rocky_rec uniqueidentifier = (SELECT TOP 1 [Id] FROM [ClinicalRecords] WHERE [PetId] = @rocky);
IF NOT EXISTS (SELECT 1 FROM [AttendingVetRelationships] WHERE [PetId] = @rocky AND [IsActive] = 1)
    INSERT INTO [AttendingVetRelationships] ([Id],[PetId],[VetUserId],[ClinicId],[EstablishedAt],[RevokedAt],[IsActive],[IsDeleted])
    VALUES (NEWID(), @rocky, @dr_mora_u, @clinic, @now, NULL, 1, 0);
DECLARE @kiwi uniqueidentifier = (SELECT TOP 1 [Id] FROM [Pets] WHERE [OwnerId] = @ana_vargas_u AND [Name] = N'Kiwi');
IF @kiwi IS NULL
BEGIN
    SET @kiwi = NEWID();
    INSERT INTO [Pets] ([Id],[OwnerId],[Name],[Species],[Breed],[BirthDate],[Status],[QrCode],[CreatedAt],[IsDeleted])
    VALUES (@kiwi, @ana_vargas_u, N'Kiwi', N'Ave', N'Perico australiano', '2023-02-10', N'Active', LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), '-', '')), @now, 0);
END
IF NOT EXISTS (SELECT 1 FROM [ClinicalRecords] WHERE [PetId] = @kiwi)
    INSERT INTO [ClinicalRecords] ([Id],[PetId],[CreatedAt],[IsDeleted]) VALUES (NEWID(), @kiwi, @now, 0);
DECLARE @kiwi_rec uniqueidentifier = (SELECT TOP 1 [Id] FROM [ClinicalRecords] WHERE [PetId] = @kiwi);

-- ---------- Consultas (insertadas en orden distinto al cronologico) ----------
IF NOT EXISTS (SELECT 1 FROM [Consultations] WHERE [ClinicalRecordId] = @firulais_rec AND [Notes] = N'Vacunación anual (rabia y polivalente). Paciente en buen estado general, 18 kg.')
BEGIN
    DECLARE @c0 uniqueidentifier = NEWID();
    INSERT INTO [Consultations] ([Id],[ClinicalRecordId],[VetUserId],[AttendanceDate],[RegistrationDate],[Notes],[Instructions],[Status],[SourceType],[IsDeleted])
    VALUES (@c0, @firulais_rec, @dra_solis_u, '2026-01-05T09:30:00-06:00', DATEADD(hour, -7000, @now), N'Vacunación anual (rabia y polivalente). Paciente en buen estado general, 18 kg.', N'Próxima vacuna en 12 meses.', N'Confirmed', N'Routine', 0);
END
IF NOT EXISTS (SELECT 1 FROM [Consultations] WHERE [ClinicalRecordId] = @firulais_rec AND [Notes] = N'Dermatitis alérgica en abdomen y patas. Prurito moderado.')
BEGIN
    DECLARE @c1 uniqueidentifier = NEWID();
    INSERT INTO [Consultations] ([Id],[ClinicalRecordId],[VetUserId],[AttendanceDate],[RegistrationDate],[Notes],[Instructions],[Status],[SourceType],[IsDeleted])
    VALUES (@c1, @firulais_rec, @dra_solis_u, '2026-05-12T11:15:00-06:00', DATEADD(hour, -4000, @now), N'Dermatitis alérgica en abdomen y patas. Prurito moderado.', N'Baño medicado semanal por 4 semanas.', N'Confirmed', N'Routine', 0);
    INSERT INTO [Prescriptions] ([Id],[ConsultationId],[VetUserId],[Medication],[Dosage],[Route],[Frequency],[DurationDays],[CreatedAt],[IsDeleted])
    VALUES (NEWID(), @c1, @dra_solis_u, N'Apoquel', N'16 mg', N'Oral', N'Cada 12 horas', 14, DATEADD(hour, -4000, @now), 0);
    INSERT INTO [Addenda] ([Id],[ConsultationId],[AuthorUserId],[Reason],[Content],[CreatedAt],[IsDeleted])
    VALUES (NEWID(), @c1, @dra_solis_u, N'Aclaración', N'Evitar paseos en zonas de pasto alto durante el tratamiento.', DATEADD(hour, -3976, @now), 0);
END
IF NOT EXISTS (SELECT 1 FROM [Consultations] WHERE [ClinicalRecordId] = @firulais_rec AND [Notes] = N'Control dental: sarro moderado en premolares (registro inicial).')
BEGIN
    DECLARE @c2 uniqueidentifier = NEWID();
    INSERT INTO [Consultations] ([Id],[ClinicalRecordId],[VetUserId],[AttendanceDate],[RegistrationDate],[Notes],[Instructions],[Status],[SourceType],[IsDeleted])
    VALUES (@c2, @firulais_rec, @dra_solis_u, '2026-09-20T15:00:00-06:00', DATEADD(hour, -1000, @now), N'Control dental: sarro moderado en premolares (registro inicial).', N'Programar limpieza dental.', N'Confirmed', N'Routine', 0);
END
IF NOT EXISTS (SELECT 1 FROM [Consultations] WHERE [ClinicalRecordId] = @firulais_rec AND [Notes] = N'Control dental: radiografía sin hallazgos de fractura (mismo día, registrado después).')
BEGIN
    DECLARE @c3 uniqueidentifier = NEWID();
    INSERT INTO [Consultations] ([Id],[ClinicalRecordId],[VetUserId],[AttendanceDate],[RegistrationDate],[Notes],[Instructions],[Status],[SourceType],[IsDeleted])
    VALUES (@c3, @firulais_rec, @dra_solis_u, '2026-09-20T15:00:00-06:00', DATEADD(hour, -999, @now), N'Control dental: radiografía sin hallazgos de fractura (mismo día, registrado después).', NULL, N'Confirmed', N'Routine', 0);
    INSERT INTO [Prescriptions] ([Id],[ConsultationId],[VetUserId],[Medication],[Dosage],[Route],[Frequency],[DurationDays],[CreatedAt],[IsDeleted])
    VALUES (NEWID(), @c3, @dra_solis_u, N'Clorhexidina gel', N'Aplicación tópica', N'Bucal', N'Cada 24 horas', 10, DATEADD(hour, -999, @now), 0);
END
IF NOT EXISTS (SELECT 1 FROM [Consultations] WHERE [ClinicalRecordId] = @rocky_rec AND [Notes] = N'Cojera en miembro posterior derecho tras ejercicio.')
BEGIN
    DECLARE @c4 uniqueidentifier = NEWID();
    INSERT INTO [Consultations] ([Id],[ClinicalRecordId],[VetUserId],[AttendanceDate],[RegistrationDate],[Notes],[Instructions],[Status],[SourceType],[IsDeleted])
    VALUES (@c4, @rocky_rec, @dr_mora_u, '2026-03-02T10:00:00-06:00', DATEADD(hour, -5500, @now), N'Cojera en miembro posterior derecho tras ejercicio.', N'Reposo relativo 10 días.', N'Confirmed', N'Routine', 0);
    INSERT INTO [Prescriptions] ([Id],[ConsultationId],[VetUserId],[Medication],[Dosage],[Route],[Frequency],[DurationDays],[CreatedAt],[IsDeleted])
    VALUES (NEWID(), @c4, @dr_mora_u, N'Carprofeno', N'75 mg', N'Oral', N'Cada 24 horas', 7, DATEADD(hour, -5500, @now), 0);
END
IF NOT EXISTS (SELECT 1 FROM [Consultations] WHERE [ClinicalRecordId] = @rocky_rec AND [Notes] = N'Control de cojera: evolución favorable, sin dolor a la palpación.')
BEGIN
    DECLARE @c5 uniqueidentifier = NEWID();
    INSERT INTO [Consultations] ([Id],[ClinicalRecordId],[VetUserId],[AttendanceDate],[RegistrationDate],[Notes],[Instructions],[Status],[SourceType],[IsDeleted])
    VALUES (@c5, @rocky_rec, @dr_mora_u, '2026-10-18T16:30:00-06:00', DATEADD(hour, -600, @now), N'Control de cojera: evolución favorable, sin dolor a la palpación.', N'Retomar actividad física gradualmente.', N'Confirmed', N'Routine', 0);
END

COMMIT TRANSACTION;

-- Resumen
SELECT (SELECT COUNT(*) FROM [Users] WHERE [Email] LIKE N'%@vetdemo.test') AS CuentasPrueba,
       (SELECT COUNT(*) FROM [Pets]) AS Mascotas,
       (SELECT COUNT(*) FROM [Consultations]) AS Consultas,
       (SELECT COUNT(*) FROM [Prescriptions]) AS Prescripciones;
