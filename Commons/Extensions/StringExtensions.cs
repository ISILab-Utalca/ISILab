using System.Text;
using UnityEngine;

namespace ISILab.Commons.Extensions
{
    public static class StringExtensions
    {
        // Source - https://stackoverflow.com/a/272929
        // Posted by Binary Worrier, modified by community. See post 'Timeline' for change history
        // Retrieved 2026-03-25, License - CC BY-SA 4.0

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

        public static string ReplaceOrErase(this string text, string oldValue, string newValue, bool replaceCondition)
        {
            string newText = text.Replace(oldValue, replaceCondition ? newValue : string.Empty);
            Debug.Log(newText);
            return newText;
        }
    }
}

