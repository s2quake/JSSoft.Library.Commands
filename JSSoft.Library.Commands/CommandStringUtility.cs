// Released under the MIT License.
// 
// Copyright (c) 2018 Ntreev Soft co., Ltd.
// Copyright (c) 2020 Jeesu Choi
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit
// persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
// Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
// Forked from https://github.com/NtreevSoft/CommandLineParser
// Namespaces and files starting with "Ntreev" have been renamed to "JSSoft".

using JSSoft.Library.Commands.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JSSoft.Library.Commands
{
    /// <summary>
    /// https://rubular.com/r/MEDnmumVL45rDS
    /// </summary>
    public static class CommandStringUtility
    {
        private const string doubleQuotesPattern = "(?<!\\\\)\"(?:\\\\\"|\\\\\\\\|(?!=\\\\)[^\"])*(?<!\\\\)\"";
        private const string singleQuotePattern = "'[^']*'";
        private const string escapedTextPattern = "(?:(?<!\\\\)[^\"'\\s]|(?<=\\\\).)+";
        private const string stringPattern = "[^ \"']+";
        private static readonly string fullPattern;
        private static readonly (string name, string value, Func<string, string> escape)[] patterns =
        {
            ("double", doubleQuotesPattern, EscapeDoubleQuotes),
            ("single", singleQuotePattern, EscapeSingleQuote),
            ("escape", escapedTextPattern, EscapeEscapedText),
            ("string", stringPattern, EscapeEscapedText),
            ("space", "\\s+", (s) => s),
            ("etc", ".+", (s) => s),
        };

        static CommandStringUtility()
        {
            fullPattern = string.Join("|", patterns.Select(item => $"(?<{item.name}>{item.value})"));
        }

        public static bool VerifyEscapeString(string text)
        {
            return GetMatches(text, true) != null;
        }

        public static string[] EscapeString(string text)
        {
            return GetMatches(text, false);
        }

        public static string AggregateString(string[] items)
        {
            return AggregateString(items as IEnumerable<string>);
        }

        public static string AggregateString(IEnumerable<string> items)
        {
            var length = items.Sum(item => item.Length + 3);
            var itemList = new List<string>(items.Count());
            foreach (var item in items)
            {
                var text = Regex.Replace(item, "([\\\\\"])", "\\$1");
                if (text.IndexOf(' ') >= 0)
                    text = $"\"{text}\"";
                itemList.Add(text);
            }
            return string.Join(" ", itemList);
        }

        public static (string first, string rest) Split(string text)
        {
            var items = EscapeString(text);
            var first = items.FirstOrDefault() ?? string.Empty;
            var rest = items.Count() > 0 ? AggregateString(items.Skip(1)) : string.Empty;
            return (first, rest);
        }

        /// <summary>
        /// a=1, a="123", a='123' 과 같은 문자열을 키와 값으로 분리하는 메소드
        /// </summary>
        public static bool TryGetKeyValue(string text, out string key, out string value)
        {
            var capturePattern = string.Format("((?<key>{2})=(?<value>{0})|(?<key>{2})=(?<value>{1})|(?<key>{2})=(?<value>.+))", doubleQuotesPattern, singleQuotePattern, stringPattern);
            var match = Regex.Match(text, capturePattern, RegexOptions.ExplicitCapture);
            if (match.Success)
            {
                key = match.Groups["key"].Value;
                value = match.Groups["value"].Value;
                return true;
            }
            key = null;
            value = null;
            return false;
        }

        [Obsolete]
        public static Match[] MatchAll(string text)
        {
            var matches = Regex.Matches(text, fullPattern);
            var argList = new List<Match>();
            foreach (Match item in matches)
            {
                argList.Add(item);
            }
            return argList.ToArray();
        }

        public static Match[] MatchCompletion(string text)
        {
            var matches = Regex.Matches(text, fullPattern);
            var argList = new List<Match>(matches.Count);
            foreach (Match item in matches)
            {
                argList.Add(item);
            }
            return argList.ToArray();
        }

        public static bool IsMultipleSwitch(string argument)
        {
            return Regex.IsMatch(argument, @$"^{CommandSettings.ShortDelimiter}\w{{2,}}");
        }

        public static bool IsOption(string argument)
        {
            if (argument == null)
                return false;
            return Regex.IsMatch(argument, $"^{CommandSettings.Delimiter}{CommandSettings.OptionPattern}$|^{CommandSettings.ShortDelimiter}{CommandSettings.ShortOptionPattern}$");
        }

        public static string ToSpinalCase(string text)
        {
            return Regex.Replace(text, @"([a-z])([A-Z])", "$1-$2").ToLower();
        }

        public static string ToSpinalCase(Type type)
        {
            var name = Regex.Replace(type.Name, @"(Command)$", string.Empty);
            return Regex.Replace(name, @"([a-z])([A-Z])", "$1-$2").ToLower();
        }

        public static IDictionary<string, object> ArgumentsToDictionary(string[] arguments)
        {
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));

            var properties = new Dictionary<string, object>(arguments.Length);
            foreach (var item in arguments)
            {
                var text = item;

                if (CommandStringUtility.TryGetKeyValue(text, out var key, out var value) == true)
                {
                    if (decimal.TryParse(value, out decimal l) == true)
                    {
                        properties.Add(key, l);
                    }
                    else if (bool.TryParse(value, out bool b) == true)
                    {
                        properties.Add(key, b);
                    }
                    else
                    {
                        properties.Add(key, value);
                    }
                }
                else
                {
                    throw new ArgumentException(string.Format(Resources.Exception_InvalidValue_Format, item));
                }
            }
            return properties;
        }

        private static string[] GetMatches(string text, bool verify)
        {
            var matches = Regex.Matches(text, fullPattern);
            var itemList = new List<string>();
            var sb = new StringBuilder(text.Length);
            foreach (Match item in matches)
            {
                var (name, value) = EscapeValue(item);
                if (name == "space")
                {
                    itemList.Add(sb.ToString());
                    sb.Clear();
                }
                else if (name == "etc" || name == "unknown")
                {
                    if (verify == true)
                        return null;
                    throw new ArgumentException();
                }
                else
                {
                    sb.Append(value);
                }
            }
            if (sb.Length != 0)
            {
                itemList.Add(sb.ToString());
            }

            return itemList.ToArray();

            static (string name, string value) EscapeValue(Match match)
            {
                for (var i = 0; i < patterns.Length; i++)
                {
                    if (match.Groups[i + 1].Value == match.Value)
                        return (patterns[i].name, patterns[i].escape(match.Value));
                }
                return ("unknown", "value");
            }
        }

        private static string EscapeDoubleQuotes(string text)
        {
            var value = Regex.Replace(text, "^\"(.+)\"$", "$1", RegexOptions.Singleline);
            return EscapeEscapedText(value);
        }

        private static string EscapeSingleQuote(string text)
        {
            return Regex.Replace(text, "^'(.+)'$", "$1");
        }

        private static string EscapeEscapedText(string text)
        {
            return Regex.Replace(text, "(\\\\)(.)", "$2");
        }
    }
}
