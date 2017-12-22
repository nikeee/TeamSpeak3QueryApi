namespace TeamSpeak3QueryApi.Net
{
    internal static class StringExtensions
    {
        /// <summary>Escapes a string so it can be safely used for querying the api.</summary>
        /// <param name="s">The string to escape.</param>
        /// <returns>An escaped string.</returns>
        public static string TeamSpeakEscape(this string s)
        {
            if (s == string.Empty)
                return s;

            s = s.Replace("\\", "\\\\"); // Backslash
            s = s.Replace("/", "\\/"); // Slash
            s = s.Replace("|", "\\p"); // Pipe
            s = s.Replace("\n", "\\n"); // Newline
            //r = r.replace("\b", "\\b"); // Info: Backspace fails
            //r = r.replace("\a", "\\a"); // Info: Bell fails
            s = s.Replace("\r", "\\r"); // Carriage Return
            s = s.Replace("\t", "\\t"); // Tab
            s = s.Replace("\v", "\\v"); // Vertical Tab
            s = s.Replace("\f", "\\f"); // Formfeed
            s = s.Replace(" ", "\\s"); // Whitespace
            return s;
        }

        /// <summary>Unescapes a string so it can be used for processing the rawResponse of the api.</summary>
        /// <param name="s">The string to unescape.</param>
        /// <returns>An unescaped string.</returns>
        public static string TeamSpeakUnescape(this string s)
        {
            if (s == string.Empty)
                return s;

            s = s.Replace("\\s", " ");	// Whitespace
            s = s.Replace("\\p", "|"); // Pipe
            s = s.Replace("\\n", "\n"); // Newline
            //r = r.replace(/\\b/g, "\b"); // Info: Backspace fails
            //r = r.replace(/\\a/g, "\a"); // Info: Bell fails
            s = s.Replace("\\f", "\f"); // Formfeed
            s = s.Replace("\\r", "\r"); // Carriage Return
            s = s.Replace("\\t", "\t"); // Tab
            s = s.Replace("\\v", "\v"); // Vertical Tab
            s = s.Replace("\\/", "/"); // Slash
            s = s.Replace("\\\\", "\\"); // Backslash
            return s;
        }
    }
}
