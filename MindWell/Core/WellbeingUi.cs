using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;

namespace MindWell
{
    // Shared presentation only: all user/data text remains encoded at the call site.
    public static class WellbeingUi
    {
        private static string TopicSymbol(int moduleId)
        {
            string[] shapes = { "seed", "ripple", "sun", "bloom" };
            return moduleId >= 1 && moduleId <= 4 ? shapes[moduleId - 1] : "seed";
        }

        public static string Artwork(int moduleId)
        {
            string shape = TopicSymbol(moduleId);
            return "<svg class='topic-art' viewBox='0 0 160 130' aria-hidden='true' focusable='false'><use href='" +
                HttpUtility.HtmlAttributeEncode(VirtualPathUtility.ToAbsolute("~/Content/illustrations.svg")) + "#" + shape + "'></use></svg>";
        }

        public static string Scene(int moduleId, string setting = "topic")
        {
            int topic = moduleId >= 1 && moduleId <= 4 ? moduleId : 0;
            string asset = HttpUtility.HtmlAttributeEncode(VirtualPathUtility.ToAbsolute("~/Content/illustrations.svg"));
            var sb = new StringBuilder("<svg class='wellbeing-scene scene-topic-").Append(topic)
                .Append("' data-scene viewBox='0 0 360 220' aria-hidden='true' focusable='false'>")
                .Append("<path class='scene-wash' d='M50 38C96 2 157 25 189 14c64-21 141 23 136 88 8 62-28 102-109 96-75 18-164 6-178-54C21 101 22 64 50 38Z'/>")
                .Append("<circle class='scene-sun scene-rise' cx='273' cy='54' r='28'/><path class='scene-line scene-draw' d='M29 180c70-44 122 20 175-10s92-17 128 4'/>");
            if (setting == "articles" || setting == "dashboard")
            {
                sb.Append("<g class='scene-bob'><path class='scene-paper scene-ink-stroke' d='M67 91q43-15 98 15 51-30 96-15v90q-45-9-96 13-54-22-98-13Z'/>")
                    .Append("<path class='scene-line' d='M165 106v88m-76-79q26-2 55 10m-55 8q26-2 55 10m-55 8q26-2 55 10'/>")
                    .Append("<path class='scene-accent' d='M185 116v44l13-9 13 9v-54Z'/></g>");
                if (setting == "dashboard")
                    sb.Append("<g class='scene-sway'><path class='scene-line' d='M274 175V97'/><path class='scene-leaf' d='M274 131q-46 0-42-36 38-2 42 36m1-12q-2-46 34-43 6 39-34 43'/></g>");
                else
                    sb.Append("<g class='scene-sway'><path class='scene-leaf' d='M241 70q-39 0-38-30 36-4 38 30'/><path class='scene-line' d='M241 99V56'/></g>");
            }
            else if (setting == "resources")
            {
                sb.Append("<g class='scene-bob'><rect class='scene-accent scene-ink-stroke' x='63' y='80' width='36' height='105' rx='7'/>")
                    .Append("<rect class='scene-paper scene-ink-stroke' x='104' y='58' width='44' height='127' rx='7'/><path class='scene-line' d='M77 98h9m-9 62h9m32-82h16m-16 14h16'/>")
                    .Append("<rect class='scene-peach scene-ink-stroke' x='161' y='70' width='83' height='115' rx='10'/><path class='scene-line' d='M177 90h48m-48 13h32'/>")
                    .Append("<circle class='scene-paper' cx='204' cy='141' r='21'/><path class='scene-line scene-draw' d='m193 141 7 7 15-16'/></g>")
                    .Append("<g class='scene-sway'><path class='scene-line' d='M282 181v-50'/><path class='scene-leaf' d='M282 150q-36-1-31-27 28 0 31 27m0-14q0-38 28-38 4 29-28 38'/></g>");
            }
            else
            {
                sb.Append("<g class='scene-bob'><use href='").Append(asset).Append("#").Append(TopicSymbol(moduleId))
                    .Append("' x='91' y='42' width='180' height='147'></use></g>")
                    .Append("<path class='scene-line scene-draw' d='M43 140c-14-27-6-59 18-73m234 32c19 10 23 34 11 50'/>")
                    .Append("<circle class='scene-peach scene-rise' cx='63' cy='57' r='14'/>");
            }
            return sb.Append("<g class='scene-spark scene-line'><path d='M306 101v16m-8-8h16M39 109v12m-6-6h12'/></g><circle class='scene-ink scene-rise' cx='311' cy='150' r='3'/></svg>").ToString();
        }

