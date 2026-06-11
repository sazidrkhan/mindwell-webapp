<%@ Page Title="FAQ" Language="C#" MasterPageFile="~/Public/Site.Master" AutoEventWireup="true" CodeBehind="FAQ.aspx.cs" Inherits="MindWell.Public.FAQ" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="container" style="padding: 3rem 0;">
        <h1>Frequently Asked Questions</h1>

        <div style="max-width: 800px; margin: 2rem 0;">
            <div class="card mb-lg">
                <h3>What is MindWell?</h3>
                <p>MindWell is a comprehensive web-based learning platform dedicated to mental health and wellbeing education. We provide evidence-based content across four core learning modules: Introduction to Mental Health, Stress Management, Mindfulness, and Self-Care.</p>
            </div>

            <div class="card mb-lg">
                <h3>Is MindWell free?</h3>
                <p>Yes! MindWell is completely free to use. Create an account, and you'll have access to all learning modules, quizzes, and progress tracking.</p>
            </div>

            <div class="card mb-lg">
                <h3>Do I need to create an account?</h3>
                <p>To access learning modules, quizzes, and track your progress, yes, you'll need a free account. However, you can browse the public pages (About, FAQ, Contact) without an account.</p>
            </div>

            <div class="card mb-lg">
                <h3>What devices does MindWell work on?</h3>
                <p>MindWell works on any device with a web browser: desktop computers, tablets, and smartphones. We optimize our design for all screen sizes, including mobile-first responsive design.</p>
            </div>

            <div class="card mb-lg">
                <h3>Do I need JavaScript enabled?</h3>
                <p>No! All core features of MindWell work without JavaScript. You can learn, take quizzes, and track progress with JavaScript disabled. Optional JavaScript may enhance your experience, but it's not required.</p>
            </div>

            <div class="card mb-lg">
                <h3>Is my data private?</h3>
                <p>Yes. Your personal information and learning data are used only for account access, progress tracking, support, and administration. We do not sell or share your data with third parties.</p>
            </div>

            <div class="card mb-lg">
                <h3>How long do the learning modules take?</h3>
                <p>Each module can typically be completed in 2-4 hours, though you can take as much time as you need. You can pause and resume at any time.</p>
            </div>

            <div class="card mb-lg">
                <h3>Do I get a certificate?</h3>
                <p>After completing each module and passing the quiz with 70% or higher, MindWell records your quiz completion so you can track completion and progress.</p>
            </div>

            <div class="card mb-lg">
                <h3>Can I download my certificate?</h3>
                <p>This version focuses on progress and quiz completion records. Certificates are not a separate page in the approved 30-page structure.</p>
            </div>

            <div class="card mb-lg">
                <h3>What if I need mental health support?</h3>
                <p>MindWell is educational and does not replace professional mental health care. If you're in crisis or need immediate support, please contact:</p>
                <ul>
                    <li><strong>National Suicide Prevention Lifeline:</strong> 988 (US)</li>
                    <li><strong>Crisis Text Line:</strong> Text HOME to 741741</li>
                    <li><strong>Emergency Services:</strong> 911</li>
                </ul>
            </div>

            <div class="card mb-lg">
                <h3>How do I contact support?</h3>
                <p>Have a question not answered here? <a href="~/Public/Contact.aspx" runat="server">Contact us</a>. We typically respond within 24-48 hours.</p>
            </div>
        </div>

        <div class="alert alert-info" style="margin-top: 2rem;">
            <strong>Still have questions?</strong> <a href="~/Public/Contact.aspx" runat="server">Get in touch</a> and we'll be happy to help!
        </div>
    </div>
</asp:Content>
