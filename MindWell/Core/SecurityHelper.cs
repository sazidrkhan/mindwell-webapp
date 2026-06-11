// Purpose: Security utilities for validation, encoding, authentication state,
// CSRF protection, session recovery, and audit logging across MindWell.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.Security;
using System.Text.RegularExpressions;

namespace MindWell
{
    /// <summary>
    /// SecurityHelper provides input validation, output encoding, and authorization functions.
    /// </summary>
    public class SecurityHelper
    {
        private const string CsrfSessionKey = "MindWellCsrfToken";
        private const string AdminUserViewModeKey = "AdminUserViewMode";

        /// <summary>
        /// HTML-encodes user input for safe display (prevents XSS attacks).
        /// </summary>
        /// <param name="input">User input to encode</param>
        /// <returns>HTML-encoded string safe for display</returns>
        public static string HtmlEncode(string input)
        {
            return HttpUtility.HtmlEncode(input);
        }

        /// <summary>
        /// HTML-decodes encoded strings (use carefully, only when necessary).
        /// </summary>
        /// <param name="input">HTML-encoded string</param>
        /// <returns>Decoded string</returns>
        public static string HtmlDecode(string input)
        {
            return HttpUtility.HtmlDecode(input);
        }

        /// <summary>
        /// URL-encodes a string for safe use in URLs.
        /// </summary>
        /// <param name="input">String to encode</param>
        /// <returns>URL-encoded string</returns>
        public static string UrlEncode(string input)
        {
            return HttpUtility.UrlEncode(input);
        }

        /// <summary>
        /// Validates email format.
        /// </summary>
        /// <param name="email">Email address to validate</param>
        /// <returns>True if valid email format</returns>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || email.Length > 100)
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates username format (alphanumeric and underscore only).
        /// </summary>
        /// <param name="username">Username to validate</param>
        /// <returns>True if valid username</returns>
        public static bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            if (!IsValidUsernameLength(username))
                return false;

            // Alphanumeric and underscore only
            return Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$");
        }

        public static bool IsValidUsernameLength(string username)
        {
            username = username == null ? string.Empty : username.Trim();
            return username.Length >= 3 && username.Length <= 50;
        }

        /// <summary>
        /// Validates that a string is numeric only.
        /// </summary>
        /// <param name="input">String to validate</param>
        /// <returns>True if numeric</returns>
        public static bool IsNumeric(string input)
        {
            return !string.IsNullOrWhiteSpace(input) && Regex.IsMatch(input, @"^\d+$");
        }

        /// <summary>
        /// Validates that an integer is within a specified range.
        /// </summary>
        /// <param name="value">Value to check</param>
        /// <param name="min">Minimum value (inclusive)</param>
        /// <param name="max">Maximum value (inclusive)</param>
        /// <returns>True if within range</returns>
        public static bool IsInRange(int value, int min, int max)
        {
            return value >= min && value <= max;
        }

        /// <summary>
        /// Trims and truncates input to prevent database overflow.
        /// </summary>
        /// <param name="input">Input string</param>
        /// <param name="maxLength">Maximum allowed length</param>
        /// <returns>Trimmed and truncated string</returns>
        public static string SafeTrim(string input, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return input.Trim().Length > maxLength 
                ? input.Trim().Substring(0, maxLength) 
                : input.Trim();
        }

        public const string PlainTextValidationMessage = "Please enter plain text only. HTML tags or script-like symbols are not allowed.";
        private const string PlainTextValidationDetail = "Please remove angle brackets, script URLs, and event-handler text before submitting.";

        public static bool IsBlankAfterTrim(string input)
        {
            return string.IsNullOrWhiteSpace(input);
        }

        public static bool IsPlainTextSafe(string input)
        {
            if (string.IsNullOrEmpty(input))
                return true;

            if (input.IndexOf('<') >= 0 || input.IndexOf('>') >= 0)
                return false;

            string lowered = input.ToLowerInvariant();
            return !lowered.Contains("javascript:")
                && !lowered.Contains("vbscript:")
                && !lowered.Contains("data:text/html")
                && !lowered.Contains("onerror=")
                && !lowered.Contains("onload=")
                && !lowered.Contains("onclick=")
                && !lowered.Contains("srcdoc=");
        }

        public static string GetPlainTextValidationDetail()
        {
            return PlainTextValidationMessage + " " + PlainTextValidationDetail;
        }

        public static bool TryGetPositiveIntQuery(string key, out int value)
        {
            value = 0;
            HttpContext context = HttpContext.Current;
            if (context == null || context.Request == null)
                return false;

            return TryGetPositiveInt(context.Request.QueryString[key], out value);
        }

