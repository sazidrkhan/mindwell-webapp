using System;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace MindWell.Auth
{
    public partial class Login
    {
        protected global::System.Web.UI.WebControls.TextBox UsernameTextBox;
        protected global::System.Web.UI.WebControls.TextBox PasswordTextBox;
        protected global::System.Web.UI.WebControls.Panel CredentialPanel;
        protected global::System.Web.UI.WebControls.Panel ErrorPanel;
        protected global::System.Web.UI.WebControls.Label ErrorLabel;
        protected global::System.Web.UI.WebControls.CheckBox RememberMeCheckBox;
        protected global::System.Web.UI.WebControls.Button LoginButton;
        protected global::System.Web.UI.WebControls.Panel MfaPanel;
        protected global::System.Web.UI.WebControls.TextBox MfaCodeTextBox;
        protected global::System.Web.UI.WebControls.Button MfaVerifyButton;
    }
}
