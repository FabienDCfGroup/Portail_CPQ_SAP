BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015095232_Authentification'
)
BEGIN
    DROP TABLE [Utilisateurs];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015095232_Authentification'
)
BEGIN
    CREATE TABLE [JournalConnexions] (
        [Id] bigint NOT NULL IDENTITY,
        [Date] datetime2 NOT NULL,
        [Resultat] nvarchar(30) NOT NULL,
        [ObjetId] nvarchar(50) NULL,
        [Compte] nvarchar(256) NULL,
        [Nom] nvarchar(200) NULL,
        [Role] nvarchar(30) NULL,
        [Message] nvarchar(500) NULL,
        [AdresseIp] nvarchar(64) NULL,
        [Navigateur] nvarchar(512) NULL,
        CONSTRAINT [PK_JournalConnexions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015095232_Authentification'
)
BEGIN
    CREATE INDEX [IX_JournalConnexions_Compte] ON [JournalConnexions] ([Compte]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015095232_Authentification'
)
BEGIN
    CREATE INDEX [IX_JournalConnexions_Date] ON [JournalConnexions] ([Date]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015095232_Authentification'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261015095232_Authentification', N'8.0.11');
END;
GO

COMMIT;
GO

