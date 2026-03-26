USE [SpokenEnglish]
GO
/****** Object:  Table [dbo].[AppUser]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppUser](
	[UserID] [int] IDENTITY(1,1) NOT NULL,
	[UserName] [nvarchar](100) NULL,
	[MobileNo] [nvarchar](20) NULL,
	[CreatedDate] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[UserID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ArrangeSentence]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ArrangeSentence](
	[ArrangeSentenceID] [int] IDENTITY(1,1) NOT NULL,
	[LessonID] [int] NULL,
	[CorrectSentence] [nvarchar](500) NULL,
PRIMARY KEY CLUSTERED 
(
	[ArrangeSentenceID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Language]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Language](
	[LanguageID] [int] IDENTITY(1,1) NOT NULL,
	[LanguageName] [nvarchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[LanguageID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Lesson]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Lesson](
	[LessonID] [int] IDENTITY(1,1) NOT NULL,
	[LessonName] [nvarchar](200) NULL,
	[LessonTypeID] [int] NULL,
	[LessonOrder] [int] NULL,
	[IsActive] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[LessonID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Lesson_Lang]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Lesson_Lang](
	[LessonLangID] [int] IDENTITY(1,1) NOT NULL,
	[LessonID] [int] NOT NULL,
	[LanguageID] [int] NOT NULL,
	[LessonName] [nvarchar](200) NOT NULL,
	[Description] [nvarchar](500) NULL,
	[CreatedDate] [datetime] NOT NULL,
	[UpdatedDate] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[LessonLangID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[LessonType]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[LessonType](
	[LessonTypeID] [int] IDENTITY(1,1) NOT NULL,
	[TypeName] [nvarchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[LessonTypeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MeaningOption]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MeaningOption](
	[OptionID] [int] IDENTITY(1,1) NOT NULL,
	[QuestionID] [int] NULL,
	[OptionText] [nvarchar](200) NULL,
	[IsCorrect] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[OptionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MeaningQuestion]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MeaningQuestion](
	[QuestionID] [int] IDENTITY(1,1) NOT NULL,
	[LessonID] [int] NULL,
	[QuestionText] [nvarchar](300) NULL,
PRIMARY KEY CLUSTERED 
(
	[QuestionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PasswordResetTokens]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PasswordResetTokens](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NULL,
	[Token] [nvarchar](100) NULL,
	[ExpiresAt] [datetime] NULL,
	[IsUsed] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ReadingSentence]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ReadingSentence](
	[ReadingSentenceID] [int] IDENTITY(1,1) NOT NULL,
	[LessonID] [int] NULL,
	[SentenceText] [nvarchar](500) NULL,
PRIMARY KEY CLUSTERED 
(
	[ReadingSentenceID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RefreshTokens]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RefreshTokens](
	[RefreshTokenId] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[Token] [nvarchar](500) NOT NULL,
	[ExpiresAt] [datetime] NOT NULL,
	[IsRevoked] [bit] NULL,
	[CreatedDate] [datetime] NULL,
	[ExpiryDate] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[RefreshTokenId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserAnswer]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserAnswer](
	[AnswerID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NULL,
	[LessonID] [int] NULL,
	[ActivityType] [nvarchar](50) NULL,
	[ReferenceID] [int] NULL,
	[IsCorrect] [bit] NULL,
	[AttemptDate] [datetime] NULL,
	[LanguageID] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[AnswerID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserOtp]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserOtp](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NULL,
	[Otp] [nvarchar](6) NULL,
	[ExpiresAt] [datetime] NULL,
	[IsUsed] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserRefreshTokens]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserRefreshTokens](
	[RefreshTokenID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NOT NULL,
	[RefreshToken] [nvarchar](500) NOT NULL,
	[ExpiryDate] [datetime] NOT NULL,
	[IsRevoked] [bit] NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[RefreshTokenID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Users]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[UserGuid] [uniqueidentifier] NOT NULL,
	[Email] [nvarchar](200) NOT NULL,
	[PasswordHash] [nvarchar](500) NOT NULL,
	[ApiKey] [nvarchar](200) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedBy] [int] NULL,
	[CreatedDate] [datetime] NOT NULL,
	[ModifyBy] [int] NULL,
	[ModifyDate] [datetime] NULL,
	[MobileNumber] [varchar](15) NULL,
	[Role] [nvarchar](50) NOT NULL,
	[Mobile] [varchar](15) NULL,
	[RefreshToken] [varchar](200) NULL,
	[RefreshTokenExpiry] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AppUser] ADD  DEFAULT (getdate()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[Lesson_Lang] ADD  DEFAULT (getdate()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[PasswordResetTokens] ADD  DEFAULT ((0)) FOR [IsUsed]
GO
ALTER TABLE [dbo].[RefreshTokens] ADD  DEFAULT ((0)) FOR [IsRevoked]
GO
ALTER TABLE [dbo].[RefreshTokens] ADD  DEFAULT (getdate()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[RefreshTokens] ADD  DEFAULT (dateadd(day,(7),getdate())) FOR [ExpiryDate]
GO
ALTER TABLE [dbo].[UserAnswer] ADD  DEFAULT (getdate()) FOR [AttemptDate]
GO
ALTER TABLE [dbo].[UserOtp] ADD  DEFAULT ((0)) FOR [IsUsed]
GO
ALTER TABLE [dbo].[UserRefreshTokens] ADD  DEFAULT ((0)) FOR [IsRevoked]
GO
ALTER TABLE [dbo].[UserRefreshTokens] ADD  DEFAULT (getdate()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT (getutcdate()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT ('User') FOR [Role]
GO
ALTER TABLE [dbo].[ArrangeSentence]  WITH CHECK ADD FOREIGN KEY([LessonID])
REFERENCES [dbo].[Lesson] ([LessonID])
GO
ALTER TABLE [dbo].[Lesson]  WITH CHECK ADD FOREIGN KEY([LessonTypeID])
REFERENCES [dbo].[LessonType] ([LessonTypeID])
GO
ALTER TABLE [dbo].[Lesson_Lang]  WITH CHECK ADD  CONSTRAINT [FK_LessonLang_Lesson] FOREIGN KEY([LessonID])
REFERENCES [dbo].[Lesson] ([LessonID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Lesson_Lang] CHECK CONSTRAINT [FK_LessonLang_Lesson]
GO
ALTER TABLE [dbo].[MeaningOption]  WITH CHECK ADD FOREIGN KEY([QuestionID])
REFERENCES [dbo].[MeaningQuestion] ([QuestionID])
GO
ALTER TABLE [dbo].[MeaningQuestion]  WITH CHECK ADD FOREIGN KEY([LessonID])
REFERENCES [dbo].[Lesson] ([LessonID])
GO
ALTER TABLE [dbo].[ReadingSentence]  WITH CHECK ADD FOREIGN KEY([LessonID])
REFERENCES [dbo].[Lesson] ([LessonID])
GO
ALTER TABLE [dbo].[UserAnswer]  WITH CHECK ADD FOREIGN KEY([LessonID])
REFERENCES [dbo].[Lesson] ([LessonID])
GO
ALTER TABLE [dbo].[UserAnswer]  WITH CHECK ADD FOREIGN KEY([UserID])
REFERENCES [dbo].[AppUser] ([UserID])
GO
ALTER TABLE [dbo].[UserRefreshTokens]  WITH CHECK ADD  CONSTRAINT [FK_UserRefreshTokens_Users] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([ID])
GO
ALTER TABLE [dbo].[UserRefreshTokens] CHECK CONSTRAINT [FK_UserRefreshTokens_Users]
GO
/****** Object:  StoredProcedure [dbo].[sp_ArrangeSentence]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[sp_ArrangeSentence]
(
    @Mode NVARCHAR(20),
    @ArrangeSentenceID INT = NULL,
    @LessonID INT = NULL,
    @LanguageID INT = NULL,
    @CorrectSentence NVARCHAR(500) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @Mode = 'ADD'
    BEGIN
        INSERT INTO ArrangeSentence (LessonID)
        VALUES (@LessonID);

        SET @ArrangeSentenceID = SCOPE_IDENTITY();

        INSERT INTO ArrangeSentence_Lang
        VALUES (@ArrangeSentenceID, @LanguageID, @CorrectSentence);
    END

    ELSE IF @Mode = 'EDIT'
    BEGIN
        UPDATE ArrangeSentence_Lang
        SET CorrectSentence = @CorrectSentence
        WHERE ArrangeSentenceID = @ArrangeSentenceID
          AND LanguageID = @LanguageID;
    END

    ELSE IF @Mode = 'DELETE'
    BEGIN
        DELETE FROM ArrangeSentenceWord_Lang
        WHERE WordID IN (
            SELECT WordID FROM ArrangeSentenceWord
            WHERE ArrangeSentenceID = @ArrangeSentenceID
        );

        DELETE FROM ArrangeSentenceWord WHERE ArrangeSentenceID = @ArrangeSentenceID;
        DELETE FROM ArrangeSentence_Lang WHERE ArrangeSentenceID = @ArrangeSentenceID;
        DELETE FROM ArrangeSentence WHERE ArrangeSentenceID = @ArrangeSentenceID;
    END
END
GO
/****** Object:  StoredProcedure [dbo].[sp_ArrangeSentence_Get]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_ArrangeSentence_Get]
(
    @LessonID INT,
    @LanguageID INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        ASL.ArrangeSentenceID,
        ASL.CorrectSentence,
        ASW.WordID,
        ASWL.WordText,
        ASW.CorrectOrder
    FROM ArrangeSentence ASN
    JOIN ArrangeSentence_Lang ASL 
        ON ASN.ArrangeSentenceID = ASL.ArrangeSentenceID 
       AND ASL.LanguageID = @LanguageID
    JOIN ArrangeSentenceWord ASW 
        ON ASN.ArrangeSentenceID = ASW.ArrangeSentenceID
    JOIN ArrangeSentenceWord_Lang ASWL 
        ON ASW.WordID = ASWL.WordID 
       AND ASWL.LanguageID = @LanguageID
    WHERE ASN.LessonID = @LessonID
    ORDER BY ASW.CorrectOrder;
END
GO
/****** Object:  StoredProcedure [dbo].[sp_Insert_ApiUsage]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_Insert_ApiUsage]
(
    @UserGuid UNIQUEIDENTIFIER,
    @Endpoint NVARCHAR(100)
)
AS
BEGIN
    IF EXISTS (
        SELECT 1 FROM ApiUsage
        WHERE UserGuid = @UserGuid
          AND Endpoint = @Endpoint
          AND UsageDate = CAST(GETUTCDATE() AS DATE)
    )
    BEGIN
        UPDATE ApiUsage
        SET RequestCount = RequestCount + 1
        WHERE UserGuid = @UserGuid
          AND Endpoint = @Endpoint
          AND UsageDate = CAST(GETUTCDATE() AS DATE);
    END
    ELSE
    BEGIN
        INSERT INTO ApiUsage(UserGuid, Endpoint, RequestCount, UsageDate)
        VALUES (@UserGuid, @Endpoint, 1, CAST(GETUTCDATE() AS DATE));
    END
END

GO
/****** Object:  StoredProcedure [dbo].[sp_Lesson]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[sp_Lesson]
(
    @Mode NVARCHAR(20),

    -- Common
    @LessonID INT = NULL,
    @LessonTypeID INT = NULL,
    @LessonOrder INT = NULL,
    @IsActive BIT = NULL,

    -- Language
    @LanguageID INT = NULL,
    @LessonName NVARCHAR(200) = NULL,
    @Description NVARCHAR(500) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    /* ================= ADD ================= */
    IF @Mode = 'ADD'
    BEGIN
        INSERT INTO Lesson (LessonTypeID, LessonOrder, IsActive)
        VALUES (@LessonTypeID, @LessonOrder, 1);

        SET @LessonID = SCOPE_IDENTITY();

        INSERT INTO Lesson_Lang (LessonID, LanguageID, LessonName, Description)
        VALUES (@LessonID, @LanguageID, @LessonName, @Description);

        SELECT @LessonID AS LessonID;
    END

    /* ================= EDIT ================= */
    ELSE IF @Mode = 'EDIT'
    BEGIN
        UPDATE Lesson
        SET LessonTypeID = @LessonTypeID,
            LessonOrder = @LessonOrder,
            IsActive = @IsActive
        WHERE LessonID = @LessonID;

        UPDATE Lesson_Lang
        SET LessonName = @LessonName,
            Description = @Description
        WHERE LessonID = @LessonID
          AND LanguageID = @LanguageID;
    END

    /* ================= DELETE ================= */
    ELSE IF @Mode = 'DELETE'
    BEGIN
        DELETE FROM Lesson_Lang WHERE LessonID = @LessonID;
        DELETE FROM Lesson WHERE LessonID = @LessonID;
    END

    /* ================= GET ================= */
    ELSE IF @Mode = 'GET'
    BEGIN
        SELECT 
            L.LessonID,
            LL.LessonName,
            LL.Description,
            LT.TypeName,
            L.LessonOrder,
            L.IsActive
        FROM Lesson L
        JOIN LessonType LT ON L.LessonTypeID = LT.LessonTypeID
        JOIN Lesson_Lang LL ON L.LessonID = LL.LessonID
        WHERE LL.LanguageID = @LanguageID;
    END

    /* ================= GETDETAIL ================= */
    ELSE IF @Mode = 'GETDETAIL'
    BEGIN
        SELECT 
            L.LessonID,
            L.LessonTypeID,
            L.LessonOrder,
            L.IsActive,
            LL.LanguageID,
            LL.LessonName,
            LL.Description
        FROM Lesson L
        JOIN Lesson_Lang LL ON L.LessonID = LL.LessonID
        WHERE L.LessonID = @LessonID;
    END
