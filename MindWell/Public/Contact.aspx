<%@ Page Title="Contact Us" Language="C#" MasterPageFile="~/Public/Site.Master" AutoEventWireup="true" CodeBehind="Contact.aspx.cs" Inherits="MindWell.Public.Contact" ValidateRequest="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="container" style="padding: 3rem 0;">
        <h1>Contact Us</h1>
        <p>Have questions or feedback? We'd love to hear from you.</p>

        <div style="max-width: 600px; margin: 2rem 0;">
            <asp:Panel ID="SuccessPanel" runat="server" Visible="false">
                <div class="alert alert-success">
                    <strong>Success!</strong> Your message has been sent. We'll get back to you soon.
                </div>
            </asp:Panel>

            <div class="form-group">
                <label for="NameTextBox">Name <span style="color: var(--color-danger);">*</span></label>
                <asp:TextBox ID="NameTextBox" runat="server" CssClass="form-control"></asp:TextBox>
                <asp:RequiredFieldValidator ControlToValidate="NameTextBox" ErrorMessage="Name is required" Display="Dynamic" CssClass="form-error" runat="server" />
            </div>

            <div class="form-group">
                <label for="EmailTextBox">Email <span style="color: var(--color-danger);">*</span></label>
                <asp:TextBox ID="EmailTextBox" runat="server" TextMode="Email" CssClass="form-control"></asp:TextBox>
                <asp:RequiredFieldValidator ControlToValidate="EmailTextBox" ErrorMessage="Email is required" Display="Dynamic" CssClass="form-error" runat="server" />
                <asp:RegularExpressionValidator ControlToValidate="EmailTextBox" ValidationExpression="^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$" ErrorMessage="Please enter a valid email address" Display="Dynamic" CssClass="form-error" runat="server" />
            </div>

            <div class="form-group">
                <label for="SubjectTextBox">Subject <span style="color: var(--color-danger);">*</span></label>
                <asp:TextBox ID="SubjectTextBox" runat="server" CssClass="form-control"></asp:TextBox>
                <asp:RequiredFieldValidator ControlToValidate="SubjectTextBox" ErrorMessage="Subject is required" Display="Dynamic" CssClass="form-error" runat="server" />
            </div>

            <div class="form-group">
                <label for="MessageTextBox">Message <span style="color: var(--color-danger);">*</span></label>
                <asp:TextBox ID="MessageTextBox" runat="server" TextMode="MultiLine" Rows="6" CssClass="form-control"></asp:TextBox>
                <asp:RequiredFieldValidator ControlToValidate="MessageTextBox" ErrorMessage="Message is required" Display="Dynamic" CssClass="form-error" runat="server" />
            </div>

            <asp:Label ID="ErrorLabel" runat="server" CssClass="alert alert-danger" Visible="false"></asp:Label>

            <asp:Button ID="SubmitButton" runat="server" Text="Send Message" CssClass="btn btn-primary" OnClick="SubmitButton_Click" />
        </div>

        <hr style="margin: 3rem 0;">

        <div class="row cols-3" style="margin-top: 2rem;">
            <div class="card">
                <h3>Email</h3>
                <p><a href="mailto:admin@mindwell.local">admin@mindwell.local</a></p>
            </div>
            <div class="card">
                <h3>Hours</h3>
                <p>Monday - Friday<br>9:00 AM - 5:00 PM</p>
            </div>
            <div class="card">
                <h3>Support</h3>
                <p>We typically respond within 24-48 hours</p>
            </div>
        </div>
    </div>
</asp:Content>
