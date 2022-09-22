

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[WTPApp].[WTPDispatchAck]') AND type in (N'U'))
ALTER TABLE [WTPApp].[WTPDispatchAck] ADD [correlationId] [varchar](100) NULL;
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[WTPApp].[WTPDispatchAck_Archive]') AND type in (N'U'))
ALTER TABLE [WTPApp].[WTPDispatchAck_Archive] ADD [correlationId] [varchar](100) NULL;
GO