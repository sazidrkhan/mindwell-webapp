<%@ Page Title="Welcome - MindWell" Language="C#" MasterPageFile="~/Public/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="MindWell.Default" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
    <style>
        .hero-section {
            display: grid;
            grid-template-columns: minmax(0, 1.05fr) minmax(16rem, 0.95fr);
            align-items: center;
            gap: var(--spacing-xl);
            background: linear-gradient(135deg, #2c4a4a 0%, #1a3535 100%);
            color: #F9F7F3;
            padding: var(--spacing-3xl) var(--spacing-xl);
            text-align: left;
            margin-bottom: var(--spacing-3xl);
            border-radius: var(--radius-2xl);
            border: 1px solid color-mix(in srgb, var(--border-color) 58%, transparent);
            box-shadow: var(--shadow-xl);
            transition:
                background-color var(--transition-base),
                color var(--transition-base),
                border-color var(--transition-base),
                box-shadow var(--transition-base);
            overflow: hidden;
        }

        html[data-theme="light"] .hero-section {
            background: linear-gradient(135deg, var(--bg-secondary) 0%, color-mix(in srgb, var(--bg-soft) 78%, white) 100%);
            color: var(--text-primary);
            border-color: var(--border-color);
            box-shadow: var(--shadow-md);
        }

        html[data-theme="dark"] .hero-section {
            background: linear-gradient(135deg, #2c4a4a 0%, #1a3535 100%);
            color: #F9F7F3;
            border-color: color-mix(in srgb, var(--color-primary) 42%, transparent);
            box-shadow: var(--shadow-xl);
        }

        .hero-section h1 {
            font-size: 2.5rem;
            color: #F9F7F3;
            margin-bottom: var(--spacing-md);
            text-shadow: 0 2px 14px rgba(0, 0, 0, 0.28);
        }

        html[data-theme="light"] .hero-section h1 {
            color: var(--text-primary);
            text-shadow: none;
        }

        .hero-section p {
            font-size: 1.1rem;
            color: rgba(255, 255, 255, 0.9);
            margin-bottom: var(--spacing-lg);
        }

        html[data-theme="light"] .hero-section p {
            color: var(--text-secondary);
        }

        .hero-eyebrow {
            display: inline-flex;
            align-items: center;
            width: fit-content;
            padding: 0.35rem 0.8rem;
            border-radius: 999px;
            background: rgba(143, 208, 202, 0.18);
            color: #DDF7F2;
            font-weight: var(--font-weight-semibold);
            font-size: var(--font-size-sm);
            margin-bottom: var(--spacing-md);
        }

        html[data-theme="light"] .hero-eyebrow {
            background: var(--color-primary-light);
            color: var(--text-primary);
        }

        .hero-content,
        .hero-visual {
            min-width: 0;
        }

        .hero-visual {
            margin: 0;
            display: grid;
            place-items: center;
            min-height: 18rem;
            border-radius: var(--radius-2xl);
            border: 1px solid rgba(143, 208, 202, 0.34);
            background:
                radial-gradient(circle at 30% 25%, rgba(168, 217, 184, 0.34), transparent 42%),
                rgba(255, 255, 255, 0.08);
            box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.14);
        }

        html[data-theme="light"] .hero-visual {
            border-color: var(--border-light);
            background:
                radial-gradient(circle at 30% 25%, color-mix(in srgb, var(--color-secondary) 24%, transparent), transparent 44%),
                color-mix(in srgb, var(--bg-soft) 72%, white 28%);
        }

        .wellbeing-hero-art {
            width: min(100%, 24rem);
            height: auto;
            color: currentColor;
        }

        .wellbeing-hero-art .art-stroke {
            fill: none;
            stroke: currentColor;
            stroke-width: 8;
            stroke-linecap: round;
            stroke-linejoin: round;
            opacity: 0.88;
        }

        .wellbeing-hero-art .art-fill-primary {
            fill: #8FD0CA;
            opacity: 0.86;
        }

        .wellbeing-hero-art .art-fill-secondary {
            fill: #A8D9B8;
            opacity: 0.82;
        }

        html[data-theme="light"] .wellbeing-hero-art .art-stroke {
            color: var(--color-primary);
            opacity: 0.9;
        }

        @media (max-width: 780px) {
            .hero-section {
                grid-template-columns: 1fr;
                text-align: center;
            }

            .hero-eyebrow {
                margin-left: auto;
                margin-right: auto;
            }

            .hero-visual {
                min-height: 14rem;
            }
        }

        .cta-button {
            display: inline-block;
            background-color: #6ba8a8;
            color: white;
            padding: 0.75rem 2rem;
            border-radius: var(--radius-lg);
            text-decoration: none;
            font-weight: var(--font-weight-semibold);
            transition: background-color var(--transition-base);
        }

        .cta-button:hover {
            background-color: #5a9898;
        }

        .modules-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
            gap: var(--spacing-lg);
            margin-bottom: var(--spacing-3xl);
        }

        .module-card {
            background: var(--bg-secondary);
            border: 1px solid var(--border-color);
            border-radius: var(--radius-lg);
            padding: var(--spacing-lg);
            text-align: center;
            transition: box-shadow var(--transition-base), transform var(--transition-base);
        }

        .module-card:hover {
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
            transform: translateY(-2px);
        }

        .module-card h3 {
            color: var(--color-primary);
            margin-bottom: var(--spacing-md);
        }

        .module-card p {
            color: var(--text-secondary);
            margin-bottom: var(--spacing-lg);
        }

        .module-card .btn {
            display: inline-block;
        }

        .features-section {
            background: var(--bg-secondary);
            padding: var(--spacing-3xl) var(--spacing-xl);
            border-radius: var(--radius-lg);
            margin-bottom: var(--spacing-3xl);
        }

        .features-section h2 {
            text-align: center;
            margin-bottom: var(--spacing-2xl);
        }

        .features-list {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: var(--spacing-lg);
        }

        .feature-item {
            background: var(--bg-secondary);
            padding: var(--spacing-lg);
            border-radius: var(--radius-md);
            text-align: center;
        }

        .feature-item h4 {
            color: var(--color-primary);
            margin-bottom: var(--spacing-sm);
        }

        .feature-item p {
            color: var(--text-secondary);
            font-size: var(--font-size-sm);
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <%= RenderHomeAlert() %>

    <!-- Hero Section -->
    <section class="hero-section">
        <div class="hero-content">
            <p class="hero-eyebrow">Calm learning for student wellbeing</p>
            <h1>Welcome to MindWell</h1>
            <p>Your comprehensive platform for mental health and wellbeing education</p>
            <p style="margin-bottom: 0;">
                <a href="~/Public/Modules.aspx" runat="server" class="btn btn-primary btn-lg hero-cta">Explore Modules</a>
            </p>
        </div>

        <figure
            class="hero-visual"
            aria-label="Abstract learning path illustration with calm leaves and stepping stones">
            <svg
                class="wellbeing-hero-art"
                viewBox="0 0 420 300"
                role="img"
                aria-labelledby="homeHeroVisualTitle">
                <title id="homeHeroVisualTitle">MindWell calm learning path illustration</title>
                <circle class="art-fill-primary" cx="116" cy="94" r="54" />
                <circle class="art-fill-secondary" cx="292" cy="116" r="70" />
                <path class="art-stroke" d="M86 210c52-60 112-68 178-24 34 23 66 27 98 12" />
                <path class="art-stroke" d="M146 86c42 5 72 34 78 76-43-5-73-34-78-76Z" />
                <path class="art-stroke" d="M284 64c-35 20-48 53-36 94 36-19 49-52 36-94Z" />
                <circle class="art-stroke" cx="118" cy="214" r="18" />
                <circle class="art-stroke" cx="214" cy="178" r="18" />
                <circle class="art-stroke" cx="322" cy="202" r="18" />
            </svg>
            <figcaption class="sr-only">
                A calm abstract path showing growth, support, and learning progress.
            </figcaption>
        </figure>
    </section>

    <section class="features-section">
        <h2>How MindWell learning works</h2>
        <div class="features-list">
            <div class="feature-item">
                <h4>Preview</h4>
                <p>Visitors can explore modules, article previews, and official source links before registering.</p>
            </div>
            <div class="feature-item">
                <h4>Learn</h4>
                <p>Registered learners unlock full lessons, guided activities, journals, and module-specific progress.</p>
            </div>
            <div class="feature-item">
                <h4>Practice</h4>
                <p>Quizzes and reflection prompts turn mental health concepts into practical daily habits.</p>
            </div>
            <div class="feature-item">
                <h4>Review</h4>
                <p>Quiz answer feedback, progress cards, and mood tools help learners reflect and retry safely.</p>
            </div>
        </div>
    </section>

    <!-- Learning Modules -->
    <section>
        <h2 style="text-align: center; margin-bottom: var(--spacing-2xl);">Explore Our Learning Modules</h2>
        <div class="modules-grid">
            <div class="module-card">
                <h3>Mental Health Fundamentals</h3>
                <p>Understand the fundamentals of mental health, common conditions, and available resources.</p>
                <a href="~/Public/Modules.aspx" runat="server" class="btn btn-primary">Learn More</a>
            </div>

            <div class="module-card">
                <h3>Stress Management</h3>
                <p>Learn evidence-based techniques to manage stress and build resilience.</p>
                <a href="~/Public/Modules.aspx" runat="server" class="btn btn-primary">Learn More</a>
            </div>

            <div class="module-card">
                <h3>Mindfulness Practices</h3>
                <p>Explore mindfulness practices to enhance awareness and reduce anxiety.</p>
                <a href="~/Public/Modules.aspx" runat="server" class="btn btn-primary">Learn More</a>
            </div>

            <div class="module-card">
                <h3>Self-Care Strategies</h3>
                <p>Develop practical self-care habits and techniques for daily wellbeing.</p>
                <a href="~/Public/Modules.aspx" runat="server" class="btn btn-primary">Learn More</a>
            </div>
        </div>
    </section>

    <!-- Features Section -->
    <section class="features-section">
        <h2>Why Choose MindWell?</h2>
        <div class="features-list">
            <div class="feature-item">
                <h4>Evidence-Based</h4>
                <p>Content developed from research-backed mental health practices</p>
            </div>
            <div class="feature-item">
                <h4>Interactive Learning</h4>
                <p>Quizzes and assessments to reinforce your learning</p>
            </div>
            <div class="feature-item">
                <h4>Track Progress</h4>
                <p>Monitor your journey and celebrate milestones</p>
            </div>
            <div class="feature-item">
                <h4>Accessible Anytime</h4>
                <p>Learn at your own pace, whenever you're ready</p>
            </div>
        </div>
    </section>

    <!-- Call to Action -->
    <section style="text-align: center; padding: var(--spacing-3xl) 0;">
        <h2>Ready to Start Your Wellness Journey?</h2>
        <p style="margin-bottom: var(--spacing-lg); color: var(--text-secondary);">
            Join hundreds of learners improving their mental health and wellbeing
        </p>
        <a href="~/Auth/Register.aspx" runat="server" class="btn btn-primary btn-lg">Register</a>
    </section>
</asp:Content>
