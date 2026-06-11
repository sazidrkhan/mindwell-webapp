// Purpose: Application lifecycle hooks for MindWell, including database startup
// initialization and diagnostic logging for IIS Express/runtime troubleshooting.
using System;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Web.SessionState;

namespace MindWell
{
    public class Global : HttpApplication
    {
        private static readonly object LogLock = new object();

        protected void Application_Start(object sender, EventArgs e)
        {
            // Registers diagnostic hooks before initializing the database so startup failures are captured.
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            LogDiagnostic("Application_Start");

            try
            {
                DatabaseInitializer.EnsureInitialized(Server.MapPath("~/App_Data"), Server.MapPath("~/SQL"));
                LogDiagnostic("Database initialization completed");
                TotpHelper.SelfTest();
            }
            catch (Exception ex)
            {
                LogDiagnostic("Database initialization failed", ex);
            }
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            string path = Request.AppRelativeCurrentExecutionFilePath;
            RedirectOldUrl(path, "~/User/Activity.aspx", "~/User/Assignment.aspx");
            RedirectOldUrl(path, "~/Admin/ManageContent.aspx", "~/Admin/Content.aspx");
            RedirectOldUrl(path, "~/Admin/Feedback.aspx", "~/Admin/ManageFeedback.aspx");
        }

        private void RedirectOldUrl(string currentPath, string oldPath, string newPath)
        {
            if (!string.Equals(currentPath, oldPath, StringComparison.OrdinalIgnoreCase))
                return;

            Response.Redirect(newPath, false);
            CompleteRequest();
        }

        protected void Application_End(object sender, EventArgs e)
        {
            LogDiagnostic("Application_End. ShutdownReason=" + HostingEnvironment.ShutdownReason);
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();
            if (ex != null)
            {
                LogDiagnostic("Application_Error", ex);
            }

            if (HandleRequestValidationException(ex))
                return;

            if (HandleHttpException(ex))
                return;
        }

        private bool HandleRequestValidationException(Exception ex)
        {
            if (FindException<HttpRequestValidationException>(ex) == null)
                return false;

            LogDiagnostic("HandleRequestValidationException: redirecting to plain-text guidance.");
            Server.ClearError();
            Response.Redirect(GetInvalidInputRedirect(), false);
            CompleteRequest();
            return true;
        }

        private bool HandleHttpException(Exception ex)
        {
            HttpException httpException = FindException<HttpException>(ex);
            if (httpException == null)
                return false;

            if (httpException.GetHttpCode() == 404)
            {
                LogDiagnostic("HandleHttpException: transferring 404 to home page.");
                Server.ClearError();
                Response.StatusCode = 404;
                Response.TrySkipIisCustomErrors = true;
                Server.Transfer("~/Default.aspx?error=404", false);
                return true;
            }

            return false;
        }

        private string GetInvalidInputRedirect()
        {
            string path = Request.AppRelativeCurrentExecutionFilePath ?? string.Empty;
            if (string.Equals(path, "~/Public/Contact.aspx", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(path, "~/User/Journal.aspx", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(path, "~/User/Profile.aspx", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(path, "~/User/Forum.aspx", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(path, "~/User/ForumTopic.aspx", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(path, "~/User/Assignment.aspx", StringComparison.OrdinalIgnoreCase))
                return AppendInvalidInputFlag(path);

            return "~/Default.aspx?error=invalid-input";
        }

        private string AppendInvalidInputFlag(string path)
        {
            string query = Request.Url == null ? string.Empty : Request.Url.Query;
            if (string.IsNullOrEmpty(query))
                return path + "?invalidInput=1";

            return path + query + "&invalidInput=1";
        }

        private static T FindException<T>(Exception ex) where T : Exception
        {
            while (ex != null)
            {
                T matched = ex as T;
                if (matched != null)
                    return matched;

                ex = ex.InnerException;
            }

            return null;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogDiagnostic("UnhandledException. IsTerminating=" + e.IsTerminating, e.ExceptionObject as Exception);
        }

        private static void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            LogDiagnostic("AppDomain DomainUnload");
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            LogDiagnostic("AppDomain ProcessExit");
        }

        private static void LogDiagnostic(string message, Exception ex = null)
        {
            // Writes lightweight diagnostics to Debug output and App_Data/MindWellDiagnostics.log.
            try
            {
                var builder = new StringBuilder();
                builder.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                builder.Append(" | ");
                builder.Append(message);

                if (ex != null)
                {
                    builder.AppendLine();
                    builder.Append(ex);
                }

                string line = builder.ToString();
                System.Diagnostics.Debug.WriteLine(line);

                string appDataPath = GetAppDataPath();
                Directory.CreateDirectory(appDataPath);

                lock (LogLock)
                {
                    File.AppendAllText(Path.Combine(appDataPath, "MindWellDiagnostics.log"), line + Environment.NewLine);
                }
            }
            catch
            {
                // Diagnostics must never bring down the application.
            }
        }

        private static string GetAppDataPath()
        {
            HttpContext context = HttpContext.Current;
            if (context != null && context.Server != null)
                return context.Server.MapPath("~/App_Data");

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
        }

        protected void Session_Start(object sender, EventArgs e)
        {
            // Code that runs when a session starts
        }

        protected void Session_End(object sender, EventArgs e)
        {
            // Code that runs when a session ends
        }
    }
}
