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
    public static class CommandStringUtility
    {
        private const string doubleQuotesPattern = "(?<![\\\\])[\"](?:.(?!(?<![\\\\])(?:(?<![\\\\])[\"])))*.?(?<![\\\\])[\"]";
        private const string singleQuotePattern = "(?<![\\\\])['](?:.(?!(?<![\\\\])(?:(?<![\\\\])['])))*.?(?<![\\\\])[']";
        private const string textPattern = "\\S+";

        private readonly static string fullPattern = "\"(?:(?<=\\\\)\"|[^\"])*\"";
        private readonly static string completionPattern;

        static CommandStringUtility()
        {
            // fullPattern = string.Format("({0}|{1}|{2}={0}|{2}={1}|{2}={2}|{2})", doubleQuotesPattern, singleQuotePattern, textPattern);
            completionPattern = string.Format("({0}|{1}|{2}|\\s+$)", doubleQuotesPattern, singleQuotePattern, textPattern);
        }

        public static bool VerifyEscapeString(string text)
        {
            return GetEscapeString(text) != null;
        }

        public static string[] EscapeString(string text)
        {
            if (GetEscapeString(text) is string[] items)
                return items;
            throw new ArgumentException();
        }

        public static string AggregateString(string[] items)
        {
            return AggregateString(items);
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

        public static string[] SplitAll(string text)
        {
            return EscapeString(text);
            //     return SplitAll(text, false);
            // }

            // public static string[] SplitAll(string text, bool removeQuote)
            // {
            //     var matches = Regex.Matches(text, fullPattern);
            //     var argList = new List<string>();
            //     foreach (Match item in matches)
            //     {
            //         if (removeQuote == true)
            //         {
            //             argList.Add(TrimQuot(item.Value));
            //         }
            //         else
            //         {
            //             argList.Add(item.Value);
            //         }
            //     }
            //     return argList.ToArray();
        }

        /// <summary>
        /// a=1, a="123", a='123' 과 같은 문자열을 키와 값으로 분리하는 메소드
        /// </summary>
        public static bool TryGetKeyValue(string text, out string key, out string value)
        {
            var capturePattern = string.Format("((?<key>{2})=(?<value>{0})|(?<key>{2})=(?<value>{1})|(?<key>{2})=(?<value>.+))", doubleQuotesPattern, singleQuotePattern, textPattern);
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
            var matches = Regex.Matches(text, completionPattern);
            var argList = new List<Match>();
            foreach (Match item in matches)
            {
                argList.Add(item);
            }
            return argList.ToArray();
        }

        [Obsolete]
        public static string WrapSingleQuot(string text)
        {
            if (Regex.IsMatch(text, "^" + singleQuotePattern) == true || Regex.IsMatch(text, "^" + doubleQuotesPattern) == true)
                throw new ArgumentException(nameof(text));
            return string.Format("'{0}'", text);
        }

        [Obsolete]
        public static string WrapDoubleQuote(string text)
        {
            if (Regex.IsMatch(text, "^" + singleQuotePattern) == true || Regex.IsMatch(text, "^" + doubleQuotesPattern) == true)
                throw new ArgumentException(nameof(text));
            return string.Format("\"{0}\"", text);
        }

        [Obsolete]
        public static bool IsWrappedOfSingleQuot(string text)
        {
            return Regex.IsMatch(text, "^" + singleQuotePattern);
        }

        [Obsolete]
        public static bool IsWrappedOfDoubleQuote(string text)
        {
            return Regex.IsMatch(text, "^" + doubleQuotesPattern);
        }

        [Obsolete]
        public static bool IsWrappedOfQuote(string text)
        {
            return IsWrappedOfSingleQuot(text) || IsWrappedOfDoubleQuote(text);
        }

        [Obsolete]
        public static string TrimQuot(string text)
        {
            if (IsWrappedOfSingleQuot(text) == true)
            {
                text = text.Substring(1);
                text = text.Remove(text.Length - 1);
                text = text.Replace("\\'", "'");
            }
            else if (IsWrappedOfDoubleQuote(text) == true)
            {
                text = text.Substring(1);
                text = text.Remove(text.Length - 1);
                text = text.Replace("\\\"", "\"");
            }
            return text;
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
                var text = IsWrappedOfQuote(item) ? TrimQuot(item) : item;
                text = Regex.Unescape(text);

                if (CommandStringUtility.TryGetKeyValue(text, out var key, out var value) == true)
                {
                    if (CommandStringUtility.IsWrappedOfQuote(value))
                    {
                        properties.Add(key, CommandStringUtility.TrimQuot(value));
                    }
                    else if (decimal.TryParse(value, out decimal l) == true)
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

        private static string[] GetEscapeString(string text)
        {
            var itemList = new List<string>();
            var sb = new StringBuilder();
            var isEscpae = false;
            var isSingle = false;
            var isDouble = false;

            foreach (var item in text)
            {
                if (item == '\\')
                {
                    if (isSingle == true)
                    {
                        sb.Append(item);
                    }
                    else if (isEscpae == false)
                    {
                        isEscpae = true;
                    }
                    else
                    {
                        sb.Append('\\');
                    }
                }
                else if (item == '\'')
                {
                    if (isEscpae == true)
                    {
                        sb.Append(item);
                        isEscpae = false;
                    }
                    else if (isDouble == true)
                    {
                        sb.Append(item);
                    }
                    else if (isSingle == false)
                    {
                        isSingle = true;
                    }
                    else
                    {
                        isSingle = false;
                    }
                }
                else if (item == '"')
                {
                    if (isSingle == true)
                    {
                        sb.Append(item);
                    }
                    else if (isEscpae == true)
                    {
                        sb.Append(item);
                        isEscpae = false;
                    }
                    else if (isDouble == true)
                    {
                        isDouble = false;
                    }
                    else
                    {
                        isDouble = true;
                    }
                }
                else if (item == ' ')
                {
                    if (isEscpae == true || isSingle == true || isDouble == true)
                    {
                        sb.Append(item);
                    }
                    else
                    {
                        InsertText(itemList, sb);
                    }
                }
                else
                {
                    if (isEscpae == true)
                    {
                        sb.Append(item);
                        isEscpae = false;
                    }
                    else if (isSingle == true)
                    {
                        sb.Append(item);
                    }
                    else if (isDouble == true)
                    {
                        sb.Append(item);
                    }
                    else
                    {
                        sb.Append(item);
                    }
                }
            }
            if (isEscpae == true || isSingle == true || isDouble == true)
                return null;
            InsertText(itemList, sb);
            return itemList.ToArray();

            static void InsertText(List<string> itemList, StringBuilder sb)
            {
                var t = sb.ToString().Trim();
                if (t != string.Empty)
                    itemList.Add(t);
                sb.Clear();
            }
        }
    }
}
