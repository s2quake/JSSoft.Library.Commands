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
using System.CodeDom.Compiler;
using System.IO;

namespace JSSoft.Library.Commands
{
    public class CommandTextWriter : IndentedTextWriter
    {
        public CommandTextWriter()
            : this(new StringWriter(), Console.IsOutputRedirected == true ? int.MaxValue : Console.BufferWidth)
        {
        }

        public CommandTextWriter(TextWriter writer)
            : this(writer, Console.IsOutputRedirected == true ? int.MaxValue : Console.BufferWidth)
        {
        }

        public CommandTextWriter(TextWriter writer, int width)
            : base(writer)
        {
            this.Width = width;
        }

        public override string ToString()
        {
            return this.InnerWriter.ToString();
        }

        public void BeginGroup(string text)
        {
            if (this.IsAnsiSupported == true)
            {
                var tb = new TerminalStringBuilder() { Graphic = TerminalGraphic.Bold };
                tb.Append(text);
                tb.AppendEnd();
                this.WriteLine(tb.ToString());
            }
            else
            {
                this.WriteLine(text);
            }
            this.Indent++;
        }

        public void EndGroup()
        {
            this.Indent--;
            this.WriteLine();
        }

        public void WriteMultiline(string s)
        {
            foreach (var item in s.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
            {
                if (item == string.Empty)
                    this.WriteLine();
                else
                    this.WriteLine(item);
            }
        }

        public string TabString => IndentedTextWriter.DefaultTabString;

        public int Width { get; private set; }

        public bool IsAnsiSupported { get; set; }
    }
}
