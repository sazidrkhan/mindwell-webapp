-- ==========================================
-- MindWell Database Schema Creation Script
-- ==========================================
-- This script creates the complete MindWell database schema for the current 30-page learning platform

-- Create database (if using script directly in SSMS)
-- Uncomment below if creating database from scratch
-- CREATE DATABASE MindWellDB;
-- GO
-- USE MindWellDB;
-- GO

-- ==========================================
-- 1. User Roles Table
-- ==========================================
CREATE TABLE UserRoles (
    RoleID INT PRIMARY KEY IDENTITY(1,1),
    RoleName NVARCHAR(50) UNIQUE NOT NULL,
    Description NVARCHAR(200),
    CreatedDate DATETIME DEFAULT GETDATE()
);

-- ==========================================
-- 2. Users Table
-- ==========================================
CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) UNIQUE NOT NULL,
    Email NVARCHAR(100) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(256) NOT NULL,
    FirstName NVARCHAR(50),
    LastName NVARCHAR(50),
    RoleID INT NOT NULL DEFAULT 2,
    ProfileBioText NVARCHAR(500),
    CreatedDate DATETIME DEFAULT GETDATE(),
    LastLoginDate DATETIME,
    IsActive BIT DEFAULT 1,
    IsBanned BIT DEFAULT 0,
    PreferredTheme NVARCHAR(10) DEFAULT 'light',
    FailedAttempts INT DEFAULT 0,
    LastFailedAttempt DATETIME,
    TotpSecret NVARCHAR(64) NULL,
    IsTotpEnabled BIT NOT NULL DEFAULT 0,
    PasswordResetRequired BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (RoleID) REFERENCES UserRoles(RoleID)
);

-- ==========================================
-- 3. Learning Modules Table
-- ==========================================
CREATE TABLE LearningModules (
    ModuleID INT PRIMARY KEY IDENTITY(1,1),
    ModuleName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    LearningOutcomes NVARCHAR(700),
    EstimatedMinutes INT,
    PracticePrompt NVARCHAR(700),
    ImageURL NVARCHAR(255),
    OrderIndex INT,
    CreatedDate DATETIME DEFAULT GETDATE(),
    UpdatedDate DATETIME
);

-- ==========================================
-- 4. Articles Table
-- ==========================================
CREATE TABLE Articles (
    ArticleID INT PRIMARY KEY IDENTITY(1,1),
    ModuleID INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    PreviewContent NVARCHAR(MAX),
    Content NVARCHAR(MAX),
    ImageURL NVARCHAR(255),
    CreatedDate DATETIME DEFAULT GETDATE(),
    UpdatedDate DATETIME,
    IsPublished BIT DEFAULT 1,
    IsMembersOnly BIT DEFAULT 1,
    FOREIGN KEY (ModuleID) REFERENCES LearningModules(ModuleID)
);

-- ==========================================
-- 5. Quizzes Table
-- ==========================================
CREATE TABLE Quizzes (
    QuizID INT PRIMARY KEY IDENTITY(1,1),
    ModuleID INT NOT NULL,
    QuizTitle NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500),
    PassingScore INT DEFAULT 70,
    IsPublished BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT GETDATE(),
    UpdatedDate DATETIME,
    FOREIGN KEY (ModuleID) REFERENCES LearningModules(ModuleID)
);

-- ==========================================
-- 6. Quiz Questions Table
-- ==========================================
CREATE TABLE QuizQuestions (
    QuestionID INT PRIMARY KEY IDENTITY(1,1),
    QuizID INT NOT NULL,
    QuestionText NVARCHAR(500) NOT NULL,
    OptionA NVARCHAR(200),
    OptionB NVARCHAR(200),
    OptionC NVARCHAR(200),
    OptionD NVARCHAR(200),
    CorrectAnswer CHAR(1),
    OrderIndex INT,
    SortOrder INT,
    Explanation NVARCHAR(700),
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (QuizID) REFERENCES Quizzes(QuizID)
);

-- ==========================================
-- 7. User Quiz Attempts Table
-- ==========================================
CREATE TABLE UserQuizAttempts (
    AttemptID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    QuizID INT NOT NULL,
    Score INT,
    Passed BIT,
    AttemptDate DATETIME DEFAULT GETDATE(),
    TimeSpentSeconds INT,
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (QuizID) REFERENCES Quizzes(QuizID)
);

-- ==========================================
-- 7b. User Quiz Attempt Answers Table
-- ==========================================
CREATE TABLE UserQuizAttemptAnswers (
    AttemptAnswerID INT PRIMARY KEY IDENTITY(1,1),
    AttemptID INT NOT NULL,
    QuestionID INT NOT NULL,
    SelectedAnswer CHAR(1) NULL,
    IsCorrect BIT NOT NULL,
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (AttemptID) REFERENCES UserQuizAttempts(AttemptID),
    FOREIGN KEY (QuestionID) REFERENCES QuizQuestions(QuestionID)
);

-- ==========================================
-- 8. User Progress Table
-- ==========================================
CREATE TABLE UserProgress (
    ProgressID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    ModuleID INT NOT NULL,
    IsStarted BIT DEFAULT 0,
    IsCompleted BIT DEFAULT 0,
    CompletionDate DATETIME,
    PercentageComplete INT DEFAULT 0,
    StartedDate DATETIME DEFAULT GETDATE(),
    UNIQUE(UserID, ModuleID),
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (ModuleID) REFERENCES LearningModules(ModuleID)
);

-- ==========================================
-- 9. Certificates Table
-- ==========================================
CREATE TABLE Certificates (
    CertificateID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    ModuleID INT NOT NULL,
    CertificateURL NVARCHAR(255),
    IssuedDate DATETIME DEFAULT GETDATE(),
    UNIQUE(UserID, ModuleID),
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (ModuleID) REFERENCES LearningModules(ModuleID)
);

