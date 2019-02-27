-- This script enriches raw logs by geo-coding with state information and partially masking IP address

-- Required parameters of this script and their example values:
-- %default LOGINPUT 'wasb://adfwalkthrough@mystorage.blob.core.windows.net/logs/partitionedgameevents/yearno=2014/monthno=5/dayno=1/'
-- %default MAPINPUT 'wasb://adfwalkthrough@mystorage.blob.core.windows.net/refdata/refgeocodedictionary/'
-- %default LOGOUTPUT 'wasb://adfwalkthrough@mystorage.blob.core.windows.net/logs/enrichedgameevents/yearno=2014/monthno=5/dayno=1/'

-- remove output if it already exists, to ensure idempotency if job is rerun
fs -mkdir -p $LOGOUTPUT;
fs -touchz $LOGOUTPUT/_tmp;
fs -rmr -skipTrash $LOGOUTPUT;

-- load raw stats from appropriate partition
RawStats = LOAD '$LOGINPUT' USING PigStorage(',') AS (ProfileID:chararray, SessionStart:chararray, Duration:int, SrcIPAddress:chararray, GameType:chararray, Multiplayer:chararray, EndRank:int, WeaponsUsed:int, UsersInteractedWith:chararray);

-- load IP address dictionary for geo-coding
IPAddressDictionary = LOAD '$MAPINPUT' USING PigStorage(',') AS (IPAddr:chararray, State:chararray);

-- geo-code stats with state
RawStatsByState = JOIN RawStats by SrcIPAddress, IPAddressDictionary by IPAddr;

-- mask IP address by dropping last octet
CleanStats = FOREACH RawStatsByState GENERATE ProfileID, SessionStart, Duration, State, REGEX_EXTRACT(SrcIPAddress, '([0-9]+\\.[0-9]+\\.[0-9]+\\.)', 0) as SrcIPAddress, GameType, Multiplayer, EndRank, WeaponsUsed, UsersInteractedWith;

-- save results into appropriate partition
STORE CleanStats INTO '$LOGOUTPUT' USING PigStorage (',');
