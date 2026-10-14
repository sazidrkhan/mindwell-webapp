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
                    return "<section class='card auth-card'><h1>Forgot Password</h1><div id='forgotPasswordMessage' class='alert alert-danger' role='alert'>Invalid security token. Please refresh the page and try again.</div><p class='page-actions'><a class='btn btn-outline' href='Login.aspx'>Back to sign in</a></p></section>";

                string email = SecurityHelper.SafeTrim(Request.Form["email"], 100);
                if (SecurityHelper.IsValidEmail(email))
                {
                    MindWellServices.NonQuery("INSERT INTO Contacts (Name, Email, Subject, Message, IsResolved, Status, CreatedDate) VALUES ('Password Reset Request', @Email, 'Password reset request', 'User requested password reset assistance.', 0, 'New', GETDATE())", new SqlParameter("@Email", email));
                    message = "<div id='forgotPasswordMessage' class='alert alert-success' role='status'>If this email is registered, support will contact you with reset instructions.</div>";
                }
                else
                {
                    message = "<div id='forgotPasswordMessage' class='alert alert-danger' role='alert'>Enter a valid email address.</div>";
                }
            }
            return "<section class='card auth-card'><h1>Forgot Password</h1>" + message + "<p id='forgotPasswordHint'>Enter your account email and an administrator will help reset your password.</p><div class='form-group'><label for='forgotPasswordEmail'>Email</label><input id='forgotPasswordEmail' type='email' name='email' autocomplete='email' aria-describedby='forgotPasswordHint' required /></div><p class='page-actions'><button class='btn btn-primary'>Request Reset</button><a class='btn btn-outline' href='Login.aspx'>Back to sign in</a></p></section>";
        }
    }
}