        public static bool TryGetPositiveIntForm(string key, out int value)
        {
            value = 0;
            HttpContext context = HttpContext.Current;
            if (context == null || context.Request == null)
                return false;

            return TryGetPositiveInt(context.Request.Unvalidated.Form[key], out value);
        }

        private static bool TryGetPositiveInt(string raw, out int value)
        {
            value = 0;
            int parsed;
            if (!int.TryParse(raw, out parsed) || parsed <= 0)
                return false;

            value = parsed;
            return true;
        }

        /// <summary>
        /// Gets the user's IP address from HTTP request.
        /// </summary>
        /// <returns>IP address string</returns>
        public static string GetUserIPAddress()
        {
            HttpRequest request = HttpContext.Current != null ? HttpContext.Current.Request : null;
            if (request == null)
                return "Unknown";

            string ip = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(ip))
            {
                ip = request.ServerVariables["REMOTE_ADDR"];
            }
            return !string.IsNullOrEmpty(ip) ? ip : "Unknown";
        }

        /// <summary>
        /// Checks if user is authenticated by verifying Session["UserID"].
        /// </summary>
        /// <returns>True if user is authenticated</returns>
        public static bool IsUserAuthenticated()
        {
            HttpContext context = HttpContext.Current;
            if (context == null || context.Session == null)
                return false;

            if (context.Session["UserID"] != null)
                return true;

            return TryRestoreUserSession(context);
        }

        private static bool TryRestoreUserSession(HttpContext context)
        {
            try
            {
                if (context.User == null || context.User.Identity == null || !context.User.Identity.IsAuthenticated)
                    return false;

                string username = SafeTrim(context.User.Identity.Name, 50);
                if (string.IsNullOrEmpty(username))
                    return false;

                DataTable user = MindWellServices.Query(@"
SELECT TOP 1 u.UserID, u.Username, u.FirstName, u.IsActive, u.IsBanned, u.IsTotpEnabled, r.RoleName
FROM Users u
INNER JOIN UserRoles r ON u.RoleID = r.RoleID
WHERE u.Username = @Username",
                    new SqlParameter("@Username", username));

                if (user.Rows.Count == 0)
                    return false;

                DataRow row = user.Rows[0];
                if (!Convert.ToBoolean(row["IsActive"]) || Convert.ToBoolean(row["IsBanned"]))
                {
                    FormsAuthentication.SignOut();
                    return false;
                }

                if (row["IsTotpEnabled"] != DBNull.Value && Convert.ToBoolean(row["IsTotpEnabled"]))
                {
                    FormsAuthentication.SignOut();
                    return false;
                }

                context.Session["UserID"] = Convert.ToInt32(row["UserID"]);
                context.Session["Username"] = row["Username"].ToString();
                context.Session["UserRole"] = row["RoleName"].ToString();
                context.Session["FirstName"] = row["FirstName"] == DBNull.Value ? string.Empty : row["FirstName"].ToString();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Session restore failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Checks if user has admin role.
        /// </summary>
        /// <returns>True if user is admin</returns>
        public static bool IsUserAdmin()
        {
            if (!IsUserAuthenticated())
                return false;

            object roleObj = HttpContext.Current.Session["UserRole"];
            string userRole = roleObj != null ? roleObj.ToString() : null;
            return userRole == "Admin";
        }

        public static bool IsAdminUserViewMode()
        {
            HttpContext context = HttpContext.Current;
            if (context == null || context.Session == null)
                return false;

            object value = context.Session[AdminUserViewModeKey];
            return value != null && value.ToString() == "1";
        }

        public static void EnableAdminUserViewMode()
        {
            HttpContext context = HttpContext.Current;
            if (context != null && context.Session != null && IsUserAdmin())
                context.Session[AdminUserViewModeKey] = "1";
        }

        public static void ClearAdminUserViewMode()
        {
            HttpContext context = HttpContext.Current;
            if (context != null && context.Session != null)
                context.Session.Remove(AdminUserViewModeKey);
        }

        /// <summary>
        /// Gets the current user ID from session.
        /// </summary>
        /// <returns>User ID or null if not authenticated</returns>
        public static int? GetCurrentUserId()
        {
            if (IsUserAuthenticated())
            {
                int userId;
                if (int.TryParse(HttpContext.Current.Session["UserID"].ToString(), out userId))
                    return userId;
            }

            return null;
        }

        /// <summary>
        /// Gets the current user role from session.
        /// </summary>
        /// <returns>Role name or null if not authenticated</returns>
        public static string GetCurrentUserRole()
        {
            if (IsUserAuthenticated())
            {
                object roleObj = HttpContext.Current.Session["UserRole"];
                return roleObj != null ? roleObj.ToString() : null;
            }

            return null;
        }

        /// <summary>
        /// Gets the current username from session.
        /// </summary>
        /// <returns>Username or null if not authenticated</returns>
        public static string GetCurrentUsername()
        {
            if (IsUserAuthenticated())
            {
                object usernameObj = HttpContext.Current.Session["Username"];
                return usernameObj != null ? usernameObj.ToString() : null;
            }

            return null;
        }

        /// <summary>
        /// Clears all user session data (logout).
        /// </summary>
        public static void ClearUserSession()
        {
            FormsAuthentication.SignOut();

            if (HttpContext.Current != null && HttpContext.Current.Session != null)
            {
                HttpContext.Current.Session["UserID"] = null;
                HttpContext.Current.Session["UserRole"] = null;
                HttpContext.Current.Session["Username"] = null;
                HttpContext.Current.Session.Clear();
                HttpContext.Current.Session.Abandon();
            }
        }

        public static string RenderCsrfToken()
        {
            HttpContext context = HttpContext.Current;
            if (context == null || context.Session == null)
                return string.Empty;

            string token = context.Session[CsrfSessionKey] as string;
            if (string.IsNullOrEmpty(token))
            {
                token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                context.Session[CsrfSessionKey] = token;
            }

            return "<input type='hidden' name='MindWellCsrfToken' value='" + HtmlEncode(token) + "' />";
        }

        public static void ResetCsrfToken()
        {
            HttpContext context = HttpContext.Current;
            if (context != null && context.Session != null)
                context.Session.Remove(CsrfSessionKey);
        }

        public static bool ValidateCsrfToken()
        {
            HttpContext context = HttpContext.Current;
            if (context == null || context.Request == null || context.Session == null)
                return false;

            if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
                return true;

            string expected = context.Session[CsrfSessionKey] as string;
            string actual = context.Request.Unvalidated.Form["MindWellCsrfToken"];
            return !string.IsNullOrEmpty(expected) && string.Equals(expected, actual, StringComparison.Ordinal);
        }

        public static bool ValidateCsrfOrEndRequest()
        {
            if (ValidateCsrfToken())
                return true;

            HttpContext context = HttpContext.Current;
            if (context != null && context.Response != null)
            {
                context.Response.StatusCode = 400;
                context.Response.Write("Invalid security token. Please refresh the page and try again.");
                if (context.ApplicationInstance != null)
                    context.ApplicationInstance.CompleteRequest();
            }

            return false;
        }

        /// <summary>
        /// Validates a date format (yyyy-MM-dd).
        /// </summary>
        /// <param name="dateString">Date string to validate</param>
        /// <returns>True if valid date</returns>
        public static bool IsValidDate(string dateString)
        {
            DateTime result;
            return DateTime.TryParse(dateString, out result);
        }

        /// <summary>
        /// Validates a date is within acceptable range.
        /// </summary>
        /// <param name="dateString">Date string to validate</param>
        /// <param name="minDate">Minimum date</param>
        /// <param name="maxDate">Maximum date</param>
        /// <returns>True if valid and within range</returns>
        public static bool IsValidDateRange(string dateString, DateTime minDate, DateTime maxDate)
        {
            DateTime date;
            if (!DateTime.TryParse(dateString, out date))
                return false;

            return date >= minDate && date <= maxDate;
        }

        /// <summary>
        /// Logs an action to the audit log table.
        /// </summary>
        /// <param name="userId">User ID (null for anonymous)</param>
        /// <param name="action">Action description</param>
        /// <param name="tableName">Table affected</param>
        /// <param name="recordId">Record ID affected</param>
        /// <param name="oldValue">Old value before change</param>
        /// <param name="newValue">New value after change</param>
        public static void LogAction(int? userId, string action, string tableName, int recordId, string oldValue, string newValue)
        {
            try
            {
                string query = @"INSERT INTO AuditLog (UserID, Action, TableName, RecordID, OldValue, NewValue, IPAddress, CreatedDate)
                               VALUES (@UserID, @Action, @TableName, @RecordID, @OldValue, @NewValue, @IPAddress, GETDATE())";

                System.Data.SqlClient.SqlParameter[] parameters = new System.Data.SqlClient.SqlParameter[]
                {
                    new System.Data.SqlClient.SqlParameter("@UserID", userId ?? (object)System.DBNull.Value),
                    new System.Data.SqlClient.SqlParameter("@Action", action),
                    new System.Data.SqlClient.SqlParameter("@TableName", tableName),
                    new System.Data.SqlClient.SqlParameter("@RecordID", recordId),
                    new System.Data.SqlClient.SqlParameter("@OldValue", oldValue ?? ""),
                    new System.Data.SqlClient.SqlParameter("@NewValue", newValue ?? ""),
                    new System.Data.SqlClient.SqlParameter("@IPAddress", GetUserIPAddress())
                };

                DatabaseHelper.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Audit log error: " + ex.Message);
                // Fail silently to not interrupt application flow
            }
        }
    }
}
