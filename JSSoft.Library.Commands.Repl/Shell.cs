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

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Text.RegularExpressions;

namespace JSSoft.Library.Commands.Repl
{
    [Export(typeof(IShell))]
    class Shell : CommandContextTerminal, IShell
    {
        private string currentDirectory = Directory.GetCurrentDirectory();

        [ImportingConstructor]
        public Shell(ShellCommandContext commandContext)
           : base(commandContext)
        {
            this.Prompt = $"{Directory.GetCurrentDirectory()}$ ";
        }

        public string CurrentDirectory
        {
            get => this.currentDirectory;
            set
            {
                this.currentDirectory = value ?? throw new ArgumentNullException(nameof(value));
                this.UpdatePrompt();
            }
        }

        protected override string FormatPrompt(string prompt)
        {
            var match = Regex.Match(prompt, "(.+)(\\$.+)");
            if (match.Success == true)
            {
                var path = match.Groups[1].Value;
                var post = TerminalStrings.Foreground(match.Groups[2].Value, TerminalColor.BrightGreen);
                var coloredSeparator = TerminalStrings.Foreground($"{Path.AltDirectorySeparatorChar}", TerminalColor.Red);
                var coloredPath = Regex.Replace(path, $"\\{Path.AltDirectorySeparatorChar}", coloredSeparator);
                return coloredPath + post;
            }
            return prompt;
        }

        private void UpdatePrompt()
        {
            if (Terminal.IsUnix == true)
                this.Prompt = $"{this.currentDirectory}$ ";
            else if (Terminal.IsWin32NT == true)
                this.Prompt = $"{this.currentDirectory}>";
            else
                this.Prompt = $"{this.currentDirectory}>";
        }
    }
}
