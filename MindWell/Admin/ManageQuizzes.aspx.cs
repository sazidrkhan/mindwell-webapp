// Purpose: Admin quiz-management page for creating quizzes and adding multiple-choice questions.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace MindWell.Admin
{
    public partial class ManageQuizzes : AdminPageBase
    {
        protected string RenderManageQuizzes()
        {
            string message = HandleQuizPost();
            int editQuizId = 0;
            if (Request.QueryString["editQuiz"] != null && !SecurityHelper.TryGetPositiveIntQuery("editQuiz", out editQuizId))
                message += "<div class='alert alert-warning'>Invalid quiz selected.</div>";

            int quizModuleId = 0;
            if (Request.QueryString["quizModuleId"] != null)
                SecurityHelper.TryGetPositiveIntQuery("quizModuleId", out quizModuleId);
            string quizStatus = SecurityHelper.SafeTrim(Request.QueryString["quizStatus"], 20);
            if (string.IsNullOrEmpty(quizStatus))
                quizStatus = "Active"; // Default view is Active.

            string activeQuizWhere = MindWellServices.ActiveQuizWhere;
            if (string.IsNullOrEmpty(activeQuizWhere))
                return MindWellServices.RenderDataUnavailable("Active quiz filter is unavailable.");

            QueryResult quizzesResult = MindWellServices.TryQuery(@"
SELECT q.QuizID, q.QuizTitle, q.Description, q.PassingScore, q.IsPublished, q.ModuleID, lm.ModuleName
FROM Quizzes q
INNER JOIN LearningModules lm ON q.ModuleID=lm.ModuleID
WHERE NULLIF(LTRIM(RTRIM(q.QuizTitle)), '') IS NOT NULL
  AND " + MindWellServices.ActiveModuleWhere + @"
  AND (@ModuleID=0 OR q.ModuleID=@ModuleID)
  AND (@Status='All' OR (@Status='Active' AND q.IsPublished=1) OR (@Status='Archived' AND q.IsPublished=0))
ORDER BY lm.OrderIndex, q.QuizTitle", new SqlParameter("@ModuleID", quizModuleId), new SqlParameter("@Status", quizStatus));
            QueryResult questionsResult = MindWellServices.TryQuery(@"SELECT qq.QuestionID, qq.QuizID, qq.QuestionText, qq.CorrectAnswer, qq.SortOrder, qq.Explanation, q.QuizTitle FROM QuizQuestions qq INNER JOIN Quizzes q ON qq.QuizID=q.QuizID INNER JOIN LearningModules lm ON q.ModuleID=lm.ModuleID WHERE NULLIF(LTRIM(RTRIM(q.QuizTitle)), '') IS NOT NULL AND " + MindWellServices.ActiveModuleWhere + @" AND (@ModuleID=0 OR q.ModuleID=@ModuleID) AND (@Status='All' OR (@Status='Active' AND q.IsPublished=1) OR (@Status='Archived' AND q.IsPublished=0)) ORDER BY q.ModuleID, ISNULL(qq.SortOrder, qq.OrderIndex), qq.QuestionID", new SqlParameter("@ModuleID", quizModuleId), new SqlParameter("@Status", quizStatus));
            QueryResult archivedQuizzesResult = MindWellServices.TryQuery(@"SELECT q.QuizID, q.QuizTitle, lm.ModuleName FROM Quizzes q INNER JOIN LearningModules lm ON q.ModuleID=lm.ModuleID WHERE q.IsPublished=0 AND " + MindWellServices.ActiveModuleWhere + @" ORDER BY lm.OrderIndex, q.QuizTitle");
            if (quizzesResult.HasError || questionsResult.HasError || archivedQuizzesResult.HasError)
                return message + MindWellServices.RenderDataUnavailable("Learning content is temporarily unavailable: quiz management data could not be loaded.");

            DataTable quizzes = quizzesResult.Data;
            DataTable questions = questionsResult.Data;
            DataTable archivedQuizzes = archivedQuizzesResult.Data;
            QueryResult moduleOptionsResult = MindWellServices.TryGetModuleOptions();
            if (moduleOptionsResult.HasError)
                return message + MindWellServices.RenderDataUnavailable("Learning content is temporarily unavailable: quiz management module options could not be loaded.");

            DataTable moduleOptions = moduleOptionsResult.Data;
            string filterModuleOptions = "<option value='0'>All modules</option>" + MindWellServices.RenderModuleOptions(moduleOptions, quizModuleId);

            var sb = new StringBuilder("<p class='back-link'><a class='btn btn-outline btn-sm' href='Dashboard.aspx'>Back to Admin Dashboard</a></p><section class='section-header'><h1>Manage Quizzes</h1><p>Active clean quizzes are shown by default; archived quizzes stay hidden from learners.</p></section>")
                .Append(message)
                .Append("<section class='card admin-filters'><h2>Filters</h2><p id='quizFilterHelp' class='form-helper'>Filter quizzes by module and publication status.</p><div class='admin-filter-grid'><div class='admin-filter-field'><label for='quizModuleId'>Module</label><select id='quizModuleId' name='quizModuleId' autocomplete='off' aria-describedby='quizFilterHelp'>").Append(filterModuleOptions).Append("</select></div><div class='admin-filter-field'><label for='quizStatus'>Status</label><select id='quizStatus' name='quizStatus' autocomplete='off' aria-describedby='quizFilterHelp'>").Append(RenderQuizStatusOptions(quizStatus)).Append("</select></div><button type='button' class='btn btn-primary' onclick=\"window.location='ManageQuizzes.aspx?quizModuleId='+encodeURIComponent(document.getElementById('quizModuleId').value)+'&quizStatus='+encodeURIComponent(document.getElementById('quizStatus').value)\">Apply Filters</button></div></section>")
                .Append("<details class='card admin-entry-panel'><summary>Add Quiz Metadata</summary><div class='admin-entry-body'><p id='quizMetadataHelp' class='form-helper'>Enter quiz details and choose an active module.</p><input type='hidden' name='quizId' value='0' /><label for='quizModule'>Module</label><select id='quizModule' name='moduleId' autocomplete='off' aria-describedby='quizMetadataHelp'>")
                .Append(MindWellServices.RenderModuleOptions(moduleOptions, 1))
                .Append("</select><label for='quizTitle'>Quiz title</label><input id='quizTitle' name='quizTitle' autocomplete='off' aria-describedby='quizMetadataHelp' /><label for='quizDescription'>Description</label><input id='quizDescription' name='description' autocomplete='off' aria-describedby='quizMetadataHelp' /><label for='passingScore'>Passing score</label><input id='passingScore' type='number' name='passingScore' autocomplete='off' aria-describedby='quizMetadataHelp' value='70' min='0' max='100' /><label for='quizPublished'>Published</label><input id='quizPublished' type='checkbox' name='IsPublished' autocomplete='off' aria-describedby='quizMetadataHelp' value='1' checked /> <p class='page-actions'><button class='btn btn-primary' name='quizAction' value='updateQuiz'>Save Quiz Metadata</button></p></div></details><details class='card admin-entry-panel'><summary>Save Question</summary><div class='admin-entry-body'><p id='questionHelp' class='form-helper'>Add one multiple-choice question to a quiz.</p><label for='questionId'>Question ID (0 creates new)</label><input id='questionId' type='number' name='questionId' autocomplete='off' aria-describedby='questionHelp' value='0' /><label for='questionQuizId'>Quiz</label><select id='questionQuizId' name='questionQuizId' autocomplete='off' aria-describedby='questionHelp'>")
                .Append(RenderQuizOptions(quizzes))
                .Append("</select><label for='question'>Question</label><textarea id='question' name='question' autocomplete='off' aria-describedby='questionHelp'></textarea><label for='optionA'>A</label><input id='optionA' name='optionA' autocomplete='off' aria-describedby='questionHelp' /><label for='optionB'>B</label><input id='optionB' name='optionB' autocomplete='off' aria-describedby='questionHelp' /><label for='optionC'>C</label><input id='optionC' name='optionC' autocomplete='off' aria-describedby='questionHelp' /><label for='optionD'>D</label><input id='optionD' name='optionD' autocomplete='off' aria-describedby='questionHelp' /><label for='correct'>Correct answer</label><select id='correct' name='correct' autocomplete='off' aria-describedby='questionHelp'><option>A</option><option>B</option><option>C</option><option>D</option></select><label for='sortOrder'>SortOrder</label><input id='sortOrder' type='number' name='SortOrder' autocomplete='off' aria-describedby='questionHelp' value='1' /><label for='explanation'>Explanation</label><textarea id='explanation' name='Explanation' autocomplete='off' aria-describedby='questionHelp' rows='3'></textarea><p class='page-actions'><button class='btn btn-primary' name='questionAction' value='saveQuestion'>Save Question</button></p></div></details><details class='card admin-list-panel'><summary>Quizzes (").Append(quizzes.Rows.Count).Append(" shown)<span class='admin-status-chip'>Default: Active</span></summary><div class='admin-list-body'><div class='table-responsive'><table class='data-table admin-compact-table'><tr><th>ID</th><th>Quiz</th><th>Module</th><th>Passing</th><th>Published</th><th>Actions</th></tr>");
            foreach (DataRow row in quizzes.Rows)
            {
                if (MindWellServices.ToInt(row["QuizID"], 0) == editQuizId)
                    AppendQuizEditRow(sb, row, moduleOptions);
                else
                    AppendQuizDisplayRow(sb, row);
            }

            sb.Append("</table></div></div></details><details class='card admin-list-panel'><summary>Archived Quizzes (").Append(archivedQuizzes.Rows.Count).Append(" shown)<span class='admin-status-chip'>Hidden from learners</span></summary><div class='admin-list-body'><p>Archived quizzes are hidden from learners and excluded from active counts.</p><ul class='learning-list'>");
            foreach (DataRow row in archivedQuizzes.Rows)
                sb.Append("<li>").Append(MindWellServices.Encode(row["ModuleName"])).Append(" - ").Append(MindWellServices.Encode(row["QuizTitle"])).Append("</li>");

            sb.Append("</ul></div></details><details class='card admin-list-panel'><summary>Questions (").Append(questions.Rows.Count).Append(" shown)<span class='admin-status-chip'>Filtered with quizzes</span></summary><div class='admin-list-body'><div class='table-responsive'><table class='data-table admin-compact-table'><tr><th>Quiz</th><th>Question</th><th>Correct</th><th>SortOrder</th><th>Explanation</th><th>Action</th></tr>");
            foreach (DataRow row in questions.Rows)
                sb.Append("<tr><td>").Append(MindWellServices.Encode(row["QuizTitle"])).Append("</td><td>").Append(MindWellServices.Encode(row["QuestionText"])).Append("</td><td>").Append(MindWellServices.Encode(row["CorrectAnswer"])).Append("</td><td>").Append(MindWellServices.Encode(row["SortOrder"])).Append("</td><td>").Append(MindWellServices.Encode(row["Explanation"])).Append("</td><td><button formnovalidate data-confirm='Are you sure you want to delete this test record?' class='btn btn-danger btn-sm' name='questionAction' value='deleteQuestion:").Append(row["QuestionID"]).Append("'>Delete</button></td></tr>");
            return sb.Append("</table></div></div></details>").ToString();
        }

        private string HandleQuizPost()
        {
            if (!IsPostBack)
                return string.Empty;

            var form = Request.Unvalidated.Form;
            string quizAction = form["quizAction"] ?? string.Empty;
            if (quizAction == "updateQuiz")
                return SaveQuizMetadata(0, string.Empty);

            if (quizAction.StartsWith("saveQuizEdit:", StringComparison.Ordinal))
                return SaveQuizMetadata(MindWellServices.ToInt(quizAction.Split(':')[1], 0), "_" + quizAction.Split(':')[1]);

            if (quizAction.StartsWith("archiveQuiz:", StringComparison.Ordinal))
            {
                int quizId = MindWellServices.ToInt(quizAction.Split(':')[1], 0);
                int rowsAffected = MindWellServices.NonQuery("UPDATE Quizzes SET IsPublished=0, UpdatedDate=GETDATE() WHERE QuizID=@QuizID", new SqlParameter("@QuizID", quizId));
                return rowsAffected > 0 ? "<div class='alert alert-warning'>Quiz archived.</div>" : "<div class='alert alert-warning'>Quiz was not archived.</div>";
            }

            if (quizAction.StartsWith("deleteQuiz:", StringComparison.Ordinal))
            {
                int quizId = MindWellServices.ToInt(quizAction.Split(':')[1], 0);
                if (!QuizCanHardDelete(quizId))
                {
                    int archiveRows = MindWellServices.NonQuery("UPDATE Quizzes SET IsPublished=0, UpdatedDate=GETDATE() WHERE QuizID=@QuizID", new SqlParameter("@QuizID", quizId));
                    return archiveRows > 0 ? "<div class='alert alert-warning'>Quiz archived.</div>" : "<div class='alert alert-warning'>Quiz could not be removed.</div>";
                }

                MindWellServices.NonQuery("DELETE FROM QuizQuestions WHERE QuizID=@QuizID", new SqlParameter("@QuizID", quizId));
                int rowsAffected = MindWellServices.NonQuery("DELETE FROM Quizzes WHERE QuizID=@QuizID", new SqlParameter("@QuizID", quizId));
                return rowsAffected > 0 ? "<div class='alert alert-success'>Quiz deleted.</div>" : "<div class='alert alert-warning'>Quiz was not deleted.</div>";
            }

            if (form["questionAction"] == "saveQuestion")
            {
                int questionId = MindWellServices.ToInt(form["questionId"], 0);
                int sortOrder = MindWellServices.ToInt(form["SortOrder"], 1);
                if (questionId > 0)
                {
                    MindWellServices.NonQuery("UPDATE QuizQuestions SET QuizID=@QuizID, QuestionText=@Question, OptionA=@A, OptionB=@B, OptionC=@C, OptionD=@D, CorrectAnswer=@Correct, OrderIndex=@SortOrder, SortOrder=@SortOrder, Explanation=@Explanation WHERE QuestionID=@QuestionID", new SqlParameter("@QuizID", MindWellServices.ToInt(form["questionQuizId"], 1)), new SqlParameter("@Question", SecurityHelper.SafeTrim(form["question"], 500)), new SqlParameter("@A", SecurityHelper.SafeTrim(form["optionA"], 200)), new SqlParameter("@B", SecurityHelper.SafeTrim(form["optionB"], 200)), new SqlParameter("@C", SecurityHelper.SafeTrim(form["optionC"], 200)), new SqlParameter("@D", SecurityHelper.SafeTrim(form["optionD"], 200)), new SqlParameter("@Correct", SecurityHelper.SafeTrim(form["correct"], 1)), new SqlParameter("@SortOrder", sortOrder), new SqlParameter("@Explanation", SecurityHelper.SafeTrim(form["Explanation"], 700)), new SqlParameter("@QuestionID", questionId));
                }
                else
                {
                    MindWellServices.NonQuery("INSERT INTO QuizQuestions (QuizID, QuestionText, OptionA, OptionB, OptionC, OptionD, CorrectAnswer, OrderIndex, SortOrder, Explanation) VALUES (@QuizID,@Question,@A,@B,@C,@D,@Correct,@SortOrder,@SortOrder,@Explanation)", new SqlParameter("@QuizID", MindWellServices.ToInt(form["questionQuizId"], 1)), new SqlParameter("@Question", SecurityHelper.SafeTrim(form["question"], 500)), new SqlParameter("@A", SecurityHelper.SafeTrim(form["optionA"], 200)), new SqlParameter("@B", SecurityHelper.SafeTrim(form["optionB"], 200)), new SqlParameter("@C", SecurityHelper.SafeTrim(form["optionC"], 200)), new SqlParameter("@D", SecurityHelper.SafeTrim(form["optionD"], 200)), new SqlParameter("@Correct", SecurityHelper.SafeTrim(form["correct"], 1)), new SqlParameter("@SortOrder", sortOrder), new SqlParameter("@Explanation", SecurityHelper.SafeTrim(form["Explanation"], 700)));
                }

                return "<div class='alert alert-success'>Question saved.</div>";
            }

            if (form["questionAction"] != null && form["questionAction"].StartsWith("deleteQuestion:", StringComparison.Ordinal))
            {
                int questionId = MindWellServices.ToInt(form["questionAction"].Split(':')[1], 0);
                int attemptAnswerCount = MindWellServices.ToInt(MindWellServices.Scalar("SELECT COUNT(*) FROM UserQuizAttemptAnswers WHERE QuestionID=@QuestionID", new SqlParameter("@QuestionID", questionId)), 0);
                if (attemptAnswerCount > 0)
                    return "<div class='alert alert-warning'>Question cannot be deleted because attempts reference it.</div>";

                MindWellServices.NonQuery("DELETE FROM QuizQuestions WHERE QuestionID=@QuestionID", new SqlParameter("@QuestionID", questionId));
                return "<div class='alert alert-success'>Question deleted.</div>";
            }

            return string.Empty;
        }

        private string SaveQuizMetadata(int quizId, string suffix)
        {
            var form = Request.Unvalidated.Form;
            string quizTitle = SecurityHelper.SafeTrim(form["quizTitle" + suffix], 200);
            string description = SecurityHelper.SafeTrim(form["description" + suffix], 500);
            int selectedModuleId = MindWellServices.ToInt(form["moduleId" + suffix], 0);
            int passingScore = MindWellServices.ToInt(form["passingScore" + suffix], 70);
            string validation = ValidateQuizInput(quizTitle, selectedModuleId, passingScore);
            if (!string.IsNullOrEmpty(validation))
                return validation;

            int rowsAffected;
            if (quizId > 0)
            {
                rowsAffected = MindWellServices.NonQuery("UPDATE Quizzes SET ModuleID=@ModuleID, QuizTitle=@Title, Description=@Description, PassingScore=@PassingScore, IsPublished=@IsPublished, UpdatedDate=GETDATE() WHERE QuizID=@QuizID", new SqlParameter("@ModuleID", selectedModuleId), new SqlParameter("@Title", quizTitle), new SqlParameter("@Description", description), new SqlParameter("@PassingScore", passingScore), new SqlParameter("@IsPublished", form["IsPublished" + suffix] == "1"), new SqlParameter("@QuizID", quizId));
            }
            else
            {
                rowsAffected = MindWellServices.NonQuery("INSERT INTO Quizzes (ModuleID, QuizTitle, Description, PassingScore, IsPublished, CreatedDate) VALUES (@ModuleID,@Title,@Description,@PassingScore,@IsPublished,GETDATE())", new SqlParameter("@ModuleID", selectedModuleId), new SqlParameter("@Title", quizTitle), new SqlParameter("@Description", description), new SqlParameter("@PassingScore", passingScore), new SqlParameter("@IsPublished", form["IsPublished"] == "1"));
            }

            return rowsAffected > 0 ? "<div class='alert alert-success'>Quiz metadata saved.</div>" : "<div class='alert alert-warning'>Quiz metadata was not saved.</div>";
        }

        private string ValidateQuizInput(string quizTitle, int moduleId, int passingScore)
        {
            if (string.IsNullOrWhiteSpace(quizTitle) || !ModuleExists(moduleId))
                return "<div class='alert alert-danger'>Quiz title and module are required.</div>";

            if (passingScore < 0 || passingScore > 100)
                return "<div class='alert alert-danger'>Passing score must be between 0 and 100.</div>";

            return string.Empty;
        }

        private bool ModuleExists(int moduleId)
        {
            return MindWellServices.ToInt(MindWellServices.Scalar("SELECT COUNT(*) FROM LearningModules lm WHERE lm.ModuleID=@ModuleID AND " + MindWellServices.ActiveModuleWhere, new SqlParameter("@ModuleID", moduleId)), 0) > 0;
        }

        private bool QuizCanHardDelete(int quizId)
        {
            int attemptCount = MindWellServices.ToInt(MindWellServices.Scalar("SELECT COUNT(*) FROM UserQuizAttempts WHERE QuizID=@QuizID", new SqlParameter("@QuizID", quizId)), 0);
            int questionCount = MindWellServices.ToInt(MindWellServices.Scalar("SELECT COUNT(*) FROM QuizQuestions WHERE QuizID=@QuizID", new SqlParameter("@QuizID", quizId)), 0);
            return attemptCount == 0 && questionCount == 0;
        }

        private void AppendQuizDisplayRow(StringBuilder sb, DataRow row)
        {
            int quizId = MindWellServices.ToInt(row["QuizID"], 0);
            sb.Append("<tr><td>").Append(quizId).Append("</td><td>").Append(MindWellServices.Encode(row["QuizTitle"])).Append("</td><td>").Append(MindWellServices.Encode(row["ModuleName"])).Append("</td><td>").Append(row["PassingScore"]).Append("%</td><td>").Append(MindWellServices.Encode(row["IsPublished"])).Append("</td><td><div class='button-row'><a class='btn btn-primary btn-sm' href='ManageQuizzes.aspx?editQuiz=").Append(quizId).Append("'>Edit</a><button formnovalidate data-confirm='Are you sure you want to archive this quiz? It will be hidden from learners.' class='btn btn-sm' name='quizAction' value='archiveQuiz:").Append(quizId).Append("'>Archive</button><button formnovalidate data-confirm='Are you sure you want to delete this test record?' class='btn btn-danger btn-sm' name='quizAction' value='deleteQuiz:").Append(quizId).Append("'>Delete</button></div></td></tr>");
        }

        private void AppendQuizEditRow(StringBuilder sb, DataRow row, DataTable moduleOptions)
        {
            int quizId = MindWellServices.ToInt(row["QuizID"], 0);
            int selectedModuleId = MindWellServices.ToInt(row["ModuleID"], 0);
            bool isPublished = Convert.ToBoolean(row["IsPublished"]);
            sb.Append("<tr class='editing-row'><td>").Append(quizId).Append("</td><td><label for='quizTitle_").Append(quizId).Append("'>Quiz title</label><input id='quizTitle_").Append(quizId).Append("' name='quizTitle_").Append(quizId).Append("' autocomplete='off' aria-describedby='quizEditHelp_").Append(quizId).Append("' value='").Append(MindWellServices.Encode(row["QuizTitle"])).Append("' /><label for='quizDescription_").Append(quizId).Append("'>Description</label><input id='quizDescription_").Append(quizId).Append("' name='description_").Append(quizId).Append("' autocomplete='off' aria-describedby='quizEditHelp_").Append(quizId).Append("' value='").Append(MindWellServices.Encode(row["Description"])).Append("' /></td><td><label for='quizModule_").Append(quizId).Append("'>Module</label><select id='quizModule_").Append(quizId).Append("' name='moduleId_").Append(quizId).Append("' autocomplete='off' aria-describedby='quizEditHelp_").Append(quizId).Append("'>").Append(MindWellServices.RenderModuleOptions(moduleOptions, selectedModuleId)).Append("</select></td><td><label for='quizPassingScore_").Append(quizId).Append("'>Passing score</label><input id='quizPassingScore_").Append(quizId).Append("' type='number' name='passingScore_").Append(quizId).Append("' autocomplete='off' aria-describedby='quizEditHelp_").Append(quizId).Append("' value='").Append(row["PassingScore"]).Append("' min='0' max='100' /></td><td><label for='quizPublished_").Append(quizId).Append("'>Published</label><input id='quizPublished_").Append(quizId).Append("' type='checkbox' name='IsPublished_").Append(quizId).Append("' autocomplete='off' aria-describedby='quizEditHelp_").Append(quizId).Append("' value='1'").Append(isPublished ? " checked" : string.Empty).Append(" /></td><td><small id='quizEditHelp_").Append(quizId).Append("' class='form-helper'>Update this quiz, then save.</small><div class='button-row'><button class='btn btn-primary btn-sm' name='quizAction' value='saveQuizEdit:").Append(quizId).Append("'>Save</button><a class='btn btn-sm' href='ManageQuizzes.aspx'>Cancel</a><button formnovalidate data-confirm='Are you sure you want to archive this quiz? It will be hidden from learners.' class='btn btn-sm' name='quizAction' value='archiveQuiz:").Append(quizId).Append("'>Archive</button><button formnovalidate data-confirm='Are you sure you want to delete this test record?' class='btn btn-danger btn-sm' name='quizAction' value='deleteQuiz:").Append(quizId).Append("'>Delete</button></div></td></tr>");
        }

        private string RenderQuizStatusOptions(string selected)
        {
            string[] statuses = { "Active", "Archived", "All" };
            var sb = new StringBuilder();
            foreach (string status in statuses)
            {
                sb.Append("<option");
                if (string.Equals(status, selected, StringComparison.OrdinalIgnoreCase))
                    sb.Append(" selected");
                sb.Append(">").Append(status).Append("</option>");
            }

            return sb.ToString();
        }

        private string RenderQuizOptions(DataTable quizzes)
        {
            var sb = new StringBuilder();
            foreach (DataRow row in quizzes.Rows)
                sb.Append("<option value='").Append(row["QuizID"]).Append("'>").Append(MindWellServices.Encode(row["QuizTitle"])).Append("</option>");
            return sb.ToString();
        }
    }
}
