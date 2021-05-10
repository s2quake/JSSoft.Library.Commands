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

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Text.RegularExpressions;

namespace JSSoft.Library.Commands.Repl
{
    [Export]
    class ShellTerminal : CommandContextTerminal
    {
        private static readonly string postfix = Terminal.IsWin32NT == true ? ">" : "$ ";
        private static readonly string postfixC = TerminalStrings.Foreground(postfix, TerminalColor.BrightGreen);
        private static readonly string separatorC = TerminalStrings.Foreground($"{Path.DirectorySeparatorChar}", TerminalColor.Red);
        private readonly IShell shell;

        [ImportingConstructor]
        public ShellTerminal(IShell shell, ShellCommandContext commandContext)
           : base(commandContext)
        {
            this.shell = shell;
            this.shell.DirectoryChanged += Shell_DirectoryChanged;
            this.UpdatePrompt();
        }

        protected override string FormatPrompt(string prompt)
        {
            if (prompt.EndsWith(postfix) == true)
            {
                var text = prompt.Substring(0, prompt.Length - postfix.Length);
                var textC = Regex.Replace(text, $"\\{Path.DirectorySeparatorChar}", separatorC);
                return textC + postfixC;
            }
            return prompt;
        }

        private void Shell_DirectoryChanged(object sender, EventArgs e)
        {
            this.UpdatePrompt();
        }

        private void UpdatePrompt()
        {
            this.Prompt = $"{this.shell.CurrentDirectory}{postfix}";
        }
    }
}