        public static string LibraryTools(DataTable rows, string prefix, bool formats)
        {
            var topics = new SortedDictionary<int, string>();
            var types = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in rows.Rows)
            {
                int id = MindWellServices.ToInt(row["ModuleID"], 0);
                if (!topics.ContainsKey(id))
                    topics.Add(id, string.IsNullOrWhiteSpace(Convert.ToString(row["ModuleName"])) ? "General wellbeing" : Convert.ToString(row["ModuleName"]));
                if (formats && !string.IsNullOrWhiteSpace(Convert.ToString(row["ResourceType"])))
                    types.Add(Convert.ToString(row["ResourceType"]).Trim());
            }
            string safePrefix = MindWellServices.Encode(prefix);
            var sb = new StringBuilder("<div class='library-tools' data-library-controls hidden><div class='library-search-row'><div class='library-search-field'><label for='")
                .Append(safePrefix).Append("-search'>Search ").Append(safePrefix).Append("</label><input id='").Append(safePrefix)
                .Append("-search' type='search' data-library-search autocomplete='off' placeholder='Find a topic or title' /></div>");
            if (formats)
            {
                sb.Append("<div class='library-format-field'><label for='").Append(safePrefix).Append("-format'>Format</label><select id='")
                    .Append(safePrefix).Append("-format' data-kind-filter><option value=''>All formats</option>");
                foreach (string type in types)
                    sb.Append("<option value='").Append(MindWellServices.Encode(type)).Append("'>").Append(MindWellServices.Encode(type)).Append("</option>");
                sb.Append("</select></div>");
            }
            sb.Append("<button type='button' class='btn btn-ghost library-reset' data-library-reset>Clear filters</button></div><div class='library-topics' role='group' aria-label='Filter by topic'><button type='button' class='topic-chip' data-topic-filter='' aria-pressed='true'>All topics</button>");
            foreach (KeyValuePair<int, string> topic in topics)
                sb.Append("<button type='button' class='topic-chip' data-topic-filter='").Append(topic.Key).Append("' aria-pressed='false'>").Append(MindWellServices.Encode(topic.Value)).Append("</button>");
            return sb.Append("</div><p class='library-count' data-library-count role='status' aria-live='polite'></p></div>").ToString();
        }

        public static string MoodFace(int mood)
        {
            mood = Math.Max(1, Math.Min(5, mood));
            string[] mouths = { "M29 48Q40 35 51 48", "M31 46Q40 40 49 46", "M31 44H49", "M29 42Q40 54 51 42", "M27 40Q40 60 53 40Z" };
            return "<svg class='mood-face mood-face-" + mood + "' viewBox='0 0 80 80' aria-hidden='true' focusable='false'>" +
                "<circle class='face-fill' cx='40' cy='40' r='35'/><g fill='none' stroke='currentColor' stroke-width='3' stroke-linecap='round'>" +
                "<path d='M29 29v3m22-3v3'/><path d='" + mouths[mood - 1] + "'/></g></svg>";
        }

        public static string BreathingExercise(string id)
        {
            string safeId = HttpUtility.HtmlAttributeEncode(id);
            return "<section class='breathing-card' data-breathing data-state='idle' aria-labelledby='" + safeId + "-title'>" +
                "<div class='breathing-copy'><span class='small-label'>A moment for yourself</span><h2 id='" + safeId + "-title'>Find a little breathing space.</h2>" +
                "<p>Follow a gentle breath in, then a longer breath out. One minute, at your own pace.</p>" +
                "<p class='form-helper'>Breathe comfortably. Stop whenever you need to.</p></div>" +
                "<div class='breathing-stage'><div class='breathing-halo' aria-hidden='true'><div class='breathing-disc'></div></div>" +
                "<p class='breathing-status' data-breathing-status role='status' aria-live='polite'>Ready when you are</p>" +
                "<span class='breathing-time' data-breathing-time>60 seconds</span></div>" +
                "<div class='breathing-controls' data-breathing-controls hidden><button type='button' class='btn btn-primary' data-breathing-start>Start breathing</button>" +
                "<button type='button' class='btn btn-ghost' data-breathing-reset disabled>Reset</button></div>" +
                "<noscript><p>Try a gentle breath in and a slower breath out. Repeat at a comfortable pace.</p></noscript></section>";
        }
    }
}
