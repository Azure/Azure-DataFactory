$df = 'ContosoTwitterSample'

New-AzureDataFactory -Location WestUS -Name $df -ResourceGroupName ADF

#Create Linked Services
New-AzureDataFactoryLinkedService -DataFactoryName $df -File .\LinkedServices\StorageLinkedService.json -Name StorageLinkedService -ResourceGroupName ADF
New-AzureDataFactoryLinkedService -DataFactoryName $df -File .\LinkedServices\HDInsightLinkedService.json -Name HDInsightLinkedService -ResourceGroupName ADF
New-AzureDataFactoryLinkedService -DataFactoryName $df -File .\LinkedServices\AzureSqlLinkedService.json -Name AzureSqlLinkedService -ResourceGroupName ADF

#Create Tables
New-AzureDataFactoryTable -DataFactoryName $df -File .\Tables\ContosoTwitterRaw.json -Name ContosoTwitterRaw -ResourceGroupName adf
New-AzureDataFactoryTable -DataFactoryName $df -File .\Tables\ContosoTweets.json -Name ContosoTweets -ResourceGroupName adf
New-AzureDataFactoryTable -DataFactoryName $df -File .\Tables\ContosoTweetsBlob.json -Name ContosoTweetsBlob -ResourceGroupName adf
New-AzureDataFactoryTable -DataFactoryName $df -File .\Tables\ContosoTwitterAggregation.json -Name ContosoTwitterAggregation -ResourceGroupName adf
New-AzureDataFactoryTable -DataFactoryName $df -File .\Tables\ContosoTweetsSentimentBlob.json -Name ContosoTweetsSentimentBlob -ResourceGroupName adf
New-AzureDataFactoryTable -DataFactoryName $df -File .\Tables\ContosoTweetsAggSentimentBlob.json -Name ContosoTweetsAggSentimentBlob -ResourceGroupName adf
New-AzureDataFactoryTable -DataFactoryName $df -File .\Tables\ContosoTweetsAggSentimentSQLTable.json -Name ContosoTweetsAggSentimentSQLTable -ResourceGroupName adf

#Create Pipelines
New-AzureDataFactoryPipeline -DataFactoryName $df -File .\Pipelines\AnalyzeContosoTwitterFeed.json -Name AnalyzeContosoTwitterFeed -ResourceGroupName adf
New-AzureDataFactoryPipeline -DataFactoryName $df -File .\Pipelines\AnalyzeContosoTweetsSentimentML.json -Name AnalyzeContosoTweetsSentimentML -ResourceGroupName adf
New-AzureDataFactoryPipeline -DataFactoryName $df -File .\Pipelines\AggregateContosoTweetsSentiment.json -Name AggregateContosoTweetsSentiment -ResourceGroupName adf
New-AzureDataFactoryPipeline -DataFactoryName $df -File .\Pipelines\EgressContosoAggTweetsSentimentSqlAzure.json -Name EgressContosoAggTweetsSentimentSqlAzure -ResourceGroupName adf

#Set Active Period
Set-AzureDataFactoryPipelineActivePeriod -ResourceGroupName ADF -DataFactoryName $df -StartDateTime "11/08/2014 10:09:00 PM" -EndDateTime "11/08/2014 10:11:00 PM" -Name AnalyzeContosoTwitterFeed
Set-AzureDataFactoryPipelineActivePeriod -ResourceGroupName ADF -DataFactoryName $df -StartDateTime "11/08/2014 10:09:00 PM" -EndDateTime "11/08/2014 10:11:00 PM" -Name AnalyzeContosoTweetsSentimentML
Set-AzureDataFactoryPipelineActivePeriod -ResourceGroupName ADF -DataFactoryName $df -StartDateTime "11/08/2014 10:09:00 PM" -EndDateTime "11/08/2014 10:11:00 PM" -Name AggregateContosoTweetsSentiment
Set-AzureDataFactoryPipelineActivePeriod -ResourceGroupName ADF -DataFactoryName $df -StartDateTime "11/08/2014 10:09:00 PM" -EndDateTime "11/08/2014 10:11:00 PM" -Name EgressContosoAggTweetsSentimentSqlAzure