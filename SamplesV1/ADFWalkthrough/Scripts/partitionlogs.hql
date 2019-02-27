SET hive.exec.dynamic.partition=true;
SET hive.exec.dynamic.partition.mode = nonstrict;


DROP TABLE IF EXISTS RawGameEvents; 
CREATE EXTERNAL TABLE RawGameEvents 
(
               	ProfileID 	string, 
                SessionStart 	string, 
                Duration 	int, 
                SrcIPAddress 	string, 
                GameType 	string, 
                Multiplayer 	string, 
                EndRank 	int, 
                WeaponsUsed 	int, 
                UsersInteractedWith string
) ROW FORMAT DELIMITED FIELDS TERMINATED BY ',' LINES TERMINATED BY '10' STORED AS TEXTFILE LOCATION '${hiveconf:RAWINPUT}'; 

DROP TABLE IF EXISTS PartitionedGameEvents; 
CREATE EXTERNAL TABLE PartitionedGameEvents 
(
                ProfileID 	string, 
                SessionStart 	string, 
                Duration 	int, 
                SrcIPAddress 	string, 
                GameType 	string, 
                Multiplayer 	string, 
                EndRank 	int, 
                WeaponsUsed 	int, 
                UsersInteractedWith string) partitioned by (YearNo int, MonthNo int, DayNo int) ROW FORMAT DELIMITED FIELDS TERMINATED BY ',' LINES TERMINATED BY '10' STORED AS TEXTFILE LOCATION '${hiveconf:PARTITIONEDOUTPUT}';

DROP TABLE IF EXISTS Stage_RawGameEvents; 
CREATE TABLE IF NOT EXISTS Stage_RawGameEvents 
(
		ProfileID	String,
		SessionStart	String,
		Duration	int,
		SrcIPAddress	String,
		GameType	String,
		Multiplayer	String,
		EndRank		Int,
		WeaponsUsed	Int,
		UsersInteractedWith	String,
		YearNo 		int,
		MonthNo 	int,
		DayNo 		int ) ROW FORMAT delimited fields terminated by ',' LINES TERMINATED BY '10';

INSERT OVERWRITE TABLE Stage_RawGameEvents
SELECT
	ProfileID,
	SessionStart,
	Duration,
	SrcIPAddress,
	GameType,
	Multiplayer,
	EndRank,
	WeaponsUsed,
	UsersInteractedWith,
	Year(SessionStart),
	Month(SessionStart),
	Day(SessionStart) 
FROM RawGameEvents WHERE Year(SessionStart) = ${hiveconf:Year} AND Month(SessionStart) = ${hiveconf:Month} AND Day(SessionStart) = ${hiveconf:Day}; 

INSERT OVERWRITE TABLE PartitionedGameEvents PARTITION(YearNo, MonthNo, DayNo) 
SELECT
	ProfileID,
	SessionStart,
	Duration,
	SrcIPAddress,
	GameType,
	Multiplayer,
	EndRank,
	WeaponsUsed,
	UsersInteractedWith,
	YearNo,
	MonthNo,
	DayNo
FROM Stage_RawGameEvents WHERE YearNo = ${hiveconf:Year} AND MonthNo = ${hiveconf:Month} AND DayNo = ${hiveconf:Day};
