using System.Text;

namespace ISILab.Commons.Extensions
{
    /// <summary>
    /// Class with extension methods for strings.
    /// </summary>
    public static class StringExtensions
    {
        // Source - https://stackoverflow.com/a/272929
        // Posted by Binary Worrier, modified by community. See post 'Timeline' for change history
        // Retrieved 2026-03-25, License - CC BY-SA 4.0

        /// <summary>
        /// Separates a string by words.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="preserveAcronyms">Wether to preserve acronyms or to split them.</param>
        /// <returns>The same string, with separated words.</returns>
        public static string AddSpacesToSentence(this string text, bool preserveAcronyms = true)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
            StringBuilder newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]))
                    if ((text[i - 1] != ' ' && !char.IsUpper(text[i - 1])) ||
                        (preserveAcronyms && char.IsUpper(text[i - 1]) &&
                         i < text.Length - 1 && !char.IsUpper(text[i + 1])))
                        newText.Append(' ');
                newText.Append(text[i]);
            }
            return newText.ToString();
        }

        /// <summary>
        /// Replaces part of a text with a value, as long as the condition is met. <b>If not, erases that part.</b>
        /// </summary>
        /// <remarks>Based on string.Replace().</remarks>
        /// <param name="text">Full text.</param>
        /// <param name="oldValue">Part of the text to replace.</param>
        /// <param name="newValue">Replacement text.</param>
        /// <param name="replaceCondition">Requirement to perform replacement.</param>
        /// <returns>The full text modified, according to the requirement.</returns>
        public static string ReplaceOrErase(this string text, string oldValue, string newValue, bool replaceCondition)
        {
            string newText = text.Replace(oldValue, replaceCondition ? newValue : string.Empty);
            return newText;
        }

        /// <summary>
        /// Makes the first character of a string lowercase.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>The string with the first character in lowercase.</returns>
        public static string LowerFirst(this string text)
        {
            if(string.IsNullOrEmpty(text)) return string.Empty;

            char[] a = text.ToCharArray();
            a[0] = char.ToLower(a[0]);

            return new string(a);
        }

        /// <summary>
        /// Makes the first character of a string uppercase.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>The string with the first character in uppercase.</returns>
        public static string UpperFirst(this string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            char[] a = text.ToCharArray();
            a[0] = char.ToUpper(a[0]);

            return new string(a);
        }
    }
}

