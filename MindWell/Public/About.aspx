<%@ Page Title="About MindWell" Language="C#" MasterPageFile="~/Public/Site.Master" AutoEventWireup="true" CodeBehind="About.aspx.cs" Inherits="MindWell.Public.About" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="container" style="padding: 3rem 0;">
        <h1>About MindWell</h1>
        <p class="lead">A comprehensive web-based learning platform for mental health and wellbeing education.</p>

        <div style="max-width: 800px; margin: 2rem 0;">
            <h2>Our Mission</h2>
            <p>At MindWell, we believe that mental health education is fundamental to overall wellbeing. Our mission is to provide accessible, evidence-based learning resources that empower individuals to understand, manage, and improve their mental health.</p>

            <h2>What We Offer</h2>
            <div class="row cols-2">
                <div class="card">
                    <h3>Expert Content</h3>
                    <p>Curated learning materials based on research from leading mental health organizations (WHO, NAMI, NHS, Mayo Clinic).</p>
                </div>
                <div class="card">
                    <h3>Interactive Learning</h3>
                    <p>Engage with quizzes, mood tracking, and progress monitoring to reinforce your learning journey.</p>
                </div>
                <div class="card">
                    <h3>Supportive Community</h3>
                    <p>Connect with others, share experiences, and access community support forums.</p>
                </div>
                <div class="card">
                    <h3>Accessible Design</h3>
                    <p>Works offline, supports dark mode, and requires no plugins. Learn at your own pace.</p>
                </div>
            </div>

            <h2>Our Values</h2>
            <ul>
                <li><strong>Accessibility:</strong> Mental health education should be available to everyone</li>
                <li><strong>Evidence-Based:</strong> All content is grounded in scientific research</li>
                <li><strong>Privacy-First:</strong> Your data is secure and private</li>
                <li><strong>Inclusive:</strong> We serve diverse learners with varying backgrounds and needs</li>
                <li><strong>Empowering:</strong> Education leads to understanding and positive change</li>
            </ul>

            <h2>Learning Modules</h2>
            <p>We focus on four core areas of mental health:</p>
            <ul>
                <li><strong>Introduction to Mental Health:</strong> Fundamentals, common conditions, and getting help</li>
                <li><strong>Stress Management:</strong> Evidence-based techniques for managing stress</li>
                <li><strong>Mindfulness:</strong> Meditation and present-moment awareness practices</li>
                <li><strong>Self-Care:</strong> Developing sustainable wellness habits</li>
            </ul>

            <div class="alert alert-info" style="margin-top: 2rem;">
                <strong>Ready to get started?</strong> <a href="~/Auth/Register.aspx" runat="server">Register</a> to access all learning modules and track completion and progress.
            </div>
        </div>
    </div>
</asp:Content>
