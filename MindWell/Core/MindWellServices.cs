// Purpose: Shared service helpers for MindWell pages, including module lookup,
// safe database wrappers, HTML/JSON formatting, authorization gates, and progress updates.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;

namespace MindWell
{
    public static class MindWellServices
    {
        // Maps stable module slugs used by pages/links to the database module IDs and display names.
        public static readonly string[] ModuleSlugs = { "MentalHealth", "StressManagement", "Mindfulness", "SelfCare" };
        public static readonly string[] ModuleNames = { "Introduction to Mental Health", "Stress Management", "Mindfulness", "Self-Care" };
        public const string ActiveModuleWhere = "NULLIF(LTRIM(RTRIM(lm.ModuleName)), '') IS NOT NULL AND NULLIF(LTRIM(RTRIM(lm.Description)), '') IS NOT NULL AND lm.ModuleName NOT LIKE '[[]Archived] %' AND lm.ModuleName NOT LIKE '%QA%' AND lm.ModuleName NOT LIKE '%ROUTEAUDIT%' AND lm.ModuleName NOT LIKE '%Deep%' AND lm.ModuleName NOT LIKE '%Temporary%' AND lm.ModuleName NOT LIKE '%Automated%'";
        public const string ActiveQuizWhere = "NULLIF(LTRIM(RTRIM(q.QuizTitle)), '') IS NOT NULL AND q.IsPublished=1 AND q.ModuleID IS NOT NULL AND q.QuizTitle NOT LIKE '%QA%' AND q.QuizTitle NOT LIKE '%ROUTEAUDIT%' AND q.QuizTitle NOT LIKE '%Deep%' AND q.QuizTitle NOT LIKE '%Temporary%' AND q.QuizTitle NOT LIKE '%Automated%'";
        public const string ActiveModuleCountSql = "SELECT COUNT(*) FROM LearningModules lm WHERE " + ActiveModuleWhere;
        public const string ActiveQuizCountSql = "SELECT COUNT(*) FROM Quizzes q INNER JOIN LearningModules lm ON q.ModuleID=lm.ModuleID WHERE " + ActiveQuizWhere + " AND " + ActiveModuleWhere;
        public const string ActiveModuleSelectSql = "SELECT lm.ModuleID, lm.ModuleName, lm.Description FROM LearningModules lm WHERE " + ActiveModuleWhere + " ORDER BY lm.OrderIndex, lm.ModuleName";

        public static string CleanDemoTextWhere(string expression)
        {
            return expression + " NOT LIKE '%QA%' AND " +
                   expression + " NOT LIKE '%QA FINAL%' AND " +
                   expression + " NOT LIKE '%ROUTEAUDIT%' AND " +
                   expression + " NOT LIKE '%Deep QA%' AND " +
                   expression + " NOT LIKE '%Deep%' AND " +
                   expression + " NOT LIKE '%Temporary%' AND " +
                   expression + " NOT LIKE '% temp %' AND " +
                   expression + " NOT LIKE 'temp%' AND " +
                   expression + " NOT LIKE '% test %' AND " +
                   expression + " NOT LIKE 'test%' AND " +
                   expression + " NOT LIKE '%Ultimate%' AND " +
                   expression + " NOT LIKE '%Automated%' AND " +
                   expression + " NOT LIKE '%automation%' AND " +
                   expression + " NOT LIKE '%cleanup check%' AND " +
                   expression + " NOT LIKE '%cleanup%' AND " +
                   expression + " NOT LIKE '%octest%' AND " +
                   expression + " NOT LIKE '%qauser%' AND " +
                   expression + " NOT LIKE '%deepauth%' AND " +
                   expression + " NOT LIKE '%example.test%'";
        }

        public static string ExcludeTestDataWhere(string usernameExpression, string emailExpression)
        {
            return usernameExpression + " NOT LIKE 'qa%' AND " +
                   usernameExpression + " NOT LIKE 'routeaudit%' AND " +
                   usernameExpression + " NOT LIKE 'test%' AND " +
                   usernameExpression + " NOT LIKE 'temp%' AND " +
                   usernameExpression + " NOT LIKE 'octest%' AND " +
                   usernameExpression + " NOT LIKE 'deepauth%' AND " +
                   usernameExpression + " NOT LIKE 'deepadmin%' AND " +
                   "LEN(" + usernameExpression + ") <= 50 AND " +
                   emailExpression + " NOT LIKE '%@example.test' AND " +
                   emailExpression + " NOT LIKE '%routeaudit%'";
        }

        public static int GetModuleId(string slug)
        {
            for (int i = 0; i < ModuleSlugs.Length; i++)
            {
                if (string.Equals(ModuleSlugs[i], slug, StringComparison.OrdinalIgnoreCase))
                    return i + 1;
            }

            return 1;
        }

        public static string GetModuleName(string slug)
        {
            return ModuleNames[GetModuleId(slug) - 1];
        }

