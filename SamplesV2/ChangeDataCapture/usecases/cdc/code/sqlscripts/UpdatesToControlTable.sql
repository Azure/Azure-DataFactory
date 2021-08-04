/****** Script for SelectTopNRows command from SSMS  ******/
SELECT [PartitionID]
      ,[ProjectName]
      ,[SubjectArea]
      ,[SourceSystem]
      ,[SourceDatabaseServer]
      ,[SourceDatabase]
      ,[SourceSchemaName]
      ,[SourceTableName]
      ,[SourceWaterMarkColumn]
      ,[WatermarkValue]
      ,[FilterQuery]
      ,[DestinationStorageAccount]
      ,[DestinationContainer]
      ,[LoadPartionDate]
      ,[ModifyDate]
      ,[ModifiedBy]
  FROM
  --UPDATE
  [dbo].[ControlTableForSourceToSink]
  --Set DestinationContainer = 'bronze'
  --Set WatermarkValue = NULL