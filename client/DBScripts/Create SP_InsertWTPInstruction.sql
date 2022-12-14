/****** Object:  StoredProcedure [WTPApp].[InsertWTPInstructions]    Script Date: Monday, 2 Nov 2020 3:56:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [WTPApp].[InsertWTPInstructions]
    @InstructionList [WTPInstructionType] READONLY
AS
SET NOCOUNT ON
BEGIN
	BEGIN TRY
			DECLARE @DisPatchGroupName NVARCHAR(50);
			SELECT @DisPatchGroupName = IL.DispatchGroupName FROM @InstructionList IL
			
			-- Update Heart Beat.
			UPDATE [WTPApp].[WTPDispatchGroup] SET [LastRequestUTC] = GETUTCDATE(), 
													[AuditDateUpdated] = GETUTCDATE(),
													[AuditUserUpdated] = SYSTEM_USER,
													[AuditProcessUpdated] = '[WTPApp].[InsertWTPInstructions]'
													WHERE [Description] = @DisPatchGroupName
			
			-- Insert in WTPInstructions table
			INSERT INTO [WTPApp].[WTPDispatchInstruction] SELECT * FROM (
				SELECT 
							pdt1.PrimaryDispatchTypeCode AS [PrimaryDispatchTypeCode]
						   ,n1.NodeCode AS [NodeCode]
						   ,b1.BlockCode AS [BlockCode]
						   ,ins.correlationId AS [correlationId]
						   ,ins.sequenceNumber AS [sequenceNumber]
						   ,ins.dispatchEndpointOwner AS [dispatchEndpointOwner]
						   ,ins.isUserResend AS [isUserResend]
						   ,ins.messageSentTime AS [messageSentTime]
						   ,ins.traderCode AS [traderCode]
						   ,ins.dispatchValue AS [dispatchValue]
						   ,ins.dispatchTime AS [dispatchTime]
						   ,ins.dispatchIssueTime AS [dispatchIssueTime]
						   ,GETUTCDATE() AS [AuditDateCreated]
						   ,SYSTEM_USER AS [AuditUserCreated]
						   ,'[WTPApp].[InsertWTPInstructions]' AS [AuditProcessCreated]
						   ,GETUTCDATE() AS [AuditDateUpdated]
						   ,ins.AuditUserUpdated AS [AuditUserUpdated]
						   ,ins.AuditProcessUpdated AS [AuditProcessUpdated]
				FROM @InstructionList ins
				INNER JOIN [WTPApp].[WTPPrimaryDispatchType] pdt1 ON pdt1.PrimaryDispatchTypeCode = ins.PrimaryDispatchTypeCode
				INNER JOIN [WTPApp].[WTPDispatchGroup] dg1 ON dg1.DispatchGroup = pdt1.DispatchGroup AND dg1.[Description] = ins.DispatchGroupName
				LEFT JOIN [WTPApp].[WTPNode] n1 ON n1.NodeCode = ins.NodeCode
				LEFT JOIN [WTPApp].[WTPBlock] b1 On b1.BlockCode = ins.BlockCode) AS tempins
				WHERE NOT EXISTS (SELECT *
                     FROM [WTPApp].[WTPDispatchInstruction]
                     WHERE sequenceNumber = tempins.sequenceNumber
                     AND   PrimaryDispatchTypeCode = tempins.PrimaryDispatchTypeCode);
	END TRY
	BEGIN CATCH
		DECLARE @error INT, @message VARCHAR(4000);
        SELECT @error = ERROR_NUMBER(), @message = ERROR_MESSAGE();
		RAISERROR ('WTPDispatchInstruction: %d: %s', 16, 1, @error, @message) 
    END CATCH
END;


