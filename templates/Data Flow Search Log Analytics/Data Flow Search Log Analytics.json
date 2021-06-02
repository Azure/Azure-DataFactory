{
	"$schema": "http://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
	"contentVersion": "1.0.0.0",
	"parameters": {
		"factoryName": {
			"type": "string",
			"metadata": "Data Factory Name"
		},
		"AzureBlobStorage1": {
			"type": "string"
		},
		"AzureSqlDW1": {
			"type": "string"
		}
	},
	"variables": {
		"factoryId": "[concat('Microsoft.DataFactory/factories/', parameters('factoryName'))]"
	},
	"resources": [
		{
			"name": "[concat(parameters('factoryName'), '/SearchLogAnalytics')]",
			"type": "Microsoft.DataFactory/factories/pipelines",
			"apiVersion": "2018-06-01",
			"properties": {
				"description": "This is a sample that takes the U-SQL SearchLog analytics example and turns it into an ADF Data Flow: https://kromerbigdata.com/2019/03/03/u-sql-searchlog-aggregations-as-adf-data-flows/",
				"activities": [
					{
						"name": "SearchLog",
						"type": "ExecuteDataFlow",
						"policy": {
							"timeout": "7.00:00:00",
							"retry": 0,
							"retryIntervalInSeconds": 30,
							"secureOutput": false,
							"secureInput": false
						},
						"typeProperties": {
							"dataflow": {
								"referenceName": "SearchLog",
								"type": "DataFlowReference"
							},
							"compute": {
								"computeType": "General",
								"dataTransformationUnits": 4,
								"coreCount": 8,
								"numberOfNodes": 0
							}
						}
					}
				]
			},
			"dependsOn": [
				"[concat(variables('factoryId'), '/dataflows/SearchLog')]"
			]
		},
		{
			"name": "[concat(parameters('factoryName'), '/SearchLog')]",
			"type": "Microsoft.DataFactory/factories/dataflows",
			"apiVersion": "2018-06-01",
			"properties": {
				"type": "MappingDataFlow",
				"typeProperties": {
					"sources": [
						{
							"dataset": {
								"referenceName": "searchLog",
								"type": "DatasetReference"
							},
							"name": "searchLog",
							"script": "source(output(\n\t\t{_col0_} as integer,\n\t\t{_col1_} as string,\n\t\t{_col2_} as string,\n\t\t{_col3_} as string,\n\t\t{_col4_} as integer,\n\t\t{_col5_} as string,\n\t\t{_col6_} as string\n\t),\n\tallowSchemaDrift: true,\n\tvalidateSchema: false) ~> searchLog"
						}
					],
					"sinks": [
						{
							"dataset": {
								"referenceName": "AzureSqlDWTable1",
								"type": "DatasetReference"
							},
							"name": "sinkIntoDW",
							"script": "DurationFilter sink(allowSchemaDrift: false,\n\tvalidateSchema: false,\n\tformat: 'table',\n\tstaged: false,\n\tdeletable:false,\n\tinsertable:true,\n\tupdateable:false,\n\tupsertable:false) ~> sinkIntoDW"
						}
					],
					"transformations": [
						{
							"name": "totalDurationByRegion",
							"script": "DateFilter aggregate(groupBy(region),\n\ttotalduration = sum(duration)) ~> totalDurationByRegion"
						},
						{
							"name": "RenameColumns",
							"script": "searchLog select(mapColumn(\n\t\tuserid = {_col0_},\n\t\tstart = {_col1_},\n\t\tregion = {_col2_},\n\t\tquery = {_col3_},\n\t\tduration = {_col4_},\n\t\turls = {_col5_},\n\t\tclickedurls = {_col6_}\n\t))~> RenameColumns"
						},
						{
							"name": "DateFilter",
							"script": "ConvertDate filter(newdate > toDate('2012-02-06','yyyy-MM-dd')) ~> DateFilter"
						},
						{
							"name": "ConvertDate",
							"script": "RenameColumns derive(newdate = toDate(left(start,instr(start,' ')-1),'MM/dd/yyyy')) ~> ConvertDate"
						},
						{
							"name": "DurationFilter",
							"script": "totalDurationByRegion filter(totalduration > 200) ~> DurationFilter"
						}
					]
				}
			},
			"dependsOn": [
				"[concat(variables('factoryId'), '/datasets/searchLog')]",
				"[concat(variables('factoryId'), '/datasets/AzureSqlDWTable1')]"
			]
		},
		{
			"name": "[concat(parameters('factoryName'), '/searchLog')]",
			"type": "Microsoft.DataFactory/factories/datasets",
			"apiVersion": "2018-06-01",
			"properties": {
				"linkedServiceName": {
					"referenceName": "[parameters('AzureBlobStorage1')]",
					"type": "LinkedServiceReference"
				},
				"type": "DelimitedText",
				"typeProperties": {
					"location": {
						"type": "AzureBlobStorageLocation",
						"fileName": "SearchLog.tsv",
						"folderPath": "SampleData/USQL",
						"container": "mycontainer"
					},
					"columnDelimiter": "\t"
				},
				"schema": [
					{
						"name": "_c0",
						"type": "String"
					},
					{
						"name": "_c1",
						"type": "String"
					},
					{
						"name": "_c2",
						"type": "String"
					},
					{
						"name": "_c3",
						"type": "String"
					},
					{
						"name": "_c4",
						"type": "String"
					},
					{
						"name": "_c5",
						"type": "String"
					},
					{
						"name": "_c6",
						"type": "String"
					}
				]
			}
		},
		{
			"name": "[concat(parameters('factoryName'), '/AzureSqlDWTable1')]",
			"type": "Microsoft.DataFactory/factories/datasets",
			"apiVersion": "2018-06-01",
			"properties": {
				"linkedServiceName": {
					"referenceName": "[parameters('AzureSqlDW1')]",
					"type": "LinkedServiceReference"
				},
				"folder": {
					"name": "Misc"
				},
				"type": "AzureSqlDWTable",
				"typeProperties": {
					"tableName": "templatedemodata"
				}
			}
		}
	]
}
