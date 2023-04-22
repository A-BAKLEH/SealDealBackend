using System.Globalization;
using System.Text;

namespace Web.ControllerServices.StaticMethods
{
    public static class AddressHelper
    {
        /// <summary>
        /// trim,  double spaces replaced by single space, spaces around '-' removed, remove dialerics,
        /// lowercase ,virgules removed,replace double or triple spaces by one space.
        /// building number should be separated by first empty space from rest.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string FormatStreetAddress(this string input)
        {
            input = input.Trim().Replace(" -", "-").Replace("- ", "-")
                .Replace(",", " ")
                .Replace("   "," ")
                .Replace("  "," ");
            input = RemoveDiacritics(input).ToLower();

            return input;
        }

        public static string RemoveDiacritics(string text)
        {
            ReadOnlySpan<char> normalizedString = text.Normalize(NormalizationForm.FormD);
            int i = 0;
            Span<char> span = text.Length < 1000
                ? stackalloc char[text.Length]
                : new char[text.Length];

            foreach (char c in normalizedString)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    span[i++] = c;
            }

            return new string(span).Normalize(NormalizationForm.FormC);
        }
    }
}
