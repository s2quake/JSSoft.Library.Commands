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
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace JSSoft.Library.Commands.Repl
{
    class Program
    {
        static async Task Main(string[] _)
        {
            // Console.WriteLine("\x1b[0;31mHello");
            // Console.WriteLine("\x1b[0m\x1b[1;31mWorld");
            // Console.WriteLine("\x1b[0m\x1b[2;31mHello");
            // Console.WriteLine("\x1b[0m\x1b[3;31mWorld");
            // Console.WriteLine("\x1b[0m\x1b[4;31mHello");
            // Console.WriteLine("\x1b[0m\x1b[5;31mWorld");
            // Console.WriteLine("\x1b[0m\x1b[6;31mWorld");
            // Console.WriteLine("\x1b[0m\x1b[7;31mWorld");
            // Console.WriteLine("\x1b[0m\x1b[8;31mWorld");
            // Console.WriteLine("\x1b[0m\x1b[9;31mWorld");
            // Console.Write("최지수\x08");
            // string source = "\u001b[2JThis text \x1b[3;20Hcontains several ANSI escapes\x1b[1;2;30;43m to format the text\x1b[K";
            // string result = System.Text.RegularExpressions.Regex.Replace(source, @"\e\[(\d+;)*(\d+)?[ABCDHJKfmsu]", string.Empty);
            // var matches = System.Text.RegularExpressions.Regex.Matches(source, @"\e\[(\d+;)*(\d+)?[ABCDHJKfmsu]");
            var shell = Container.GetService<IShell>();
            Console.WriteLine();
            await shell.StartAsync();
            Console.WriteLine("\u001b0");
        }
    }
}
