using System;

namespace MindWell.Public
{
    public partial class About : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Load about page content
            }
        }
    }
}
