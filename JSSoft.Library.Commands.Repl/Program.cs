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
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JSSoft.Library.Commands.Repl
{
    class Program
    {
        private const string doubleQuotesPattern = "\"(?:(?<=\\\\)\"|[^\"])*\"";
        private const string singleQuotesPattern = "'[^']*'";
        private const string escapedSpacePattern = "'[^']*'";

        public static string[] EscapeString(string text)
        {
            var itemList = new List<string>();
            var sb = new StringBuilder();
            var isEscpae = false;
            var isSingle = false;
            var isDouble = false;

            foreach (var item in text)
            {
                if (item == '\\')
                {
                    if (isSingle == true)
                    {
                        sb.Append(item);
                    }
                    else if (isEscpae == false)
                    {
                        isEscpae = true;
                    }
                    else
                    {
                        sb.Append('\\');
                    }
                }
                else if (item == '\'')
                {
                    if (isEscpae == true)
                    {
                        sb.Append(item);
                        isEscpae = false;
                    }
                    else if (isSingle == false)
                    {
                        isSingle = true;
                    }
                    else
                    {
                        isSingle = false;
                    }
                }
                else if (item == '"')
                {
                    if (isSingle == true)
                    {
                        sb.Append(item);
                    }
                    else if (isEscpae == true)
                    {
                        sb.Append(item);
                        isEscpae = false;
                    }
                    else if (isDouble == true)
                    {
                        isDouble = false;
                    }
                    else
                    {
                        isDouble = true;
                    }
                }
                else if (item == ' ')
                {
                    if (isEscpae == true || isSingle == true || isDouble == true)
                    {
                        sb.Append(item);
                    }
                    else
                    {
                        InsertText(itemList, sb);
                    }
                }
                else
                {
                    if (isEscpae == true)
                    {
                        sb.Append(item);
                        isEscpae = false;
                    }
                    else if (isSingle == true)
                    {
                        sb.Append(item);
                    }
                    else if (isDouble == true)
                    {
                        sb.Append(item);
                    }
                    else
                    {
                        sb.Append(item);
                    }
                }
            }
            if (isEscpae == true || isSingle == true || isDouble == true)
            {
                throw new ArgumentException();
            }
            InsertText(itemList, sb);
            return itemList.ToArray();

            static void InsertText(List<string> itemList, StringBuilder sb)
            {
                var t = sb.ToString().Trim();
                if (t != string.Empty)
                    itemList.Add(t);
                sb.Clear();
            }
        }

        static async Task Main(string[] _)
        {
            var ss = EscapeString("a\\ b c d=\"f\"    h 'i \\' 'a '=\"werwer\"" + Environment.NewLine + "\\ sldkfjsldkfj\\ sdklfjsdlkfj\\ sdaf");
            for (var i = 0; i < _.Length; i++)
            {
                Console.WriteLine($"{i}: {_[i]}");
            }
            Console.WriteLine(Environment.CommandLine);
            var match = System.Text.RegularExpressions.Regex.Match(Environment.CommandLine, "\"(?:(?<=\\\\)\"|[^\"])+\"", System.Text.RegularExpressions.RegexOptions.Singleline);
            var items = CommandStringUtility.SplitAll(Environment.CommandLine);
            // return;
            var shell = Container.GetService<IShell>();
            Console.WriteLine();
            await shell.StartAsync();
            Console.WriteLine("\u001b0");
        }
    }
}