        public static DataTable Query(string query, params SqlParameter[] parameters)
        {
            // Centralizes SELECT execution so page code stays compact and database errors are logged safely.
            try
            {
                return DatabaseHelper.ExecuteQuery(query, parameters);
            }
            catch (Exception ex)
            {
                LogDiagnostic("MindWell query failed", ex);
                return new DataTable();
            }
        }

        public static object Scalar(string query, params SqlParameter[] parameters)
        {
            try
            {
                return DatabaseHelper.ExecuteScalar(query, parameters);
            }
            catch (Exception ex)
            {
                LogDiagnostic("MindWell scalar failed", ex);
                return null;
            }
        }

        public static int NonQuery(string query, params SqlParameter[] parameters)
        {
            try
            {
                return DatabaseHelper.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                LogDiagnostic("MindWell command failed", ex);
                return 0;
            }
        }

        public static void LogDiagnostic(string message, Exception ex = null)
        {
            try
            {
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " | " + message;
                if (ex != null)
                    line += Environment.NewLine + ex;

                Debug.WriteLine(line);
                string appData = null;
                HttpContext context = HttpContext.Current;
                if (context != null && context.Server != null)
                    appData = context.Server.MapPath("~/App_Data");

                if (string.IsNullOrEmpty(appData))
                    appData = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");

                Directory.CreateDirectory(appData);
                File.AppendAllText(Path.Combine(appData, "MindWellDiagnostics.log"), line + Environment.NewLine);
            }
            catch (Exception logException)
            {
                Debug.WriteLine("MindWell diagnostic logging failed: " + logException.Message);
            }
        }

        public static string RenderDataUnavailable(string detail)
        {
            return "<section class='card empty-state'><h1>Learning content is temporarily unavailable.</h1><p>Please rebuild/restart the app and let the database initializer finish. If this continues, check MindWellDiagnostics.log.</p><p>" + Encode(detail) + "</p></section>";
        }

        public static int ToInt(object value, int fallback)
        {
            if (value == null || value == DBNull.Value)
                return fallback;

            int parsed;
            return int.TryParse(value.ToString(), out parsed) ? parsed : fallback;
        }

        public static string Encode(object value)
        {
            return HttpUtility.HtmlEncode(value == null || value == DBNull.Value ? string.Empty : value.ToString());
        }

        public static string HtmlOrEmpty(object value)
        {
            return value == null || value == DBNull.Value ? string.Empty : value.ToString();
        }

        public static string PlainTextFromArticle(object value)
        {
            string text = HtmlOrEmpty(value);
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            text = Regex.Replace(text, "<br\\s*/?>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "</p\\s*>", "\n\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "<[^>]+>", string.Empty);
            text = HttpUtility.HtmlDecode(text) ?? string.Empty;
            return text.Trim();
        }

        public static string RenderArticleBody(object value)
        {
            string text = PlainTextFromArticle(value);
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string[] paragraphs = Regex.Split(text, @"\r?\n\s*\r?\n");
            var sb = new StringBuilder();
            foreach (string paragraph in paragraphs)
            {
                string clean = paragraph.Trim();
                if (clean.Length == 0)
                    continue;

                sb.Append("<p>").Append(Encode(clean)).Append("</p>");
            }

            return sb.ToString();
        }

        public static string ArticlePreview(object preview, object content, int maxLength)
        {
            string text = PlainTextFromArticle(preview);
            if (string.IsNullOrWhiteSpace(text))
                text = PlainTextFromArticle(content);

            if (text.Length > maxLength)
                text = text.Substring(0, maxLength).TrimEnd() + "...";

            return text;
        }

        public static bool IsSafeDisplayUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return true;

            string trimmed = url.Trim();
            if (trimmed.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("vbscript:", StringComparison.OrdinalIgnoreCase))
                return false;

            if (trimmed.StartsWith("~/", StringComparison.Ordinal) || trimmed.StartsWith("/", StringComparison.Ordinal))
                return !trimmed.StartsWith("//", StringComparison.Ordinal);

