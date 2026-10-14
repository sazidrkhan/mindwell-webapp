// Purpose: User profile page for editing display-name fields and profile bio text.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MindWell.User
{
    public partial class Profile : UserPageBase
    {
        private const string PendingTotpSecretKey = "PendingTotpSecret";

        protected string RenderProfile()
        {
            // Updates editable profile fields while keeping account identifiers such as email read-only.
            int userId = SecurityHelper.GetCurrentUserId() ?? 0;
            string message = Request.QueryString["invalidInput"] == "1" ? "<div class='alert alert-danger'>" + SecurityHelper.PlainTextValidationMessage + "</div>" : string.Empty;
            var form = Request.Unvalidated.Form;
            string profileAction = form["profileAction"];

            if (IsPostBack && profileAction == "save")
            {
                string firstName = SecurityHelper.SafeTrim(form["firstName"], 50);
                string lastName = SecurityHelper.SafeTrim(form["lastName"], 50);
                string bio = SecurityHelper.SafeTrim(form["bio"], 500);
                if (SecurityHelper.IsBlankAfterTrim(firstName) || SecurityHelper.IsBlankAfterTrim(lastName))
                {
                    message = "<div class='alert alert-danger'>First name and last name are required.</div>";
                }
                else if (!SecurityHelper.IsPlainTextSafe(firstName) || !SecurityHelper.IsPlainTextSafe(lastName) || !SecurityHelper.IsPlainTextSafe(bio))
                {
                    message = "<div class='alert alert-danger'>" + SecurityHelper.PlainTextValidationMessage + "</div>";
                }
                else
                {
                    MindWellServices.NonQuery(@"UPDATE Users SET FirstName=@FirstName, LastName=@LastName, ProfileBioText=@Bio WHERE UserID=@UserID",
                        new SqlParameter("@FirstName", firstName),
                        new SqlParameter("@LastName", lastName),
                        new SqlParameter("@Bio", bio),
                        new SqlParameter("@UserID", userId));
                    message = "<div class='alert alert-success'>Profile saved.</div>";
                }
            }
            else if (IsPostBack && profileAction == "startTotpEnrollment")
            {
                Session[PendingTotpSecretKey] = TotpHelper.GenerateSecret();
                message = "<div class='alert alert-info'>Authenticator app setup started. Scan the QR code and enter the 6-digit code.</div>";
            }
            else if (IsPostBack && profileAction == "cancelTotpEnrollment")
            {
                Session.Remove(PendingTotpSecretKey);
                message = "<div class='alert alert-info'>Authenticator setup cancelled. MFA remains off.</div>";
            }
            else if (IsPostBack && profileAction == "confirmTotpEnrollment")
            {
                string pendingSecret = Session[PendingTotpSecretKey] as string;
                if (string.IsNullOrWhiteSpace(pendingSecret))
                {
                    message = "<div class='alert alert-danger'>Authenticator setup expired. Please start again.</div>";
                }
                else if (!TotpHelper.VerifyCode(pendingSecret, form["totpCode"]))
                {
                    message = "<div class='alert alert-danger'>That authenticator code was not valid. Please try again.</div>";
                }
                else
                {
                    MindWellServices.NonQuery("UPDATE Users SET TotpSecret=@TotpSecret, IsTotpEnabled=1 WHERE UserID=@UserID",
                        new SqlParameter("@TotpSecret", pendingSecret),
                        new SqlParameter("@UserID", userId));
                    Session.Remove(PendingTotpSecretKey);
                    SecurityHelper.LogAction(userId, "EnableMfa", "Users", userId, null, "User enabled multi-factor authentication");
                    message = "<div class='alert alert-success'>Authenticator app verification is enabled for your account.</div>";
                }
            }
            else if (IsPostBack && profileAction == "disableTotp")
            {
                MindWellServices.NonQuery("UPDATE Users SET TotpSecret=NULL, IsTotpEnabled=0 WHERE UserID=@UserID", new SqlParameter("@UserID", userId));
                Session.Remove(PendingTotpSecretKey);
                SecurityHelper.LogAction(userId, "DisableMfa", "Users", userId, null, "User disabled multi-factor authentication");
                message = "<div class='alert alert-success'>Authenticator app verification is disabled.</div>";
            }

            DataTable dt = MindWellServices.Query("SELECT Username, Email, FirstName, LastName, ProfileBioText, TotpSecret, IsTotpEnabled FROM Users WHERE UserID=@UserID", new SqlParameter("@UserID", userId));
            DataRow row = dt.Rows.Count > 0 ? dt.Rows[0] : null;

            var sb = new StringBuilder();
            sb.Append("<section class='card'><h1>Profile & Settings</h1>").Append(message).Append(@"
<div class='form-group'><label for='firstName'>First name</label><input id='firstName' name='firstName' autocomplete='given-name' value='").Append(MindWellServices.Encode(row == null ? "" : row["FirstName"])).Append(@"' /></div>
<div class='form-group'><label for='lastName'>Last name</label><input id='lastName' name='lastName' autocomplete='family-name' value='").Append(MindWellServices.Encode(row == null ? "" : row["LastName"])).Append(@"' /></div>
<div class='form-group'><label for='profileEmail'>Email</label><input id='profileEmail' autocomplete='email' disabled value='").Append(MindWellServices.Encode(row == null ? "" : row["Email"])).Append(@"' /></div>
<div class='form-group'><label for='bio'>Bio</label><textarea id='bio' name='bio' autocomplete='off'>").Append(MindWellServices.Encode(row == null ? "" : row["ProfileBioText"])).Append(@"</textarea></div>
<button class='btn btn-primary' name='profileAction' value='save' type='submit'>Save Profile</button> <a class='btn btn-outline' href='ChangePassword.aspx'>Change Password</a></section>");
            sb.Append(RenderTotpSettings(row));
            return sb.ToString();
        }

        private string RenderTotpSettings(DataRow row)
        {
            string username = row == null ? SecurityHelper.GetCurrentUsername() ?? "MindWell user" : row["Username"].ToString();
            bool isEnabled = row != null && row["IsTotpEnabled"] != DBNull.Value && Convert.ToBoolean(row["IsTotpEnabled"]);
            string pendingSecret = Session[PendingTotpSecretKey] as string;

            var sb = new StringBuilder();
            sb.Append("<section class='card totp-card'><div class='security-status ")
                .Append(isEnabled ? "security-status-enabled'>MFA enabled" : "security-status-pending'>Optional security")
                .Append("</div><h2>Authenticator app setup</h2>")
                .Append("<p id='totpCodeHelp'>Add a time-based one-time password from an authenticator app for stronger account protection.</p>");

            if (isEnabled)
            {
                sb.Append("<p>Your account asks for a 6-digit authenticator code after your password.</p>")
                    .Append("<button class='btn btn-danger' type='submit' name='profileAction' value='disableTotp' data-confirm='Disable authenticator app verification?'>Disable MFA</button>");
            }
            else if (!string.IsNullOrWhiteSpace(pendingSecret))
            {
                string uri = TotpHelper.GenerateProvisioningUri(username, pendingSecret);
                sb.Append(TotpHelper.GenerateQrCodeSvg(uri))
                    .Append("<p class='totp-secret'>Manual key: <code>").Append(MindWellServices.Encode(pendingSecret)).Append("</code></p>")
                    .Append("<div class='form-group'><label for='totpCode'>6-digit code</label><input id='totpCode' class='totp-code-input' name='totpCode' maxlength='6' inputmode='numeric' autocomplete='one-time-code' aria-describedby='totpCodeHelp' /></div>")
                    .Append("<div class='button-row totp-action-row'><button class='btn btn-primary totp-action-button' type='submit' name='profileAction' value='confirmTotpEnrollment'>Verify and enable MFA</button>")
                    .Append("<button class='btn btn-outline totp-action-button' type='submit' name='profileAction' value='startTotpEnrollment'>Generate a new QR code</button>")
                    .Append("<button class='btn btn-outline totp-action-button' type='submit' name='profileAction' value='cancelTotpEnrollment'>Cancel setup</button></div>");
            }
            else
            {
                sb.Append("<p>MFA is currently off. You can enable it without changing your password.</p>")
                    .Append("<button class='btn btn-primary totp-action-button' type='submit' name='profileAction' value='startTotpEnrollment'>Set up authenticator app</button>");
            }

            return sb.Append("</section>").ToString();
        }
    }
}
