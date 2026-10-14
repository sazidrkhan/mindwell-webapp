// Purpose: Creates, migrates, and seeds the LocalDB database automatically for demos,
// allowing the WebForms site to run without manual SQL setup during assessment.
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace MindWell
{
    public static class DatabaseInitializer
    {
        private static string _diagnosticsPath;

        public static void EnsureInitialized(string appDataPath, string sqlPath)
        {
            // Startup entry point: create the database schema, add supplemental tables, and seed demo content.
            string enabled = ConfigurationManager.AppSettings["AutoInitializeDatabase"];
            if (string.Equals(enabled, "false", StringComparison.OrdinalIgnoreCase))
                return;

            Directory.CreateDirectory(appDataPath);
            _diagnosticsPath = Path.Combine(appDataPath, "MindWellDiagnostics.log");

            try
            {
                using (var connection = OpenApplicationConnection(appDataPath))
                {
                    if (!TableExists(connection, "Users"))
                        ExecuteBatches(connection, File.ReadAllText(Path.Combine(sqlPath, "MindWellDB_Schema.sql")));

                    EnsureExtraTables(connection);
                    SeedCoreData(connection);
                    EnsureSupplementalLearningContent(connection);
                    EnsureFinalCleanDemoDatabase(connection);
                    LogSeedSummary(connection);
                }
            }
            catch (Exception ex)
            {
                LogDiagnostic("Database initialization failed", ex);
                throw;
            }
        }

        private static string GetConnectionString()
        {
            var configured = ConfigurationManager.ConnectionStrings["MindWellConnection"];
            return configured != null
                ? configured.ConnectionString
                : @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\MindWellDB.mdf;Initial Catalog=MindWellDB;Integrated Security=true;Connect Timeout=60;MultipleActiveResultSets=true;";
        }

        private static SqlConnection OpenApplicationConnection(string appDataPath)
        {
            // Retries connection and attempts LocalDB create/attach so first launch is more reliable.
            string connectionString = GetConnectionString();
            SqlException lastException = null;

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    var connection = new SqlConnection(connectionString);
                    connection.Open();
                    return connection;
                }
                catch (SqlException ex)
                {
                    lastException = ex;
                    Debug.WriteLine("MindWell database connection attempt " + attempt + " failed: " + ex.Message);
                    SqlConnection.ClearAllPools();

                    try
                    {
                        CreateLocalDatabase(appDataPath);
                    }
                    catch (SqlException createException)
                    {
                        lastException = createException;
                        Debug.WriteLine("MindWell database create/attach attempt " + attempt + " failed: " + createException.Message);
                    }

                    Thread.Sleep(1000 * attempt);
                }
            }

            throw lastException;
        }

        private static bool TableExists(SqlConnection connection, string tableName)
        {
            using (var command = new SqlCommand("SELECT COUNT(*) FROM sys.tables WHERE name = @Name", connection))
            {
                command.Parameters.AddWithValue("@Name", tableName);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static bool ColumnExists(SqlConnection connection, string tableName, string columnName)
        {
            using (var command = new SqlCommand("SELECT CASE WHEN COL_LENGTH(@TableName, @ColumnName) IS NULL THEN 0 ELSE 1 END", connection))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                command.Parameters.AddWithValue("@ColumnName", columnName);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static bool IndexExists(SqlConnection connection, string tableName, string indexName)
        {
            using (var command = new SqlCommand(@"
SELECT COUNT(*)
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name = @TableName AND i.name = @IndexName", connection))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                command.Parameters.AddWithValue("@IndexName", indexName);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static bool ForeignKeyExists(SqlConnection connection, string fkName)
        {
            using (var command = new SqlCommand("SELECT COUNT(*) FROM sys.foreign_keys WHERE name = @Name", connection))
            {
                command.Parameters.AddWithValue("@Name", fkName);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static void ExecuteBatches(SqlConnection connection, string sql)
        {
            foreach (string batch in Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(batch))
                    continue;

                using (var command = new SqlCommand(batch, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void Execute(SqlConnection connection, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection))
            {
                if (parameters != null && parameters.Length > 0)
                    command.Parameters.AddRange(parameters);

                command.ExecuteNonQuery();
            }
        }

        private static int ExecuteCount(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                if (parameters != null && parameters.Length > 0)
                    command.Parameters.AddRange(parameters);

                return command.ExecuteNonQuery();
            }
        }

        private static void LogCleanupCount(string label, int count)
        {
            LogDiagnostic("Cleanup summary: " + label + "=" + count);
        }

        private static string PollutionWhere(string expression)
        {
            return "(" + expression + " LIKE @Pattern OR " +
                   expression + " LIKE @PatternQaTemporary OR " +
                   expression + " LIKE @PatternQaFinal OR " +
                   expression + " LIKE @PatternRouteAudit OR " +
                   expression + " LIKE @PatternUltimateQa OR " +
                   expression + " LIKE @PatternDeepQa OR " +
                    expression + " LIKE @PatternDeepAdmin OR " +
                    expression + " LIKE @PatternAutomated OR " +
                    expression + " LIKE @PatternAutomation OR " +
                    expression + " LIKE @PatternCleanupCheck OR " +
                    expression + " LIKE @PatternCleanup OR " +
                    expression + " LIKE @PatternQaUser OR " +
                   expression + " LIKE @PatternOctest OR " +
                   expression + " LIKE @PatternDeepAuth OR " +
                   expression + " LIKE @PatternTemporary)";
        }

        private static SqlParameter[] PollutionParameters()
        {
            return new[]
            {
                new SqlParameter("@Pattern", "%QA%"),
                new SqlParameter("@PatternQaTemporary", "%QA Temporary%"),
                new SqlParameter("@PatternQaFinal", "%QA FINAL%"),
                new SqlParameter("@PatternRouteAudit", "%ROUTEAUDIT%"),
                new SqlParameter("@PatternUltimateQa", "%Ultimate QA%"),
                new SqlParameter("@PatternDeepQa", "%Deep QA%"),
                new SqlParameter("@PatternDeepAdmin", "%DeepAdmin%"),
                new SqlParameter("@PatternAutomated", "%Automated%"), // includes Automated contact records
                new SqlParameter("@PatternAutomation", "%automation%"),
                new SqlParameter("@PatternCleanupCheck", "%cleanup check%"),
                new SqlParameter("@PatternCleanup", "%cleanup%"),
                new SqlParameter("@PatternQaUser", "%qauser%"),
                new SqlParameter("@PatternOctest", "%octest%"),
                new SqlParameter("@PatternDeepAuth", "%deepauth%"),
                new SqlParameter("@PatternTemporary", "%Temporary%"),
                new SqlParameter("@QaPrefix", "qa%"),
                new SqlParameter("@QaUserPrefix", "qauser%"),
                new SqlParameter("@QaStudentPrefix", "qa[_]student%"),
                new SqlParameter("@QaSpecialPrefix", "qa[_]special%"),
                new SqlParameter("@RouteAuditPrefix", "routeaudit%"),
                new SqlParameter("@OctestPrefix", "octest%"),
                new SqlParameter("@DeepAuthPrefix", "deepauth%"),
                new SqlParameter("@ExampleTestEmail", "%@example.test")
            };
        }

        private static void CreateLocalDatabase(string appDataPath)
        {
            string mdfPath = Path.Combine(appDataPath, "MindWellDB.mdf");
            string ldfPath = Path.Combine(appDataPath, "MindWellDB_log.ldf");
            string expectedMdfPath = Path.GetFullPath(mdfPath);
            string mdf = mdfPath.Replace("'", "''");
            string ldf = ldfPath.Replace("'", "''");

            using (var connection = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=true;Connect Timeout=60;"))
            {
                connection.Open();
                string existingPath = GetAttachedDatabasePath(connection);
                if (string.IsNullOrEmpty(existingPath))
                {
                    LogDiagnostic("LocalDB attach check: MindWellDB is not currently attached.");
                }
                else if (PathsMatch(existingPath, expectedMdfPath))
                {
                    LogDiagnostic("LocalDB attach check: MindWellDB already attached to App_Data MDF at " + existingPath);
                    return;
                }
                else
                {
                    LogDiagnostic("LocalDB attach check: detaching existing MindWellDB from " + existingPath + " before using " + expectedMdfPath);
                    Execute(connection, "ALTER DATABASE MindWellDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
                    Execute(connection, "EXEC sp_detach_db @dbname = N'MindWellDB'");
                    LogDiagnostic("LocalDB attach check: detached old MindWellDB attachment; App_Data MDF remains intact.");
                }

                if (File.Exists(mdfPath))
                {
                    string attachSql = File.Exists(ldfPath)
                        ? @"
IF DB_ID('MindWellDB') IS NULL
CREATE DATABASE MindWellDB
ON (FILENAME = N'" + mdf + @"'),
   (FILENAME = N'" + ldf + @"')
FOR ATTACH"
                        : @"
IF DB_ID('MindWellDB') IS NULL
CREATE DATABASE MindWellDB
ON (FILENAME = N'" + mdf + @"')
FOR ATTACH_REBUILD_LOG";

                    LogDiagnostic(File.Exists(ldfPath)
                        ? "LocalDB attach check: attaching existing App_Data MDF and LDF."
                        : "LocalDB attach check: attaching existing App_Data MDF and rebuilding log.");
                    Execute(connection, attachSql);
                    return;
                }

                LogDiagnostic("LocalDB attach check: App_Data MDF missing; creating MindWellDB at " + expectedMdfPath);
                Execute(connection, @"
IF DB_ID('MindWellDB') IS NULL
CREATE DATABASE MindWellDB
ON (NAME = N'MindWellDB', FILENAME = N'" + mdf + @"')
LOG ON (NAME = N'MindWellDB_log', FILENAME = N'" + ldf + @"')");
            }
        }

        private static string GetAttachedDatabasePath(SqlConnection connection)
        {
            using (var command = new SqlCommand(@"
SELECT TOP 1 physical_name
FROM sys.master_files
WHERE database_id = DB_ID(N'MindWellDB') AND type = 0", connection))
            {
                object result = command.ExecuteScalar();
                return result == null || result == DBNull.Value ? string.Empty : result.ToString();
            }
        }

        private static bool PathsMatch(string existingPath, string expectedPath)
        {
            if (string.IsNullOrWhiteSpace(existingPath) || string.IsNullOrWhiteSpace(expectedPath))
                return false;

            return string.Equals(Path.GetFullPath(existingPath), Path.GetFullPath(expectedPath), StringComparison.OrdinalIgnoreCase);
        }

        private static int Count(SqlConnection connection, string table)
        {
            using (var command = new SqlCommand("SELECT COUNT(*) FROM " + table, connection))
            {
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static void LogSeedSummary(SqlConnection connection)
        {
            LogDiagnostic("Seed summary: " +
                "LearningModules=" + Count(connection, "LearningModules") + "; " +
                "Articles=" + Count(connection, "Articles") + "; " +
                "Resources=" + Count(connection, "Resources") + "; " +
                "Quizzes=" + Count(connection, "Quizzes") + "; " +
                "QuizQuestions=" + Count(connection, "QuizQuestions") + "; " +
                "UserQuizAttempts=" + Count(connection, "UserQuizAttempts") + "; " +
                "UserQuizAttemptAnswers=" + Count(connection, "UserQuizAttemptAnswers") + "; " +
                "Activities=" + Count(connection, "Activities") + "; " +
                "ActivitySubmissions=" + Count(connection, "ActivitySubmissions") + "; " +
                "MoodEntries=" + Count(connection, "MoodEntries") + "; " +
                "JournalEntries=" + Count(connection, "JournalEntries") + "; " +
                "ForumTopics=" + Count(connection, "ForumTopics") + "; " +
                "Contacts=" + Count(connection, "Contacts"));
        }

        private static void LogDiagnostic(string message, Exception ex = null)
        {
            try
            {
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " | " + message;
                if (ex != null)
                    line += Environment.NewLine + ex;

                Debug.WriteLine(line);
                string path = _diagnosticsPath;
                if (string.IsNullOrEmpty(path))
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "MindWellDiagnostics.log");

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.AppendAllText(path, line + Environment.NewLine);
            }
            catch (Exception logException)
            {
                Debug.WriteLine("MindWell diagnostic logging failed: " + logException.Message);
            }
        }

        private static void EnsureExtraTables(SqlConnection connection)
        {
            // Adds project-specific tables that extend the base schema for journals, activities, resources, and audit logs.
            Execute(connection, @"
IF OBJECT_ID('Resources','U') IS NULL
CREATE TABLE Resources (
    ResourceID INT PRIMARY KEY IDENTITY(1,1),
    ModuleID INT NULL,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500),
    ResourceType NVARCHAR(50),
    ResourceUrl NVARCHAR(255),
    IsPublished BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT GETDATE()
)");

            Execute(connection, @"
IF OBJECT_ID('Activities','U') IS NULL
CREATE TABLE Activities (
    ActivityID INT PRIMARY KEY IDENTITY(1,1),
    ModuleID INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Prompt NVARCHAR(MAX) NOT NULL,
    IsPublished BIT DEFAULT 1,
    SortOrder INT DEFAULT 1,
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (ModuleID) REFERENCES LearningModules(ModuleID)
)");

            Execute(connection, @"
IF OBJECT_ID('JournalEntries','U') IS NULL
CREATE TABLE JournalEntries (
    JournalID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    Title NVARCHAR(200),
    ReflectionText NVARCHAR(MAX),
    CreatedDate DATETIME DEFAULT GETDATE(),
    UpdatedDate DATETIME NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
)");

            Execute(connection, @"
IF OBJECT_ID('ActivitySubmissions','U') IS NULL
CREATE TABLE ActivitySubmissions (
    SubmissionID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    ActivityID INT NULL,
    ModuleID INT NULL,
    ActivityTitle NVARCHAR(200),
    ResponseText NVARCHAR(MAX),
    SubmittedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
)");

            EnsureSchemaMigrations(connection);

            Execute(connection, @"
IF OBJECT_ID('Certificates','U') IS NULL
CREATE TABLE Certificates (
    CertificateID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    ModuleID INT NOT NULL,
    CertificateURL NVARCHAR(255),
    IssuedDate DATETIME DEFAULT GETDATE(),
    UNIQUE(UserID, ModuleID),
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (ModuleID) REFERENCES LearningModules(ModuleID)
)");

            Execute(connection, @"
IF OBJECT_ID('UserAchievements','U') IS NULL
CREATE TABLE UserAchievements (
    AchievementID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    AchievementTitle NVARCHAR(100),
    AchievementIcon NVARCHAR(255),
    EarnedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
)");

            Execute(connection, @"
IF OBJECT_ID('AuditLog','U') IS NULL
CREATE TABLE AuditLog (
    LogID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NULL,
    Action NVARCHAR(200),
    TableName NVARCHAR(50),
    RecordID INT,
    OldValue NVARCHAR(MAX),
    NewValue NVARCHAR(MAX),
    IPAddress NVARCHAR(45),
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
)");
        }

        private static void EnsureSchemaMigrations(SqlConnection connection)
        {
            AddColumnIfMissing(connection, "Articles", "PreviewContent", "NVARCHAR(MAX) NULL");
            AlterColumn(connection, "Articles", "PreviewContent", "NVARCHAR(MAX) NULL");
            AddColumnIfMissing(connection, "Articles", "IsMembersOnly", "BIT NOT NULL CONSTRAINT DF_Articles_IsMembersOnly DEFAULT 1");
            AddColumnIfMissing(connection, "LearningModules", "LearningOutcomes", "NVARCHAR(700) NULL");
            AddColumnIfMissing(connection, "LearningModules", "EstimatedMinutes", "INT NULL");
            AddColumnIfMissing(connection, "LearningModules", "PracticePrompt", "NVARCHAR(700) NULL");
            AddColumnIfMissing(connection, "Quizzes", "IsPublished", "BIT NOT NULL CONSTRAINT DF_Quizzes_IsPublished DEFAULT 1");
            AddColumnIfMissing(connection, "QuizQuestions", "Explanation", "NVARCHAR(700) NULL");
            AddColumnIfMissing(connection, "QuizQuestions", "SortOrder", "INT NULL");
            AddColumnIfMissing(connection, "ActivitySubmissions", "ActivityID", "INT NULL");
            AddColumnIfMissing(connection, "Contacts", "Status", "NVARCHAR(20) NOT NULL CONSTRAINT DF_Contacts_Status DEFAULT 'New'");
            AddColumnIfMissing(connection, "Contacts", "AdminNotes", "NVARCHAR(1000) NULL");
            AddColumnIfMissing(connection, "Contacts", "UpdatedDate", "DATETIME NULL");
            AddColumnIfMissing(connection, "Contacts", "ResolvedDate", "DATETIME NULL");
            AddColumnIfMissing(connection, "Users", "TotpSecret", "NVARCHAR(64) NULL");
            AddColumnIfMissing(connection, "Users", "IsTotpEnabled", "BIT NOT NULL CONSTRAINT DF_Users_IsTotpEnabled DEFAULT 0");
            AddColumnIfMissing(connection, "Users", "PasswordResetRequired", "BIT NOT NULL DEFAULT 0");

            if (!TableExists(connection, "UserQuizAttemptAnswers"))
            {
                Execute(connection, @"
IF OBJECT_ID('UserQuizAttemptAnswers','U') IS NULL
CREATE TABLE UserQuizAttemptAnswers (
    AttemptAnswerID INT PRIMARY KEY IDENTITY(1,1),
    AttemptID INT NOT NULL,
    QuestionID INT NOT NULL,
    SelectedAnswer CHAR(1) NULL,
    IsCorrect BIT NOT NULL,
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (AttemptID) REFERENCES UserQuizAttempts(AttemptID),
    FOREIGN KEY (QuestionID) REFERENCES QuizQuestions(QuestionID)
)");
            }

            if (ColumnExists(connection, "ActivitySubmissions", "ActivityID") && !ForeignKeyExists(connection, "FK_ActivitySubmissions_Activities"))
                Execute(connection, @"ALTER TABLE ActivitySubmissions ADD CONSTRAINT FK_ActivitySubmissions_Activities FOREIGN KEY (ActivityID) REFERENCES Activities(ActivityID)");

            Execute(connection, @"UPDATE QuizQuestions SET SortOrder = OrderIndex WHERE SortOrder IS NULL");
            Execute(connection, @"UPDATE Articles SET PreviewContent = LEFT(REPLACE(REPLACE(REPLACE(Content, '<p>', ''), '</p>', ' '), '<br />', ' '), 4000) WHERE PreviewContent IS NULL OR PreviewContent = ''");
            Execute(connection, @"UPDATE Contacts SET Status = CASE WHEN IsResolved = 1 THEN 'Resolved' ELSE 'New' END WHERE Status IS NULL OR Status = ''");
            Execute(connection, @"UPDATE Quizzes SET IsPublished = 1 WHERE IsPublished IS NULL");

            CreateIndexIfMissing(connection, "UserQuizAttemptAnswers", "IDX_UserQuizAttemptAnswers_AttemptID", "CREATE INDEX IDX_UserQuizAttemptAnswers_AttemptID ON UserQuizAttemptAnswers(AttemptID)");
            CreateIndexIfMissing(connection, "Activities", "IDX_Activities_ModuleID", "CREATE INDEX IDX_Activities_ModuleID ON Activities(ModuleID)");
            CreateIndexIfMissing(connection, "Contacts", "IDX_Contacts_Status", "CREATE INDEX IDX_Contacts_Status ON Contacts(Status)");
            CreateIndexIfMissing(connection, "Quizzes", "IDX_Quizzes_IsPublished", "CREATE INDEX IDX_Quizzes_IsPublished ON Quizzes(IsPublished)");
        }

        private static void AddColumnIfMissing(SqlConnection connection, string tableName, string columnName, string definition)
        {
            if (!ColumnExists(connection, tableName, columnName))
                Execute(connection, "ALTER TABLE " + tableName + " ADD " + columnName + " " + definition);
        }

        private static void AlterColumn(SqlConnection connection, string tableName, string columnName, string definition)
        {
            if (ColumnExists(connection, tableName, columnName))
                Execute(connection, "ALTER TABLE " + tableName + " ALTER COLUMN " + columnName + " " + definition);
        }

        private static void CreateIndexIfMissing(SqlConnection connection, string tableName, string indexName, string createSql)
        {
            if (!IndexExists(connection, tableName, indexName))
                Execute(connection, createSql);
        }

        private static void SeedCoreData(SqlConnection connection)
        {
            // Inserts the core roles, demo accounts, learning modules, articles, and quiz records when absent.
            UpsertRole(connection, "Admin", "Administrator");
            UpsertRole(connection, "User", "Registered learner");
            UpsertRole(connection, "Guest", "Public visitor");

            UpsertModule(connection, 1, "Introduction to Mental Health", "Understand mental health foundations, warning signs, stigma, support systems, and help-seeking.", "Define mental health and wellbeing|Recognise common warning signs|Explain when and how to seek support", 45, "Create a personal support map with at least three trusted people or services.", 1);
            UpsertModule(connection, 2, "Stress Management", "Learn practical stress-management tools, boundaries, breathing, time management, and resilience habits.", "Identify personal stress triggers|Describe how stress affects the body|Build a realistic stress relief toolkit", 50, "Choose one current stressor and write a two-step coping plan for it.", 2);
            UpsertModule(connection, 3, "Mindfulness Basics", "Explore present-moment awareness, breathing, grounding, mindful study habits, and anxiety reduction.", "Practice grounding and breathing|Use mindful study routines|Plan a sustainable mindfulness routine", 40, "Complete a three-minute breathing practice and note what changed.", 3);
            UpsertModule(connection, 4, "Self-Care and Wellbeing", "Build sustainable sleep, nutrition, movement, emotional, social, and reflective self-care routines.", "Separate real self-care from avoidance|Review sleep, nutrition, and movement foundations|Build a weekly self-care plan", 45, "Plan one small sleep, movement, or connection habit for this week.", 4);

            UpsertQuiz(connection, 1, "Mental Health Foundations Quiz", "Check your understanding of core mental health concepts.", 70);
            UpsertQuiz(connection, 2, "Stress Management Quiz", "Check your understanding of stress-management tools.", 70);
            UpsertQuiz(connection, 3, "Mindfulness Basics Quiz", "Check your understanding of mindfulness concepts.", 70);
            UpsertQuiz(connection, 4, "Self-Care and Wellbeing Quiz", "Check your understanding of sustainable self-care.", 70);

            AddQuizQuestion(connection, 1, "What does mental health affect?", "Only emotions", "Thinking, feeling, relationships, and coping", "Only physical health", "Only work performance", "B", 1, "Mental health influences thoughts, emotions, relationships, learning, and coping.");
            AddQuizQuestion(connection, 1, "When should someone seek support?", "Never", "Only after a crisis", "When distress is persistent or unsafe", "Only if others insist", "C", 2, "Persistent, intense, unsafe, or disruptive distress is a clear signal to seek help.");
            AddQuizQuestion(connection, 2, "What is the first step in stress management?", "Ignore stress", "Identify stress signals and causes", "Stop all responsibilities", "Avoid people", "B", 1, "Noticing triggers and early warning signs helps you choose the right coping tool.");
            AddQuizQuestion(connection, 2, "Which is a healthy stress tool?", "Deep breathing", "Overworking", "Avoiding sleep", "Suppressing emotions", "A", 2, "Slow breathing can calm the body and create a pause before choosing a response.");
            AddQuizQuestion(connection, 3, "What is mindfulness?", "Judging every thought", "Present-moment awareness without judgement", "Avoiding emotions", "Thinking only about the future", "B", 1, "Mindfulness means noticing present experience without immediately judging or reacting.");
            AddQuizQuestion(connection, 3, "Which is a grounding technique?", "5-4-3-2-1 senses practice", "Skipping meals", "Staying online all night", "Ignoring anxiety", "A", 2, "The 5-4-3-2-1 practice uses senses to reconnect attention with the present.");
            AddQuizQuestion(connection, 4, "What is self-care?", "Selfish behaviour", "Routine wellbeing maintenance", "Only expensive activities", "Only relaxation", "B", 1, "Self-care is regular maintenance for physical, emotional, and social wellbeing.");
            AddQuizQuestion(connection, 4, "What makes self-care sustainable?", "Small repeatable habits", "All-or-nothing plans", "No routine", "Doing everything alone", "A", 2, "Small repeatable actions are easier to maintain than perfect all-or-nothing plans.");

            UpsertSetting(connection, "SiteName", "MindWell", "string");
            UpsertSetting(connection, "SiteEmail", "admin@mindwell.local", "string");
        }

        private static void UpsertRole(SqlConnection connection, string roleName, string description)
        {
            Execute(connection, @"
IF EXISTS (SELECT 1 FROM UserRoles WHERE RoleName=@RoleName)
    UPDATE UserRoles SET Description=@Description WHERE RoleName=@RoleName
ELSE
    INSERT INTO UserRoles (RoleName, Description, CreatedDate) VALUES (@RoleName,@Description,GETDATE())",
                new SqlParameter("@RoleName", roleName),
                new SqlParameter("@Description", description));
        }

        private static void UpsertModule(SqlConnection connection, int moduleId, string name, string description, string outcomes, int minutes, string prompt, int orderIndex)
        {
            Execute(connection, @"
IF EXISTS (SELECT 1 FROM LearningModules WHERE ModuleID=@ModuleID)
BEGIN
    UPDATE LearningModules
    SET ModuleName=@Name, Description=@Description, LearningOutcomes=@Outcomes, EstimatedMinutes=@Minutes, PracticePrompt=@Prompt, OrderIndex=@OrderIndex, UpdatedDate=GETDATE()
    WHERE ModuleID=@ModuleID
END
ELSE
BEGIN
    SET IDENTITY_INSERT LearningModules ON;
    INSERT INTO LearningModules (ModuleID, ModuleName, Description, LearningOutcomes, EstimatedMinutes, PracticePrompt, OrderIndex, CreatedDate)
    VALUES (@ModuleID,@Name,@Description,@Outcomes,@Minutes,@Prompt,@OrderIndex,GETDATE());
    SET IDENTITY_INSERT LearningModules OFF;
END",
                new SqlParameter("@ModuleID", moduleId),
                new SqlParameter("@Name", name),
                new SqlParameter("@Description", description),
                new SqlParameter("@Outcomes", outcomes),
                new SqlParameter("@Minutes", minutes),
                new SqlParameter("@Prompt", prompt),
                new SqlParameter("@OrderIndex", orderIndex));
        }

        private static void UpsertQuiz(SqlConnection connection, int moduleId, string title, string description, int passingScore)
        {
            Execute(connection, @"
DECLARE @QuizID INT = (SELECT TOP 1 QuizID FROM Quizzes WHERE ModuleID=@ModuleID AND QuizTitle=@Title ORDER BY QuizID);

IF @QuizID IS NULL
    SET @QuizID = (SELECT TOP 1 QuizID FROM Quizzes WHERE ModuleID=@ModuleID AND NULLIF(LTRIM(RTRIM(ISNULL(QuizTitle, ''))), '') IS NOT NULL ORDER BY QuizID);

IF @QuizID IS NOT NULL
BEGIN
    UPDATE Quizzes SET QuizTitle=@Title, Description=@Description, PassingScore=@PassingScore, IsPublished=1, UpdatedDate=GETDATE() WHERE QuizID=@QuizID;
    UPDATE Quizzes SET IsPublished=0, UpdatedDate=GETDATE() WHERE ModuleID=@ModuleID AND QuizID<>@QuizID;
END
ELSE
BEGIN
    INSERT INTO Quizzes (ModuleID, QuizTitle, Description, PassingScore, IsPublished, CreatedDate) VALUES (@ModuleID,@Title,@Description,@PassingScore,1,GETDATE());
END",
                new SqlParameter("@ModuleID", moduleId),
                new SqlParameter("@Title", title),
                new SqlParameter("@Description", description),
                new SqlParameter("@PassingScore", passingScore));
        }

        private static void UpsertSetting(SqlConnection connection, string key, string value, string dataType)
        {
            Execute(connection, @"
IF EXISTS (SELECT 1 FROM Settings WHERE SettingKey=@Key)
    UPDATE Settings SET SettingValue=@Value, DataType=@Type, UpdatedDate=GETDATE() WHERE SettingKey=@Key
ELSE
    INSERT INTO Settings (SettingKey, SettingValue, DataType, UpdatedDate) VALUES (@Key,@Value,@Type,GETDATE())",
                new SqlParameter("@Key", key),
                new SqlParameter("@Value", value),
                new SqlParameter("@Type", dataType));
        }

        private static void FixDemoUsers(SqlConnection connection)
        {
            UpsertUser(connection, "admin", "admin@mindwell.local", "Admin", "User", "Admin", "Admin123!");
            UpsertUser(connection, "student1", "student1@mindwell.local", "Student", "One", "User", "Student123!");
            UpsertUser(connection, "sazidrkhan", "sazidrkhan@mindwell.local", "Sazid R", "Khan", "Admin", "Sazid123!");
            UpsertUser(connection, "yal", "yal@mindwell.local", "Mariyam Iyala", "Ali", "Admin", "Yal123!");
            UpsertUser(connection, "eem", "eem@mindwell.local", "Mariyam Eemau", "Riza", "Admin", "Eem123!");
            UpsertUser(connection, "esha", "esha@mindwell.local", "Esha Tariq", "Want", "Admin", "Esha123!");
            UpsertUser(connection, "lecturer", "lecturer@mindwell.local", "Lecturer", "", "Admin", "Lecturer123!");
            UpsertUser(connection, "student2", "student2@mindwell.local", "Demo Student", "Two", "User", "Student123!");
            UpsertUser(connection, "student3", "student3@mindwell.local", "Demo Student", "Three", "User", "Student123!");
        }

        private static void EnsureSupplementalLearningContent(SqlConnection connection)
        {
            Execute(connection, @"
UPDATE LearningModules SET LearningOutcomes='Define mental health and wellbeing|Recognise common warning signs|Explain when and how to seek support', EstimatedMinutes=45, PracticePrompt='Create a personal support map with at least three trusted people or services.' WHERE ModuleID=1;
UPDATE LearningModules SET LearningOutcomes='Identify personal stress triggers|Describe how stress affects the body|Build a realistic stress relief toolkit', EstimatedMinutes=50, PracticePrompt='Choose one current stressor and write a two-step coping plan for it.' WHERE ModuleID=2;
UPDATE LearningModules SET LearningOutcomes='Practice grounding and breathing|Use mindful study routines|Plan a sustainable mindfulness routine', EstimatedMinutes=40, PracticePrompt='Complete a three-minute breathing practice and note what changed.' WHERE ModuleID=3;
UPDATE LearningModules SET LearningOutcomes='Separate real self-care from avoidance|Review sleep, nutrition, and movement foundations|Build a weekly self-care plan', EstimatedMinutes=45, PracticePrompt='Plan one small sleep, movement, or connection habit for this week.' WHERE ModuleID=4;
");

            AddArticle(connection, 1, "What Mental Health Really Means", MentalPreview("mental health as part of everyday learning, relationships, and coping"), BuildLesson("Mental health is part of overall health. It shapes how people think, feel, relate, learn, work, recover from stress, and ask for support.", "Good mental health does not mean being cheerful all the time. It means having enough safety, support, skills, rest, and connection to handle normal ups and downs. A person can live with a diagnosed condition and still build wellbeing through treatment, routines, trusted relationships, and practical coping tools. It is also possible to seem successful on the outside while needing help on the inside, so mental health should be discussed with care rather than assumptions.", "Write one sentence that describes what good mental health would look like during a normal study week for you."), true, null);
            AddArticle(connection, 1, "The Mental Health Continuum", MentalPreview("the way wellbeing can move from thriving to coping, struggling, or crisis"), BuildLesson("The mental health continuum is a practical model for noticing change before a situation becomes a crisis.", "People move along the continuum. A learner may feel connected and focused at the start of term, pressured during assessments, and overwhelmed after several nights of poor sleep. These shifts do not mean failure; they are information. Signs such as withdrawal, irritability, loss of interest, panic, hopelessness, or difficulty meeting normal responsibilities suggest it is time to respond earlier rather than wait.", "Choose one warning sign you notice in yourself and one supportive action you can take before it grows."), true, null);
            AddArticle(connection, 1, "Common Mental Health Conditions and Warning Signs", MentalPreview("common conditions, warning signs, and when support should become urgent"), BuildLesson("Mental health conditions can affect mood, thinking, behaviour, sleep, appetite, attention, energy, and relationships.", "Depression, anxiety disorders, trauma-related distress, eating disorders, substance-use problems, and other concerns are not character flaws. They are health issues that deserve accurate information and support. Warning signs include distress that lasts for weeks, sudden withdrawal, major sleep or appetite changes, panic, hopelessness, unsafe thoughts, self-harm, or a drop in daily functioning. One sign alone may not prove a diagnosis, but repeated or intense signs should not be ignored.", "List two signs that would tell you to speak to a trusted adult, counsellor, doctor, or emergency service."), true, null);
            AddArticle(connection, 1, "Breaking Stigma and Seeking Help", MentalPreview("respectful language, privacy, and practical first steps for asking for help"), BuildLesson("Stigma grows when mental health concerns are treated as weakness, attention-seeking, or personal failure.", "Helpful support starts with respectful language and privacy. Avoid diagnosing friends, sharing their story without consent, minimising their pain, or telling them to simply be positive. A better response is to listen, thank them for trusting you, ask what would help, and encourage qualified support when concerns are persistent, intense, unsafe, or difficult to manage alone. Seeking help can begin with one message: I am not coping well and I need support.", "Write a short help-seeking message you could send to a trusted person if you were struggling."), true, null);

            AddArticle(connection, 2, "Understanding Stress and Your Triggers", MentalPreview("how triggers, thoughts, body signals, and actions connect during stress"), BuildLesson("Stress is the body and mind responding to demand, uncertainty, pressure, or perceived threat.", "Some stress can support focus in short bursts, but persistent stress can drain energy and make ordinary tasks feel heavy. A trigger may be a deadline, conflict, financial pressure, health worry, perfectionism, noise, social pressure, or unclear expectations. The same situation can affect people differently, so stress management starts with observation rather than judgement. Notice what happened, what you thought, what your body did, what you did next, and what helped even slightly.", "Track one stressful moment today using four headings: trigger, body signal, thought, next helpful action."), true, null);
            AddArticle(connection, 2, "What Stress Does to the Body", MentalPreview("the stress response, chronic activation, and body-based recovery cues"), BuildLesson("When the brain senses pressure or threat, the body prepares for action by increasing alertness, heart rate, breathing, and muscle tension.", "This response is useful when it is short and matched to the situation. Long-term activation is different. It can affect sleep, digestion, concentration, immune function, mood, memory, and decision-making. People may feel restless, numb, irritable, tearful, or exhausted. Calming routines work because they send safety signals to the body. Slow breathing, walking, stretching, hydration, rest, realistic planning, and supportive conversation can help the nervous system reset.", "Choose one body-based strategy and practice it for two minutes before your next study block."), true, null);
            AddArticle(connection, 2, "Evidence-Based Stress Relief Toolkit", MentalPreview("a small toolkit of body, thinking, planning, and support strategies"), BuildLesson("A stress toolkit works best when it is small enough to use during a real busy day.", "Choose one body tool, one thinking tool, one planning tool, and one support tool. Body tools include slow breathing, stretching, walking, water, and grounding. Thinking tools include naming the problem, checking all-or-nothing thoughts, and asking what one next step is possible. Planning tools include time-boxing, prioritising, and breaking large assignments into smaller actions. Support tools include asking questions early, using campus or community support, and staying connected.", "Build a four-part toolkit with one tool from each category and keep it somewhere visible."), true, null);
            AddArticle(connection, 2, "Time Management and Boundaries for Students", MentalPreview("time-boxing, buffer time, and respectful boundaries for sustainable studying"), BuildLesson("Many students do not need a perfect schedule; they need a plan that makes starting easier and protects recovery time.", "Time-boxing means assigning a focused block to one task, followed by a short break or review. It reduces avoidance because the task has a defined beginning and end. Boundaries reduce stress by clarifying what you can do, when you are available, and what needs to wait. A boundary might be turning off notifications, asking for clarification early, declining an extra task, or setting a stopping time. A plan that requires constant exhaustion is not sustainable.", "Choose one task and time-box it for twenty minutes. Afterward, write what helped and what distracted you."), true, null);

            AddArticle(connection, 3, "Mindfulness Basics: Present-Moment Awareness", MentalPreview("mindfulness as present-moment awareness rather than forcing the mind to be empty"), BuildLesson("Mindfulness means paying attention to the present moment with curiosity and less judgement.", "It can involve breath, sound, movement, body sensations, eating, walking, or studying. Mindfulness is not forcing the mind to be blank. Minds wander; the practice is noticing the wandering and gently returning attention. This small return builds awareness and choice. For learners, mindfulness can create a pause between stress and reaction, which may help with overthinking, emotional awareness, and focus when practiced in short repeatable ways.", "Pause for three breaths. Label one thing you hear, one thing you feel, and one thing you can choose next."), true, null);
            AddArticle(connection, 3, "Breathing and Grounding Practices", MentalPreview("short breathing and grounding exercises for anxious or overwhelmed moments"), BuildLesson("Breathing and grounding practices give attention a safe place to land when anxiety or overload rises.", "A grounding practice uses the senses to reconnect with the present. The 5-4-3-2-1 method asks you to notice five things you can see, four you can feel, three you can hear, two you can smell, and one you can taste. A breathing practice can be simple: inhale slowly, exhale a little longer, and repeat for one to three minutes. The aim is not perfect calm; the aim is a safer pause and a little more choice.", "Try one grounding round and record which sense was easiest to notice."), true, null);
            AddArticle(connection, 3, "Mindful Study Habits and Overthinking", MentalPreview("using awareness to reduce rumination, distraction, and perfectionism while studying"), BuildLesson("Overthinking can feel productive, but it often repeats the same worry without moving toward action.", "Mindful study starts by noticing the loop: What am I thinking? What am I avoiding? What is the next small task? Choose one task, set a short timer, place distractions away, and begin imperfectly. When attention wanders, label it gently: planning, worrying, remembering, judging. Then return to the task. This does not remove pressure, but it reduces the fight with your own mind and turns attention into a skill you can practice.", "Before your next study session, write the single smallest next action and start with that."), true, null);
            AddArticle(connection, 3, "Building a Sustainable Mindfulness Routine", MentalPreview("a short, flexible routine connected to daily life instead of perfection"), BuildLesson("A mindfulness routine works best when it is small enough to repeat during ordinary days.", "One minute of breathing after waking, a mindful walk between classes, or three slow breaths before coursework can be enough to start. Link practice to an existing habit. After brushing teeth, pause for one breath. Before studying, notice posture and sound. After eating, notice one thing you appreciated. Consistency matters more than intensity. If you miss a day, restart with the smallest possible practice; that restart is part of the skill.", "Choose one daily anchor habit and attach a one-minute mindfulness practice to it."), true, null);

            AddArticle(connection, 4, "What Self-Care Really Means", MentalPreview("self-care as practical maintenance, not selfishness, luxury, or avoidance"), BuildLesson("Self-care means regular actions that protect health, functioning, and connection.", "It can include sleep, food, movement, medication routines, therapy, boundaries, rest, reflection, and asking for help. Self-care is not always comfortable. Sometimes it means doing laundry, making an appointment, leaving a harmful situation, or telling someone you are not okay. Rest is valid, but avoidance becomes a problem when it repeatedly makes life smaller or more stressful later. A useful question is: will this action support future me?", "Write one self-care action that is supportive but not necessarily easy."), true, null);
            AddArticle(connection, 4, "Sleep, Nutrition, and Movement Foundations", MentalPreview("how everyday physical routines support learning, mood, and stress recovery"), BuildLesson("Sleep, nutrition, and movement are foundations for mental wellbeing because they affect energy, attention, emotion regulation, and recovery.", "They do not solve every problem, and they are not substitutes for professional help, but they influence how much capacity a person has to cope. Sleep routines are strengthened by consistent timing, wind-down cues, lower light, and fewer late-night stimulants. Nutrition routines are strengthened by regular meals, hydration, and enough fuel for the day. Movement can be gentle, short, and realistic; a walk counts.", "Choose one foundation and make it specific: what, when, where, and how small can it be?"), true, null);
            AddArticle(connection, 4, "Emotional and Social Self-Care", MentalPreview("naming emotions, setting boundaries, and choosing supportive connection"), BuildLesson("Emotional self-care includes noticing feelings, naming needs, journaling, using coping skills, and seeking professional support when needed.", "It is not about suppressing emotions or staying positive all the time. Social self-care includes connection, boundaries, repair, and belonging. Supportive relationships can reduce isolation, but boundaries are also care. You can care about people and still limit requests, conversations, or environments that harm you. Healthy social care asks: who helps me feel safe, honest, and respected?", "Message one trusted person or write one boundary sentence you may need this week."), true, null);
            AddArticle(connection, 4, "Building a Weekly Self-Care Plan", MentalPreview("building a small weekly plan that fits coursework, energy, and support needs"), BuildLesson("A weekly self-care plan should be realistic enough to use during a busy week.", "Include one physical action, one emotional action, one social action, and one recovery action. Keep the plan visible and specific. Instead of exercise more, write walk for ten minutes after lunch on Tuesday. Instead of relax, write phone off at 10:30 and stretch for two minutes. Review the plan at the end of the week. What helped? What was too ambitious? What barrier appeared? Adjust the next plan instead of judging yourself.", "Create a four-part weekly plan with actions small enough to complete on a difficult day."), true, null);

            AddResource(connection, 1, "Further reading: What Mental Health Really Means", "WHO mental health topic page with broad public health context.", "Website", "https://www.who.int/health-topics/mental-health");
            AddResource(connection, 1, "Further reading: The Mental Health Continuum", "CDC mental health topic hub for understanding wellbeing and conditions.", "Website", "https://www.cdc.gov/mental-health/");
            AddResource(connection, 1, "Further reading: Common Mental Health Conditions and Warning Signs", "NIMH health topics index for mental health conditions and support information.", "Website", "https://www.nimh.nih.gov/health/topics");
            AddResource(connection, 1, "Further reading: Breaking Stigma and Seeking Help", "SAMHSA mental health information and help-seeking resources.", "Website", "https://www.samhsa.gov/mental-health");
            AddResource(connection, 2, "Further reading: Understanding Stress and Your Triggers", "APA stress topic hub with practical coping guidance.", "Website", "https://www.apa.org/topics/stress");
            AddResource(connection, 2, "Further reading: What Stress Does to the Body", "APA stress topic hub for understanding stress effects and supports.", "Website", "https://www.apa.org/topics/stress");
            AddResource(connection, 2, "Further reading: Evidence-Based Stress Relief Toolkit", "NHS self-help guides and activities for everyday mental wellbeing.", "Guide", "https://www.nhs.uk/mental-health/self-help/guides-tools-and-activities/");
            AddResource(connection, 2, "Further reading: Time Management and Boundaries for Students", "NHS five steps to mental wellbeing for practical routines.", "Guide", "https://www.nhs.uk/mental-health/self-help/guides-tools-and-activities/five-steps-to-mental-wellbeing/");
            AddResource(connection, 3, "Further reading: Mindfulness Basics", "NCCIH meditation and mindfulness overview with safety context.", "Research", "https://www.nccih.nih.gov/health/meditation-and-mindfulness-effectiveness-and-safety");
            AddResource(connection, 3, "Further reading: Breathing and Grounding Practices", "NHS mindfulness guidance for present-moment practice.", "Guide", "https://www.nhs.uk/mental-health/self-help/tips-and-support/mindfulness/");
            AddResource(connection, 3, "Further reading: Mindful Study Habits and Overthinking", "NIMH self-care guidance that supports focus and mental wellbeing.", "Guide", "https://www.nimh.nih.gov/health/topics/caring-for-your-mental-health");
            AddResource(connection, 3, "Further reading: Building a Sustainable Mindfulness Routine", "NCCIH mindfulness resource for safe, repeatable practice.", "Research", "https://www.nccih.nih.gov/health/meditation-and-mindfulness-effectiveness-and-safety");
            AddResource(connection, 4, "Further reading: What Self-Care Really Means", "NIMH caring for your mental health guide.", "Guide", "https://www.nimh.nih.gov/health/topics/caring-for-your-mental-health");
            AddResource(connection, 4, "Further reading: Sleep/Nutrition/Movement Foundations", "NHS sleep and tiredness guidance for healthy routines.", "Guide", "https://www.nhs.uk/live-well/sleep-and-tiredness/");
            AddResource(connection, 4, "Further reading: Emotional and Social Self-Care", "NHS Live Well guidance for everyday health routines.", "Website", "https://www.nhs.uk/live-well/");
            AddResource(connection, 4, "Further reading: Building a Weekly Self-Care Plan", "MedlinePlus mental health reference page for practical wellbeing habits.", "Website", "https://medlineplus.gov/mentalhealth.html");

            AddActivity(connection, 1, "Personal Support Map", "List three trusted people or services, one warning sign that means you should reach out, and one first message you could send.", 1);
            AddActivity(connection, 2, "Stress Trigger Plan", "Choose one current stress trigger. Write the body signs you notice, one coping tool, and one boundary or planning action.", 1);
            AddActivity(connection, 3, "Three-Minute Grounding Practice", "Complete a short breathing or 5-4-3-2-1 grounding practice, then describe what you noticed before and after.", 1);
            AddActivity(connection, 4, "Weekly Self-Care Plan", "Choose one physical, emotional, social, and recovery action for the next seven days. Keep each action small and specific.", 1);

            AddQuizQuestion(connection, 1, "What is a personal support map used for?", "Tracking grades only", "Identifying trusted people, services, and next steps", "Avoiding all difficult emotions", "Replacing professional help forever", "B", 3, "A support map helps you choose help quickly when stress or distress rises.");
            AddQuizQuestion(connection, 1, "Which response helps reduce mental health stigma?", "Using blame", "Ignoring concerns", "Using respectful and accurate language", "Sharing private details without consent", "C", 4, "Respectful language and privacy make help-seeking safer.");
            AddQuizQuestion(connection, 2, "What can long-term stress affect?", "Only free time", "Sleep, mood, concentration, and physical wellbeing", "Nothing important", "Only social media use", "B", 3, "Chronic stress can affect both body and mind.");
            AddQuizQuestion(connection, 2, "What is time-boxing?", "Doing every task at once", "Assigning focused time blocks to tasks", "Skipping breaks", "Waiting until motivation appears", "B", 4, "Time-boxing makes large tasks more manageable by creating focused blocks.");
            AddQuizQuestion(connection, 3, "What should you do when your mind wanders during breathing practice?", "Give up immediately", "Criticise yourself", "Gently return attention to the breath", "Hold your breath", "C", 3, "Noticing and returning is the practice.");
            AddQuizQuestion(connection, 3, "Mindfulness during difficult emotions helps you", "Notice emotions before reacting", "Delete emotions completely", "Avoid all responsibilities", "Win every argument", "A", 4, "Mindfulness creates a pause before response.");
            AddQuizQuestion(connection, 4, "Which habit supports better sleep?", "Irregular sleep timing every night", "A consistent wind-down routine", "Caffeine late at night", "Studying in bed for hours", "B", 3, "Predictable wind-down cues help the body prepare for sleep.");
            AddQuizQuestion(connection, 4, "A realistic self-care plan should be", "Large and perfect", "Small, repeatable, and matched to your week", "Only done during holidays", "Focused on one expensive activity", "B", 4, "Small actions are easier to repeat during real coursework weeks.");
        }

        private static void EnsureFinalCleanDemoDatabase(SqlConnection connection)
        {
            FixDemoUsers(connection);
            CleanupTestData(connection);
            EnsureCanonicalQuizzes(connection);
            CleanupTestData(connection);
            FixDemoUsers(connection);
            SeedCuratedDemoLearnerData(connection);
        }

        private static void CleanupInvalidQaData(SqlConnection connection)
        {
            CleanupTestData(connection);
        }

        private static void CleanupTestData(SqlConnection connection)
        {
            // Final coursework demo cleanup removes known QA/temp pollution without resetting real local usage data.
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                try
                {
                    // Create in the connection scope. Parameterized commands execute
                    // in sp_executesql; tables created inside it vanish on return.
                    ExecuteCount(connection, transaction, @"
IF OBJECT_ID('tempdb..#PollutedUsers') IS NOT NULL DROP TABLE #PollutedUsers;
CREATE TABLE #PollutedUsers (UserID INT PRIMARY KEY);
IF OBJECT_ID('tempdb..#PollutedModules') IS NOT NULL DROP TABLE #PollutedModules;
CREATE TABLE #PollutedModules (ModuleID INT PRIMARY KEY);
IF OBJECT_ID('tempdb..#PollutedQuizzes') IS NOT NULL DROP TABLE #PollutedQuizzes;
CREATE TABLE #PollutedQuizzes (QuizID INT PRIMARY KEY);
IF OBJECT_ID('tempdb..#PollutedQuestions') IS NOT NULL DROP TABLE #PollutedQuestions;
CREATE TABLE #PollutedQuestions (QuestionID INT PRIMARY KEY);
IF OBJECT_ID('tempdb..#PollutedActivities') IS NOT NULL DROP TABLE #PollutedActivities;
CREATE TABLE #PollutedActivities (ActivityID INT PRIMARY KEY);
IF OBJECT_ID('tempdb..#PollutedTopics') IS NOT NULL DROP TABLE #PollutedTopics;
CREATE TABLE #PollutedTopics (TopicID INT PRIMARY KEY);");
                    ExecuteCount(connection, transaction, @"
INSERT INTO #PollutedUsers (UserID)
SELECT UserID
FROM Users
WHERE Username LIKE @QaPrefix
   OR Username LIKE @QaUserPrefix
   OR Username LIKE @QaStudentPrefix
   OR Username LIKE @QaSpecialPrefix
   OR Username LIKE @RouteAuditPrefix
   OR Username LIKE @OctestPrefix
   OR Username LIKE @DeepAuthPrefix
   OR Email LIKE @ExampleTestEmail
   OR " + PollutionWhere("ISNULL(Username, '')") + " OR " + PollutionWhere("ISNULL(Email, '')") + " OR " + PollutionWhere("ISNULL(FirstName, '')") + " OR " + PollutionWhere("ISNULL(LastName, '')") + @";

INSERT INTO #PollutedModules (ModuleID)
SELECT ModuleID
FROM LearningModules
WHERE NULLIF(LTRIM(RTRIM(ISNULL(ModuleName, ''))), '') IS NULL
   OR " + PollutionWhere("ISNULL(ModuleName, '')") + " OR " + PollutionWhere("ISNULL(Description, '')") + @";

INSERT INTO #PollutedQuizzes (QuizID)
SELECT QuizID
FROM Quizzes
WHERE ModuleID IN (SELECT ModuleID FROM #PollutedModules)
   OR NULLIF(LTRIM(RTRIM(ISNULL(QuizTitle, ''))), '') IS NULL
   OR " + PollutionWhere("ISNULL(QuizTitle, '')") + " OR " + PollutionWhere("ISNULL(Description, '')") + @";

INSERT INTO #PollutedQuestions (QuestionID)
SELECT qq.QuestionID
FROM QuizQuestions qq
LEFT JOIN Quizzes q ON qq.QuizID=q.QuizID
WHERE qq.QuizID IN (SELECT QuizID FROM #PollutedQuizzes)
   OR NULLIF(LTRIM(RTRIM(ISNULL(qq.QuestionText, ''))), '') IS NULL
   OR " + PollutionWhere("ISNULL(q.QuizTitle, '')") + " OR " + PollutionWhere("ISNULL(qq.QuestionText, '')") + " OR " + PollutionWhere("ISNULL(qq.Explanation, '')") + @";

INSERT INTO #PollutedActivities (ActivityID)
SELECT ActivityID
FROM Activities
WHERE ModuleID IN (SELECT ModuleID FROM #PollutedModules)
   OR NULLIF(LTRIM(RTRIM(ISNULL(Title, ''))), '') IS NULL
   OR NULLIF(LTRIM(RTRIM(ISNULL(Prompt, ''))), '') IS NULL
   OR " + PollutionWhere("ISNULL(Title, '')") + " OR " + PollutionWhere("ISNULL(Prompt, '')") + @";

INSERT INTO #PollutedTopics (TopicID)
SELECT TopicID
FROM ForumTopics
WHERE UserID IN (SELECT UserID FROM #PollutedUsers)
   OR ModuleID IN (SELECT ModuleID FROM #PollutedModules)
   OR " + PollutionWhere("ISNULL(Title, '')") + " OR " + PollutionWhere("ISNULL(Content, '')"), PollutionParameters());

                    LogCleanupCount("polluted quiz attempt answers", ExecuteCount(connection, transaction, @"
DELETE aa
FROM UserQuizAttemptAnswers aa
INNER JOIN UserQuizAttempts a ON aa.AttemptID=a.AttemptID
WHERE a.UserID IN (SELECT UserID FROM #PollutedUsers)
   OR a.QuizID IN (SELECT QuizID FROM #PollutedQuizzes)
   OR aa.QuestionID IN (SELECT QuestionID FROM #PollutedQuestions);"));

                    LogCleanupCount("polluted quiz attempts", ExecuteCount(connection, transaction, @"
DELETE FROM UserQuizAttempts
WHERE UserID IN (SELECT UserID FROM #PollutedUsers)
   OR QuizID IN (SELECT QuizID FROM #PollutedQuizzes);"));

                    LogCleanupCount("polluted activity submissions", ExecuteCount(connection, transaction, @"
DELETE FROM ActivitySubmissions
WHERE UserID IN (SELECT UserID FROM #PollutedUsers)
   OR ActivityID IN (SELECT ActivityID FROM #PollutedActivities)
   OR ModuleID IN (SELECT ModuleID FROM #PollutedModules)
   OR " + PollutionWhere("ISNULL(ActivityTitle, '')") + " OR " + PollutionWhere("ISNULL(ResponseText, '')"), PollutionParameters()));

                    LogCleanupCount("polluted journal entries", ExecuteCount(connection, transaction, @"
DELETE FROM JournalEntries
WHERE UserID IN (SELECT UserID FROM #PollutedUsers)
   OR " + PollutionWhere("ISNULL(Title, '')") + " OR " + PollutionWhere("ISNULL(ReflectionText, '')"), PollutionParameters()));

                    LogCleanupCount("polluted mood entries", ExecuteCount(connection, transaction, @"
DELETE FROM MoodEntries
WHERE UserID IN (SELECT UserID FROM #PollutedUsers)
   OR " + PollutionWhere("ISNULL(Notes, '')"), PollutionParameters()));

                    LogCleanupCount("polluted certificates", ExecuteCount(connection, transaction, @"
DELETE FROM Certificates
WHERE UserID IN (SELECT UserID FROM #PollutedUsers)
   OR ModuleID IN (SELECT ModuleID FROM #PollutedModules);"));

                    LogCleanupCount("polluted achievements", ExecuteCount(connection, transaction, @"
DELETE FROM UserAchievements
WHERE UserID IN (SELECT UserID FROM #PollutedUsers)
   OR " + PollutionWhere("ISNULL(AchievementTitle, '')"), PollutionParameters()));

                    LogCleanupCount("polluted progress", ExecuteCount(connection, transaction, @"
DELETE FROM UserProgress
WHERE UserID IN (SELECT UserID FROM #PollutedUsers)
   OR ModuleID IN (SELECT ModuleID FROM #PollutedModules);"));

                    LogCleanupCount("polluted forum replies", ExecuteCount(connection, transaction, @"
DELETE FROM ForumReplies
WHERE TopicID IN (SELECT TopicID FROM #PollutedTopics)
   OR UserID IN (SELECT UserID FROM #PollutedUsers)
   OR " + PollutionWhere("ISNULL(Content, '')"), PollutionParameters()));

                    LogCleanupCount("polluted forum topics", ExecuteCount(connection, transaction, @"
DELETE FROM ForumTopics
WHERE TopicID IN (SELECT TopicID FROM #PollutedTopics);"));

                    LogCleanupCount("polluted contacts", ExecuteCount(connection, transaction, @"
DELETE FROM Contacts
WHERE Email LIKE @ExampleTestEmail
   OR " + PollutionWhere("ISNULL(Name, '')") + " OR " + PollutionWhere("ISNULL(Email, '')") + " OR " + PollutionWhere("ISNULL(Subject, '')") + " OR " + PollutionWhere("ISNULL(Message, '')") + " OR " + PollutionWhere("ISNULL(AdminNotes, '')"), PollutionParameters()));

                    LogCleanupCount("polluted audit log", ExecuteCount(connection, transaction, @"
DELETE FROM AuditLog
WHERE UserID IN (SELECT UserID FROM #PollutedUsers)
   OR " + PollutionWhere("ISNULL(Action, '')") + " OR " + PollutionWhere("ISNULL(TableName, '')") + " OR " + PollutionWhere("ISNULL(OldValue, '')") + " OR " + PollutionWhere("ISNULL(NewValue, '')"), PollutionParameters()));

                    LogCleanupCount("polluted users", ExecuteCount(connection, transaction, @"
DELETE FROM Users
WHERE UserID IN (SELECT UserID FROM #PollutedUsers);"));

                    LogCleanupCount("polluted quiz questions", ExecuteCount(connection, transaction, @"
DELETE FROM QuizQuestions
WHERE QuestionID IN (SELECT QuestionID FROM #PollutedQuestions);"));

                    LogCleanupCount("polluted quizzes", ExecuteCount(connection, transaction, @"
DELETE FROM Quizzes
WHERE QuizID IN (SELECT QuizID FROM #PollutedQuizzes);"));

                    LogCleanupCount("articles", ExecuteCount(connection, transaction, @"
DELETE FROM Articles
WHERE ModuleID IN (SELECT ModuleID FROM #PollutedModules)
   OR " + PollutionWhere("ISNULL(Title, '')") + " OR " + PollutionWhere("ISNULL(PreviewContent, '')") + " OR " + PollutionWhere("ISNULL(Content, '')"), PollutionParameters()));

                    LogCleanupCount("resources", ExecuteCount(connection, transaction, @"
DELETE FROM Resources
WHERE ModuleID IN (SELECT ModuleID FROM #PollutedModules)
   OR " + PollutionWhere("ISNULL(Title, '')") + " OR " + PollutionWhere("ISNULL(Description, '')") + " OR " + PollutionWhere("ISNULL(ResourceUrl, '')"), PollutionParameters()));

                    LogCleanupCount("polluted activities", ExecuteCount(connection, transaction, @"
DELETE FROM Activities
WHERE ActivityID IN (SELECT ActivityID FROM #PollutedActivities);"));

                    LogCleanupCount("polluted modules", ExecuteCount(connection, transaction, @"
DELETE FROM LearningModules
WHERE ModuleID IN (SELECT ModuleID FROM #PollutedModules);"));

                    transaction.Commit();
                    LogDiagnostic("Cleanup summary: persistence-safe demo cleanup committed.");
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private static void EnsureCanonicalQuizzes(SqlConnection connection)
        {
            // one active quiz per module for the coursework demo; duplicate quiz records stay archived unless safe to delete.
            ArchiveDuplicateQuizzes(connection);
            Execute(connection, @"
DECLARE @CanonicalQuizIds TABLE (ModuleID INT PRIMARY KEY, QuizID INT NOT NULL, QuizTitle NVARCHAR(200) NOT NULL);

INSERT INTO @CanonicalQuizIds (ModuleID, QuizID, QuizTitle)
SELECT canonical.ModuleID, canonical.QuizID, canonical.QuizTitle
FROM (VALUES
    (1, N'Mental Health Foundations Quiz'),
    (2, N'Stress Management Quiz'),
    (3, N'Mindfulness Basics Quiz'),
    (4, N'Self-Care and Wellbeing Quiz')
) expected(ModuleID, QuizTitle)
CROSS APPLY (
    SELECT TOP 1 q.ModuleID, q.QuizID, expected.QuizTitle
    FROM Quizzes q
    WHERE q.ModuleID=expected.ModuleID
    ORDER BY CASE WHEN q.QuizTitle=expected.QuizTitle THEN 0 ELSE 1 END, q.QuizID
) canonical;

UPDATE q
SET QuizTitle=c.QuizTitle,
    Description=CASE c.ModuleID
        WHEN 1 THEN N'Check your understanding of core mental health concepts.'
        WHEN 2 THEN N'Check your understanding of stress-management tools.'
        WHEN 3 THEN N'Check your understanding of mindfulness concepts.'
        ELSE N'Check your understanding of sustainable self-care.' END,
    PassingScore=70,
    IsPublished=1,
    UpdatedDate=GETDATE()
FROM Quizzes q
INNER JOIN @CanonicalQuizIds c ON q.QuizID=c.QuizID;

UPDATE q
SET IsPublished=0, UpdatedDate=GETDATE()
FROM Quizzes q
WHERE q.ModuleID BETWEEN 1 AND 4
  AND NOT EXISTS (SELECT 1 FROM @CanonicalQuizIds c WHERE c.QuizID=q.QuizID);

;WITH DuplicateQuestions AS (
    SELECT qq.QuestionID,
           ROW_NUMBER() OVER (PARTITION BY qq.QuizID, LTRIM(RTRIM(qq.QuestionText)) ORDER BY qq.QuestionID) AS DuplicateNumber
    FROM QuizQuestions qq
    INNER JOIN @CanonicalQuizIds c ON qq.QuizID=c.QuizID
)
DELETE aa
FROM UserQuizAttemptAnswers aa
INNER JOIN DuplicateQuestions dq ON aa.QuestionID=dq.QuestionID
WHERE dq.DuplicateNumber > 1;

;WITH DuplicateQuestions AS (
    SELECT qq.QuestionID,
           ROW_NUMBER() OVER (PARTITION BY qq.QuizID, LTRIM(RTRIM(qq.QuestionText)) ORDER BY qq.QuestionID) AS DuplicateNumber
    FROM QuizQuestions qq
    INNER JOIN @CanonicalQuizIds c ON qq.QuizID=c.QuizID
)
DELETE FROM QuizQuestions
WHERE QuestionID IN (SELECT QuestionID FROM DuplicateQuestions WHERE DuplicateNumber > 1);");
        }

        private static void ArchiveDuplicateQuizzes(SqlConnection connection)
        {
            Execute(connection, @"
;WITH RankedQuizzes AS (
    SELECT QuizID, ModuleID, QuizTitle,
           ROW_NUMBER() OVER (PARTITION BY ModuleID ORDER BY CASE WHEN IsPublished=1 THEN 0 ELSE 1 END, QuizID) AS rn
    FROM Quizzes
    WHERE ModuleID BETWEEN 1 AND 4
      AND NULLIF(LTRIM(RTRIM(ISNULL(QuizTitle, ''))), '') IS NOT NULL
)
UPDATE q
SET IsPublished=0, UpdatedDate=GETDATE()
FROM Quizzes q
INNER JOIN RankedQuizzes ranked ON q.QuizID=ranked.QuizID
WHERE ranked.rn > 1;");
        }

        private static void SeedCuratedDemoLearnerData(SqlConnection connection)
        {
            // Rebuilds deterministic learner history after cleanup so demos show realistic activity without QA/test accounts.
            UpsertDemoProgress(connection, "student1", 1, true, 100, -18);
            UpsertDemoProgress(connection, "student1", 2, true, 100, -12);
            UpsertDemoProgress(connection, "student1", 3, false, 70, -6);
            UpsertDemoProgress(connection, "student2", 1, false, 55, -15);
            UpsertDemoProgress(connection, "student2", 2, true, 100, -10);
            UpsertDemoProgress(connection, "student2", 3, false, 45, -4);
            UpsertDemoProgress(connection, "student3", 2, false, 50, -14);
            UpsertDemoProgress(connection, "student3", 3, true, 100, -8);
            UpsertDemoProgress(connection, "student3", 4, false, 60, -2);

            UpsertDemoQuizAttempt(connection, "student1", 1, 88, true, -17, 540);
            UpsertDemoQuizAttempt(connection, "student1", 2, 82, true, -11, 620);
            UpsertDemoQuizAttempt(connection, "student1", 3, 68, false, -5, 575);
            UpsertDemoQuizAttempt(connection, "student2", 1, 64, false, -14, 590);
            UpsertDemoQuizAttempt(connection, "student2", 2, 92, true, -9, 575);
            UpsertDemoQuizAttempt(connection, "student2", 3, 58, false, -3, 510);
            UpsertDemoQuizAttempt(connection, "student3", 2, 72, true, -13, 560);
            UpsertDemoQuizAttempt(connection, "student3", 3, 100, true, -7, 480);
            UpsertDemoQuizAttempt(connection, "student3", 4, 76, true, -2, 530);

            UpsertDemoMood(connection, "student1", -24, 3, "Started the week tired but noticed stress earlier than usual.");
            UpsertDemoMood(connection, "student1", -21, 4, "Felt steadier after writing a support plan and taking a short walk.");
            UpsertDemoMood(connection, "student1", -18, 4, "Asked for clarification before the assignment felt too large.");
            UpsertDemoMood(connection, "student1", -15, 3, "Energy dipped after a late night, so I protected a shorter study block.");
            UpsertDemoMood(connection, "student1", -12, 5, "Finished a module and felt proud without rushing into the next task.");
            UpsertDemoMood(connection, "student1", -9, 4, "Used a short breathing reset before revising.");
            UpsertDemoMood(connection, "student1", -6, 3, "Busy coursework day, but breathing practice helped before studying.");
            UpsertDemoMood(connection, "student1", -2, 4, "A message to a classmate made the workload feel less isolating.");
            UpsertDemoMood(connection, "student2", -23, 4, "Used time-boxing and finished one task without rushing.");
            UpsertDemoMood(connection, "student2", -20, 3, "Felt distracted, then set a boundary around notifications.");
            UpsertDemoMood(connection, "student2", -17, 4, "A water break and task list made the afternoon easier.");
            UpsertDemoMood(connection, "student2", -14, 2, "Stress was high, so I chose one essential task instead of everything.");
            UpsertDemoMood(connection, "student2", -11, 4, "The stress map helped me separate triggers from body signals.");
            UpsertDemoMood(connection, "student2", -8, 5, "Completed a quiz and noticed confidence improving.");
            UpsertDemoMood(connection, "student2", -5, 4, "Tried a short walk before study and returned calmer.");
            UpsertDemoMood(connection, "student2", -1, 3, "Still busy, but planning tomorrow reduced rumination.");
            UpsertDemoMood(connection, "student3", -22, 3, "Grounding practice helped settle pre-class worry.");
            UpsertDemoMood(connection, "student3", -19, 4, "Named five visible details before presenting and felt more present.");
            UpsertDemoMood(connection, "student3", -16, 4, "Used a self-care check to choose rest instead of more scrolling.");
            UpsertDemoMood(connection, "student3", -13, 3, "Felt overloaded but wrote down a realistic next step.");
            UpsertDemoMood(connection, "student3", -10, 5, "A good sleep routine made studying feel easier.");
            UpsertDemoMood(connection, "student3", -7, 4, "Completed the grounding module and saved the exercise for later.");
            UpsertDemoMood(connection, "student3", -4, 4, "Connection helped; I checked in with a friend after class.");
            UpsertDemoMood(connection, "student3", -2, 3, "Used reflection instead of judging a difficult day.");

            UpsertDemoJournal(connection, "student1", "Support plan check-in", "I listed trusted people, early warning signs, and one message I can send before stress becomes too high.", -18);
            UpsertDemoJournal(connection, "student1", "Reframing help-seeking", "Asking early felt awkward, but it gave me options before the deadline became urgent.", -12);
            UpsertDemoJournal(connection, "student1", "Module three reflection", "Mindfulness was more useful when I treated it as a pause rather than a perfect calm state.", -5);
            UpsertDemoJournal(connection, "student2", "Stress reset notes", "Time-boxing one task made the work feel smaller. Next time I will add a short break before starting again.", -16);
            UpsertDemoJournal(connection, "student2", "Boundary practice", "Stopping at a planned time made me less resentful and helped me restart the next morning.", -10);
            UpsertDemoJournal(connection, "student2", "Quiz review plan", "The missed questions showed I need to notice body signals earlier, not just work harder.", -3);
            UpsertDemoJournal(connection, "student3", "Grounding reflection", "The 5-4-3-2-1 practice made the room feel more present and gave me a clearer next step.", -14);
            UpsertDemoJournal(connection, "student3", "Self-care foundations", "A simple sleep cue and a message to a friend feel more realistic than a perfect routine.", -8);
            UpsertDemoJournal(connection, "student3", "Weekly wellbeing review", "Small routines were easier to repeat when I picked one area instead of trying to fix everything.", -2);

            UpsertDemoActivitySubmission(connection, "student1", 1, "I would contact a trusted friend, my lecturer, and campus support. My first message would be: I am not coping well today and need help choosing the next step.", -17);
            UpsertDemoActivitySubmission(connection, "student1", 2, "My current trigger is deadline pressure. Body signs are tight shoulders and fast thoughts. I will time-box twenty minutes and ask one clarification question.", -11);
            UpsertDemoActivitySubmission(connection, "student2", 1, "My support map includes a classmate, my tutor, and a quiet study place where I can reset before decisions feel urgent.", -14);
            UpsertDemoActivitySubmission(connection, "student2", 3, "Before grounding I felt scattered. After naming senses, my breathing slowed and I chose to review one paragraph instead of the whole chapter.", -6);
            UpsertDemoActivitySubmission(connection, "student3", 3, "I used sight, touch, and sound to slow down before class. The exercise helped me choose one practical next step.", -9);
            UpsertDemoActivitySubmission(connection, "student3", 4, "This week I will protect sleep, one short walk, and one honest check-in instead of trying to perfect every habit.", -2);

            UpsertDemoForumTopic(connection, "student1", "What helps you ask for support early?", "I am trying to ask before stress becomes a crisis. Short scripts make it easier for me.", 1, "student2", "A prepared message helps me too. I keep one in my notes app.", -12);
            UpsertDemoForumTopic(connection, "student2", "Small reset routines during assessments", "Time-boxing and a water break helped me restart without overworking.", 2, "student3", "I like pairing that with one grounding round before I continue.", -7);
            UpsertDemoForumTopic(connection, "student3", "Making self-care realistic", "I do better when I choose one repeatable habit instead of a long checklist.", 4, "student1", "That makes sense. One small habit is easier to protect during coursework.", -3);
        }

        private static void UpsertDemoProgress(SqlConnection connection, string username, int moduleId, bool isCompleted, int percentage, int daysOffset)
        {
            Execute(connection, @"
DECLARE @UserID INT = (SELECT UserID FROM Users WHERE Username=@Username);
IF @UserID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM UserProgress WHERE UserID=@UserID AND ModuleID=@ModuleID)
BEGIN
    INSERT INTO UserProgress (UserID, ModuleID, IsStarted, IsCompleted, CompletionDate, PercentageComplete, StartedDate)
    VALUES (@UserID,@ModuleID,1,@IsCompleted,CASE WHEN @IsCompleted=1 THEN DATEADD(day,@DaysOffset + 1,GETDATE()) ELSE NULL END,@Percentage,DATEADD(day,@DaysOffset,GETDATE()));
END",
                new SqlParameter("@Username", username),
                new SqlParameter("@ModuleID", moduleId),
                new SqlParameter("@IsCompleted", isCompleted),
                new SqlParameter("@Percentage", percentage),
                new SqlParameter("@DaysOffset", daysOffset));
        }

        private static void UpsertDemoQuizAttempt(SqlConnection connection, string username, int moduleId, int score, bool passed, int daysOffset, int seconds)
        {
            int correctCount = Math.Max(1, Math.Min(4, (score + 24) / 25));
            Execute(connection, @"
DECLARE @UserID INT = (SELECT UserID FROM Users WHERE Username=@Username);
DECLARE @QuizID INT = (SELECT TOP 1 QuizID FROM Quizzes WHERE ModuleID=@ModuleID AND IsPublished=1 ORDER BY QuizID);
DECLARE @AttemptDate DATETIME = DATEADD(day,@DaysOffset,GETDATE());
IF @UserID IS NOT NULL AND @QuizID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM UserQuizAttempts WHERE UserID=@UserID AND QuizID=@QuizID)
BEGIN
    INSERT INTO UserQuizAttempts (UserID, QuizID, Score, Passed, AttemptDate, TimeSpentSeconds)
    VALUES (@UserID,@QuizID,@Score,@Passed,@AttemptDate,@Seconds);

    DECLARE @AttemptID INT = SCOPE_IDENTITY();
    ;WITH OrderedQuestions AS (
        SELECT TOP 4 QuestionID, CorrectAnswer,
               ROW_NUMBER() OVER (ORDER BY ISNULL(SortOrder, OrderIndex), QuestionID) AS rn
        FROM QuizQuestions
        WHERE QuizID=@QuizID
        ORDER BY ISNULL(SortOrder, OrderIndex), QuestionID
    )
    INSERT INTO UserQuizAttemptAnswers (AttemptID, QuestionID, SelectedAnswer, IsCorrect, CreatedDate)
    SELECT @AttemptID,
           QuestionID,
           CASE WHEN rn <= @CorrectCount THEN CorrectAnswer ELSE CASE WHEN CorrectAnswer='A' THEN 'B' ELSE 'A' END END,
           CASE WHEN rn <= @CorrectCount THEN 1 ELSE 0 END,
           DATEADD(minute,rn,@AttemptDate)
    FROM OrderedQuestions;
END",
                new SqlParameter("@Username", username),
                new SqlParameter("@ModuleID", moduleId),
                new SqlParameter("@Score", score),
                new SqlParameter("@Passed", passed),
                new SqlParameter("@DaysOffset", daysOffset),
                new SqlParameter("@Seconds", seconds),
                new SqlParameter("@CorrectCount", correctCount));
        }

        private static void UpsertDemoMood(SqlConnection connection, string username, int daysOffset, int moodLevel, string notes)
        {
            Execute(connection, @"
DECLARE @UserID INT = (SELECT UserID FROM Users WHERE Username=@Username);
DECLARE @EntryDate DATE = CAST(DATEADD(day,@DaysOffset,GETDATE()) AS DATE);
IF @UserID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM MoodEntries WHERE UserID=@UserID AND Notes=@Notes)
BEGIN
    INSERT INTO MoodEntries (UserID, EntryDate, MoodLevel, Notes, CreatedDate) VALUES (@UserID,@EntryDate,@MoodLevel,@Notes,DATEADD(day,@DaysOffset,GETDATE()));
END",
                new SqlParameter("@Username", username),
                new SqlParameter("@DaysOffset", daysOffset),
                new SqlParameter("@MoodLevel", moodLevel),
                new SqlParameter("@Notes", notes));
        }

        private static void UpsertDemoJournal(SqlConnection connection, string username, string title, string reflection, int daysOffset)
        {
            Execute(connection, @"
DECLARE @UserID INT = (SELECT UserID FROM Users WHERE Username=@Username);
IF @UserID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM JournalEntries WHERE UserID=@UserID AND Title=@Title)
BEGIN
    INSERT INTO JournalEntries (UserID, Title, ReflectionText, CreatedDate, UpdatedDate)
    VALUES (@UserID,@Title,@Reflection,DATEADD(day,@DaysOffset,GETDATE()),DATEADD(day,@DaysOffset,GETDATE()));
END",
                new SqlParameter("@Username", username),
                new SqlParameter("@Title", title),
                new SqlParameter("@Reflection", reflection),
                new SqlParameter("@DaysOffset", daysOffset));
        }

        private static void UpsertDemoActivitySubmission(SqlConnection connection, string username, int moduleId, string response, int daysOffset)
        {
            Execute(connection, @"
DECLARE @UserID INT = (SELECT UserID FROM Users WHERE Username=@Username);
DECLARE @ActivityID INT = (SELECT TOP 1 ActivityID FROM Activities WHERE ModuleID=@ModuleID AND IsPublished=1 ORDER BY SortOrder, ActivityID);
DECLARE @ActivityTitle NVARCHAR(200) = (SELECT Title FROM Activities WHERE ActivityID=@ActivityID);
IF @UserID IS NOT NULL AND @ActivityID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ActivitySubmissions WHERE UserID=@UserID AND ActivityID=@ActivityID)
BEGIN
    INSERT INTO ActivitySubmissions (UserID, ActivityID, ModuleID, ActivityTitle, ResponseText, SubmittedDate)
    VALUES (@UserID,@ActivityID,@ModuleID,@ActivityTitle,@Response,DATEADD(day,@DaysOffset,GETDATE()));
END",
                new SqlParameter("@Username", username),
                new SqlParameter("@ModuleID", moduleId),
                new SqlParameter("@Response", response),
                new SqlParameter("@DaysOffset", daysOffset));
        }

        private static void UpsertDemoForumTopic(SqlConnection connection, string username, string title, string content, int moduleId, string replyUsername, string replyContent, int daysOffset)
        {
            Execute(connection, @"
DECLARE @UserID INT = (SELECT UserID FROM Users WHERE Username=@Username);
DECLARE @ReplyUserID INT = (SELECT UserID FROM Users WHERE Username=@ReplyUsername);
IF @UserID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ForumTopics WHERE UserID=@UserID AND Title=@Title)
BEGIN
    INSERT INTO ForumTopics (ModuleID, UserID, Title, Content, ViewCount, CreatedDate, UpdatedDate)
    VALUES (@ModuleID,@UserID,@Title,@Content,3,DATEADD(day,@DaysOffset,GETDATE()),DATEADD(day,@DaysOffset,GETDATE()));

    DECLARE @TopicID INT = SCOPE_IDENTITY();
    IF @ReplyUserID IS NOT NULL
        INSERT INTO ForumReplies (TopicID, UserID, Content, IsLiked, CreatedDate, UpdatedDate)
        VALUES (@TopicID,@ReplyUserID,@ReplyContent,0,DATEADD(day,@DaysOffset + 1,GETDATE()),DATEADD(day,@DaysOffset + 1,GETDATE()));
END",
                new SqlParameter("@Username", username),
                new SqlParameter("@Title", title),
                new SqlParameter("@Content", content),
                new SqlParameter("@ModuleID", moduleId),
                new SqlParameter("@ReplyUsername", replyUsername),
                new SqlParameter("@ReplyContent", replyContent),
                new SqlParameter("@DaysOffset", daysOffset));
        }

        private static string MentalPreview(string focus)
        {
            return "This lesson introduces " + focus + ". It gives learners enough context to understand the topic before signing in for the full guided lesson. The preview is educational, practical, and designed for students who need clear language rather than clinical jargon. It encourages early support, safe reflection, and small next steps while reminding visitors that MindWell is a learning platform, not a replacement for professional care.";
        }

        private static string BuildLesson(string opening, string learning, string practice)
        {
            return opening + "\n\n" +
                   learning + "\n\n" +
                   "A useful way to work with this lesson is to connect the idea to a real week rather than treating it as abstract advice. Notice what happens in your body, what thoughts repeat, what support you already have, and which small action would make tomorrow safer or more manageable. Learning about mental health is most helpful when it becomes a practical habit, not another standard to judge yourself by.\n\n" +
                   "Key takeaway: Small, timely support actions are usually more effective than waiting until stress or distress becomes overwhelming. Awareness, respectful language, routines, and trusted support can all protect wellbeing.\n\n" +
                   "Try this / reflection prompt: " + practice + "\n\n" +
                   "MindWell is educational and does not provide diagnosis, therapy, crisis response, or emergency care. If distress feels persistent, intense, unsafe, or difficult to manage alone, contact a trusted person, qualified professional, crisis line, or emergency service.";
        }

        private static void AddArticle(SqlConnection connection, int moduleId, string title, string preview, string content, bool isMembersOnly, string imageUrl)
        {
            Execute(connection, @"
IF NOT EXISTS (SELECT 1 FROM Articles WHERE ModuleID=@ModuleID AND Title=@Title)
BEGIN
    INSERT INTO Articles (ModuleID, Title, PreviewContent, Content, ImageURL, IsMembersOnly, IsPublished, CreatedDate)
    VALUES (@ModuleID, @Title, @PreviewContent, @Content, @ImageURL, @IsMembersOnly, 1, GETDATE())
END
ELSE
BEGIN
    UPDATE Articles
    SET PreviewContent=@PreviewContent,
        Content=@Content,
        ImageURL=@ImageURL,
        IsMembersOnly=@IsMembersOnly,
        IsPublished=1,
        UpdatedDate=GETDATE()
    WHERE ModuleID=@ModuleID AND Title=@Title
    AND (Content IS NULL OR LEN(Content) < 1000 OR PreviewContent IS NULL OR LEN(PreviewContent) < 300)
END",
                new SqlParameter("@ModuleID", moduleId),
                new SqlParameter("@Title", title),
                new SqlParameter("@PreviewContent", preview),
                new SqlParameter("@Content", content),
                new SqlParameter("@ImageURL", (object)imageUrl ?? DBNull.Value),
                new SqlParameter("@IsMembersOnly", isMembersOnly));
        }

        private static void AddResource(SqlConnection connection, int moduleId, string title, string description, string resourceType, string resourceUrl)
        {
            Execute(connection, @"
IF NOT EXISTS (SELECT 1 FROM Resources WHERE ModuleID=@ModuleID AND Title=@Title)
INSERT INTO Resources (ModuleID, Title, Description, ResourceType, ResourceUrl, IsPublished, CreatedDate)
VALUES (@ModuleID, @Title, @Description, @ResourceType, @ResourceUrl, 1, GETDATE())
ELSE
UPDATE Resources SET Description=@Description, ResourceType=@ResourceType, ResourceUrl=@ResourceUrl, IsPublished=1 WHERE ModuleID=@ModuleID AND Title=@Title",
                new SqlParameter("@ModuleID", moduleId),
                new SqlParameter("@Title", title),
                new SqlParameter("@Description", description),
                new SqlParameter("@ResourceType", resourceType),
                new SqlParameter("@ResourceUrl", resourceUrl));
        }

        private static void AddActivity(SqlConnection connection, int moduleId, string title, string prompt, int sortOrder)
        {
            Execute(connection, @"
IF NOT EXISTS (SELECT 1 FROM Activities WHERE ModuleID=@ModuleID AND Title=@Title)
INSERT INTO Activities (ModuleID, Title, Prompt, SortOrder, IsPublished, CreatedDate)
VALUES (@ModuleID, @Title, @Prompt, @SortOrder, 1, GETDATE())
ELSE
UPDATE Activities SET Prompt=@Prompt, SortOrder=@SortOrder, IsPublished=1 WHERE ModuleID=@ModuleID AND Title=@Title",
                new SqlParameter("@ModuleID", moduleId),
                new SqlParameter("@Title", title),
                new SqlParameter("@Prompt", prompt),
                new SqlParameter("@SortOrder", sortOrder));
        }

        private static void AddQuizQuestion(SqlConnection connection, int moduleId, string question, string optionA, string optionB, string optionC, string optionD, string correctAnswer, int orderIndex, string explanation)
        {
            Execute(connection, @"
INSERT INTO QuizQuestions (QuizID, QuestionText, OptionA, OptionB, OptionC, OptionD, CorrectAnswer, OrderIndex, SortOrder, Explanation)
SELECT TOP 1 QuizID, @QuestionText, @OptionA, @OptionB, @OptionC, @OptionD, @CorrectAnswer, @OrderIndex, @OrderIndex, @Explanation
FROM Quizzes
WHERE ModuleID=@ModuleID
AND IsPublished=1
AND NOT EXISTS (
    SELECT 1
    FROM QuizQuestions qq
    WHERE qq.QuizID = Quizzes.QuizID AND qq.QuestionText = @QuestionText
)
ORDER BY QuizID",
                new SqlParameter("@ModuleID", moduleId),
                new SqlParameter("@QuestionText", question),
                new SqlParameter("@OptionA", optionA),
                new SqlParameter("@OptionB", optionB),
                new SqlParameter("@OptionC", optionC),
                new SqlParameter("@OptionD", optionD),
                new SqlParameter("@CorrectAnswer", correctAnswer),
                new SqlParameter("@OrderIndex", orderIndex),
                new SqlParameter("@Explanation", explanation));
        }

        private static int GetRoleId(SqlConnection connection, string roleName, int fallbackRoleId)
        {
            using (var command = new SqlCommand("SELECT RoleID FROM UserRoles WHERE RoleName=@RoleName", connection))
            {
                command.Parameters.AddWithValue("@RoleName", roleName);
                object roleId = command.ExecuteScalar();
                return roleId == null || roleId == DBNull.Value ? fallbackRoleId : Convert.ToInt32(roleId);
            }
        }

        private static void UpsertUser(SqlConnection connection, string username, string email, string firstName, string lastName, string roleName, string password)
        {
            int roleId = GetRoleId(connection, roleName, string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase) ? 1 : 2);
            using (var command = new SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username=@Username)
BEGIN
    INSERT INTO Users (Username, Email, PasswordHash, FirstName, LastName, RoleID, CreatedDate, IsActive, IsBanned, PreferredTheme, TotpSecret, IsTotpEnabled, PasswordResetRequired)
    VALUES (@Username,@Email,@PasswordHash,@FirstName,@LastName,@RoleID,GETDATE(),1,0,'light',NULL,0,0)
END", connection))
            {
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@PasswordHash", PasswordHelper.HashPassword(password));
                command.Parameters.AddWithValue("@FirstName", firstName);
                command.Parameters.AddWithValue("@LastName", lastName);
                command.Parameters.AddWithValue("@RoleID", roleId);
                command.ExecuteNonQuery();
            }
        }
    }
}
