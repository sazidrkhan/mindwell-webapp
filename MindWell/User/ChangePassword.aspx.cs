// Purpose: User password-change page that verifies the current password before storing a new hash.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MindWell.User
{
    public partial class ChangePassword : UserPageBase
    {
        protected string RenderChangePassword()
        {
            // Validates the old password and new password rules before updating the stored hash.
            string message = string.Empty;
            bool forcePasswordChange = IsForcePasswordChange();
            if (IsPostBack)
            {
                int userId = SecurityHelper.GetCurrentUserId() ?? 0;
                string current = Request.Form["currentPassword"] ?? string.Empty;
                string next = Request.Form["newPassword"] ?? string.Empty;
                string confirm = Request.Form["confirmPassword"] ?? string.Empty;
                DataTable user = MindWellServices.Query("SELECT PasswordHash FROM Users WHERE UserID=@UserID", new SqlParameter("@UserID", userId));
                if (user.Rows.Count == 0 || !PasswordHelper.VerifyPassword(current, user.Rows[0]["PasswordHash"].ToString()))
                    message = "<div id='changePasswordMessage' class='alert alert-danger' role='alert'>Current password is incorrect.</div>";
                else if (next != confirm || !PasswordHelper.IsValidPassword(next))
                    message = "<div id='changePasswordMessage' class='alert alert-danger' role='alert'>New password must match and meet the strength requirements.</div>";
                else
                {
                    MindWellServices.NonQuery("UPDATE Users SET PasswordHash=@PasswordHash, PasswordResetRequired=0 WHERE UserID=@UserID", new SqlParameter("@PasswordHash", PasswordHelper.HashPassword(next)), new SqlParameter("@UserID", userId));
                    Session.Remove("ForcePasswordChange");
                    if (forcePasswordChange)
                    {
                        Response.Redirect("~/User/Dashboard.aspx", false);
                        Context.ApplicationInstance.CompleteRequest();
                        return string.Empty;
                    }

                    message = "<div id='changePasswordMessage' class='alert alert-success' role='status'>Password updated.</div>";
                }
            }
            string forceBanner = forcePasswordChange ? "<div id='forcePasswordMessage' class='alert alert-warning' role='status'>You must set a new password before continuing.</div>" : string.Empty;
            var sb = new StringBuilder("<p class='back-link'><a class='btn btn-outline btn-sm' href='Profile.aspx'>Back to Profile</a></p><section class='card change-password-card' data-password-strength-scope><h1>Change Password</h1>")
                .Append(forceBanner)
                .Append(message)
                .Append("<div class='form-group'><label for='currentPassword'>Current password</label><div class='password-field'><input id='currentPassword' type='password' name='currentPassword' autocomplete='current-password' required />")
                .Append(PasswordToggleMarkup())
                .Append("</div></div><div class='form-group'><label for='newPassword'>New password</label><div class='password-field'><input id='newPassword' type='password' name='newPassword' autocomplete='new-password' aria-describedby='passwordRequirements' required data-password-strength />")
                .Append(PasswordToggleMarkup())
                .Append("</div>")
                .Append(PasswordStrengthMarkup())
                .Append("</div><div class='form-group'><label for='confirmPassword'>Confirm new password</label><div class='password-field'><input id='confirmPassword' type='password' name='confirmPassword' autocomplete='new-password' aria-describedby='passwordMatch' required data-password-confirm />")
                .Append(PasswordToggleMarkup())
                .Append("</div><div id='passwordMatch' class='match-indicator' role='status' aria-live='polite'></div></div><p class='page-actions'><button class='btn btn-primary'>Update Password</button></p></section>");
            return sb.ToString();
        }

        private bool IsForcePasswordChange()
        {
            return Session["ForcePasswordChange"] != null || Request.QueryString["force"] == "1";
        }

        private string PasswordToggleMarkup()
        {
            return "<button type=\"button\" class='password-toggle password-eye' data-password-toggle aria-label='Show password' aria-pressed='false'><span class='password-eye-open' aria-hidden='true'><svg viewBox='0 0 24 24'><path d='M2.5 12s3.5-6 9.5-6 9.5 6 9.5 6-3.5 6-9.5 6-9.5-6-9.5-6Z' /><circle cx='12' cy='12' r='2.7' /></svg></span><span class='password-eye-closed' aria-hidden='true'><svg viewBox='0 0 24 24'><path d='M3 3l18 18' /><path d='M10.6 10.6A2.7 2.7 0 0 0 12 14.7c.8 0 1.52-.34 2.02-.88' /><path d='M7.35 7.53C4.28 9.25 2.5 12 2.5 12s3.5 6 9.5 6c1.66 0 3.1-.46 4.31-1.14' /><path d='M14.2 6.25C19.05 7.12 21.5 12 21.5 12s-.78 1.34-2.24 2.73' /></svg></span><span class='sr-only'>Show password</span></button>";
        }

        private string PasswordStrengthMarkup()
        {
            return @"<div id='passwordRequirements' class='password-strength-wrapper' role='status' aria-live='polite'>
<div class='strength-bar'><div class='strength-fill empty'></div></div>
<div class='strength-text'>Strength: <span></span></div>
<div class='password-criteria'>
  <div class='criteria-item'><span class='criteria-icon criteria-length'>No</span><span>At least 8 characters</span></div>
  <div class='criteria-item'><span class='criteria-icon criteria-uppercase'>No</span><span>Uppercase letter</span></div>
  <div class='criteria-item'><span class='criteria-icon criteria-lowercase'>No</span><span>Lowercase letter</span></div>
  <div class='criteria-item'><span class='criteria-icon criteria-number'>No</span><span>Number</span></div>
  <div class='criteria-item'><span class='criteria-icon criteria-special'>No</span><span>Special character</span></div>
</div>
</div>";
        }
    }
}
