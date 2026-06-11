// Purpose: User-area master page guard that ensures member pages require login.
using System;
using System.Text;

namespace MindWell.User
{
    public partial class Site : System.Web.UI.MasterPage
    {
        protected string RenderNavigation()
        {
            var sb = new StringBuilder("<ul class='navbar-nav'>");
            AppendNavLink(sb, "~/User/Dashboard.aspx", "Dashboard");
            AppendNavLink(sb, "~/Public/Modules.aspx", "Modules");
            AppendNavLink(sb, "~/Public/Resources.aspx", "Resources");
            AppendNavLink(sb, "~/User/Quiz.aspx", "Quiz");
            AppendNavLink(sb, "~/User/MoodTracker.aspx", "Mood");
            AppendNavLink(sb, "~/User/Journal.aspx", "Journal");
            AppendNavLink(sb, "~/User/Forum.aspx", "Forum");
            AppendNavLink(sb, "~/User/Assignment.aspx", "Activity");
            AppendNavLink(sb, "~/User/Progress.aspx", "Progress");
            AppendNavLink(sb, "~/User/Profile.aspx", "Profile");
            if (SecurityHelper.IsUserAdmin() && SecurityHelper.IsAdminUserViewMode())
                AppendNavLink(sb, "~/Admin/Dashboard.aspx?exitUserView=1", "Exit User View");
            AppendNavLink(sb, "~/Auth/Login.aspx?logout=1", "Sign Out");
            return sb.Append("</ul>").ToString();
        }

        private void AppendNavLink(StringBuilder sb, string url, string text)
        {
            sb.Append("<li><a href='").Append(ResolveUrl(url)).Append("'>").Append(text).Append("</a></li>");
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // The visual layout lives in markup; this hook enforces access for pages using the master.
            MindWellServices.RequireLogin(Page);
            SecurityHelper.EnableAdminUserViewMode();
        }
    }
}
