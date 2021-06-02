DROP TABLE IF EXISTS EnrichedGameEvents;
CREATE EXTERNAL TABLE EnrichedGameEvents
(
	ProfileID string,
	SessionStart string,
	Duration int,
	State string,
	SrcIPAddress string,
	GameType string,
	Multiplayer string,
	EndRank int,
	WeaponsUsed int,
	UsersInteractedWith string
) ROW FORMAT DELIMITED FIELDS TERMINATED BY ',' ESCAPED BY '@!&#' LINES TERMINATED BY '10' STORED AS TEXTFILE LOCATION '${hiveconf:EventsInput}';
 
DROP TABLE IF EXISTS RegionalCampaignsData;
CREATE EXTERNAL TABLE RegionalCampaignsData
(
	ID string,
	Day string,
	City string,
	State string,
	Type string,
	Impressions int
) ROW FORMAT DELIMITED FIELDS TERMINATED BY ',' ESCAPED BY '@!&#' LINES TERMINATED BY '10' STORED AS TEXTFILE LOCATION '${hiveconf:CampaignInput}';

DROP TABLE IF EXISTS MarketingCampaignEffectiveness;
CREATE EXTERNAL TABLE MarketingCampaignEffectiveness
(
	ProfileID string,
	SessionStart string,
	Duration int,
	State string,
	SrcIPAddress string,
	GameType string,
	Multiplayer string,
	EndRank int,
	WeaponsUsed int,
	UsersInteractedWith string,
	Impressions int
) ROW FORMAT DELIMITED FIELDS TERMINATED BY ',' ESCAPED BY '@!&#' LINES TERMINATED BY '10' STORED AS TEXTFILE LOCATION '${hiveconf:CampaignOutput}';

INSERT OVERWRITE TABLE MarketingCampaignEffectiveness
SELECT 
	ege.ProfileID as ProfileID,
	ege.SessionStart as SessionStart,
	ege.Duration as Duration,
	ege.State as State,
	ege.SrcIPAddress,
	ege.GameType as GameType,
	ege.Multiplayer as Multiplayer,
	ege.EndRank as EndRank,
	ege.WeaponsUsed as WeaponsUsed,
	ege.UsersInteractedWith as UsersInteractedWith,
	rcd.Impressions as Impressions
FROM EnrichedGameEvents ege
JOIN RegionalCampaignsData rcd
ON (ege.State = rcd.State);
