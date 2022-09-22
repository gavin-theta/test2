
IF EXISTS (select * from dbo.sysobjects where id = object_id(N'[dbo].[InsertWTPInstructionsAndACK]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
DROP PROCEDURE [dbo].[InsertWTPInstructionsAndACK]
GO

IF EXISTS (select * from dbo.sysobjects where id = object_id(N'[WTPApp].[InsertWTPInstructions]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
DROP PROCEDURE [WTPApp].[InsertWTPInstructions]
GO

IF type_id('[dbo].[WTPInstructionType]') IS NOT NULL
        DROP TYPE [dbo].[WTPInstructionType];

/****** Object:  UserDefinedTableType [dbo].[WTPInstructionType] ******/
CREATE TYPE [dbo].[WTPInstructionType] AS TABLE(
	[DispatchGroupName] [varchar](30) NOT NULL,
	[PrimaryDispatchTypeCode] [varchar](30) NOT NULL,
	[NodeCode] [varchar](30) NULL,
	[BlockCode] [varchar](30) NULL,
	[correlationId] [varchar](100) NOT NULL,
	[sequenceNumber] [int] NOT NULL,
	[dispatchEndpointOwner] [varchar](100) NOT NULL,
	[isUserResend] [int] NULL,
	[messageSentTime] [datetime] NULL,
	[traderCode] [varchar](30) NULL,
	[dispatchValue] [decimal](18, 2) NULL,
	[dispatchTime] [datetime] NULL,
	[dispatchIssueTime] [datetime] NULL,
	[AuditDateCreated] [datetime] NOT NULL,
	[AuditUserCreated] [varchar](100) NOT NULL,
	[AuditProcessCreated] [varchar](100) NOT NULL,
	[AuditDateUpdated] [datetime] NULL,
	[AuditUserUpdated] [varchar](100) NULL,
	[AuditProcessUpdated] [varchar](100) NULL
)
GO

IF EXISTS (select * from dbo.sysobjects where id = object_id(N'[dbo].[InsertWTPInstructionsAndACK]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
DROP PROCEDURE [dbo].[InsertWTPInstructionsAndACK]
GO

IF EXISTS (select * from dbo.sysobjects where id = object_id(N'[WTPApp].[InsertWTPAck]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
DROP PROCEDURE [WTPApp].[InsertWTPAck]
GO

IF type_id('[dbo].[WTPDispatchAckType]') IS NOT NULL
        DROP TYPE [dbo].[WTPDispatchAckType];

/****** Object:  UserDefinedTableType [dbo].[WTPDispatchAckType] ******/
CREATE TYPE [dbo].[WTPDispatchAckType] AS TABLE(
	[DispatchGroup] [varchar](30) NOT NULL,
	[ackType] [varchar](30) NULL,
	[sequenceNumber] [int] NOT NULL,
	[AuditDateCreated] [datetime] NOT NULL,
	[AuditUserCreated] [varchar](100) NOT NULL,
	[AuditProcessCreated] [varchar](100) NOT NULL,
	[AuditDateUpdated] [datetime] NULL,
	[AuditUserUpdated] [varchar](100) NULL,
	[AuditProcessUpdated] [varchar](100) NULL,
	[correlationId] [varchar](100) NULL
)
GO



