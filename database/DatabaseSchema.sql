-- VetPlatform: esquema para SQL Server (Somee). Idempotente: se puede ejecutar mas de una vez.
-- Generado con: dotnet ef migrations script --idempotent
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [Appointments] (
        [Id] uniqueidentifier NOT NULL,
        [PetId] uniqueidentifier NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [ClinicId] uniqueidentifier NOT NULL,
        [VetUserId] uniqueidentifier NOT NULL,
        [SlotId] uniqueidentifier NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Appointments] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [AttendingVetRelationships] (
        [Id] uniqueidentifier NOT NULL,
        [PetId] uniqueidentifier NOT NULL,
        [VetUserId] uniqueidentifier NOT NULL,
        [ClinicId] uniqueidentifier NULL,
        [EstablishedAt] datetimeoffset NOT NULL,
        [RevokedAt] datetimeoffset NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_AttendingVetRelationships] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] uniqueidentifier NOT NULL,
        [ActorUserId] uniqueidentifier NOT NULL,
        [Action] nvarchar(30) NOT NULL,
        [ResourceType] nvarchar(450) NOT NULL,
        [ResourceId] nvarchar(450) NOT NULL,
        [OrganizationId] uniqueidentifier NULL,
        [Result] nvarchar(max) NOT NULL,
        [Timestamp] datetimeoffset NOT NULL,
        [Context] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [AvailabilitySlots] (
        [Id] uniqueidentifier NOT NULL,
        [ClinicId] uniqueidentifier NOT NULL,
        [VetUserId] uniqueidentifier NOT NULL,
        [StartTime] datetimeoffset NOT NULL,
        [EndTime] datetimeoffset NOT NULL,
        [IsBooked] bit NOT NULL,
        [Version] int NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_AvailabilitySlots] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [Clinics] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Status] int NOT NULL,
        [RepresentativeUserId] uniqueidentifier NOT NULL,
        [Address] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Clinics] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [EmergencyAccesses] (
        [Id] uniqueidentifier NOT NULL,
        [PetId] uniqueidentifier NOT NULL,
        [VetUserId] uniqueidentifier NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [GrantedAt] datetimeoffset NOT NULL,
        [ExpiresAt] datetimeoffset NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [RevokedAt] datetimeoffset NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_EmergencyAccesses] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [EmergencyAttendances] (
        [Id] uniqueidentifier NOT NULL,
        [EmergencyAccessId] uniqueidentifier NOT NULL,
        [ClinicalRecordId] uniqueidentifier NOT NULL,
        [VetUserId] uniqueidentifier NOT NULL,
        [AttendanceDate] datetimeoffset NOT NULL,
        [RegistrationDate] datetimeoffset NOT NULL,
        [Findings] nvarchar(max) NOT NULL,
        [Actions] nvarchar(max) NOT NULL,
        [Instructions] nvarchar(max) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_EmergencyAttendances] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [InventoryMovements] (
        [Id] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [Quantity] int NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_InventoryMovements] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] uniqueidentifier NOT NULL,
        [RecipientUserId] uniqueidentifier NOT NULL,
        [Subject] nvarchar(max) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [SentAt] datetimeoffset NULL,
        [RetryCount] int NOT NULL,
        [RelatedEntityId] uniqueidentifier NULL,
        [RelatedEntityType] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [Orders] (
        [Id] uniqueidentifier NOT NULL,
        [BuyerUserId] uniqueidentifier NOT NULL,
        [StoreId] uniqueidentifier NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        [Notes] nvarchar(max) NULL,
        [IdempotencyKey] nvarchar(100) NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [OwnerProfiles] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [IdentificationNumber] nvarchar(20) NOT NULL,
        [Phone] nvarchar(20) NOT NULL,
        [Address] nvarchar(300) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_OwnerProfiles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [Pets] (
        [Id] uniqueidentifier NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Species] nvarchar(50) NOT NULL,
        [Breed] nvarchar(max) NULL,
        [BirthDate] date NULL,
        [Status] nvarchar(20) NOT NULL,
        [QrCode] nvarchar(64) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Pets] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] uniqueidentifier NOT NULL,
        [StoreId] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(max) NULL,
        [Price] decimal(12,2) NOT NULL,
        [IsPublished] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        [StockOnHand] int NOT NULL,
        [ReservedStock] int NOT NULL,
        [Version] int NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [Stores] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Status] int NOT NULL,
        [OperatorUserId] uniqueidentifier NOT NULL,
        [Address] nvarchar(max) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Stores] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [FullName] nvarchar(200) NOT NULL,
        [IsVerified] bit NOT NULL,
        [IsSuspended] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [ClinicMembers] (
        [Id] uniqueidentifier NOT NULL,
        [ClinicId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [JoinedAt] datetimeoffset NOT NULL,
        [LeftAt] datetimeoffset NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_ClinicMembers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ClinicMembers_Clinics_ClinicId] FOREIGN KEY ([ClinicId]) REFERENCES [Clinics] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [OrderItems] (
        [Id] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [ProductName] nvarchar(max) NOT NULL,
        [UnitPrice] decimal(12,2) NOT NULL,
        [Quantity] int NOT NULL,
        [ReservedQuantity] int NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [ClinicalRecords] (
        [Id] uniqueidentifier NOT NULL,
        [PetId] uniqueidentifier NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_ClinicalRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ClinicalRecords_Pets_PetId] FOREIGN KEY ([PetId]) REFERENCES [Pets] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [UserRoleAssignments] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Role] nvarchar(30) NOT NULL,
        [OrganizationId] uniqueidentifier NULL,
        [GrantedAt] datetimeoffset NOT NULL,
        [RevokedAt] datetimeoffset NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_UserRoleAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserRoleAssignments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [Consultations] (
        [Id] uniqueidentifier NOT NULL,
        [ClinicalRecordId] uniqueidentifier NOT NULL,
        [VetUserId] uniqueidentifier NOT NULL,
        [AttendanceDate] datetimeoffset NOT NULL,
        [RegistrationDate] datetimeoffset NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [Instructions] nvarchar(max) NULL,
        [Status] nvarchar(20) NOT NULL,
        [SourceType] nvarchar(20) NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Consultations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Consultations_ClinicalRecords_ClinicalRecordId] FOREIGN KEY ([ClinicalRecordId]) REFERENCES [ClinicalRecords] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [Addenda] (
        [Id] uniqueidentifier NOT NULL,
        [ConsultationId] uniqueidentifier NOT NULL,
        [AuthorUserId] uniqueidentifier NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Addenda] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Addenda_Consultations_ConsultationId] FOREIGN KEY ([ConsultationId]) REFERENCES [Consultations] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [Prescriptions] (
        [Id] uniqueidentifier NOT NULL,
        [ConsultationId] uniqueidentifier NOT NULL,
        [VetUserId] uniqueidentifier NOT NULL,
        [Medication] nvarchar(200) NOT NULL,
        [Dosage] nvarchar(100) NOT NULL,
        [Route] nvarchar(50) NOT NULL,
        [Frequency] nvarchar(100) NOT NULL,
        [DurationDays] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Prescriptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Prescriptions_Consultations_ConsultationId] FOREIGN KEY ([ConsultationId]) REFERENCES [Consultations] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE TABLE [PrescriptionAddenda] (
        [Id] uniqueidentifier NOT NULL,
        [PrescriptionId] uniqueidentifier NOT NULL,
        [AuthorUserId] uniqueidentifier NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_PrescriptionAddenda] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PrescriptionAddenda_Prescriptions_PrescriptionId] FOREIGN KEY ([PrescriptionId]) REFERENCES [Prescriptions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Addenda_ConsultationId] ON [Addenda] ([ConsultationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Appointments_PetId] ON [Appointments] ([PetId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Appointments_SlotId] ON [Appointments] ([SlotId]) WHERE [Status] = ''Booked''');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AttendingVetRelationships_PetId] ON [AttendingVetRelationships] ([PetId]) WHERE [IsActive] = 1');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_ResourceType_ResourceId_Timestamp] ON [AuditLogs] ([ResourceType], [ResourceId], [Timestamp]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AvailabilitySlots_VetUserId_StartTime] ON [AvailabilitySlots] ([VetUserId], [StartTime]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ClinicalRecords_PetId] ON [ClinicalRecords] ([PetId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ClinicMembers_ClinicId_UserId] ON [ClinicMembers] ([ClinicId], [UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Consultations_ClinicalRecordId] ON [Consultations] ([ClinicalRecordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmergencyAccesses_PetId_VetUserId_Status] ON [EmergencyAccesses] ([PetId], [VetUserId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Notifications_Status_CreatedAt] ON [Notifications] ([Status], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_BuyerUserId_IdempotencyKey] ON [Orders] ([BuyerUserId], [IdempotencyKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_OwnerProfiles_IdentificationNumber] ON [OwnerProfiles] ([IdentificationNumber]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_OwnerProfiles_UserId] ON [OwnerProfiles] ([UserId]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Pets_OwnerId] ON [Pets] ([OwnerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Pets_QrCode] ON [Pets] ([QrCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PrescriptionAddenda_PrescriptionId] ON [PrescriptionAddenda] ([PrescriptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Prescriptions_ConsultationId] ON [Prescriptions] ([ConsultationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_StoreId] ON [Products] ([StoreId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserRoleAssignments_UserId] ON [UserRoleAssignments] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113210941_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261113210941_InitialCreate', N'8.0.8');
END;
GO

COMMIT;
GO

