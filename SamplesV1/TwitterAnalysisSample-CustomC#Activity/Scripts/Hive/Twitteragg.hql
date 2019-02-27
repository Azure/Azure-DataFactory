DROP TABLE IF EXISTS TwitterRaw;
CREATE EXTERNAL TABLE TwitterRaw
(
	ID  int,
	StatusId  string,
	Tweet string,
	Source string,
	UserID	string,
	Latitude string,
	Longitutde string,
	InReplyToScreenName string,
	InReplyToStatusID string,
	InReplyToUserID string,
	RetweetedStatus_StatusID string,
	RetweetCount  string,
	InsertedDate string,
	CreatedDate string,
	Query string,
	HourOfDay string,
	TweetType string,
	Platform string,
	Devices string,
	App string
) ROW FORMAT DELIMITED FIELDS TERMINATED BY ',' LINES TERMINATED BY '10' STORED AS TEXTFILE LOCATION '${hiveconf:TwitterRaw}';

DROP TABLE IF EXISTS TweetCounts;
CREATE EXTERNAL TABLE TweetCounts
(
	TweetCounts string
	
) ROW FORMAT delimited fields terminated by ',' LINES TERMINATED BY '10' STORED AS TEXTFILE LOCATION '${hiveconf:TweetsCount}';

INSERT OVERWRITE TABLE TweetCounts 
SELECT count(*) as TweetCounts from TwitterRaw;

DROP TABLE IF EXISTS TweetsByDate;
CREATE EXTERNAL TABLE TweetsByDate
(
	TweetsByDate string,
	CreateDate string
	
) ROW FORMAT delimited fields terminated by ',' LINES TERMINATED BY '10' STORED AS TEXTFILE LOCATION '${hiveconf:TweetsByDate}';

INSERT OVERWRITE TABLE TweetsByDate 
select count(ID) as TweetsByDate ,To_DATE(CreatedDate) from TwitterRaw group by To_DATE(CreatedDate) ;

DROP TABLE IF EXISTS Tweets;
CREATE EXTERNAL TABLE Tweets
(
	Tweets string
	
	
) ROW FORMAT delimited fields terminated by ',' LINES TERMINATED BY '10' STORED AS TEXTFILE LOCATION '${hiveconf:Tweets}';

INSERT OVERWRITE TABLE Tweets
select Tweet from TwitterRaw;

DROP TABLE IF EXISTS TweetsByHour;
CREATE EXTERNAL TABLE TweetsByHour
(
	TweetsByHour string,
	CreatedDate string,
	Hour 	    string
	
	
) ROW FORMAT delimited fields terminated by ',' LINES TERMINATED BY '10' STORED AS TEXTFILE LOCATION '${hiveconf:TweetsByHour}';

INSERT OVERWRITE TABLE TweetsByHour
select count(ID) as TweetsByHour,TO_DATE(CreatedDate),HOUR(CreatedDate)  from TwitterRaw group by HOUR(CreatedDate), TO_DATE(CreatedDate);

