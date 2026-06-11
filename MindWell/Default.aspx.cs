// Purpose: Home page code-behind for the public landing page.
using System;

namespace MindWell
{
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Load home page content
            }
        }

        protected string RenderHomeAlert()
        {
            string error = Request.QueryString["error"] ?? string.Empty;
            if (string.Equals(error, "404", StringComparison.OrdinalIgnoreCase))
            {
                Response.StatusCode = 404;
                Response.TrySkipIisCustomErrors = true;
                return "<div class='alert alert-warning'>We could not find that page. Use the navigation above to continue.</div>";
            }

            if (string.Equals(error, "invalid-input", StringComparison.OrdinalIgnoreCase))
                return "<div class='alert alert-warning'>That form contained text that looked like markup. Please enter plain text only.</div>";

            return string.Empty;
        }
    }
}
