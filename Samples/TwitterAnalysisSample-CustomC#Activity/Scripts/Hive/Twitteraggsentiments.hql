DROP TABLE IF EXISTS TweetsSentiment;
CREATE EXTERNAL TABLE TweetsSentiment
(
	Tweet  string,
	Sentiment  string,
	ConfidenceLevel string
) ROW FORMAT DELIMITED FIELDS TERMINATED BY ',' LINES TERMINATED BY '10' STORED AS TEXTFILE LOCATION '${hiveconf:TweetsSentiment}';

DROP TABLE IF EXISTS TweetsAggSentiment;
CREATE EXTERNAL TABLE TweetsAggSentiment
(
	TweetSentimentCount int,
	TweetSentiment string
	
) ROW FORMAT DELIMITED FIELDS TERMINATED BY ',' LINES TERMINATED BY '10' STORED AS TEXTFILE LOCATION '${hiveconf:TweetsSentimentAgg}';

INSERT OVERWRITE TABLE TweetsAggSentiment 
SELECT count(*) as TweetSentimentCount, Sentiment from TweetsSentiment GROUP BY Sentiment;
