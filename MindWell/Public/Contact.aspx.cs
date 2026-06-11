// Purpose: Public contact form that validates visitor input and stores messages for admin review.
using System;
using System.Data;
using System.Data.SqlClient;

namespace MindWell.Public
{
    public partial class Contact : System.Web.UI.Page
    {
        private const string PlainTextMessage = "Please enter plain text only. HTML tags or script-like symbols are not allowed.";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && Request.QueryString["invalidInput"] == "1")
            {
                ShowError(PlainTextMessage);
            }
        }

        protected void SubmitButton_Click(object sender, EventArgs e)
        {
            // Combines CSRF, ASP.NET validators, and server-side checks before saving a contact message.
            if (!SecurityHelper.ValidateCsrfOrEndRequest())
                return;

            // Validate form
            if (!Page.IsValid)
            {
                return;
            }

            var form = Request.Unvalidated.Form;
            string name = SecurityHelper.SafeTrim(form[NameTextBox.UniqueID] ?? NameTextBox.Text, 100);
            string email = SecurityHelper.SafeTrim(form[EmailTextBox.UniqueID] ?? EmailTextBox.Text, 100);
            string subject = SecurityHelper.SafeTrim(form[SubjectTextBox.UniqueID] ?? SubjectTextBox.Text, 200);
            string message = SecurityHelper.SafeTrim(form[MessageTextBox.UniqueID] ?? MessageTextBox.Text, 5000);

            // Server-side validation
            if (string.IsNullOrWhiteSpace(name))
            {
                ShowError("Name is required");
                return;
            }

            if (!SecurityHelper.IsValidEmail(email))
            {
                ShowError("Invalid email address");
                return;
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                ShowError("Subject is required");
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                ShowError("Message is required");
                return;
            }

            if (!SecurityHelper.IsPlainTextSafe(name) || !SecurityHelper.IsPlainTextSafe(subject) || !SecurityHelper.IsPlainTextSafe(message))
            {
                ShowError(PlainTextMessage);
                return;
            }

            // Insert into database
            try
            {
                // Contact messages feed the admin Manage Feedback page and audit log.
                string query = @"INSERT INTO Contacts (Name, Email, Subject, Message, CreatedDate)
                               VALUES (@Name, @Email, @Subject, @Message, GETDATE())";

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@Name", name),
                    new SqlParameter("@Email", email),
                    new SqlParameter("@Subject", subject),
                    new SqlParameter("@Message", message)
                };

                DatabaseHelper.ExecuteNonQuery(query, parameters);

                // Log action
                SecurityHelper.LogAction(null, "Contact Form Submitted", "Contacts", 0, "", email);

                // Clear form
                NameTextBox.Text = "";
                EmailTextBox.Text = "";
                SubjectTextBox.Text = "";
                MessageTextBox.Text = "";

                // Show success message
                SuccessPanel.Visible = true;
                ErrorLabel.Visible = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Contact form error: " + ex.Message);
                ShowError("An error occurred while sending your message. Please try again.");
            }
        }

        private void ShowError(string message)
        {
            ErrorLabel.Text = message;
            ErrorLabel.Visible = true;
            SuccessPanel.Visible = false;
        }
    }
}