            Uri uri;
            return Uri.TryCreate(trimmed, UriKind.Absolute, out uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        public static string SafeDisplayUrl(string url, Page page)
        {
            if (string.IsNullOrWhiteSpace(url) || !IsSafeDisplayUrl(url))
                return string.Empty;

            string trimmed = url.Trim();
            if (trimmed.StartsWith("~/", StringComparison.Ordinal) && page != null)
                return page.ResolveUrl(trimmed);

            return trimmed;
        }

        public static string EncodeLinesAsList(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var sb = new StringBuilder("<ul class='learning-list'>");
            foreach (string part in value.Split(new[] { '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string item = part.Trim();
                if (item.Length > 0)
                    sb.Append("<li>").Append(Encode(item)).Append("</li>");
            }

            return sb.Append("</ul>").ToString();
        }

        public static string JsonArray(DataTable table, string labelColumn, string valueColumn)
        {
            // Converts small chart datasets into JSON consumed by front-end visualizations such as mood history.
            var sb = new StringBuilder("[");
            for (int i = 0; i < table.Rows.Count; i++)
            {
                if (i > 0)
                    sb.Append(',');

                sb.Append("{\"label\":\"")
                    .Append(EscapeJson(table.Rows[i][labelColumn].ToString()))
                    .Append("\",\"value\":")
                    .Append(ToInt(table.Rows[i][valueColumn], 0))
                    .Append('}');
            }

            sb.Append(']');
            return sb.ToString();
        }

        public static string RenderModuleOptions(int selectedModuleId)
        {
            DataTable modules = Query("SELECT lm.ModuleID, lm.ModuleName FROM LearningModules lm WHERE " + ActiveModuleWhere + " ORDER BY lm.OrderIndex, lm.ModuleName");
            var sb = new StringBuilder();
            foreach (DataRow row in modules.Rows)
            {
                int moduleId = ToInt(row["ModuleID"], 0);
                sb.Append("<option value='").Append(moduleId).Append("'");
                if (moduleId == selectedModuleId)
                    sb.Append(" selected");

                sb.Append(">").Append(Encode(row["ModuleName"])).Append("</option>");
            }

            return sb.ToString();
        }

        public static string EscapeJson(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n");
        }

        public static void RequireLogin(Page page)
        {
            // Protects member-only pages by redirecting anonymous visitors to the login screen.
            if (!SecurityHelper.IsUserAuthenticated())
            {
                string returnUrl = page.Request == null ? string.Empty : page.Request.RawUrl;
                if (string.IsNullOrWhiteSpace(returnUrl))
                    page.Response.Redirect("~/Auth/Login.aspx", false);
                else
                    page.Response.Redirect("~/Auth/Login.aspx?returnUrl=" + SecurityHelper.UrlEncode(returnUrl), false);
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
        }

        public static void RequireAdmin(Page page)
        {
            if (page.Request != null && string.Equals(page.Request.QueryString["exitUserView"], "1", StringComparison.OrdinalIgnoreCase))
                SecurityHelper.ClearAdminUserViewMode();

            RequireLogin(page);
            if (!SecurityHelper.IsUserAdmin())
            {
                page.Response.Redirect("~/User/Dashboard.aspx", false);
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
        }

        public static void UpsertProgress(int userId, int moduleId, int percentage, bool completed)
        {
            // Updates learning progress idempotently so repeated quiz/article actions do not duplicate rows.
            NonQuery(@"
IF EXISTS (SELECT 1 FROM UserProgress WHERE UserID = @UserID AND ModuleID = @ModuleID)
BEGIN
    UPDATE UserProgress
    SET IsStarted = 1,
        IsCompleted = @Completed,
        PercentageComplete = CASE WHEN PercentageComplete > @Percentage THEN PercentageComplete ELSE @Percentage END,
        CompletionDate = CASE WHEN @Completed = 1 THEN GETDATE() ELSE CompletionDate END
    WHERE UserID = @UserID AND ModuleID = @ModuleID
END
ELSE
BEGIN
    INSERT INTO UserProgress (UserID, ModuleID, IsStarted, IsCompleted, PercentageComplete, CompletionDate)
    VALUES (@UserID, @ModuleID, 1, @Completed, @Percentage, CASE WHEN @Completed = 1 THEN GETDATE() ELSE NULL END)
END",
                new SqlParameter("@UserID", userId),
                new SqlParameter("@ModuleID", moduleId),
                new SqlParameter("@Completed", completed),
                new SqlParameter("@Percentage", percentage));

            if (completed)
            {
                NonQuery(@"
IF NOT EXISTS (SELECT 1 FROM Certificates WHERE UserID = @UserID AND ModuleID = @ModuleID)
BEGIN
    INSERT INTO Certificates (UserID, ModuleID, CertificateURL, IssuedDate)
    VALUES (@UserID, @ModuleID, '~/User/Progress.aspx', GETDATE())
END",
                    new SqlParameter("@UserID", userId),
                    new SqlParameter("@ModuleID", moduleId));
            }
        }
    }

    public class UserPageBase : Page
    {
        protected override void OnLoad(EventArgs e)
        {
            // All user pages share login and CSRF checks through this base class.
            MindWellServices.RequireLogin(this);
            if (IsPostBack && !SecurityHelper.ValidateCsrfOrEndRequest())
                return;

            base.OnLoad(e);
        }
    }

    public class AdminPageBase : Page
    {
        protected override void OnLoad(EventArgs e)
        {
            // Admin pages add role enforcement on top of the standard member-page protections.
            MindWellServices.RequireAdmin(this);
            if (IsPostBack && !SecurityHelper.ValidateCsrfOrEndRequest())
                return;

            base.OnLoad(e);
        }
    }
}
