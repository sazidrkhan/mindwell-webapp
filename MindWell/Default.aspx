<%@ Page Title="Welcome - MindWell" Language="C#" MasterPageFile="~/Public/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="MindWell.Default" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="landing-page">
        <%= RenderHomeAlert() %>

    <section class="landing-section hero-section">
        <div class="hero-content">
            <p class="hero-eyebrow"><span aria-hidden="true">✦</span> Your wellbeing, at your pace</p>
            <h1>A little space.<br />A little more <span class="hero-word">you.</span></h1>
            <p class="hero-description">Make room for yourself with practical wellbeing lessons, gentle check-ins, and small steps that fit your day.</p>
            <p class="hero-action">
                <a href="~/Public/Modules.aspx" runat="server" class="btn btn-primary btn-lg hero-cta">Explore wellbeing modules</a>
                <a href="#take-a-breath" class="quiet-link">Take a little pause <span aria-hidden="true">↗</span></a>
            </p>
        </div>

        <figure
            class="hero-visual"
            data-reveal="hero-visual"
            aria-label="An open book growing beside a sunlit plant">
            <svg
                class="wellbeing-hero-art"
                viewBox="0 0 500 400"
                role="img"
                aria-labelledby="homeHeroVisualTitle">
                <title id="homeHeroVisualTitle">Room to learn. Room to grow.</title>
                <use href='<%= ResolveUrl("~/Content/illustrations.svg#garden") %>'></use>
            </svg>
            <figcaption class="sr-only">
                Room to learn. Room to grow.
            </figcaption>
            <span class="garden-note" aria-hidden="true">Small steps count.</span>
        </figure>
    </section>

    <section class="landing-section trust-strip" aria-label="MindWell learning and support">
        <div class="trust-item">
            <span class="trust-label">Learn at your pace</span>
            <span class="trust-copy">Preview topics before you register.</span>
        </div>
        <div class="trust-item">
            <span class="trust-label">Find trusted guidance</span>
            <span class="trust-copy">Explore official source links alongside each topic.</span>
        </div>
        <div class="trust-item">
            <span class="trust-label">Take a practical next step</span>
            <span class="trust-copy">Use lessons, prompts, quizzes, and reflection tools.</span>
        </div>
    </section>

    <section class="landing-section features-section">
        <div class="landing-section-heading">
            <h2>A small step is a good start.</h2>
            <p>Come as you are. Make this space your own.</p>
        </div>
        <div class="features-list">
            <div class="feature-item">
                <span class="feature-index">01</span>
                <h3>Find your starting point</h3>
                <p>Visitors can explore modules, article previews, and official source links before registering.</p>
            </div>
            <div class="feature-item">
                <span class="feature-index">02</span>
                <h3>Learn something useful</h3>
                <p>Registered learners unlock full lessons, guided activities, journals, and module-specific progress.</p>
            </div>
            <div class="feature-item">
                <span class="feature-index">03</span>
                <h3>Give it a little practice</h3>
                <p>Quizzes and reflection prompts turn mental health concepts into practical daily habits.</p>
            </div>
            <div class="feature-item">
                <span class="feature-index">04</span>
                <h3>Notice your progress</h3>
                <p>Quiz answer feedback, progress cards, and mood tools help learners reflect and retry safely.</p>
            </div>
        </div>
    </section>

    <section class="landing-section modules-section">
        <div class="landing-section-heading"><span class="small-label">Something for every kind of day</span><h2>What would help you today?</h2><p>Four starting points. Plenty of room to grow.</p></div>
        <div class="modules-grid">
            <div class="module-card">
                <%= MindWell.WellbeingUi.Artwork(1) %>
                <h3>Mental Health Fundamentals</h3>
                <p>Understand the fundamentals of mental health, common conditions, and available resources.</p>
                <a href="~/Public/Modules.aspx" runat="server" class="topic-link" aria-label="Explore mental health modules">Explore topic <span aria-hidden="true">↗</span></a>
            </div>

            <div class="module-card">
                <%= MindWell.WellbeingUi.Artwork(2) %>
                <h3>Stress Management</h3>
                <p>Learn evidence-based techniques to manage stress and build resilience.</p>
                <a href="~/Public/Modules.aspx" runat="server" class="topic-link" aria-label="Explore stress management modules">Explore topic <span aria-hidden="true">↗</span></a>
            </div>

            <div class="module-card">
                <%= MindWell.WellbeingUi.Artwork(3) %>
                <h3>Mindfulness Practices</h3>
                <p>Explore mindfulness practices to enhance awareness and reduce anxiety.</p>
                <a href="~/Public/Modules.aspx" runat="server" class="topic-link" aria-label="Explore mindfulness modules">Explore topic <span aria-hidden="true">↗</span></a>
            </div>

            <div class="module-card">
                <%= MindWell.WellbeingUi.Artwork(4) %>
                <h3>Self-Care Strategies</h3>
                <p>Develop practical self-care habits and techniques for daily wellbeing.</p>
                <a href="~/Public/Modules.aspx" runat="server" class="topic-link" aria-label="Explore self-care modules">Explore topic <span aria-hidden="true">↗</span></a>
            </div>
        </div>
    </section>

    <div id="take-a-breath" class="landing-section pause-section" tabindex="-1">
        <%= MindWell.WellbeingUi.BreathingExercise("home-breath") %>
    </div>

    <section class="landing-section landing-cta">
        <span class="closing-flower" aria-hidden="true">✳</span>
        <h2>Your next small step starts here.</h2>
        <p class="landing-cta-copy">A place to learn, reflect, and find what works for you.</p>
        <a href="~/Auth/Register.aspx" runat="server" class="btn btn-primary btn-lg">Register</a>
    </section>
    </div>
</asp:Content>
