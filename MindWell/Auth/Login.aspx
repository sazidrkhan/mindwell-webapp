<%@ Page Title="Sign In - MindWell" Language="C#" MasterPageFile="~/Public/Site.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="MindWell.Auth.Login" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
    <style>
        /* Auth Page Centering */
        main {
            display: flex;
            align-items: center;
            justify-content: center;
            min-height: calc(100vh - 160px);
        }

        .auth-card {
            max-width: 420px;
            width: 100%;
        }

        .auth-logo {
            text-align: center;
            margin-bottom: var(--spacing-lg);
        }

        .auth-logo h2 {
            color: var(--color-primary);
            margin: 0;
            font-size: var(--font-size-3xl);
        }

        .auth-heading {
            text-align: center;
            margin-bottom: var(--spacing-xl);
        }

        .auth-form-group {
            margin-bottom: var(--spacing-lg);
        }

        .auth-form-group label {
            display: block;
            margin-bottom: var(--spacing-sm);
            color: var(--color-secondary);
            font-weight: var(--font-weight-semibold);
            font-size: 0.95rem;
        }

        .auth-form-group input[type="text"],
        .auth-form-group input[type="password"] {
            width: 100%;
            padding: 0.75rem 1rem;
            border: 1px solid var(--border-color);
            border-radius: var(--radius-lg);
            font-size: var(--font-size-base);
            font-family: var(--font-family-body);
            transition: border-color var(--transition-base), box-shadow var(--transition-base);
        }

        .auth-form-group input:focus {
            outline: none;
            border-color: var(--color-primary);
            box-shadow: 0 0 0 3px rgba(107, 168, 168, 0.15);
        }

        .remember-forgot {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin: var(--spacing-md) 0 var(--spacing-lg);
            font-size: 0.95rem;
        }

        .remember-forgot input[type="checkbox"] {
            margin-right: 0.5rem;
        }

        .remember-forgot a {
            color: var(--color-tertiary);
            text-decoration: none;
            transition: color var(--transition-fast);
        }

        .remember-forgot a:hover {
            color: var(--color-tertiary-dark);
            text-decoration: underline;
        }

        .auth-button {
            width: 100%;
            padding: 1rem 2rem;
            margin-bottom: var(--spacing-lg);
        }

        .auth-footer {
            text-align: center;
        }

        .auth-footer p {
            margin: 0;
            color: var(--text-secondary);
            font-size: 1rem;
        }

        .auth-footer a {
            color: var(--color-tertiary);
            font-weight: var(--font-weight-semibold);
            text-decoration: none;
            transition: color var(--transition-fast);
        }

        .auth-footer a:hover {
            color: var(--color-primary);
            text-decoration: underline;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card auth-card">
        <!-- Logo -->
        <div class="auth-logo">
            <h2>MindWell</h2>
        </div>

        <!-- Heading -->
        <div class="auth-heading">
            <h3>Welcome back</h3>
            <p style="color: var(--text-secondary); margin-top: var(--spacing-sm);">Continue your mental health journey with us</p>
        </div>

        <!-- Error Alert -->
        <asp:Panel ID="ErrorPanel" runat="server" Visible="false" CssClass="alert alert-danger" style="margin-bottom: var(--spacing-lg);">
            <asp:Label ID="ErrorLabel" runat="server"></asp:Label>
        </asp:Panel>

        <asp:Panel ID="CredentialPanel" runat="server">
            <!-- Login Form -->
            <div class="auth-form-group">
                <label for="UsernameTextBox">Username</label>
                <asp:TextBox ID="UsernameTextBox" runat="server" placeholder="Enter your username" CssClass="form-control"></asp:TextBox>
                <asp:RequiredFieldValidator ControlToValidate="UsernameTextBox" ErrorMessage="Username is required" Display="Dynamic" CssClass="form-error" runat="server" />
            </div>

            <div class="auth-form-group">
                <label for="PasswordTextBox">Password</label>
                <div class="password-field">
                    <asp:TextBox ID="PasswordTextBox" runat="server" TextMode="Password" placeholder="Enter your password" CssClass="form-control"></asp:TextBox>
                    <button type="button" class="password-toggle password-eye" data-password-toggle aria-label="Show password" aria-pressed="false">
                        <span class="password-eye-open" aria-hidden="true"><svg viewBox="0 0 24 24"><path d="M2.5 12s3.5-6 9.5-6 9.5 6 9.5 6-3.5 6-9.5 6-9.5-6-9.5-6Z" /><circle cx="12" cy="12" r="2.7" /></svg></span>
                        <span class="password-eye-closed" aria-hidden="true"><svg viewBox="0 0 24 24"><path d="M3 3l18 18" /><path d="M10.6 10.6A2.7 2.7 0 0 0 12 14.7c.8 0 1.52-.34 2.02-.88" /><path d="M7.35 7.53C4.28 9.25 2.5 12 2.5 12s3.5 6 9.5 6c1.66 0 3.1-.46 4.31-1.14" /><path d="M14.2 6.25C19.05 7.12 21.5 12 21.5 12s-.78 1.34-2.24 2.73" /></svg></span>
                        <span class="sr-only">Show password</span>
                    </button>
                </div>
                <asp:RequiredFieldValidator ControlToValidate="PasswordTextBox" ErrorMessage="Password is required" Display="Dynamic" CssClass="form-error" runat="server" />
            </div>

            <!-- Remember Me & Forgot Password -->
            <div class="remember-forgot">
                <label style="display: flex; align-items: center; margin: 0;">
                    <asp:CheckBox ID="RememberMeCheckBox" runat="server" />
                    <span style="margin-left: 0.5rem;">Remember me</span>
                </label>
                <a href="~/Auth/ForgotPassword.aspx" runat="server">Forgot password?</a>
            </div>

            <!-- Sign In Button -->
            <asp:Button ID="LoginButton" runat="server" Text="Sign In" CssClass="btn btn-primary btn-lg auth-button" OnClick="LoginButton_Click" />

            <!-- Sign Up Link -->
            <div class="auth-footer">
                <p>Don't have an account? <a href="<%= RenderRegisterUrl() %>">Register</a></p>
            </div>
        </asp:Panel>

        <asp:Panel ID="MfaPanel" runat="server" Visible="false" CssClass="totp-card login-mfa-card">
            <input type="hidden" name="loginStep" value="mfa" />
            <div class="security-status security-status-pending">Two-step verification</div>
            <h3>Verify Authenticator Code</h3>
            <p>Enter the 6-digit code from your authenticator app to finish signing in.</p>
            <div class="auth-form-group">
                <label for="MfaCodeTextBox">Authenticator code</label>
                <asp:TextBox ID="MfaCodeTextBox" runat="server" placeholder="123456" MaxLength="6" CssClass="form-control totp-code-input"></asp:TextBox>
            </div>
            <asp:Button ID="MfaVerifyButton" runat="server" Text="Verify Authenticator Code" CssClass="btn btn-primary btn-lg auth-button" OnClick="LoginButton_Click" CausesValidation="false" />
            <p class="form-helper"><a href="Login.aspx?logout=1">Cancel and sign in again</a></p>
        </asp:Panel>
    </div>
</asp:Content>
