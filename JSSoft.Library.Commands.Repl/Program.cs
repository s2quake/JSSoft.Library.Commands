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
            // var ss = "ㅈㄷㄱ\n".Split(Environment.NewLine, StringSplitOptions.None);
            // Console.Write("\u001b[0;31;102msdalkfjaslkjlkjlkjlkjkjijoqwjflkadjkljoijwoij\u001b[0m\u001b[24;2f");
            // Console.Write("\x1b[J");
            // Console.Write("\x1b");
            // Console.Write("\x1b[H\x1b[J\x1b[1B\x1b[J\x1b[1B\x1b[J\x1b[1B\x1b[J\x1b[1B\x1b[J\x1b[1B\x1b[J\x1b[1B");
            // Console.Write("\x1b[H\x1b[2K");
            // Console.Write("ds.Repl/bin/Debug/netcoreapp3.1$ MacBook-Pro:JSSoft.LibraryMacBook-Pro:JSSoft.Library1MacBook-Pro:JSSoft.Library.Commands s2quake-mac$\n");
            // Console.Write("최지수 기아 타이거즈 ㄴ미아러ㅣ");
            // Console.Write("we");
            // // Console.WriteLine("\x1b[2T");
            // Console.WriteLine("\x1b[=7h");
            var shell = Container.GetService<IShell>();
            Console.WriteLine();
            await shell.StartAsync();
            Console.WriteLine("\u001b0");
        }
    }
}
