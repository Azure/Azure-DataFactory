SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MarketingCampaignEffectiveness')
DROP TABLE [dbo].[MarketingCampaignEffectiveness];
GO

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_NAME = 'spEgressOverwriteMarketingCampaignEffectiveness' AND ROUTINE_SCHEMA = 'dbo' AND ROUTINE_TYPE = 'PROCEDURE')
EXEC ('DROP PROCEDURE spEgressOverwriteMarketingCampaignEffectiveness')
GO

IF EXISTS (SELECT * FROM SYS.TYPES WHERE NAME = 'MarketingCampaignEffectivenessType' AND IS_TABLE_TYPE = 1)
DROP TYPE [dbo].[MarketingCampaignEffectivenessType];
GO

CREATE TABLE [dbo].[MarketingCampaignEffectiveness](
    [McEffId] [int] IDENTITY(1,1) NOT NULL,
    [ProfileID] [varchar](256) NOT NULL,
    [SessionStart] [varchar](256) NOT NULL,
    [Duration] [varchar](256) NOT NULL,
    [State] [varchar](256) NOT NULL,
    [SrcIPAddress] [varchar](256) NOT NULL,
    [GameType] [varchar](256) NOT NULL,
    [Multiplayer] [varchar](256) NOT NULL,
    [EndRank] [varchar](256) NOT NULL,
    [WeaponsUsed] [varchar](256) NOT NULL,
    [UsersInteractedWith] [varchar](256) NOT NULL,
    [Impressions] [int] NOT NULL
    CONSTRAINT [PK_MarketingCampaignEffectiveness] PRIMARY KEY CLUSTERED 
	(
	    [McEffId] ASC
	)
 )
GO

SET ANSI_PADDING OFF
GO

--Table type
CREATE TYPE [dbo].[MarketingCampaignEffectivenessType] AS TABLE(
    [ProfileID] [varchar](256) NOT NULL,
    [SessionStart] [varchar](256) NOT NULL,
    [Duration] [varchar](256) NOT NULL,
    [State] [varchar](256) NOT NULL,
    [SrcIPAddress] [varchar](256) NOT NULL,
    [GameType] [varchar](256) NOT NULL,
    [Multiplayer] [varchar](256) NOT NULL,
    [EndRank] [varchar](256) NOT NULL,
    [WeaponsUsed] [varchar](256) NOT NULL,
    [UsersInteractedWith] [varchar](256) NOT NULL,
    [Impressions] [int] NOT NULL
)
GO

--Stored Procedure
CREATE PROCEDURE spEgressOverwriteMarketingCampaignEffectiveness @MarketingCampaignEffectiveness [dbo].[MarketingCampaignEffectivenessType] READONLY
AS
BEGIN
   DELETE FROM [dbo].[MarketingCampaignEffectiveness]
   INSERT [dbo].[MarketingCampaignEffectiveness](ProfileID, SessionStart, Duration, State, SrcIPAddress, GameType, Multiplayer, EndRank, WeaponsUsed, UsersInteractedWith, Impressions)
   SELECT * FROM @MarketingCampaignEffectiveness
END
GO

