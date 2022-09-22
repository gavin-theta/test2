/****** Object:  StoredProcedure [WTPApp].[GetDispatchInstructions]    Script Date: Tuesday, 30 Aug 2022  ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

DROP PROCEDURE IF EXISTS [WTPApp].[GetDispatchInstructions]
Go

CREATE OR ALTER PROCEDURE [WTPApp].[GetDispatchInstructions]
	@StartDate DateTime,
	@EndDate DateTime
AS
SET NOCOUNT ON
BEGIN
	BEGIN TRY
		SELECT 
			DI.[WTPDispatchInstructionID] AS DispatchInstructionID
			,DI.[sequenceNumber] AS SequenceNumber
			,DI.[PrimaryDispatchTypeCode] AS DispatchType
			,DT.[Description] AS DispatchDescription
			,DI.[BlockCode] AS [Block]
			,DI.[dispatchValue] AS DispatchValue
			,DI.[dispatchTime] AS DispatchTime
			,DI.[AuditDateCreated] AS Received
		FROM  [WTPApp].[WTPDispatchInstruction] DI
		INNER JOIN [WTPApp].[WTPPrimaryDispatchType] DT ON DT.[PrimaryDispatchTypeCode] = DI.[PrimaryDispatchTypeCode]
		WHERE DI.[AuditDateCreated] >= @StartDate and DI.[AuditDateCreated] <= DATEADD(day, 1, @EndDate)
		UNION
		SELECT 
			ADI.[WTPDispatchInstructionID] AS DispatchInstructionID
			,ADI.[sequenceNumber] AS SequenceNumber
			,ADI.[PrimaryDispatchTypeCode] AS DispatchType
			,DT.[Description] AS DispatchDescription
			,ADI.[BlockCode] AS [Block]
			,ADI.[dispatchValue] AS DispatchValue
			,ADI.[dispatchTime] AS DispatchTime
			,ADI.[AuditDateCreated] AS Received
		FROM  [WTPApp].[WTPDispatchInstruction_Archive] ADI
		INNER JOIN [WTPApp].[WTPPrimaryDispatchType] DT ON DT.[PrimaryDispatchTypeCode] = ADI.[PrimaryDispatchTypeCode]
		WHERE ADI.[AuditDateCreated] >= @StartDate and ADI.[AuditDateCreated] <= DATEADD(day, 1, @EndDate)

	END TRY
	BEGIN CATCH
		DECLARE @error INT, @message VARCHAR(4000);
        SELECT @error = ERROR_NUMBER(), @message = ERROR_MESSAGE();
		IF @@TRANCOUNT > 0
        RAISERROR ('GetDispatchInstructions: %d: %s', 16, 1, @error, @message) 
    END CATCH
END;