-- ==========================================
-- 10. User Achievements Table
-- ==========================================
CREATE TABLE UserAchievements (
    AchievementID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    AchievementTitle NVARCHAR(100),
    AchievementIcon NVARCHAR(255),
    EarnedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);

-- ==========================================
-- 11. Mood Entries Table
-- ==========================================
CREATE TABLE MoodEntries (
    MoodEntryID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    EntryDate DATE NOT NULL,
    MoodLevel INT,
    Notes NVARCHAR(500),
    CreatedDate DATETIME DEFAULT GETDATE(),
    UNIQUE(UserID, EntryDate),
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);

-- ==========================================
-- 12. Forum Topics Table
-- ==========================================
CREATE TABLE ForumTopics (
    TopicID INT PRIMARY KEY IDENTITY(1,1),
    ModuleID INT,
    UserID INT NOT NULL,
    Title NVARCHAR(300) NOT NULL,
    Content NVARCHAR(MAX),
    ViewCount INT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    UpdatedDate DATETIME,
    FOREIGN KEY (ModuleID) REFERENCES LearningModules(ModuleID),
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);

-- ==========================================
-- 13. Forum Replies Table
-- ==========================================
CREATE TABLE ForumReplies (
    ReplyID INT PRIMARY KEY IDENTITY(1,1),
    TopicID INT NOT NULL,
    UserID INT NOT NULL,
    Content NVARCHAR(MAX),
    IsLiked BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    UpdatedDate DATETIME,
    FOREIGN KEY (TopicID) REFERENCES ForumTopics(TopicID),
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);

-- ==========================================
-- 14. Contacts Table (Contact Form Submissions)
-- ==========================================
CREATE TABLE Contacts (
    ContactID INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100),
    Email NVARCHAR(100),
    Subject NVARCHAR(200),
    Message NVARCHAR(MAX),
    IsResolved BIT DEFAULT 0,
    Status NVARCHAR(20) DEFAULT 'New', -- Contacts.Status
    AdminNotes NVARCHAR(1000),
    UpdatedDate DATETIME,
    ResolvedDate DATETIME,
    CreatedDate DATETIME DEFAULT GETDATE()
);

-- ==========================================
-- 14b. Activities Table
-- ==========================================
CREATE TABLE Activities (
    ActivityID INT PRIMARY KEY IDENTITY(1,1),
    ModuleID INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Prompt NVARCHAR(MAX) NOT NULL,
    IsPublished BIT DEFAULT 1,
    SortOrder INT DEFAULT 1,
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (ModuleID) REFERENCES LearningModules(ModuleID)
);

-- ==========================================
-- 14c. Activity Submissions Table
-- ==========================================
CREATE TABLE ActivitySubmissions (
    SubmissionID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    ActivityID INT NULL,
    ModuleID INT NULL,
    ActivityTitle NVARCHAR(200),
    ResponseText NVARCHAR(MAX),
    SubmittedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (ActivityID) REFERENCES Activities(ActivityID)
);

-- ==========================================
-- 15. Audit Log Table
-- ==========================================
CREATE TABLE AuditLog (
    LogID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT,
    Action NVARCHAR(200),
    TableName NVARCHAR(50),
    RecordID INT,
    OldValue NVARCHAR(MAX),
    NewValue NVARCHAR(MAX),
    IPAddress NVARCHAR(45),
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);

-- ==========================================
-- 16. Settings Table
-- ==========================================
CREATE TABLE Settings (
    SettingID INT PRIMARY KEY IDENTITY(1,1),
    SettingKey NVARCHAR(100) UNIQUE NOT NULL,
    SettingValue NVARCHAR(MAX),
    DataType NVARCHAR(50),
    UpdatedDate DATETIME DEFAULT GETDATE()
);

-- ==========================================
-- CREATE INDEXES FOR PERFORMANCE
-- ==========================================
CREATE INDEX IDX_Users_Username ON Users(Username);
CREATE INDEX IDX_Users_Email ON Users(Email);
CREATE INDEX IDX_Users_IsActive ON Users(IsActive);
CREATE INDEX IDX_Articles_ModuleID ON Articles(ModuleID);
CREATE INDEX IDX_Articles_IsPublished ON Articles(IsPublished);
CREATE INDEX IDX_Quizzes_IsPublished ON Quizzes(IsPublished);
CREATE INDEX IDX_UserProgress_UserID ON UserProgress(UserID);
CREATE INDEX IDX_UserProgress_ModuleID ON UserProgress(ModuleID);
CREATE INDEX IDX_UserProgress_IsCompleted ON UserProgress(IsCompleted);
CREATE INDEX IDX_MoodEntries_UserID ON MoodEntries(UserID);
CREATE INDEX IDX_MoodEntries_EntryDate ON MoodEntries(EntryDate);
CREATE INDEX IDX_AuditLog_UserID ON AuditLog(UserID);
CREATE INDEX IDX_AuditLog_CreatedDate ON AuditLog(CreatedDate);
CREATE INDEX IDX_QuizAttempts_UserID ON UserQuizAttempts(UserID);
CREATE INDEX IDX_QuizAttempts_QuizID ON UserQuizAttempts(QuizID);
CREATE INDEX IDX_UserQuizAttemptAnswers_AttemptID ON UserQuizAttemptAnswers(AttemptID);
CREATE INDEX IDX_Activities_ModuleID ON Activities(ModuleID);
CREATE INDEX IDX_Contacts_Status ON Contacts(Status);

PRINT 'MindWell Database Schema created successfully!';
GO
