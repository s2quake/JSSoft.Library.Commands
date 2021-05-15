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
using System.Text;

namespace JSSoft.Library.Commands
{
    struct TerminalCommand : ITerminalString
    {
        private const string multilinePrompt = "> ";

        private readonly TerminalFormat formatter;

        public TerminalCommand(string text, TerminalFormat formatter)
        {
            this.Text = text ?? throw new ArgumentNullException(nameof(text));
            this.formatter = formatter;
            this.FormattedText = formatter?.Invoke(text) ?? text;
        }

        public override string ToString()
        {
            return this.Text;
        }

        public TerminalCommand Slice(int start, int length)
        {
            var item = this.Text.Substring(start, length);
            var formatter = this.formatter;
            return new TerminalCommand(item, formatter);
        }

        public TerminalCommand Slice(int startIndex)
        {
            var item = this.Text.Substring(startIndex);
            var formatter = this.formatter;
            return new TerminalCommand(item, formatter);
        }

        public TerminalCommand Insert(int startIndex, string value)
        {
            var item = this.Text.Insert(startIndex, value);
            var formatter = this.formatter;
            return new TerminalCommand(item, formatter);
        }

        public TerminalCommand Remove(int startIndex, int count)
        {
            var item = this.Text.Remove(startIndex, count);
            var formatter = this.formatter;
            return new TerminalCommand(item, formatter);
        }

        public TerminalPoint Next(TerminalPoint pt, int bufferWidth)
        {
            var text = this.Text.Replace(Environment.NewLine, $"{Environment.NewLine}{multilinePrompt}");
            return Terminal.NextPosition(text, bufferWidth, pt);
        }

        public string Text { get; }

        public string FormattedText { get; private set; }

        public int Length => this.Text.Length;

        public static implicit operator string(TerminalCommand s)
        {
            return s.Text.Replace(Environment.NewLine, $"{Environment.NewLine}{multilinePrompt}");
        }

        public static TerminalCommand Empty { get; } = new TerminalCommand(string.Empty, null);

        #region ITerminalString

        string ITerminalString.Text => this.FormattedText.Replace(Environment.NewLine, $"{Environment.NewLine}{multilinePrompt}");

        #endregion
    }
}
