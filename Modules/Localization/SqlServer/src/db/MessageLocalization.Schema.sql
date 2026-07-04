/*
 * RaccoonLand MessageLocalization - SQL Server schema.
 *
 * In a microservice environment a single, shared database holds every service's localizations.
 * Apply this script once against that shared database (it is idempotent and safe to re-run).
 *
 * Hierarchy: Services -> Applications -> MessageLocalizations
 *   - Services           : one row per microservice.
 *   - Applications        : logical applications that belong to a service.
 *   - MessageLocalizations: the (Key, Culture) -> Value entries an admin maintains at runtime.
 */

IF SCHEMA_ID(N'Localization') IS NULL
    EXEC (N'CREATE SCHEMA [Localization]');
GO

IF OBJECT_ID(N'[Localization].[Services]', N'U') IS NULL
BEGIN
    CREATE TABLE [Localization].[Services]
    (
        [Id]           INT            IDENTITY (1, 1) NOT NULL,
        [Name]         NVARCHAR (128) NOT NULL,
        [CreatedOnUtc] DATETIME2 (7)  NOT NULL CONSTRAINT [DF_Services_CreatedOnUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Services] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UX_Services_Name] UNIQUE ([Name])
    );
END
GO

IF OBJECT_ID(N'[Localization].[Applications]', N'U') IS NULL
BEGIN
    CREATE TABLE [Localization].[Applications]
    (
        [Id]           INT            IDENTITY (1, 1) NOT NULL,
        [ServiceId]    INT            NOT NULL,
        [Name]         NVARCHAR (128) NOT NULL,
        [CreatedOnUtc] DATETIME2 (7)  NOT NULL CONSTRAINT [DF_Applications_CreatedOnUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Applications] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Applications_Services] FOREIGN KEY ([ServiceId]) REFERENCES [Localization].[Services] ([Id]),
        CONSTRAINT [UX_Applications_Service_Name] UNIQUE ([ServiceId], [Name])
    );
END
GO

IF OBJECT_ID(N'[Localization].[MessageLocalizations]', N'U') IS NULL
BEGIN
    CREATE TABLE [Localization].[MessageLocalizations]
    (
        [Id]                  BIGINT         IDENTITY (1, 1) NOT NULL,
        [ApplicationId]       INT            NOT NULL,
        [Key]                 NVARCHAR (256) NOT NULL,
        [Culture]             NVARCHAR (16)  NOT NULL,
        [Value]               NVARCHAR (MAX) NOT NULL,
        -- Set to 1 for entries auto-created on a cache miss (Value defaults to the key itself).
        -- It signals an admin that a real translation still has to be provided.
        [RequiresTranslation] BIT            NOT NULL CONSTRAINT [DF_ML_RequiresTranslation] DEFAULT (0),
        [CreatedOnUtc]        DATETIME2 (7)  NOT NULL CONSTRAINT [DF_ML_CreatedOnUtc] DEFAULT (SYSUTCDATETIME()),
        [ModifiedOnUtc]       DATETIME2 (7)  NULL,
        CONSTRAINT [PK_MessageLocalizations] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_ML_Applications] FOREIGN KEY ([ApplicationId]) REFERENCES [Localization].[Applications] ([Id]),
        CONSTRAINT [UX_ML_Application_Key_Culture] UNIQUE ([ApplicationId], [Key], [Culture])
    );
END
GO
