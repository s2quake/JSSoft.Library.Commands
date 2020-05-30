//Released under the MIT License.
//
//Copyright (c) 2018 Ntreev Soft co., Ltd.
//
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
//documentation files (the "Software"), to deal in the Software without restriction, including without limitation the 
//rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit 
//persons to whom the Software is furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the 
//Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
//WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
//COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR 
//OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ntreev.Library.Commands
{
    public class CommandTextWriter : IndentedTextWriter
    {
        public CommandTextWriter()
            : this(new StringWriter(), Terminal.IsOutputRedirected == true ? int.MaxValue : Console.BufferWidth)
        {

        }
        public CommandTextWriter(TextWriter writer)
            : this(writer, Terminal.IsOutputRedirected == true ? int.MaxValue : Console.BufferWidth)
        {

        }

        public CommandTextWriter(TextWriter writer, int width)
            : base(writer)
        {
            this.Width = width;
        }

        public override string ToString() => this.InnerWriter.ToString();

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
    }
}