# Contoso Marketing Campaign Analysis using Azure Data Factory & Azure Machine Learning

## Description:

Contoso is a retail company that has recently launched 5 new brands in home furnishings and décor department. 
They are trying to determine the effectiveness of their marketing campaigns by leveraging the twitter data, 
analyzing and aggregating them and identifying positive, negative sentiments of their customers.
In this sample, we will showcase how Contoso can use Azure Data Factory and Azure Machine Learning to address this E2E scenario.


Using 'AzureMLBatchScoringActivity' in Azure Data Factory, you can call any Azure ML model published in Azure ML workspace.
The 'AzureMLBatchScoringActivity' enables you to call Azure ML models and do sentiment analysis, scoring, prediction etc.

## TwitterAnalysisSample:

This sample does the following:
* The sample will use your raw tweets in Azure blob store and aggregate the raw tweets to generate TweetCountPerHour, TweetCountPerDay and the Total Tweet Count.
* The Tweets will be passed to a 'SentimentAnalysis' model in Azure ML. The Sentiment Analyis Azure ML model will take the raw tweets and return whether the sentiment is 'Positive', 'Negative' or 'Neutral' along with the 'ConfidenceLevel'.
* Following that, the sample will aggregate the sentiment for individual tweets to determine the overall sentiment i.e. No of Tweets with Positive, Negative or Neutral Sentiment.
* The Aggregated Sentiment Data will be moved to Sql Azure 'ContosoTweetsAnalysis' database.


This sample contains the following:
1. Azure Data Factory Linked Services, Tables, Pipeline Jsons.
2. Hive and SQL Scripts for the sample.
3. TwitterAnalysisSample.ps1 script. This script contains ADF powershell commands to create your datafactory, linked services, tables, pipelines and setting the 'ActivePeriod' to execute the pipelines.

## Pre-Requisites:
1. Update the connection strings for different Linked Services in the 'LinkedServices' folder. Replace <> placeholders with actual values.
   Update the 'AzureMachineLearningLinkedService' to add the sentiment analysis ML model endpoint and API key. 
   This sentiment analysis model should accept a csv file with one column that contains the tweets.
2. Create a 'container' in your storage. Name is 'twitteranalysis'.
3. Upload the 'Tweets.csv' file in 'InputData' folder of the sample to 'twitteranalysis/twitter/rawdata/' folder in your storage account.
4. Upload the Hive Scripts in 'Scripts/Hive' folder of the sample to 'twitteranalysis/twitter/scripts' folder in your storage account.
5. Update the Pipelines in 'Pipelines' folder of the sample to replace the <accountname> placeholder with your storage account name.
6. Create a 'ContosoTweetsAnalysis' Azure SQL database and run the 'ContosoAggTweetsSentiment.sql' in /Scripts/Sql folder of your sample.
