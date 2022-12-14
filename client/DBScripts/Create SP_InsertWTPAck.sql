/****** Object:  StoredProcedure [WTPApp].[InsertWTPAck]    Script Date: Monday, 2 Nov 2020 3:56:23 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [WTPApp].[InsertWTPAck]
	@DisPatchAck WTPDispatchAckType READONLY
AS
SET NOCOUNT ON
BEGIN
	BEGIN TRY
			--Insert in  WTPDispatchAck table
				INSERT INTO [WTPApp].[WTPDispatchAck] SELECT * FROM (
				SELECT 
							dg1.DispatchGroup AS [DispatchGroup]
						   ,dpk.ackType AS [ackType]
						   ,dpk.sequenceNumber AS [sequenceNumber]
						   ,GETUTCDATE() AS [AuditDateCreated]
						   ,SYSTEM_USER AS [AuditUserCreated]
						   ,'[WTPApp].[InsertWTPAck]' AS [AuditProcessCreated]
						   ,GETUTCDATE() AS [AuditDateUpdated]
						   ,dpk.AuditUserUpdated AS [AuditUserUpdated]
						   ,dpk.AuditProcessUpdated AS [AuditProcessUpdated]
						   ,dpk.correlationId AS [correlationId]
				FROM @DisPatchAck dpk
				INNER JOIN [WTPApp].[WTPDispatchGroup] dg1 ON dg1.[Description] = dpk.DispatchGroup) AS tempack
				WHERE NOT EXISTS (SELECT *
                     FROM [WTPApp].[WTPDispatchAck]
                     WHERE sequenceNumber = tempack.sequenceNumber
                     AND   DispatchGroup = tempack.DispatchGroup);
	END TRY
	BEGIN CATCH
		DECLARE @error INT, @message VARCHAR(4000);
        SELECT @error = ERROR_NUMBER(), @message = ERROR_MESSAGE();
		IF @@TRANCOUNT > 0
        RAISERROR ('WTPDispatchInstruction: %d: %s', 16, 1, @error, @message) 
    END CATCH
END;


