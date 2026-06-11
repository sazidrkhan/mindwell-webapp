// Purpose: Admin-area master page guard that ensures admin pages require an administrator role.
using System;

namespace MindWell.Admin
{
    public partial class Site : System.Web.UI.MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // The admin master protects pages that share the admin layout and navigation.
            MindWellServices.RequireAdmin(Page);
        }
    }
}
