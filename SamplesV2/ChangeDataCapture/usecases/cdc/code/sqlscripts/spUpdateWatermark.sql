CREATE PROCEDURE spUpdateWatermark @LastModifyDate datetime, @PartitionID INT
 		AS

 		BEGIN

 			UPDATE [ControlTableForSourceToSink]
 			SET [WatermarkValue] = @LastModifyDate
			WHERE PartitionID = @PartitionID

 		END