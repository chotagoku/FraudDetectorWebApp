USE FraudDetectorApp
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SystemConfigurations' AND xtype='U')
BEGIN
    CREATE TABLE [SystemConfigurations] (
        [Id] int NOT NULL IDENTITY,
        [Key] nvarchar(200) NOT NULL,
        [Value] nvarchar(max) NOT NULL,
        [Category] nvarchar(100) NOT NULL,
        [DataType] nvarchar(50) NOT NULL,
        [Description] nvarchar(1000) NOT NULL,
        [DisplayName] nvarchar(200) NOT NULL,
        [IsReadOnly] bit NOT NULL,
        [RequiresRestart] bit NOT NULL,
        [IsAdvanced] bit NOT NULL,
        [ValidationRules] nvarchar(max) NULL,
        [DefaultValue] nvarchar(max) NULL,
        [AllowedValues] nvarchar(max) NULL,
        [Section] nvarchar(100) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NULL DEFAULT (GETUTCDATE()),
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_SystemConfigurations] PRIMARY KEY ([Id])
    );

    CREATE UNIQUE INDEX [IX_SystemConfigurations_Key] ON [SystemConfigurations] ([Key]);
    CREATE INDEX [IX_SystemConfigurations_Category] ON [SystemConfigurations] ([Category]);
    CREATE INDEX [IX_SystemConfigurations_Section] ON [SystemConfigurations] ([Section]);
    CREATE INDEX [IX_SystemConfigurations_IsReadOnly] ON [SystemConfigurations] ([IsReadOnly]);
    CREATE INDEX [IX_SystemConfigurations_RequiresRestart] ON [SystemConfigurations] ([RequiresRestart]);
    CREATE INDEX [IX_SystemConfigurations_IsAdvanced] ON [SystemConfigurations] ([IsAdvanced]);
    CREATE INDEX [IX_SystemConfigurations_UpdatedAt] ON [SystemConfigurations] ([UpdatedAt]);
    CREATE INDEX [IX_SystemConfigurations_Category_Section] ON [SystemConfigurations] ([Category], [Section]);
    CREATE INDEX [IX_SystemConfigurations_IsReadOnly_RequiresRestart] ON [SystemConfigurations] ([IsReadOnly], [RequiresRestart]);

    PRINT 'SystemConfigurations table created successfully';
END
ELSE
BEGIN
    PRINT 'SystemConfigurations table already exists';
END
