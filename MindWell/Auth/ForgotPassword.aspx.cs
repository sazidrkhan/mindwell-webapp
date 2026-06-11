// Purpose: Provides a simple password-reset request form by logging support requests for administrators.
using System;
using System.Data.SqlClient;

namespace MindWell.Auth
{
    public partial class ForgotPassword : System.Web.UI.Page
    {
        protected string RenderForgotPassword()
        {
            string message = string.Empty;
            if (IsPostBack)
            {
                // Valid reset requests are stored as contact/support messages because this demo has no email service.
                if (!SecurityHelper.ValidateCsrfToken())
                    return "<section class='card auth-card'><h1>Forgot Password</h1><div class='alert alert-danger'>Invalid security token. Please refresh the page and try again.</div><p class='page-actions'><a class='btn btn-outline' href='Login.aspx'>Back to sign in</a></p></section>";

                string email = SecurityHelper.SafeTrim(Request.Form["email"], 100);
                if (SecurityHelper.IsValidEmail(email))
                {
                    MindWellServices.NonQuery("INSERT INTO Contacts (Name, Email, Subject, Message, IsResolved, Status, CreatedDate) VALUES ('Password Reset Request', @Email, 'Password reset request', 'User requested password reset assistance.', 0, 'New', GETDATE())", new SqlParameter("@Email", email));
                    message = "<div class='alert alert-success'>If this email is registered, support will contact you with reset instructions.</div>";
                }
                else
                {
                    message = "<div class='alert alert-danger'>Enter a valid email address.</div>";
                }
            }
            return "<section class='card auth-card'><h1>Forgot Password</h1>" + message + "<p>Enter your account email and an administrator will help reset your password.</p><div class='form-group'><label>Email</label><input type='email' name='email' required /></div><p class='page-actions'><button class='btn btn-primary'>Request Reset</button><a class='btn btn-outline' href='Login.aspx'>Back to sign in</a></p></section>";
        }
    }
}
