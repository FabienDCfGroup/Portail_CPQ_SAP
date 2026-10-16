BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015093447_Langues'
)
BEGIN
    CREATE TABLE [Langues] (
        [Code] nvarchar(5) NOT NULL,
        [Libelle] nvarchar(50) NOT NULL,
        [Actif] bit NOT NULL,
        [Ordre] int NOT NULL,
        CONSTRAINT [PK_Langues] PRIMARY KEY ([Code])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015093447_Langues'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Code', N'Actif', N'Libelle', N'Ordre') AND [object_id] = OBJECT_ID(N'[Langues]'))
        SET IDENTITY_INSERT [Langues] ON;
    EXEC(N'INSERT INTO [Langues] ([Code], [Actif], [Libelle], [Ordre])
    VALUES (N''de'', CAST(1 AS bit), N''Deutsch'', 3),
    (N''en'', CAST(1 AS bit), N''English'', 2),
    (N''fr'', CAST(1 AS bit), N''Français'', 1),
    (N''nl'', CAST(1 AS bit), N''Nederlands'', 4),
    (N''pt'', CAST(0 AS bit), N''Português'', 5)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Code', N'Actif', N'Libelle', N'Ordre') AND [object_id] = OBJECT_ID(N'[Langues]'))
        SET IDENTITY_INSERT [Langues] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015093447_Langues'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261015093447_Langues', N'8.0.11');
END;
GO

COMMIT;
GO

