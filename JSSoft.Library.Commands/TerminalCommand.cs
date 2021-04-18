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

namespace JSSoft.Library.Commands
{
    struct TerminalCommand : ITerminalString
    {
        private const string prompt = "> ";
        private string text;
        private TerminalFormat formatter;
        private string formatText;

        public TerminalCommand(string text, TerminalFormat formatter)
        {
            this.text = text ?? throw new ArgumentNullException(nameof(text));
            this.formatter = formatter;
            this.formatText = formatter?.Invoke(text) ?? text;
        }

        public string GetText(bool isPassword)
        {
            if (isPassword == true)
                return string.Empty.PadRight(this.text.Length, Terminal.PasswordCharacter);
            return this.text;
        }

        public string Text => this.text;

        public string FormatText => this.formatText;

        public int Length => this.text.Length;

        public TerminalCommand Slice(int start, int length)
        {
            var item = this.text.Substring(start, length);
            var formatter = this.formatter;
            return new TerminalCommand(item, formatter);
        }

        public TerminalCommand Slice(int startIndex)
        {
            var item = this.text.Substring(startIndex);
            var formatter = this.formatter;
            return new TerminalCommand(item, formatter);
        }

        public TerminalCommand Insert(int startIndex, string value)
        {
            var item = this.text.Insert(startIndex, value);
            var formatter = this.formatter;
            return new TerminalCommand(item, formatter);
        }

        public TerminalCommand Remove(int startIndex, int count)
        {
            var item = this.text.Remove(startIndex, count);
            var formatter = this.formatter;
            return new TerminalCommand(item, formatter);
        }

        public TerminalPoint Next(TerminalPoint pt, int bufferWidth)
        {
            var text = this.text.Replace(Environment.NewLine, $"{Environment.NewLine}{prompt}");
            return Terminal.NextPosition(text, bufferWidth, pt);
        }

        public static implicit operator string(TerminalCommand s)
        {
            return s.text.Replace(Environment.NewLine, $"{Environment.NewLine}{prompt}");
        }

        public static TerminalCommand Empty { get; } = new TerminalCommand(string.Empty, null);

        #region ITerminalString

        string ITerminalString.Text => this.FormatText.Replace(Environment.NewLine, $"{Environment.NewLine}{prompt}");

        #endregion
    }
}
