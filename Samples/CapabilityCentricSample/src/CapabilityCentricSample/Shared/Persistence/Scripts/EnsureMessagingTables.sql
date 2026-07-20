-- Messaging tables for CleanArchitectureSample (outbox relay).
-- Run against the Command database. Safe to re-run.

IF OBJECT_ID(N'dbo.OutboxEvent', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OutboxEvent
    (
        Id                   uniqueidentifier  NOT NULL CONSTRAINT PK_OutboxEvent PRIMARY KEY,
        Category             nvarchar(32)      NOT NULL,
        EventType            nvarchar(256)     NOT NULL,
        AggregateType        nvarchar(256)     NOT NULL,
        AggregateBusinessKey uniqueidentifier  NOT NULL,
        Payload              nvarchar(max)     NOT NULL,
        CreatedBy            nvarchar(256)     NULL,
        OccurredOnUtc        datetimeoffset    NOT NULL,
        CreatedOnUtc         datetimeoffset    NOT NULL,
        ClaimedOnUtc         datetimeoffset    NULL,
        ProcessedOnUtc       datetimeoffset    NULL
    );
END
GO

IF COL_LENGTH(N'dbo.OutboxEvent', N'ProcessedOnUtc') IS NULL
    ALTER TABLE dbo.OutboxEvent ADD ProcessedOnUtc datetimeoffset NULL;
GO

IF COL_LENGTH(N'dbo.OutboxEvent', N'ClaimedOnUtc') IS NULL
    ALTER TABLE dbo.OutboxEvent ADD ClaimedOnUtc datetimeoffset NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_OutboxEvent_Pending' AND object_id = OBJECT_ID(N'dbo.OutboxEvent'))
BEGIN
    CREATE INDEX IX_OutboxEvent_Pending
        ON dbo.OutboxEvent (CreatedOnUtc, Id)
        WHERE ProcessedOnUtc IS NULL;
END
GO

-- Optional: inbox for a RabbitMQ consuming host (not required for Domain-only relay).
IF OBJECT_ID(N'dbo.InboxEvent', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InboxEvent
    (
        EventId        uniqueidentifier  NOT NULL CONSTRAINT PK_InboxEvent PRIMARY KEY,
        EventType      nvarchar(256)     NOT NULL,
        ClaimedOnUtc   datetimeoffset    NULL,
        ProcessedOnUtc datetimeoffset    NULL,
        ReceivedOnUtc  datetimeoffset    NOT NULL
    );
END
GO
