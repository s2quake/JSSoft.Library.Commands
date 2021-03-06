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
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace JSSoft.Library.Commands
{
    public class TerminalTextWriter : TextWriter
    {
        private readonly Terminal terminal;
        private readonly Encoding encoding;

        public TerminalTextWriter(Terminal terminal, Encoding encoding)
        {
            this.terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
            this.encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        }

        public override Encoding Encoding => this.encoding;

        public override void Write(char value)
        {
            this.WriteToStream(value.ToString());
        }

        public override void Write(string value)
        {
            this.WriteToStream(value);
        }

        public override void WriteLine(string value)
        {
            this.WriteToStream(value + Environment.NewLine);
        }

        public override Task WriteAsync(char value)
        {
            return this.WriteToStreamAsync(value.ToString());
        }

        public override Task WriteAsync(string value)
        {
            return this.WriteToStreamAsync(value);
        }

        public override Task WriteLineAsync(string value)
        {
            return this.WriteToStreamAsync(value + Environment.NewLine);
        }

        public TerminalColor? Foreground { get; set; }

        public TerminalColor? Background { get; set; }

        private void WriteToStream(string text)
        {
            this.terminal.EnqueueString(TerminalStrings.FromColor(text, this.Foreground, this.Background));
        }

        private Task WriteToStreamAsync(string text)
        {
            return this.terminal.EnqueueStringAsync(TerminalStrings.FromColor(text, this.Foreground, this.Background));
        }
    }
}