END;
GO
/****** Object:  StoredProcedure [dbo].[sp_Lesson_Get]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[sp_Lesson_Get]
(
    @LanguageID INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        L.LessonID,
        LT.TypeName AS LessonType,
        LL.LessonName,
        LL.Description,
        L.LessonOrder
    FROM Lesson L
    JOIN LessonType LT ON L.LessonTypeID = LT.LessonTypeID
    JOIN Lesson_Lang LL ON L.LessonID = LL.LessonID
    WHERE LL.LanguageID = @LanguageID
      AND L.IsActive = 1
    ORDER BY L.LessonOrder;
END
GO
/****** Object:  StoredProcedure [dbo].[sp_MeaningOption]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[sp_MeaningOption]
(
    @Mode NVARCHAR(20),
    @OptionID INT = NULL,
    @QuestionID INT = NULL,
    @LanguageID INT = NULL,
    @OptionText NVARCHAR(200) = NULL,
    @IsCorrect BIT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @Mode = 'ADD'
    BEGIN
        INSERT INTO MeaningOption (QuestionID, IsCorrect)
        VALUES (@QuestionID, @IsCorrect);

        SET @OptionID = SCOPE_IDENTITY();

        INSERT INTO MeaningOption_Lang
        VALUES (@OptionID, @LanguageID, @OptionText);
    END

    ELSE IF @Mode = 'EDIT'
    BEGIN
        UPDATE MeaningOption
        SET IsCorrect = @IsCorrect
        WHERE OptionID = @OptionID;

        UPDATE MeaningOption_Lang
        SET OptionText = @OptionText
        WHERE OptionID = @OptionID AND LanguageID = @LanguageID;
    END

    ELSE IF @Mode = 'DELETE'
    BEGIN
        DELETE FROM MeaningOption_Lang WHERE OptionID = @OptionID;
        DELETE FROM MeaningOption WHERE OptionID = @OptionID;
    END
END
GO
/****** Object:  StoredProcedure [dbo].[sp_MeaningQuestion]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[sp_MeaningQuestion]
(
    @Mode NVARCHAR(20),
    @QuestionID INT = NULL,
    @LessonID INT = NULL,
    @LanguageID INT = NULL,
    @QuestionText NVARCHAR(300) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @Mode = 'ADD'
    BEGIN
        INSERT INTO MeaningQuestion (LessonID)
        VALUES (@LessonID);

        SET @QuestionID = SCOPE_IDENTITY();

        INSERT INTO MeaningQuestion_Lang
        VALUES (@QuestionID, @LanguageID, @QuestionText);

        SELECT @QuestionID AS QuestionID;
    END

    ELSE IF @Mode = 'EDIT'
    BEGIN
        UPDATE MeaningQuestion_Lang
        SET QuestionText = @QuestionText
        WHERE QuestionID = @QuestionID AND LanguageID = @LanguageID;
    END

    ELSE IF @Mode = 'DELETE'
    BEGIN
        DELETE FROM MeaningOption_Lang
        WHERE OptionID IN (SELECT OptionID FROM MeaningOption WHERE QuestionID = @QuestionID);

        DELETE FROM MeaningOption WHERE QuestionID = @QuestionID;
        DELETE FROM MeaningQuestion_Lang WHERE QuestionID = @QuestionID;
        DELETE FROM MeaningQuestion WHERE QuestionID = @QuestionID;
    END

    ELSE IF @Mode = 'GET'
    BEGIN
        SELECT MQ.QuestionID, MQL.QuestionText
        FROM MeaningQuestion MQ
        JOIN MeaningQuestion_Lang MQL ON MQ.QuestionID = MQL.QuestionID
        WHERE MQ.LessonID = @LessonID AND MQL.LanguageID = @LanguageID;
    END

    ELSE IF @Mode = 'GETDETAIL'
    BEGIN
        SELECT * FROM MeaningQuestion_Lang
        WHERE QuestionID = @QuestionID;
    END
END
GO
/****** Object:  StoredProcedure [dbo].[sp_MeaningQuestion_Get]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_MeaningQuestion_Get]
(
    @LessonID INT,
    @LanguageID INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        MQ.QuestionID,
        MQL.QuestionText,
        MO.OptionID,
        MOL.OptionText
    FROM MeaningQuestion MQ
    JOIN MeaningQuestion_Lang MQL 
        ON MQ.QuestionID = MQL.QuestionID AND MQL.LanguageID = @LanguageID
    JOIN MeaningOption MO 
        ON MQ.QuestionID = MO.QuestionID
    JOIN MeaningOption_Lang MOL 
        ON MO.OptionID = MOL.OptionID AND MOL.LanguageID = @LanguageID
    WHERE MQ.LessonID = @LessonID
    ORDER BY MQ.QuestionID, MO.OptionID;
END
GO
/****** Object:  StoredProcedure [dbo].[sp_ReadingSentence]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[sp_ReadingSentence]
(
    @Mode NVARCHAR(20),
    @ReadingSentenceID INT = NULL,
    @LessonID INT = NULL,
    @LanguageID INT = NULL,
    @SentenceText NVARCHAR(500) = NULL,
    @ReferenceAudioUrl NVARCHAR(300) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @Mode = 'ADD'
    BEGIN
        INSERT INTO ReadingSentence (LessonID)
        VALUES (@LessonID);

        SET @ReadingSentenceID = SCOPE_IDENTITY();

        INSERT INTO ReadingSentence_Lang
        VALUES (@ReadingSentenceID, @LanguageID, @SentenceText, @ReferenceAudioUrl);
    END

    ELSE IF @Mode = 'EDIT'
    BEGIN
        UPDATE ReadingSentence_Lang
        SET SentenceText = @SentenceText,
            ReferenceAudioUrl = @ReferenceAudioUrl
        WHERE ReadingSentenceID = @ReadingSentenceID
          AND LanguageID = @LanguageID;
    END

    ELSE IF @Mode = 'DELETE'
    BEGIN
        DELETE FROM ReadingSentence_Lang WHERE ReadingSentenceID = @ReadingSentenceID;
        DELETE FROM ReadingSentence WHERE ReadingSentenceID = @ReadingSentenceID;
    END
END
GO
/****** Object:  StoredProcedure [dbo].[sp_ReadingSentence_Get]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[sp_ReadingSentence_Get]
(
    @LessonID INT,
    @LanguageID INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        RS.ReadingSentenceID,
        RSL.SentenceText,
        RSL.ReferenceAudioUrl
    FROM ReadingSentence RS
    JOIN ReadingSentence_Lang RSL 
        ON RS.ReadingSentenceID = RSL.ReadingSentenceID
    WHERE RS.LessonID = @LessonID
      AND RSL.LanguageID = @LanguageID;
END
GO
/****** Object:  StoredProcedure [dbo].[sp_User_Create]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_User_Create]
(
    @Email NVARCHAR(200),
    @PasswordHash NVARCHAR(500),
    @ApiKey NVARCHAR(200)
)
AS
BEGIN
    SET NOCOUNT ON;

    -- ALWAYS schema-qualify table names
    IF EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @Email)
    BEGIN
        RAISERROR('Email already exists', 16, 1);
        RETURN;
    END

    INSERT INTO dbo.Users
    (
        UserGuid,
        Email,
        PasswordHash,
        ApiKey,
        IsActive
    )
    VALUES
    (
        NEWID(),
        @Email,
        @PasswordHash,
        @ApiKey,
        1
    );
END;
GO
/****** Object:  StoredProcedure [dbo].[sp_User_Login]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_User_Login]
(
    @LoginInput NVARCHAR(100) -- email or mobile
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        ID,
        UserGuid,
        Email,
        MobileNumber,
        PasswordHash,
        ApiKey,
        IsActive,
		Role,
RefreshToken,
RefreshTokenExpiry,
RefreshTokenExpiry
    FROM Users
    WHERE IsActive = 1
      AND (Email = @LoginInput OR MobileNumber = @LoginInput);
END;
GO
/****** Object:  StoredProcedure [dbo].[sp_UserAnswer]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[sp_UserAnswer]
(
    @Mode NVARCHAR(20),
    @UserID INT = NULL,
    @LessonID INT = NULL,
    @LanguageID INT = NULL,
    @ActivityType NVARCHAR(50) = NULL,
    @ReferenceID INT = NULL,
    @IsCorrect BIT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @Mode = 'ADD'
    BEGIN
        INSERT INTO UserAnswer
        (UserID, LessonID, LanguageID, ActivityType, ReferenceID, IsCorrect)
        VALUES
        (@UserID, @LessonID, @LanguageID, @ActivityType, @ReferenceID, @IsCorrect);
    END

    ELSE IF @Mode = 'GET'
    BEGIN
        SELECT LessonID,
               COUNT(*) AS TotalAttempt,
               SUM(CASE WHEN IsCorrect = 1 THEN 1 ELSE 0 END) AS CorrectCount
        FROM UserAnswer
        WHERE UserID = @UserID AND LanguageID = @LanguageID
        GROUP BY LessonID;
    END
END
GO
/****** Object:  StoredProcedure [dbo].[sp_UserAnswer_Save]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[sp_UserAnswer_Save]
(
    @UserID INT,
    @LessonID INT,
    @LanguageID INT,
    @ActivityType NVARCHAR(50),
    @ReferenceID INT,
    @IsCorrect BIT
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO UserAnswer
    (
        UserID,
        LessonID,
        LanguageID,
        ActivityType,
        ReferenceID,
        IsCorrect
    )
    VALUES
    (
        @UserID,
        @LessonID,
        @LanguageID,
        @ActivityType,
        @ReferenceID,
        @IsCorrect
    );
END
GO
/****** Object:  StoredProcedure [dbo].[sp_UserProgress_Get]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[sp_UserProgress_Get]
(
    @UserID INT,
    @LanguageID INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        LessonID,
        COUNT(*) AS TotalAttempt,
        SUM(CASE WHEN IsCorrect = 1 THEN 1 ELSE 0 END) AS CorrectCount
    FROM UserAnswer
    WHERE UserID = @UserID
      AND LanguageID = @LanguageID
    GROUP BY LessonID;
END
GO
/****** Object:  StoredProcedure [dbo].[sp_Validate_ApiKey]    Script Date: 3/14/2026 11:24:16 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[sp_Validate_ApiKey]
(
    @ApiKey NVARCHAR(200)
)
AS
BEGIN
    SELECT UserGuid
    FROM Users
    WHERE ApiKey = @ApiKey AND IsActive = 1;
END

GO
