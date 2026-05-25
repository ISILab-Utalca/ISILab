using System.Text.RegularExpressions;

namespace ISILab.LBS.Plugin.Internal
{
    public static class LBSTextUtilities
    {
        public static string ReturnValidName(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return Regex.Replace(input, @"[^a-zA-Z0-9_]", "");
        }
    }
}
