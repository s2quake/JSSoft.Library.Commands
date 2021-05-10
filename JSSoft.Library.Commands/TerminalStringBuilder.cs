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

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JSSoft.Library.Commands
{
    public class TerminalStringBuilder
    {
        private readonly StringBuilder sb;
        private TerminalColor? foreground;
        private TerminalColor? background;
        private TerminalGraphic? graphic;
        private string p1 = string.Empty;
        private string p2 = string.Empty;

        public TerminalStringBuilder()
        {
            this.sb = new();
        }

        public TerminalStringBuilder(int capacity)
        {
            this.sb = new(capacity);
        }

        public override string ToString()
        {
            return this.sb.ToString();
        }

        public void Clear()
        {
            this.sb.Clear();
            this.p1 = string.Empty;
            this.p2 = string.Empty;
            this.foreground = null;
            this.background = null;
            this.graphic = null;
        }

        public void Append(string text)
        {
            if (this.p1 != this.p2)
            {
                this.sb.Append($"{this.p1}{text}");
                this.p2 = this.p1;
            }
            else
            {
                this.sb.Append(text);
            }
        }

        public void AppendLine()
        {
            this.AppendLine(string.Empty);
        }

        public void AppendLine(string text)
        {
            if (this.p1 != this.p2)
            {
                this.sb.AppendLine($"{this.p1}{text}");
                this.p2 = this.p1;
            }
            else
            {
                this.sb.AppendLine(text);
            }
        }

        public void AppendEnd()
        {
            this.sb.Append("\x1b[0m");
            this.p2 = string.Empty;
        }

        public TerminalColor? Foreground
        {
            get => this.foreground;
            set
            {
                this.foreground = value;
                this.UpdateEscapeCode();
            }
        }

        public TerminalColor? Background
        {
            get => this.background;
            set
            {
                this.background = value;
                this.UpdateEscapeCode();
            }
        }

        public TerminalGraphic? Graphic
        {
            get => this.graphic;
            set
            {
                this.graphic = value;
                this.UpdateEscapeCode();
            }
        }

        private void UpdateEscapeCode()
        {
            var itemList = new List<string>(3);
            if (this.graphic != null)
                itemList.Add($"{(int)this.graphic}");
            if (this.foreground != null)
                itemList.Add($"{TerminalStrings.GetForegroundValue(this.foreground.Value)}");
            if (this.background != null)
                itemList.Add($"{TerminalStrings.GetBackgroundValue(this.background.Value)}");
            if (itemList.Any() == true)
                this.p1 = $"\x1b[{string.Join(";", itemList)}m";
            else if (this.p2 != string.Empty)
                this.p1 = $"\x1b[0m";
            else
                this.p1 = string.Empty;
        }
    }
}
