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

CREATE TABLE [Categorias] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_Categorias] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Clientes] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(80) NOT NULL,
    [Apellido] nvarchar(80) NOT NULL,
    [Cedula] nvarchar(20) NOT NULL,
    [Telefono] nvarchar(20) NOT NULL,
    [Correo] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Clientes] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Guias] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(100) NOT NULL,
    [Telefono] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_Guias] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [MetodosPago] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_MetodosPago] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Paises] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_Paises] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Transportes] (
    [Id] int NOT NULL IDENTITY,
    [Tipo] nvarchar(50) NOT NULL,
    [Descripcion] nvarchar(150) NOT NULL,
    CONSTRAINT [PK_Transportes] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Usuarios] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(100) NOT NULL,
    [Correo] nvarchar(max) NOT NULL,
    [Contrasena] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Destinos] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(100) NOT NULL,
    [DuracionDias] int NOT NULL,
    [DuracionHoras] int NOT NULL,
    [PaisId] int NOT NULL,
    CONSTRAINT [PK_Destinos] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Destinos_Paises_PaisId] FOREIGN KEY ([PaisId]) REFERENCES [Paises] ([Id])
);
GO

CREATE TABLE [Tours] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(150) NOT NULL,
    [PaisId] int NOT NULL,
    [DestinoId] int NOT NULL,
    [CategoriaId] int NOT NULL,
    [GuiaId] int NOT NULL,
    [TransporteId] int NOT NULL,
    [Fecha] datetime2 NOT NULL,
    [Hora] time NOT NULL,
    [Precio] decimal(18,2) NOT NULL,
    [Itbis] decimal(18,2) NOT NULL,
    [DuracionDias] int NOT NULL,
    [DuracionHoras] int NOT NULL,
    [FechaFin] datetime2 NOT NULL,
    [Estado] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_Tours] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Tours_Categorias_CategoriaId] FOREIGN KEY ([CategoriaId]) REFERENCES [Categorias] ([Id]),
    CONSTRAINT [FK_Tours_Destinos_DestinoId] FOREIGN KEY ([DestinoId]) REFERENCES [Destinos] ([Id]),
    CONSTRAINT [FK_Tours_Guias_GuiaId] FOREIGN KEY ([GuiaId]) REFERENCES [Guias] ([Id]),
    CONSTRAINT [FK_Tours_Paises_PaisId] FOREIGN KEY ([PaisId]) REFERENCES [Paises] ([Id]),
    CONSTRAINT [FK_Tours_Transportes_TransporteId] FOREIGN KEY ([TransporteId]) REFERENCES [Transportes] ([Id])
);
GO

CREATE TABLE [Reservas] (
    [Id] int NOT NULL IDENTITY,
    [ClienteId] int NOT NULL,
    [TourId] int NOT NULL,
    [MetodoPagoId] int NOT NULL,
    [FechaReserva] datetime2 NOT NULL,
    [CantidadPersonas] int NOT NULL,
    [Total] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_Reservas] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Reservas_Clientes_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]),
    CONSTRAINT [FK_Reservas_MetodosPago_MetodoPagoId] FOREIGN KEY ([MetodoPagoId]) REFERENCES [MetodosPago] ([Id]),
    CONSTRAINT [FK_Reservas_Tours_TourId] FOREIGN KEY ([TourId]) REFERENCES [Tours] ([Id])
);
GO

CREATE INDEX [IX_Destinos_PaisId] ON [Destinos] ([PaisId]);
GO

CREATE INDEX [IX_Reservas_ClienteId] ON [Reservas] ([ClienteId]);
GO

CREATE INDEX [IX_Reservas_MetodoPagoId] ON [Reservas] ([MetodoPagoId]);
GO

CREATE INDEX [IX_Reservas_TourId] ON [Reservas] ([TourId]);
GO

CREATE INDEX [IX_Tours_CategoriaId] ON [Tours] ([CategoriaId]);
GO

CREATE INDEX [IX_Tours_DestinoId] ON [Tours] ([DestinoId]);
GO

CREATE INDEX [IX_Tours_GuiaId] ON [Tours] ([GuiaId]);
GO

CREATE INDEX [IX_Tours_PaisId] ON [Tours] ([PaisId]);
GO

CREATE INDEX [IX_Tours_TransporteId] ON [Tours] ([TransporteId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260716032709_AgenciaToursDB', N'8.0.28');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

EXEC sp_rename N'[Usuarios].[Contrasena]', N'PasswordHash', N'COLUMN';
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Usuarios]') AND [c].[name] = N'Correo');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Usuarios] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Usuarios] ALTER COLUMN [Correo] nvarchar(150) NOT NULL;
GO

CREATE UNIQUE INDEX [IX_Usuarios_Correo] ON [Usuarios] ([Correo]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260728202105_AgregarPasswordHashAUsuario', N'8.0.28');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Clientes]') AND [c].[name] = N'Correo');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Clientes] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Clientes] ALTER COLUMN [Correo] nvarchar(450) NOT NULL;
GO

CREATE UNIQUE INDEX [IX_Clientes_Cedula] ON [Clientes] ([Cedula]);
GO

CREATE UNIQUE INDEX [IX_Clientes_Correo] ON [Clientes] ([Correo]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260801145025_AgregarIndicesUnicosCliente', N'8.0.28');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE UNIQUE INDEX [IX_Guias_Telefono] ON [Guias] ([Telefono]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260804203840_AgregarTelefonoUnicoGuia', N'8.0.28');
GO

COMMIT;
GO

