﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ntreev.Library.Commands
{
    public class CommandTextWriter : IndentedTextWriter
    {
        private readonly int width;

        public CommandTextWriter(TextWriter writer)
            : this(writer, Console.BufferWidth)
        {

        }

        public CommandTextWriter(TextWriter writer, int width)
            : base(writer)
        {
            this.width = width;
        }

        public void WriteMultiline(string s)
        {
            foreach (var item in s.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
            {
                if (item == string.Empty)
                    this.WriteLine();
                else if (Console.IsOutputRedirected == true)
                    this.WriteLine(item);
                else
                    this.WriteMultilineCore(item);
            }
        }

        private void WriteMultilineCore(string s)
        {
            var indent = this.Indent;
            var emptyCount = this.TabString.Length * this.Indent;
            var width = Console.WindowWidth - emptyCount;

            var i = emptyCount;
            this.Indent = 0;
            
            foreach (var item in s)
            {
                if (Console.CursorLeft == 0)
                {
                    this.Write(string.Empty.PadRight(emptyCount));
                    if (item == ' ')
                        continue;
                }
                if (Console.CursorLeft == emptyCount && item == ' ')
                {
                    
                }
                //else
                {
                    var y = Console.CursorTop;
                    this.Write(item);
                    if (Console.CursorTop != y && item != ' ')
                    {
                        if (Console.CursorLeft != 0)
                        {
                            Console.CursorLeft = 0;
                            this.Write(string.Empty.PadRight(emptyCount));
                            this.Write(item);
                        }
                        else
                        {

                        }
                    }
                }
                

            }
            this.WriteLine();
            this.Indent = indent;
        }

        public string TabString
        {
            get { return IndentedTextWriter.DefaultTabString; }
        }

        public int Width
        {
            get
            {
                return this.width;
            }
        }
    }
}