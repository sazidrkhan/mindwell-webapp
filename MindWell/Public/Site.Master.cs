// Purpose: Public master-page code-behind for role-aware navigation and shared layout behavior.
using System;
using System.Text;

namespace MindWell
{
    public partial class Site : System.Web.UI.MasterPage
    {
        protected bool IsAuthenticated
        {
            get { return SecurityHelper.IsUserAuthenticated(); }
        }

        protected string DashboardUrl
        {
            get { return SecurityHelper.IsUserAdmin() ? "~/Admin/Dashboard.aspx" : "~/User/Dashboard.aspx"; }
        }

        protected string RenderNavigation()
        {
            // Chooses the navigation menu based on the current authenticated role.
            if (SecurityHelper.IsUserAdmin() && !SecurityHelper.IsAdminUserViewMode())
                return RenderAdminNavigation();

            if (IsAuthenticated)
                return RenderUserNavigation();

            return RenderPublicNavigation();
        }

        private string RenderPublicNavigation()
        {
            var sb = new StringBuilder("<ul class='navbar-nav'>");
            AppendNavLink(sb, "~/Default.aspx", "Home");
            AppendNavLink(sb, "~/Public/About.aspx", "About");
            AppendNavLink(sb, "~/Public/Modules.aspx", "Modules");
            AppendNavLink(sb, "~/Public/Articles.aspx", "Articles");
            AppendNavLink(sb, "~/Public/Resources.aspx", "Resources");
            AppendNavLink(sb, "~/Public/FAQ.aspx", "FAQ");
            AppendNavLink(sb, "~/Public/Contact.aspx", "Contact");
            AppendNavLink(sb, "~/Auth/Login.aspx", "Sign In");
            AppendNavLink(sb, "~/Auth/Register.aspx", "Register");
            return sb.Append("</ul>").ToString();
        }

        private string RenderUserNavigation()
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

        private string RenderAdminNavigation()
        {
            var sb = new StringBuilder("<ul class='navbar-nav'>");
            AppendNavLink(sb, "~/Admin/Dashboard.aspx", "Dashboard");
            AppendNavLink(sb, "~/Admin/Users.aspx", "Users");
            AppendNavLink(sb, "~/Admin/ManageModules.aspx", "Modules");
            AppendNavLink(sb, "~/Admin/Content.aspx", "Content");
            AppendNavLink(sb, "~/Admin/ManageQuizzes.aspx", "Quizzes");
            AppendNavLink(sb, "~/Admin/ManageFeedback.aspx", "Feedback");
            AppendNavLink(sb, "~/User/Dashboard.aspx", "User View");
            AppendNavLink(sb, "~/Auth/Login.aspx?logout=1", "Sign Out");
            return sb.Append("</ul>").ToString();
        }

        protected string RenderFooterAuthLinks()
        {
            var sb = new StringBuilder();
            if (SecurityHelper.IsUserAdmin() && !SecurityHelper.IsAdminUserViewMode())
            {
                AppendFooterLink(sb, "~/Admin/Dashboard.aspx", "Admin Dashboard");
                AppendFooterLink(sb, "~/User/Dashboard.aspx", "User View");
                AppendFooterLink(sb, "~/Auth/Login.aspx?logout=1", "Sign Out");
                return sb.ToString();
            }

            if (IsAuthenticated)
            {
                AppendFooterLink(sb, "~/User/Dashboard.aspx", "Dashboard");
                AppendFooterLink(sb, "~/User/Progress.aspx", "Progress");
                AppendFooterLink(sb, "~/User/MoodTracker.aspx", "Mood Tracker");
                if (SecurityHelper.IsUserAdmin() && SecurityHelper.IsAdminUserViewMode())
                    AppendFooterLink(sb, "~/Admin/Dashboard.aspx?exitUserView=1", "Exit User View");
                AppendFooterLink(sb, "~/Auth/Login.aspx?logout=1", "Sign Out");
                return sb.ToString();
            }

            AppendFooterLink(sb, "~/Auth/Login.aspx", "Sign In");
            AppendFooterLink(sb, "~/Auth/Register.aspx", "Register");
            return sb.ToString();
        }

        private void AppendFooterLink(StringBuilder sb, string url, string text)
        {
            sb.Append("<li><a href='").Append(ResolveUrl(url)).Append("'>").Append(text).Append("</a></li>");
        }

        private void AppendNavLink(StringBuilder sb, string url, string text, string cssClass = null)
        {
            // Central helper keeps generated navigation links consistent across public/user/admin menus.
            sb.Append("<li><a href='").Append(ResolveUrl(url)).Append("'");
            if (!string.IsNullOrEmpty(cssClass))
                sb.Append(" class='").Append(cssClass).Append("'");

            sb.Append(">").Append(text).Append("</a></li>");
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Load master page data
            }
        }
    }
}
