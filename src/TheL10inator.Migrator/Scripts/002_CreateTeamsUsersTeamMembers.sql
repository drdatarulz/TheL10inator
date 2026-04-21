-- 002_CreateTeamsUsersTeamMembers.sql
-- Creates the identity and membership tables exercised by the first vertical slice (L10-2).
-- Every entity carries a DeletedAtUtc soft-delete column and filtered unique indexes exclude
-- soft-deleted rows so an email can be re-invited after a prior member was removed.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Teams')
BEGIN
    CREATE TABLE dbo.Teams (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Teams PRIMARY KEY,
        Name            NVARCHAR(200) NOT NULL,
        CreatedAtUtc    DATETIME2 NOT NULL CONSTRAINT DF_Teams_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        DeletedAtUtc    DATETIME2 NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE dbo.Users (
        Id                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        AzureAdObjectId    NVARCHAR(100) NULL,
        Email              NVARCHAR(320) NOT NULL,
        DisplayName        NVARCHAR(200) NULL,
        InvitedAtUtc       DATETIME2 NOT NULL CONSTRAINT DF_Users_InvitedAtUtc DEFAULT SYSUTCDATETIME(),
        LastLoginAtUtc     DATETIME2 NULL,
        CreatedAtUtc       DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        DeletedAtUtc       DATETIME2 NULL
    );
    CREATE UNIQUE INDEX UQ_Users_Email
        ON dbo.Users (Email) WHERE DeletedAtUtc IS NULL;
    CREATE UNIQUE INDEX UQ_Users_AzureAdObjectId
        ON dbo.Users (AzureAdObjectId) WHERE AzureAdObjectId IS NOT NULL AND DeletedAtUtc IS NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TeamMembers')
BEGIN
    CREATE TABLE dbo.TeamMembers (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TeamMembers PRIMARY KEY,
        TeamId          INT NOT NULL CONSTRAINT FK_TeamMembers_Teams REFERENCES dbo.Teams(Id),
        UserId          INT NOT NULL CONSTRAINT FK_TeamMembers_Users REFERENCES dbo.Users(Id),
        Role            NVARCHAR(20) NOT NULL
                        CONSTRAINT CK_TeamMembers_Role CHECK (Role IN ('Member','Admin')),
        JoinedAtUtc     DATETIME2 NOT NULL CONSTRAINT DF_TeamMembers_JoinedAtUtc DEFAULT SYSUTCDATETIME(),
        CreatedAtUtc    DATETIME2 NOT NULL CONSTRAINT DF_TeamMembers_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        DeletedAtUtc    DATETIME2 NULL
    );
    CREATE UNIQUE INDEX UQ_TeamMembers_TeamId_UserId
        ON dbo.TeamMembers (TeamId, UserId) WHERE DeletedAtUtc IS NULL;
    CREATE INDEX IX_TeamMembers_UserId_Role
        ON dbo.TeamMembers (UserId, Role) WHERE DeletedAtUtc IS NULL;
END;
