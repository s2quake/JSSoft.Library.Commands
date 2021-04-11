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
    struct TerminalString
    {
        private string text;
        private string format;
        private bool isPassword;

        public TerminalString(string text)
        {
            this.text = text ?? throw new ArgumentNullException(nameof(text));
            this.format = text;
            this.isPassword = false;
        }

        public TerminalString(string text, Func<string, string> formatter)
        {
            if (formatter is null)
                throw new ArgumentNullException(nameof(formatter));
            this.text = text ?? throw new ArgumentNullException(nameof(text));
            this.format = formatter(text);
            this.isPassword = false;
        }

        public TerminalString(string text, string format)
        {
            this.text = text ?? throw new ArgumentNullException(nameof(text));
            this.format = format ?? throw new ArgumentNullException(nameof(format));
            this.isPassword = false;
        }

        public string OriginText => this.text;

        public string Text
        {
            get
            {
                if (this.isPassword == true)
                    return string.Empty.PadRight(this.text.Length, Terminal.PasswordCharacter);
                return this.text;
            }
        }

        public string Format
        {
            get
            {
                if (this.isPassword == true)
                    return string.Empty.PadRight(this.text.Length, Terminal.PasswordCharacter);
                if (this.format == string.Empty)
                    return this.text;
                return this.format;
            }
        }

        public bool IsPassword
        {
            get => this.isPassword;
            set => this.isPassword = value;
        }

        public int Length => this.Text.Length;

        public string Slice(int start, int length)
        {
            return this.Text.Substring(start, length);
        }

        public string Insert(int startIndex, string value)
        {
            return this.text.Insert(startIndex, value);
        }

        public TerminalString Insert(int startIndex, string value, Func<string, string> formatter)
        {
            var item = this.text.Insert(startIndex, value);
            return new TerminalString(item, formatter(item)) { isPassword = this.isPassword };
        }

        public string Remove(int startIndex, int count)
        {
            return this.text.Remove(startIndex, count);
        }

        public TerminalString Remove(int startIndex, int count, Func<string, string> formatter)
        {
            var item = this.text.Remove(startIndex, count);
            return new TerminalString(item, formatter(item)) { isPassword = this.isPassword };
        }

        public static implicit operator string(TerminalString s)
        {
            return s.Text;
        }

        public static implicit operator TerminalString((string text, string format) v)
        {
            return new TerminalString(v.text, v.format);
        }

        public static TerminalString operator +(TerminalString v1, TerminalString v2)
        {
            return new TerminalString(v1.Text + v2.Text, v1.Format + v2.Format);
        }

        public static TerminalString Empty { get; } = new TerminalString(string.Empty);
    }
}
