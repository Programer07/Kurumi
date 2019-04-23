using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Kurumi.Common.Extensions
{
    public static class StringExtensions
    {
        public static string Remove(this string s, string Input)
            => s.Replace(Input, "");

        public static string FirstCharToUpper(this string input)
        {
            switch (input)
            {
                case null: throw new ArgumentNullException(nameof(input));
                case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                default:
                    return input.First().ToString().ToUpper() + input.Substring(1).ToLower();
            }
        }

        public static string Unmention(this string str) => str.Replace("@", "ම");

        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }



        private static Dictionary<string, string> HtmlToDiscordTable = new Dictionary<string, string>()
        {
            { "<br>", "\n"},
            { "<i>", "*" },
            { "</i>", "*" },
            { "<q>", '"'.ToString() },
            { "</q>", '"'.ToString() },
            { "<s>", "~~" },
            { "</s>", "~~" },
            { "<strong>", "**" },
            { "</strong>", "**" },
        };

        public static string FormatHTML(this string Input)
        {
            string res = Input;
            foreach (var item in HtmlToDiscordTable)
            {
                res = res.Replace(item.Key, item.Value);
            }
            return res.UnescapeCodes();
        }

        public static string UnescapeCodes(this string src)
        {
            var rx = new Regex("\\\\([0-9A-Fa-f]+)");
            var res = new StringBuilder();
            var pos = 0;
            foreach (Match m in rx.Matches(src))
            {
                res.Append(src.Substring(pos, m.Index - pos));
                pos = m.Index + m.Length;
                res.Append((char)Convert.ToInt32(m.Groups[1].ToString(), 16));
            }
            res.Append(src.Substring(pos));
            return res.ToString();
        }

        //Discord ignores white spaces but not "⠀" (U+2800 / 0x2800)
        public static string Space(this string Text, int Count, bool Subtract = true, bool After = true)
        {
            if (Subtract)
            {
                Count -= Text.Length;
                if (Count < 1)
                    return Text;
            }

            if (After)
                return $"{Text}{new string('⠀', Count)}";
            return $"{new string('⠀', Count)}{Text}";
        }
    }
}