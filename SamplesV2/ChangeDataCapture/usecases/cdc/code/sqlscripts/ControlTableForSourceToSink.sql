CREATE TABLE [dbo].[ControlTableForSourceToSink](
	[PartitionID] [int] IDENTITY(1,1) NOT NULL,
	[ProjectName] [varchar](255) NOT NULL,
	[SubjectArea] [varchar](255) NOT NULL,
	[SourceSystem] [varchar](255) NOT NULL,
	[SourceDatabaseServer] [varchar](255) NOT NULL,
	[SourceDatabase] [varchar](255) NOT NULL,
	[SourceSchemaName] [varchar](255) NOT NULL,
	[SourceTableName] [varchar](255) NOT NULL,
	[SourceWaterMarkColumn] [varchar](255) NULL,
	[WatermarkValue] [datetime] NULL,
	[FilterQuery] [varchar](255) NOT NULL,
	[DestinationStorageAccount] [varchar](25) NOT NULL,
	[DestinationContainer] [varchar](255) NOT NULL,
	[LoadPartionDate] [datetime] NOT NULL,
	[ModifyDate] [datetime] NOT NULL,
	[ModifiedBy] [varchar](255) NOT NULL,
 CONSTRAINT [PK_ControlTableForSourceToSink] PRIMARY KEY CLUSTERED 
(
	[PartitionID] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[ControlTableForSourceToSink] ADD  DEFAULT (getdate()) FOR [ModifyDate]
GO

INSERT INTO [dbo].[ControlTableForSourceToSink]
 		(ProjectName, SubjectArea, SourceSystem, SourceDatabaseServer, SourceDatabase, SourceSchemaName, SourceTableName, SourceWaterMarkColumn, WatermarkValue, FilterQuery, DestinationStorageAccount, DestinationContainer, LoadPartionDate, ModifiedBy)
 		VALUES
		('CDC', 'Sales', 'Microsoft', 'servername.database.windows.net', 'AdventureWorksLT', 'SalesLT', 'Address', 'ModifiedDate', NULL, 'select * from SalesLT.Address', 'storageaccountname', 'bronze',  '2021-08-01 13:59:59.999', 'darsch@microsoft.com'),
		('CDC', 'Sales', 'Microsoft', 'servername.database.windows.net', 'AdventureWorksLT', 'SalesLT', 'Customer', 'ModifiedDate', NULL, 'select * from SalesLT.Customer', 'storageaccountname', 'bronze',  '2021-08-01 13:59:59.999', 'darsch@microsoft.com'),
		('CDC', 'Sales', 'Microsoft', 'servername.database.windows.net', 'AdventureWorksLT', 'SalesLT', 'CustomerAddress', 'ModifiedDate', NULL, 'select * from SalesLT.CustomerAddress', 'storageaccountname', 'bronze',  '2021-08-01 13:59:59.999', 'darsch@microsoft.com'),
		('CDC', 'Sales', 'Microsoft', 'servername.database.windows.net', 'AdventureWorksLT', 'SalesLT', 'Product', 'ModifiedDate', NULL, 'select * from SalesLT.Product', 'storageaccountname', 'bronze',  '2021-08-01 13:59:59.999', 'darsch@microsoft.com'),
		('CDC', 'Sales', 'Microsoft', 'servername.database.windows.net', 'AdventureWorksLT', 'SalesLT', 'ProductCategory', 'ModifiedDate', NULL, 'select * from SalesLT.ProductCategory', 'storageaccountname', 'bronze',  '2021-08-01 13:59:59.999', 'darsch@microsoft.com'),
		('CDC', 'Sales', 'Microsoft', 'servername.database.windows.net', 'AdventureWorksLT', 'SalesLT', 'ProductDescription', 'ModifiedDate', NULL, 'select * from SalesLT.ProductDescription', 'storageaccountname', 'bronze',  '2021-08-01 13:59:59.999', 'darsch@microsoft.com'),
		('CDC', 'Sales', 'Microsoft', 'servername.database.windows.net', 'AdventureWorksLT', 'SalesLT', 'ProductModel', 'ModifiedDate', NULL, 'select * from SalesLT.ProductModel', 'storageaccountname', 'bronze',  '2021-08-01 13:59:59.999', 'darsch@microsoft.com'),
		('CDC', 'Sales', 'Microsoft', 'servername.database.windows.net', 'AdventureWorksLT', 'SalesLT', 'ProductModelProductDescription', 'ModifiedDate', NULL, 'select * from SalesLT.ProductModelProductDescription', 'storageaccountname', 'bronze',  '2021-08-01 13:59:59.999', 'darsch@microsoft.com'),
		('CDC', 'Sales', 'Microsoft', 'servername.database.windows.net', 'AdventureWorksLT', 'SalesLT', 'SalesOrderDetail', 'ModifiedDate', NULL, 'select * from SalesLT.SalesOrderDetail', 'storageaccountname', 'bronze',  '2021-08-01 13:59:59.999', 'darsch@microsoft.com'),
		('CDC', 'Sales', 'Microsoft', 'servername.database.windows.net', 'AdventureWorksLT', 'SalesLT', 'SalesOrderHeader', 'ModifiedDate', NULL, 'select * from SalesLT.SalesOrderHeader', 'storageaccountname', 'bronze',  '2021-08-01 13:59:59.999', 'darsch@microsoft.com'),
		('CDC', 'Student', 'Microsoft', 'servername.database.windows.net', 'AdventureWorksLT', 'dbo', 'studentMath', NULL, NULL, 'select * from dbo.studentMath', 'storageaccountname', 'bronze',  '2021-08-01 13:59:59.999', 'darsch@microsoft.com')