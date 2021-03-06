﻿// Released under the MIT License.
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
using System.Linq;
using System.Text.RegularExpressions;

namespace JSSoft.Library.Commands
{
    public static class CommandSettings
    {
        public const string OptionPattern = "[a-zA-Z][-_a-zA-Z0-9]+";
        public const string ShortOptionPattern = "[a-zA-Z][a-zA-Z0-9]*";
        private static string delimiter = "--";
        private static string shortDelimiter = "-";
        private static char itemSeparator = ';';
        private static Func<string, string> nameGenerator;

        static CommandSettings()
        {
            IsConsoleMode = true;
        }

        public static string Delimiter
        {
            get => delimiter;
            set
            {
                if (value.Any(item => char.IsPunctuation(item)) == false)
                    throw new Exception(Resources.Exception_DelimiterMustBePunctuation);
                delimiter = value;
            }
        }

        public static string ShortDelimiter
        {
            get => shortDelimiter;
            set
            {
                if (value.Any(item => char.IsPunctuation(item)) == false)
                    throw new Exception(Resources.Exception_DelimiterMustBePunctuation);
                shortDelimiter = value;
            }
        }

        public static char ItemSperator
        {
            get => itemSeparator;
            set
            {
                if (char.IsPunctuation(value) == false)
                    throw new Exception(Resources.Exception_DelimiterMustBePunctuation);
                itemSeparator = value;
            }
        }

        public static Func<string, string> NameGenerator
        {
            get => nameGenerator ?? ToSpinalCase;
            set => nameGenerator = value;
        }

        public static bool IsConsoleMode { get; set; }

        private static string ToSpinalCase(string text)
        {
            ValidateIdentifier(text);
            return Regex.Replace(text, @"([a-z])([A-Z])", "$1-$2").ToLower();
        }

        internal static void ValidateIdentifier(string name)
        {
            if (Regex.IsMatch(name, $"^{OptionPattern}") == false)
                throw new ArgumentException(string.Format(Resources.Exception_InvalidValue_Format, name));
        }

        internal static bool VerifyName(string argument)
        {
            return Regex.IsMatch(argument, $"{CommandSettings.Delimiter}\\S+|{CommandSettings.ShortDelimiter}\\S+");
        }
    }
}
