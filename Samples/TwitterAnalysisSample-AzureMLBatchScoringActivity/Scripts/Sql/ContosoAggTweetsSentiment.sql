USE [ContosoTweetsAnalysis]
GO

/****** Object:  Table [dbo].[ContosoAggTweetsSentiment]    Script Date: 11/11/2014 3:21:13 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[ContosoAggTweetsSentiment](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TweetSentimentCount] [int] NULL,
	[TweetSentiment] [nvarchar](256) NULL,
 CONSTRAINT [PK_ContosoAggTweetsSentiment] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO


