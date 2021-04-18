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
        private string text;
        private TerminalFormat formatter;

        public TerminalCommand(string text, TerminalFormat formatter)
        {
            this.text = text ?? throw new ArgumentNullException(nameof(text));
            this.formatter = formatter;
        }

        public string Text => this.text;

        public string Password => string.Empty.PadRight(this.text.Length, Terminal.PasswordCharacter);

        public string Format
        {
            get
            {
                if (this.formatter != null)
                    return this.formatter(this.text);
                return this.text;
            }
        }

        public string RenderText
        {
            get
            {
                return this.Format.Replace(Environment.NewLine, $"{Environment.NewLine}> ");
            }
        }

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

        public static implicit operator string(TerminalCommand s)
        {
            return s.Text;
        }

        // public static implicit operator TerminalCommand((string text, string format) v)
        // {
        //     return new TerminalCommand(v.text, v.format);
        // }

        // public static TerminalCommand operator +(TerminalCommand v1, TerminalCommand v2)
        // {
        //     return new TerminalCommand(v1.Text + v2.Text, v1.Format + v2.Format);
        // }

        public static TerminalCommand Empty { get; } = new TerminalCommand(string.Empty, null);

        #region ITerminalString

        string ITerminalString.Text => this.RenderText;

        #endregion
    }
}
