<%@ Page Title="Register - MindWell" Language="C#" MasterPageFile="~/Public/Site.Master" AutoEventWireup="true" CodeBehind="Register.aspx.cs" Inherits="MindWell.Auth.Register" %>

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

        .form-input-wrapper {
            position: relative;
            display: flex;
            align-items: center;
        }

        .auth-form-group input[type="text"],
        .auth-form-group input[type="email"],
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

        .input-icon {
            position: absolute;
            right: 12px;
            font-size: 1.2rem;
            pointer-events: none;
        }

        .input-icon.success {
            color: var(--password-strong);
        }

        .input-icon.error {
            color: var(--password-weak);
        }

        /* Password Strength Meter */
        .password-strength-wrapper {
            margin-top: 0.75rem;
        }

        .strength-bar {
            height: 6px;
            background-color: var(--bg-tertiary);
            border-radius: 3px;
            overflow: hidden;
            margin-bottom: 0.5rem;
        }

        .strength-fill {
            height: 100%;
            width: 0%;
            transition: width 200ms ease-in-out, background-color 200ms ease-in-out;
            border-radius: 3px;
        }

        .strength-fill.empty {
            background-color: transparent;
        }

        .strength-fill.weak {
            width: 25%;
            background-color: var(--password-weak);
        }

        .strength-fill.fair {
            width: 50%;
            background-color: var(--password-fair);
        }

        .strength-fill.good {
            width: 75%;
            background-color: var(--password-good);
        }

        .strength-fill.strong {
            width: 100%;
            background-color: var(--password-strong);
        }

        .strength-text {
            font-size: var(--font-size-sm);
            color: var(--text-secondary);
        }

        .strength-text span {
            font-weight: var(--font-weight-semibold);
        }

        .password-criteria {
            font-size: 0.875rem;
            margin-top: 0.5rem;
        }

        .criteria-item {
            display: flex;
            align-items: center;
            color: var(--text-secondary);
            margin-bottom: 0.25rem;
        }

        .criteria-icon {
            margin-right: 0.5rem;
            font-weight: bold;
        }

        .criteria-icon.met {
            color: var(--password-strong);
        }

        /* Confirm Password Matcher */
        .match-indicator {
            font-size: var(--font-size-sm);
            margin-top: 0.5rem;
            display: none;
            color: var(--password-strong);
        }

        .match-indicator.error {
            color: var(--password-weak);
        }

        .match-indicator.show {
            display: block;
        }

        /* Terms Checkbox */
        .terms-checkbox {
            display: flex;
            align-items: flex-start;
            margin: var(--spacing-lg) 0;
            font-size: 0.95rem;
        }

        .terms-checkbox input[type="checkbox"] {
            width: auto;
            margin-top: 0.25rem;
            margin-right: 0.75rem;
            cursor: pointer;
        }

        .terms-checkbox label {
            margin: 0;
            color: var(--text-secondary);
            font-weight: normal;
            font-size: inherit;
        }

        .terms-checkbox a {
            color: var(--color-tertiary);
            text-decoration: none;
        }

        .terms-checkbox a:hover {
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

        .form-helper {
            color: var(--text-secondary);
            font-size: var(--font-size-sm);
            margin-top: 0.5rem;
            display: block;
        }
    </style>

    <script>
        // Password strength meter
        function checkPasswordStrength() {
            const password = document.getElementById('<%= PasswordTextBox.ClientID %>').value;
            const strengthBar = document.querySelector('.strength-fill');
            const strengthText = document.querySelector('.strength-text span');
            const criteria = {
                'length': password.length >= 8,
                'uppercase': /[A-Z]/.test(password),
                'lowercase': /[a-z]/.test(password),
                'number': /[0-9]/.test(password),
                'special': /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password)
            };

            // Update criteria display
            Object.keys(criteria).forEach(key => {
                const icon = document.querySelector(`.criteria-${key}`);
                if (icon) {
                    if (criteria[key]) {
                        icon.textContent = 'OK';
                        icon.classList.add('met');
                    } else {
                        icon.textContent = 'No';
                        icon.classList.remove('met');
                    }
                }
            });

            // Calculate strength
            const metCount = Object.values(criteria).filter(Boolean).length;
            strengthBar.className = 'strength-fill empty';

            if (password.length === 0) {
                strengthBar.className = 'strength-fill empty';
                strengthText.textContent = '';
            } else if (metCount <= 2) {
                strengthBar.className = 'strength-fill weak';
                strengthText.textContent = 'Weak';
            } else if (metCount === 3) {
                strengthBar.className = 'strength-fill fair';
                strengthText.textContent = 'Fair';
            } else if (metCount === 4) {
                strengthBar.className = 'strength-fill good';
                strengthText.textContent = 'Good';
            } else {
                strengthBar.className = 'strength-fill strong';
                strengthText.textContent = 'Strong';
            }
        }

        // Confirm password matcher
        function checkPasswordMatch() {
            const password = document.getElementById('<%= PasswordTextBox.ClientID %>').value;
            const confirm = document.getElementById('<%= ConfirmPasswordTextBox.ClientID %>').value;
            const indicator = document.querySelector('.match-indicator');

            if (confirm.length === 0) {
                indicator.classList.remove('show', 'error');
            } else if (password === confirm) {
                indicator.textContent = 'OK - Passwords match';
                indicator.classList.add('show');
                indicator.classList.remove('error');
            } else {
                indicator.textContent = 'No - Passwords do not match';
                indicator.classList.add('show', 'error');
            }
        }

        // Attach event listeners when page loads
        window.addEventListener('load', function () {
            const passwordInput = document.getElementById('<%= PasswordTextBox.ClientID %>');
            const confirmInput = document.getElementById('<%= ConfirmPasswordTextBox.ClientID %>');

            if (passwordInput) {
                passwordInput.addEventListener('input', checkPasswordStrength);
            }
            if (confirmInput) {
                confirmInput.addEventListener('input', checkPasswordMatch);
            }
        });
    </script>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="register-gate-backdrop" data-register-gate-backdrop hidden></div>
    <div class="register-gate-modal" data-register-gate-modal role="dialog" aria-modal="true" aria-labelledby="registerGateTitle" hidden>
        <p class="eyebrow">Before you register</p>
        <h2 id="registerGateTitle">MindWell is a learning and wellbeing support platform</h2>
        <p>MindWell helps students practise mental-health learning activities, mood reflection, quizzes, and supportive discussions. It is not an emergency or clinical counselling service.</p>
        <div class="button-row">
            <button type="button" class="btn btn-primary" data-register-gate-close>Continue to registration</button>
            <a class="btn btn-outline" href="~/Auth/Login.aspx" runat="server">I already have an account</a>
        </div>
    </div>

    <div class="card auth-card" data-register-gate>
        <!-- Logo -->
        <div class="auth-logo">
            <h2>MindWell</h2>
        </div>

        <!-- Heading -->
        <div class="auth-heading">
            <h3>Register through website</h3>
            <p style="color: var(--text-secondary); margin-top: var(--spacing-sm);">Register and begin learning</p>
        </div>

        <!-- Error Alert -->
        <asp:Panel ID="ErrorPanel" runat="server" Visible="false" CssClass="alert alert-danger" style="margin-bottom: var(--spacing-lg);">
            <asp:Label ID="ErrorLabel" runat="server"></asp:Label>
        </asp:Panel>

        <!-- Register Form -->
        <div class="auth-form-group">
            <label for="UsernameTextBox">Username</label>
            <div class="form-input-wrapper">
                <asp:TextBox ID="UsernameTextBox" runat="server" placeholder="Choose a username" CssClass="form-control" MaxLength="50"></asp:TextBox>
                <span class="input-icon" id="usernameIcon" style="display: none;"></span>
            </div>
            <asp:RequiredFieldValidator ControlToValidate="UsernameTextBox" ErrorMessage="Username is required" Display="Dynamic" CssClass="form-error" runat="server" />
            <asp:RegularExpressionValidator ControlToValidate="UsernameTextBox" ValidationExpression="^[A-Za-z0-9_]{3,50}$" ErrorMessage="Username must be between 3 and 50 characters." Display="Dynamic" CssClass="form-error" runat="server" />
            <small class="form-helper">3-50 characters, alphanumeric and underscore only</small>
        </div>

        <div class="auth-form-group">
            <label for="EmailTextBox">Email Address</label>
            <div class="form-input-wrapper">
                <asp:TextBox ID="EmailTextBox" runat="server" TextMode="Email" placeholder="Enter your email" CssClass="form-control"></asp:TextBox>
                <span class="input-icon" id="emailIcon" style="display: none;"></span>
            </div>
            <asp:RequiredFieldValidator ControlToValidate="EmailTextBox" ErrorMessage="Email is required" Display="Dynamic" CssClass="form-error" runat="server" />
            <asp:RegularExpressionValidator ControlToValidate="EmailTextBox" ValidationExpression="^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$" ErrorMessage="Please enter a valid email address" Display="Dynamic" CssClass="form-error" runat="server" />
        </div>

        <div class="auth-form-group">
            <label for="PasswordTextBox">Password</label>
            <div class="password-field">
                <asp:TextBox ID="PasswordTextBox" runat="server" TextMode="Password" placeholder="Create a strong password" CssClass="form-control"></asp:TextBox>
                <button type="button" class="password-toggle password-eye" data-password-toggle aria-label="Show password" aria-pressed="false">
                    <span class="password-eye-open" aria-hidden="true"><svg viewBox="0 0 24 24"><path d="M2.5 12s3.5-6 9.5-6 9.5 6 9.5 6-3.5 6-9.5 6-9.5-6-9.5-6Z" /><circle cx="12" cy="12" r="2.7" /></svg></span>
                    <span class="password-eye-closed" aria-hidden="true"><svg viewBox="0 0 24 24"><path d="M3 3l18 18" /><path d="M10.6 10.6A2.7 2.7 0 0 0 12 14.7c.8 0 1.52-.34 2.02-.88" /><path d="M7.35 7.53C4.28 9.25 2.5 12 2.5 12s3.5 6 9.5 6c1.66 0 3.1-.46 4.31-1.14" /><path d="M14.2 6.25C19.05 7.12 21.5 12 21.5 12s-.78 1.34-2.24 2.73" /></svg></span>
                    <span class="sr-only">Show password</span>
                </button>
            </div>
            
            <div class="password-strength-wrapper">
                <div class="strength-bar">
                    <div class="strength-fill empty"></div>
                </div>
                <div class="strength-text">Strength: <span></span></div>
                <div class="password-criteria">
                    <div class="criteria-item">
                         <span class="criteria-icon criteria-length">No</span>
                         <span>At least 8 characters</span>
                     </div>
                     <div class="criteria-item">
                         <span class="criteria-icon criteria-uppercase">No</span>
                         <span>Uppercase letter (A-Z)</span>
                     </div>
                     <div class="criteria-item">
                         <span class="criteria-icon criteria-lowercase">No</span>
                         <span>Lowercase letter (a-z)</span>
                     </div>
                     <div class="criteria-item">
                         <span class="criteria-icon criteria-number">No</span>
                         <span>Number (0-9)</span>
                     </div>
                     <div class="criteria-item">
                         <span class="criteria-icon criteria-special">No</span>
                         <span>Special character (!@#$%^&*)</span>
                     </div>
                </div>
            </div>
            <asp:RequiredFieldValidator ControlToValidate="PasswordTextBox" ErrorMessage="Password is required" Display="Dynamic" CssClass="form-error" runat="server" />
        </div>

        <div class="auth-form-group">
            <label for="ConfirmPasswordTextBox">Confirm Password</label>
            <div class="password-field">
                <asp:TextBox ID="ConfirmPasswordTextBox" runat="server" TextMode="Password" placeholder="Re-enter your password" CssClass="form-control"></asp:TextBox>
                <button type="button" class="password-toggle password-eye" data-password-toggle aria-label="Show password" aria-pressed="false">
                    <span class="password-eye-open" aria-hidden="true"><svg viewBox="0 0 24 24"><path d="M2.5 12s3.5-6 9.5-6 9.5 6 9.5 6-3.5 6-9.5 6-9.5-6-9.5-6Z" /><circle cx="12" cy="12" r="2.7" /></svg></span>
                    <span class="password-eye-closed" aria-hidden="true"><svg viewBox="0 0 24 24"><path d="M3 3l18 18" /><path d="M10.6 10.6A2.7 2.7 0 0 0 12 14.7c.8 0 1.52-.34 2.02-.88" /><path d="M7.35 7.53C4.28 9.25 2.5 12 2.5 12s3.5 6 9.5 6c1.66 0 3.1-.46 4.31-1.14" /><path d="M14.2 6.25C19.05 7.12 21.5 12 21.5 12s-.78 1.34-2.24 2.73" /></svg></span>
                    <span class="sr-only">Show password</span>
                </button>
            </div>
            <div class="match-indicator"></div>
            <asp:RequiredFieldValidator ControlToValidate="ConfirmPasswordTextBox" ErrorMessage="Please confirm your password" Display="Dynamic" CssClass="form-error" runat="server" />
        </div>

        <!-- Terms Agreement -->
         <div class="terms-checkbox">
             <asp:CheckBox ID="TermsCheckBox" runat="server" />
             <label for="TermsCheckBox" style="margin: 0; font-weight: normal; color: var(--text-secondary); display: inline;">
                 I agree to use MindWell responsibly and understand how my learning data is used for account progress and support.
             </label>
         </div>

        <!-- Register Button -->
        <asp:Button ID="RegisterButton" runat="server" Text="Register" CssClass="btn btn-primary btn-lg auth-button" OnClick="RegisterButton_Click" />

        <!-- Sign In Link -->
        <div class="auth-footer">
            <p>Already have an account? <a href="~/Auth/Login.aspx" runat="server">Sign in</a></p>
        </div>
    </div>
</asp:Content>
