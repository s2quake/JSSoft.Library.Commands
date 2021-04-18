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
    struct TerminalPrompt : ITerminalString
    {
        private string text;
        private TerminalFormat formatter;

        public TerminalPrompt(string text, TerminalFormat formatter)
        {
            this.text = text ?? throw new ArgumentNullException(nameof(text));
            this.formatter = formatter;
        }

        public string Text => this.text;

        public string Format
        {
            get
            {
                if (this.formatter != null)
                    return this.formatter(this.text);
                return this.text;
            }
        }

        public int Length => this.text.Length;

        public string Slice(int start, int length)
        {
            return this.Text.Substring(start, length);
        }

        public string Slice(int startIndex)
        {
            return this.Text.Substring(startIndex);
        }

        public TerminalPrompt Insert(int startIndex, string value)
        {
            var item = this.text.Insert(startIndex, value);
            var formatter = this.formatter;
            return new TerminalPrompt(item, formatter);
        }

        public TerminalPrompt Remove(int startIndex, int count)
        {
            var item = this.text.Remove(startIndex, count);
            var formatter = this.formatter;
            return new TerminalPrompt(item, formatter);
        }

        public static implicit operator string(TerminalPrompt s)
        {
            return s.Text;
        }

        // public static implicit operator TerminalPrompt((string text, string format) v)
        // {
        //     return new TerminalPrompt(v.text, v.format);
        // }

        // public static TerminalPrompt operator +(TerminalPrompt v1, TerminalPrompt v2)
        // {
        //     return new TerminalPrompt(v1.Text + v2.Text, v1.Format + v2.Format);
        // }

        public static TerminalPrompt Empty { get; } = new TerminalPrompt(string.Empty, null);

        #region ITerminalString

        string ITerminalString.Text => this.Format;

        #endregion
    }
}
