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
using System.Text;

namespace JSSoft.Library.Commands.Parse
{
    class Program
    {
        static void Main(string[] args)
        {
            var settings = new Settings();
            var parser = new CommandLineParser(settings);
            try
            {
                var sb = new StringBuilder();
                parser.Parse(args);
                sb.AppendLine($"{nameof(settings.Path)}: {settings.Path}");
                sb.AppendLine($"{nameof(settings.ServiceName)}: {settings.ServiceName}");
                sb.AppendLine($"{nameof(settings.WorkingPath)}: {settings.WorkingPath}");
                sb.AppendLine($"{nameof(settings.Port)}: {settings.Port}");
                sb.AppendLine($"{nameof(settings.UseCache)}: {settings.UseCache}");
                sb.AppendLine($"{nameof(settings.CacheSize)}: {settings.CacheSize}");
                sb.AppendLine($"{nameof(GlobalSettings.ID)}: {GlobalSettings.ID}");
                sb.AppendLine($"{nameof(GlobalSettings.Password)}: {GlobalSettings.Password}");
                sb.AppendLine($"{nameof(settings.Libraries)}:");
                foreach (var item in settings.Libraries)
                {
                    sb.AppendLine($"    {item}");
                }
                Console.WriteLine(sb.ToString());
            }
            catch (Exception e)
            {
                parser.PrintException(e);
                Environment.Exit(1);
            }
        }
    }
}
