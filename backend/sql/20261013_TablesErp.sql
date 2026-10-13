BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013113032_TablesErp'
)
BEGIN
    IF SCHEMA_ID(N'erp') IS NULL EXEC(N'CREATE SCHEMA [erp];');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013113032_TablesErp'
)
BEGIN
    CREATE TABLE [erp].[AgencesRetrait] (
        [CodeAgence] nvarchar(20) NOT NULL,
        [Libelle] nvarchar(150) NOT NULL,
        [Region] nvarchar(100) NULL,
        [Localisation] nvarchar(250) NULL,
        [Actif] bit NOT NULL DEFAULT CAST(1 AS bit),
        [DateMaj] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
        CONSTRAINT [PK_AgencesRetrait] PRIMARY KEY ([CodeAgence])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013113032_TablesErp'
)
BEGIN
    CREATE TABLE [erp].[Clients] (
        [CodeClient] nvarchar(20) NOT NULL,
        [RaisonSociale] nvarchar(200) NOT NULL,
        [Pays] nvarchar(100) NOT NULL,
        [Langue] nvarchar(5) NULL,
        [ModePaiement] nvarchar(50) NULL,
        [EstVic] bit NOT NULL DEFAULT CAST(0 AS bit),
        [Actif] bit NOT NULL DEFAULT CAST(1 AS bit),
        [DateMaj] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
        CONSTRAINT [PK_Clients] PRIMARY KEY ([CodeClient])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013113032_TablesErp'
)
BEGIN
    CREATE TABLE [erp].[ModesLivraison] (
        [Code] nvarchar(20) NOT NULL,
        [Libelle] nvarchar(100) NOT NULL,
        [Actif] bit NOT NULL DEFAULT CAST(1 AS bit),
        [DateMaj] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
        CONSTRAINT [PK_ModesLivraison] PRIMARY KEY ([Code])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013113032_TablesErp'
)
BEGIN
    CREATE TABLE [erp].[AdressesClients] (
        [IdAdresse] nvarchar(50) NOT NULL,
        [CodeClient] nvarchar(20) NOT NULL,
        [Libelle] nvarchar(100) NOT NULL,
        [EstPrincipale] bit NOT NULL DEFAULT CAST(0 AS bit),
        [NomDestinataire] nvarchar(200) NOT NULL,
        [Telephone] nvarchar(30) NULL,
        [Adresse] nvarchar(250) NOT NULL,
        [ComplementAdresse] nvarchar(250) NULL,
        [CodePostal] nvarchar(20) NOT NULL,
        [Ville] nvarchar(100) NOT NULL,
        [Pays] nvarchar(100) NOT NULL,
        [DateMaj] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
        CONSTRAINT [PK_AdressesClients] PRIMARY KEY ([IdAdresse]),
        CONSTRAINT [FK_AdressesClients_Clients_CodeClient] FOREIGN KEY ([CodeClient]) REFERENCES [erp].[Clients] ([CodeClient]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013113032_TablesErp'
)
BEGIN
    CREATE TABLE [erp].[Commandes] (
        [NumeroCommande] nvarchar(30) NOT NULL,
        [CodeClient] nvarchar(20) NOT NULL,
        [ReferenceCommandeClient] nvarchar(100) NULL,
        [ReferencePortail] nvarchar(30) NULL,
        [ConfigId] nvarchar(100) NULL,
        [DateCreation] datetime2 NOT NULL,
        [StatutErp] nvarchar(50) NOT NULL,
        [DateDebutProduction] date NULL,
        [DateFinProduction] date NULL,
        [DateLivraisonPrevue] date NULL,
        [DateLivraisonReelle] date NULL,
        [DateMaj] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
        CONSTRAINT [PK_Commandes] PRIMARY KEY ([NumeroCommande]),
        CONSTRAINT [FK_Commandes_Clients_CodeClient] FOREIGN KEY ([CodeClient]) REFERENCES [erp].[Clients] ([CodeClient]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013113032_TablesErp'
)
BEGIN
    CREATE INDEX [IX_AdressesClients_CodeClient] ON [erp].[AdressesClients] ([CodeClient]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013113032_TablesErp'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_AdressesClients_AdressePrincipale] ON [erp].[AdressesClients] ([CodeClient]) WHERE [EstPrincipale] = 1');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013113032_TablesErp'
)
BEGIN
    CREATE INDEX [IX_Clients_RaisonSociale] ON [erp].[Clients] ([RaisonSociale]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013113032_TablesErp'
)
BEGIN
    CREATE INDEX [IX_Commandes_CodeClient] ON [erp].[Commandes] ([CodeClient]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013113032_TablesErp'
)
BEGIN
    CREATE INDEX [IX_Commandes_ConfigId] ON [erp].[Commandes] ([ConfigId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013113032_TablesErp'
)
BEGIN
    CREATE INDEX [IX_Commandes_DateCreation] ON [erp].[Commandes] ([DateCreation]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013113032_TablesErp'
)
BEGIN
    CREATE INDEX [IX_Commandes_ReferencePortail] ON [erp].[Commandes] ([ReferencePortail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013113032_TablesErp'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013113032_TablesErp', N'8.0.11');
END;
GO

COMMIT;
GO

