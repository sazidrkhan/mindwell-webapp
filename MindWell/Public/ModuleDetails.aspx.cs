// Purpose: Public module-detail page that adapts actions for visitors, users, and administrators.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MindWell.Public
{
    public partial class ModuleDetails : System.Web.UI.Page
    {
        protected string RenderModuleDetails()
        {
            // Invalid/blank module rows are treated as unavailable so public pages never render empty cards.
            int id = 1;
            if (Request.QueryString["ModuleID"] != null && !SecurityHelper.TryGetPositiveIntQuery("ModuleID", out id))
                return "<section class='card empty-state'><h1>Module not available</h1><p>Invalid module selected.</p><p class='page-actions'><a class='btn btn-outline' href='Modules.aspx'>Back to Modules</a></p></section>";

            DataTable module = MindWellServices.Query(@"
SELECT ModuleID, ModuleName, Description, LearningOutcomes, EstimatedMinutes, PracticePrompt
FROM LearningModules lm
WHERE ModuleID=@ModuleID
  AND " + MindWellServices.ActiveModuleWhere, new SqlParameter("@ModuleID", id));
            if (module.Rows.Count == 0)
                return "<section class='card empty-state'><h1>Module not available</h1><p>This module is not available for learning yet.</p><p class='page-actions'><a class='btn btn-outline' href='Modules.aspx'>Back to Modules</a></p></section>";

            DataTable articles = MindWellServices.Query("SELECT TOP 6 ArticleID, Title, PreviewContent, IsMembersOnly FROM Articles WHERE ModuleID=@ModuleID AND IsPublished=1 AND NULLIF(LTRIM(RTRIM(Title)), '') IS NOT NULL AND " + MindWellServices.CleanDemoTextWhere("Title") + " AND " + MindWellServices.CleanDemoTextWhere("ISNULL(PreviewContent, '')") + " ORDER BY CreatedDate DESC", new SqlParameter("@ModuleID", id));
            DataRow row = module.Rows[0];
            bool authenticated = SecurityHelper.IsUserAuthenticated();
            bool admin = SecurityHelper.IsUserAdmin();
            string moduleName = Convert.ToString(row["ModuleName"]);
            string description = Convert.ToString(row["Description"]);
            string outcomes = Convert.ToString(row["LearningOutcomes"]);
            string practicePrompt = Convert.ToString(row["PracticePrompt"]);
            int minutes = MindWellServices.ToInt(row["EstimatedMinutes"], 0);

            var sb = new StringBuilder();
            sb.Append("<p class='back-link'><a class='btn btn-outline btn-sm' href='Modules.aspx'>Back to Modules</a></p><section class='card learning-path'><p class='badge-row'><span class='access-badge'>")
                .Append(authenticated ? "Member access" : "Preview")
                .Append("</span>");
            if (minutes > 0)
                sb.Append(" <span class='access-badge'>Estimated ").Append(minutes).Append(" minutes</span>");

            sb.Append("</p>");
            if (HasText(moduleName))
                sb.Append("<h1>").Append(MindWellServices.Encode(moduleName)).Append("</h1>");
            if (HasText(description))
                sb.Append("<p>").Append(MindWellServices.Encode(description)).Append("</p>");
            if (HasText(outcomes))
                sb.Append("<div class='outcome-row'>").Append(RenderOutcomeChips(outcomes)).Append("</div>");

            sb.Append("<p class='page-actions'>");
            if (authenticated && admin)
            {
                sb.Append("<a class='btn btn-primary' href='../Admin/ManageModules.aspx'>Manage Modules</a> ")
                    .Append("<a class='btn btn-outline' href='Articles.aspx?ModuleID=").Append(id).Append("'>Browse Articles</a>");
            }
            else if (authenticated)
            {
                sb.Append("<a class='btn btn-primary' href='../User/Quiz.aspx?ModuleID=").Append(id).Append("'>Take Module Quiz</a> ")
                    .Append("<a class='btn btn-outline' href='../User/Progress.aspx?ModuleID=").Append(id).Append("'>View My Progress</a>");
            }
            else
            {
                sb.Append("<a class='btn btn-primary' href='../Auth/Login.aspx?returnUrl=").Append(Server.UrlEncode(Request.RawUrl)).Append("'>Sign In</a> ")
                    .Append("<a class='btn btn-outline' href='Articles.aspx?ModuleID=").Append(id).Append("'>Browse Articles</a>");
            }

            sb.Append("</p></section>");
            if (authenticated)
            {
                sb.Append(RenderCourseContent(id, moduleName));
                if (HasText(outcomes))
                    sb.Append("<section class='card checklist-card'><h2>Learning Checklist</h2>").Append(MindWellServices.EncodeLinesAsList(outcomes)).Append("</section>");

                sb.Append(RenderGuidedPractice(id, practicePrompt, authenticated));
            }
            else
            {
                sb.Append(RenderLockedModulePreview());
            }

            sb.Append(RenderFeaturedArticles(articles, authenticated));
            return sb.ToString();
        }

        private bool HasText(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        private string RenderGuidedPractice(int moduleId, string practicePrompt, bool authenticated)
        {
            if (!HasText(practicePrompt))
                return string.Empty;

            string loginUrl = "../Auth/Login.aspx?returnUrl=" + Server.UrlEncode(Request.RawUrl);
            string actions = authenticated
                ? "<a class='btn btn-primary' href='../User/Assignment.aspx?ModuleID=" + moduleId + "'>Start Activity</a>"
                : "<a class='btn btn-primary' href='" + loginUrl + "'>Sign in to Start Activity</a>";
            return "<section class='card reveal-card guided-practice-card'><p class='content-kicker'>Try This Activity</p><h2>Try This Activity</h2><p>Use this short activity to apply what you learned in this module.</p><p>" +
                MindWellServices.Encode(practicePrompt) +
                "</p><p class='page-actions'>" + actions + "</p></section>";
        }

        private string RenderLockedModulePreview()
        {
            string loginUrl = "../Auth/Login.aspx?returnUrl=" + Server.UrlEncode(Request.RawUrl);
            return "<section class='card locked-preview-card'><p class='content-kicker'>Member learning path</p><h2>Sign in to continue learning</h2><p>Members can open the visual learning object, lesson cards, quick checks, checklist, and guided activity.</p><p class='page-actions'><a class='btn btn-primary' href='" + loginUrl + "'>Sign In</a></p></section>";
        }

        private string RenderCourseContent(int moduleId, string moduleName)
        {
            CourseLesson[] lessons = GetCourseLessons(moduleId);
            if (lessons.Length == 0)
                return string.Empty;

            string moduleTitle = HasText(moduleName) ? moduleName : GetModuleTitle(moduleId);
            var sb = new StringBuilder("<section class='course-content'><div class='section-header'><p class='content-kicker'>Interactive learning path</p><h2>Course Content: ")
                .Append(MindWellServices.Encode(moduleTitle))
                .Append("</h2><p>Work through the module diagram and short visual lesson cards before using the checklist, quiz, and guided activity.</p></div>")
                .Append(RenderModuleLearningObject(moduleId))
                .Append("<div class='lesson-grid lesson-card-grid'>");
            for (int i = 0; i < lessons.Length; i++)
            {
                CourseLesson lesson = lessons[i];
                sb.Append("<article class='card lesson-card' data-module-id='").Append(moduleId).Append("'><div class='lesson-card-layout'>")
                    .Append(RenderLessonVisual(lesson.VisualKey, i + 1))
                    .Append("<div class='lesson-body'><h3>").Append(MindWellServices.Encode(lesson.Title)).Append("</h3><p>")
                    .Append(MindWellServices.Encode(lesson.Explanation)).Append("</p><ul class='lesson-takeaways'>");

                foreach (string takeaway in lesson.Takeaways)
                    sb.Append("<li>").Append(MindWellServices.Encode(takeaway)).Append("</li>");

                sb.Append("</ul><div class='lesson-interactions'>").Append(RenderLessonCheck(lesson)).Append(RenderFlipCard(lesson)).Append("</div></div></div></article>");
            }

            return sb.Append("</div></section>").ToString();
        }

        private string RenderModuleLearningObject(int moduleId)
        {
            string titleId = "learning-object-title-" + moduleId;
            switch (moduleId)
            {
                case 1:
                {
                    VloDetail[] details = new[]
                    {
                        new VloDetail("Thriving", "You feel connected, able to study and rest, and able to use support before stress builds. Keep the routines and relationships that help this steadier place last."),
                        new VloDetail("Coping", "You may feel pressure, but you can still complete most daily responsibilities. Add one small support step now, such as asking a question early or protecting sleep."),
                        new VloDetail("Struggling", "Stress or distress is starting to affect focus, mood, sleep, or connection. This is a signal to reduce load where possible and speak with a trusted person or support service."),
                        new VloDetail("Crisis support", "Safety comes first when someone may harm themselves, feels unable to stay safe, or is in immediate danger. Contact trusted adults, crisis services, emergency care, or local emergency numbers right away.")
                    };
                    return "<figure class='module-learning-object module-learning-object-continuum' data-vlo-panel><div class='learning-object-copy'><p class='content-kicker'>Visual learning object</p><h3 id='" + titleId + "'>Mental health continuum</h3><p>Wellbeing moves across ordinary days, pressure, distress, and urgent support needs. The goal is to notice movement early and choose support before crisis.</p>" + RenderVloDetails(moduleId, details) + "</div><div class='learning-object-media' aria-labelledby='" + titleId + "'><svg viewBox='0 0 520 180' focusable='false' aria-hidden='true'><path class='object-line' d='M70 88 H450' /><circle cx='80' cy='88' r='32' /><circle cx='205' cy='88' r='32' /><circle cx='330' cy='88' r='32' /><circle cx='450' cy='88' r='32' /><path class='object-leaf' d='M66 73c18 2 30 14 31 31-18-2-30-14-31-31Z' /><path class='object-wave' d='M190 90c12-16 25-16 37 0' /><path class='object-signal' d='M320 72v32M304 88h32' /><path class='object-support' d='M438 76h24v24h-14l-10 10v-10h-10Z' /></svg>" + RenderVloControls(moduleId, details, "ol", "continuum-labels", false) + "</div><figcaption>Select each point on the continuum to see the matching support step.</figcaption></figure>";
                }
                case 2:
                {
                    VloDetail[] details = new[]
                    {
                        new VloDetail("Trigger", "A trigger is the moment, demand, thought, or interaction that starts the stress response. Naming it clearly helps you solve the real problem instead of blaming yourself."),
                        new VloDetail("Body signal", "Body signals such as tight shoulders, headaches, fast breathing, or stomach discomfort are information. Notice them early so you can pause before overload builds."),
                        new VloDetail("Pause", "A pause can be one slow breath, a sip of water, standing up, or naming what is happening. It creates enough space to choose instead of react."),
                        new VloDetail("Helpful response", "A helpful response is small, specific, and safe. It might be time-boxing one task, asking for clarification, setting a boundary, or reaching out for support.")
                    };
                    return "<figure class='module-learning-object module-learning-object-stress' data-vlo-panel><div class='learning-object-copy'><p class='content-kicker'>Visual learning object</p><h3 id='" + titleId + "'>Stress response map</h3><p>Stress is easier to manage when learners separate the trigger, body signal, coping choice, and recovery action instead of treating everything as one problem.</p>" + RenderVloDetails(moduleId, details) + "</div><div class='learning-object-media stress-map' aria-labelledby='" + titleId + "'><div class='breathing-orb' aria-hidden='true'><span></span></div>" + RenderVloControls(moduleId, details, "ul", "signal-list", false) + "</div><figcaption>The breathing orb is a calm visual reminder to pause before choosing the next action.</figcaption></figure>";
                }
                case 3:
                {
                    VloDetail[] details = new[]
                    {
                        new VloDetail("5 See", "Name five ordinary things you can see, such as a colour, corner, object, shadow, or line. Let the details be simple rather than impressive."),
                        new VloDetail("4 Feel", "Name four things you can feel, such as your feet on the floor, your sleeves, the chair, or air on your skin. This helps attention return to the body."),
                        new VloDetail("3 Hear", "Name three sounds near or far. If the room is quiet, notice subtle sounds like breathing, movement, or distant traffic."),
                        new VloDetail("2 Smell", "Name two smells if you can, or two smells you remember calmly such as soap, tea, rain, or clean fabric. The aim is gentle attention, not perfect answers."),
                        new VloDetail("1 Taste", "Name one taste or take a small sip of water and notice it. Finish by choosing one helpful next action for the moment you are in.")
                    };
                    return "<figure class='module-learning-object module-learning-object-grounding' data-vlo-panel><div class='learning-object-copy'><p class='content-kicker'>Visual learning object</p><h3 id='" + titleId + "'>5-4-3-2-1 grounding</h3><p>This sensory sequence gives attention a structured path back to the present moment when worry, rumination, or panic feels loud.</p>" + RenderVloDetails(moduleId, details) + "</div><div class='learning-object-media' aria-labelledby='" + titleId + "'>" + RenderVloControls(moduleId, details, "div", "grounding-steps", true) + "</div><figcaption>Move through the senses slowly; the aim is a steadier pause, not perfect calm.</figcaption></figure>";
                }
                case 4:
                {
                    VloDetail[] details = new[]
                    {
                        new VloDetail("Sleep", "Protect one repeatable wind-down cue, such as dimming lights or stopping work at a chosen time. Better sleep support starts with a small pattern."),
                        new VloDetail("Food", "Regular fuel helps mood, attention, and energy. A simple meal, snack, or water plan can be more useful than a perfect diet goal."),
                        new VloDetail("Movement", "Movement can be short, gentle, and accessible. A ten-minute walk or stretch still counts when the week is busy."),
                        new VloDetail("Connection", "Connection means reaching toward people who feel safe, respectful, or grounding. One honest message can reduce isolation."),
                        new VloDetail("Reflection", "Reflection helps you notice what is working and what needs support. A few journal lines can turn a difficult day into useful information."),
                        new VloDetail("Boundaries", "Boundaries protect time, energy, and safety. A clear no, a later reply, or a stopping time can be a form of care.")
                    };
                    return "<figure class='module-learning-object module-learning-object-selfcare' data-vlo-panel><div class='learning-object-copy'><p class='content-kicker'>Visual learning object</p><h3 id='" + titleId + "'>Self-care wheel</h3><p>Sustainable wellbeing is built from small repeatable foundations. A balanced week includes body care, emotional care, connection, and recovery.</p>" + RenderVloDetails(moduleId, details) + "</div><div class='learning-object-media' aria-labelledby='" + titleId + "'>" + RenderVloControls(moduleId, details, "div", "self-care-wheel", false) + "</div><figcaption>Pick one wedge to strengthen this week instead of trying to perfect every area at once.</figcaption></figure>";
                }
                default:
                    return string.Empty;
            }
        }

        private string RenderVloControls(int moduleId, VloDetail[] details, string wrapperTag, string wrapperClass, bool splitLeadingNumber)
        {
            bool list = wrapperTag == "ol" || wrapperTag == "ul";
            var sb = new StringBuilder("<").Append(wrapperTag).Append(" class='").Append(wrapperClass).Append("'>");
            for (int i = 0; i < details.Length; i++)
            {
                string targetId = "vlo-" + moduleId + "-" + i;
                if (list)
                    sb.Append("<li>");

                sb.Append("<button type='button' class='vlo-visual-button' data-vlo-toggle data-vlo-target='").Append(targetId)
                    .Append("' aria-controls='").Append(targetId).Append("' aria-expanded='false'>");
                AppendVloButtonLabel(sb, details[i].Label, splitLeadingNumber);
                sb.Append("</button>");

                if (list)
                    sb.Append("</li>");
            }

            return sb.Append("</").Append(wrapperTag).Append(">").ToString();
        }

        private void AppendVloButtonLabel(StringBuilder sb, string label, bool splitLeadingNumber)
        {
            if (splitLeadingNumber && label.Length > 2 && char.IsDigit(label[0]) && label[1] == ' ')
            {
                sb.Append("<strong>").Append(MindWellServices.Encode(label.Substring(0, 1))).Append("</strong><span>")
                    .Append(MindWellServices.Encode(label.Substring(2))).Append("</span>");
                return;
            }

            sb.Append(MindWellServices.Encode(label));
        }

        private string RenderVloDetails(int moduleId, VloDetail[] details)
        {
            var sb = new StringBuilder("<div class='vlo-panel'><div class='vlo-detail-grid'>");
            for (int i = 0; i < details.Length; i++)
            {
                string targetId = "vlo-" + moduleId + "-" + i;
                sb.Append("<div id='").Append(targetId).Append("' class='vlo-content vlo-detail-card' hidden><h4>")
                    .Append(MindWellServices.Encode(details[i].Label)).Append("</h4><p>")
                    .Append(MindWellServices.Encode(details[i].Content)).Append("</p></div>");
            }

            return sb.Append("</div></div>").ToString();
        }

        private string GetModuleTitle(int moduleId)
        {
            switch (moduleId)
            {
                case 1:
                    return "Introduction to Mental Health";
                case 2:
                    return "Stress Management";
                case 3:
                    return "Mindfulness Basics";
                case 4:
                    return "Self-Care and Wellbeing";
                default:
                    return "MindWell module";
            }
        }

        private string RenderLessonCheck(CourseLesson lesson)
        {
            if (!HasText(lesson.RevealPrompt) || !HasText(lesson.RevealAnswer))
                return string.Empty;

            return "<div class='lesson-check lesson-reveal'><p class='content-kicker'>Quick check</p><p>" + MindWellServices.Encode(lesson.RevealPrompt) + "</p><button type='button' class='btn btn-outline btn-sm' data-lesson-reveal aria-expanded='false'>Reveal learning check</button><p class='lesson-reveal-answer' aria-live='polite'><strong>Answer:</strong> " + MindWellServices.Encode(lesson.RevealAnswer) + "</p></div>";
        }

        private string RenderFlipCard(CourseLesson lesson)
        {
            if (!HasText(lesson.FlipFront) || !HasText(lesson.FlipBack))
                return string.Empty;

            return "<button type='button' class='flip-card' data-flip-card aria-pressed='false'><span class='flip-card-front'>" + MindWellServices.Encode(lesson.FlipFront) + "</span><span class='flip-card-back'>" + MindWellServices.Encode(lesson.FlipBack) + "</span></button>";
        }

        private string RenderLessonVisual(string key, int number)
        {
            string label = "Lesson " + number;
            string path;
            if (key == "stigma")
                path = "M7 13h10M7 17h6M8 7h8a4 4 0 0 1 0 8h-2l-3 3v-3H8a4 4 0 0 1 0-8Z";
            else if (key == "stress")
                path = "M12 4v4m0 8v4M4 12h4m8 0h4M7 7l3 3m4 4 3 3m0-10-3 3m-4 4-3 3";
            else if (key == "mindfulness")
                path = "M12 20c4-3 7-6 7-10a7 7 0 0 0-14 0c0 4 3 7 7 10Zm0-12v4l3 2";
            else if (key == "selfcare")
                path = "M12 21s-7-4.4-7-10a4 4 0 0 1 7-2 4 4 0 0 1 7 2c0 5.6-7 10-7 10Z";
            else if (key == "support")
                path = "M6 9a4 4 0 0 1 8 0v1h1a3 3 0 0 1 0 6h-2l-3 3v-3H9a3 3 0 0 1-3-3V9Zm2 1h4m-4 3h7";
            else
                path = "M5 5h14v14H5zM8 9h8M8 13h8M8 17h5";

            return "<div class='lesson-visual lesson-visual-" + MindWellServices.Encode(key) + "'><span>" + label + "</span><svg viewBox='0 0 24 24' aria-hidden='true'><path d='" + path + "' /></svg></div>";
        }

        private CourseLesson[] GetCourseLessons(int moduleId)
        {
            switch (moduleId)
            {
                case 1:
                    return new[]
                    {
                        new CourseLesson("What mental health means", "Mental health is not only the absence of illness. It includes how we think, feel, cope, connect, study, rest, and ask for help when life becomes difficult.", new[] { "Mental health changes across time and situations.", "Everyone has mental health, not only people with a diagnosis.", "Support can be practical, emotional, social, or professional.", "Early support is easier than waiting until a crisis." }, "stigma", "Myth vs fact: Is mental health only about serious illness?", "Fact: everyday stress, relationships, sleep, study pressure, and support systems all shape mental health.", "Myth vs fact: Strong students never need help.", "Fact: asking for support is a skill, not a weakness.", null),
                        new CourseLesson("Stigma and respectful language", "Stigma makes people hide problems. Respectful language, privacy, and curiosity make it safer for learners to talk about mental health without shame.", new[] { "Avoid labels that reduce someone to a condition.", "Do not share someone else's story without consent.", "Use accurate words instead of jokes or blame.", "A calm response can make help-seeking feel possible." }, "stigma", "What is one sentence that reduces stigma?", "Try: I am glad you told me. We can look for support together.", "Myth vs fact: Talking about mental health makes it worse.", "Fact: safe, respectful conversations can reduce isolation.", null),
                        new CourseLesson("Warning signs and support systems", "Warning signs are changes that suggest a learner may need more support. A support system turns concern into a clear next step.", new[] { "Notice changes in sleep, appetite, mood, focus, and withdrawal.", "Look for patterns, not one difficult day in isolation.", "Write down trusted people and services before stress peaks.", "If safety is at risk, contact emergency or crisis support immediately." }, "support", "Which sign needs urgent support?", "Any sign that someone may harm themselves or cannot stay safe needs immediate help from trusted adults, crisis services, or emergency care.", "Myth vs fact: Support maps are only for emergencies.", "Fact: they are useful for everyday stress and early help too.", null)
                    };
                case 2:
                    return new[]
                    {
                        new CourseLesson("Recognising stress triggers", "A trigger is a situation, thought, deadline, or interaction that starts a stress response. Naming triggers helps you plan instead of only reacting.", new[] { "Common triggers include deadlines, uncertainty, conflict, and overload.", "Stress can be useful in small amounts but draining when constant.", "A trigger log can show patterns across a week.", "Planning works best when it is specific and small." }, "stress", "Sort this trigger: assignment deadline, body tension, scrolling late at night.", "Assignment deadline is a trigger, body tension is a sign, and late-night scrolling may be a coping habit that needs a boundary.", "Myth vs fact: Stress is always bad.", "Fact: short stress can energise action, but chronic stress needs support and recovery.", null),
                        new CourseLesson("Physical signs of stress", "Stress is experienced in the body as well as the mind. Recognising body signals early gives you a chance to pause before overload builds.", new[] { "Tense shoulders, headaches, stomach upset, and poor sleep are common signs.", "Racing thoughts often pair with shallow breathing.", "Body signals are information, not failure.", "A short reset can lower intensity before problem-solving." }, "stress", "What is the first useful response to a body stress signal?", "Pause, name the signal, and choose one small regulation step before continuing.", "Myth vs fact: Ignoring stress makes it disappear.", "Fact: ignored stress often returns through sleep, mood, focus, or physical tension.", null),
                        new CourseLesson("Time, boundaries, and recovery", "Stress management is not only relaxation. It also includes choosing what to start, pause, delegate, or decline so your week becomes more manageable.", new[] { "Use time-boxing for large coursework tasks.", "Break work into a next action, not a perfect plan.", "Boundaries protect focus and recovery time.", "Recovery is part of productivity, not a reward after burnout." }, "stress", "Which is a better boundary: I will study all night or I will stop at 10pm and list tomorrow's first task?", "The second boundary is clearer, safer, and easier to repeat.", "Myth vs fact: More hours always means better work.", "Fact: rest and focused blocks usually beat exhausted overworking.", null)
                    };
                case 3:
                    return new[]
                    {
                        new CourseLesson("Present-moment awareness", "Mindfulness means noticing what is happening now with less judgement. It is a practice of returning attention, not forcing the mind to be empty.", new[] { "Wandering attention is normal.", "The practice is noticing and returning.", "Use neutral anchors such as breath, sound, posture, or touch.", "Mindfulness can be brief and practical during study." }, "mindfulness", "What should you do when attention wanders?", "Notice it gently and return to your anchor. That return is the skill being trained.", "Myth vs fact: Mindfulness means no thoughts.", "Fact: thoughts still happen; you practice changing your relationship with them.", null),
                        new CourseLesson("Grounding with the senses", "Grounding uses sensory details to reconnect with the present moment when thoughts feel fast or overwhelming.", new[] { "Name things you can see, feel, hear, smell, and taste.", "Keep your tone curious, not critical.", "Use grounding before sleep, exams, or difficult conversations.", "Pair grounding with slow breathing if your body feels tense." }, "mindfulness", "When is grounding useful?", "It is useful when attention is stuck in worry, rumination, panic, or replaying a stressful event.", "Myth vs fact: Grounding solves every problem.", "Fact: grounding lowers intensity so you can choose the next helpful action.", null),
                        new CourseLesson("A three-minute breathing reset", "A short breathing practice can create a pause between stress and action. Keep it comfortable; never force or strain your breathing.", new[] { "Sit or stand in a stable position.", "Let the exhale be slow and unforced.", "Return to normal breathing if you feel dizzy.", "Use one minute if three minutes feels too long." }, "mindfulness", "What makes this practice safe?", "Comfort, choice, and stopping if it feels unpleasant. MindWell teaches practice, not pressure.", "Myth vs fact: Longer practice is always better.", "Fact: short repeatable practice is more useful than rare perfect practice.", new[] { "Notice your posture", "Breathe in gently", "Exhale slowly", "Name one thing you can feel" })
                    };
                case 4:
                    return new[]
                    {
                        new CourseLesson("Self-care as foundations", "Self-care is not a luxury checklist. It is the ordinary care that protects sleep, food, movement, connection, and recovery.", new[] { "Start with the basics before complex routines.", "Small habits are easier to repeat during coursework.", "A realistic plan fits your actual week.", "Self-care should reduce pressure, not create another standard." }, "selfcare", "What is one foundation habit to protect this week?", "Choose one: a consistent wind-down, a simple meal plan, one walk, or a support message.", "Myth vs fact: Self-care has to be expensive.", "Fact: the most useful care is often ordinary, free, and repeatable.", null),
                        new CourseLesson("Sleep, food, and movement", "The body supports learning. Sleep, nutrition, hydration, and movement all influence mood, focus, memory, and stress tolerance.", new[] { "A wind-down routine helps the body expect sleep.", "Regular meals can stabilise energy and attention.", "Movement can be brief, gentle, and accessible.", "Avoid all-or-nothing thinking about healthy routines." }, "selfcare", "Which plan is more realistic: daily two-hour workouts or a ten-minute walk after class?", "The ten-minute walk is easier to start and repeat, especially during a busy study week.", "Myth vs fact: If a routine is small it does not count.", "Fact: small routines count because they are repeatable.", null),
                        new CourseLesson("Avoiding performative self-care", "Performative self-care looks good but may not meet your real need. Effective self-care asks what support, boundary, rest, or action would actually help.", new[] { "Ask: what do I need, not what looks impressive?", "Use boundaries when overcommitment is the problem.", "Use connection when isolation is the problem.", "Use rest when exhaustion is the problem." }, "selfcare", "What is the difference between helpful and performative self-care?", "Helpful care matches the real need. Performative care may look polished but leave the need untouched.", "Myth vs fact: Self-care means avoiding responsibilities.", "Fact: it helps you meet responsibilities without sacrificing wellbeing.", null)
                    };
                default:
                    return new CourseLesson[0];
            }
        }

        private string RenderFeaturedArticles(DataTable articles, bool authenticated)
        {
            var sb = new StringBuilder("<section class='card'><h2>Featured Articles</h2>");
            if (articles.Rows.Count == 0)
                return sb.Append("<div class='empty-state'><p>No articles are available for this module yet.</p></div></section>").ToString();

            sb.Append("<ul class='learning-list'>");
            foreach (DataRow article in articles.Rows)
            {
                bool membersOnly = Convert.ToBoolean(article["IsMembersOnly"]);
                sb.Append("<li><a href='ArticleDetails.aspx?ArticleID=").Append(article["ArticleID"]).Append("'>")
                    .Append(MindWellServices.Encode(article["Title"])).Append("</a>");
                if (membersOnly && !authenticated)
                    sb.Append(" <span class='access-badge'>Full lesson after sign in</span>");
                else if (membersOnly && authenticated)
                    sb.Append(" <span class='access-badge'>Read full lesson</span>");
                sb.Append("</li>");
            }

            return sb.Append("</ul></section>").ToString();
        }

        private string RenderOutcomeChips(string outcomes)
        {
            if (string.IsNullOrWhiteSpace(outcomes))
                return string.Empty;

            var sb = new StringBuilder();
            foreach (string outcome in outcomes.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string clean = outcome.Trim();
                if (clean.Length > 0)
                    sb.Append("<span class='outcome-chip'>").Append(MindWellServices.Encode(clean)).Append("</span>");
            }

            return sb.ToString();
        }

        private sealed class CourseLesson
        {
            public readonly string Title;
            public readonly string Explanation;
            public readonly string[] Takeaways;
            public readonly string VisualKey;
            public readonly string RevealPrompt;
            public readonly string RevealAnswer;
            public readonly string FlipFront;
            public readonly string FlipBack;

            public CourseLesson(string title, string explanation, string[] takeaways, string visualKey, string revealPrompt, string revealAnswer, string flipFront, string flipBack, string[] breathingSteps)
            {
                Title = title;
                Explanation = explanation;
                Takeaways = takeaways;
                VisualKey = visualKey;
                RevealPrompt = revealPrompt;
                RevealAnswer = revealAnswer;
                FlipFront = flipFront;
                FlipBack = flipBack;
            }
        }

        private sealed class VloDetail
        {
            public readonly string Label;
            public readonly string Content;

            public VloDetail(string label, string content)
            {
                Label = label;
                Content = content;
            }
        }
    }
}
