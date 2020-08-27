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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;

namespace JSSoft.Library.Commands.Repl.Commands
{
    [Export(typeof(ICommand))]
    [UsageDescriptionProvider(typeof(ResourceUsageDescriptionProvider))]
    class ChangeDirectoryCommand : CommandBase
    {
        private readonly Lazy<IShell> shell;

        [ImportingConstructor]
        public ChangeDirectoryCommand(Lazy<IShell> shell)
            : base("cd")
        {
            this.shell = shell;
            this.DirectoryName = string.Empty;
        }

        [CommandPropertyRequired("dir")]
        [DefaultValue("")]
        public string DirectoryName
        {
            get; set;
        }

        protected override void OnExecute()
        {
            var shell = this.shell.Value;
            if (this.DirectoryName == string.Empty)
            {
                this.Out.WriteLine(shell.Prompt);
            }
            else if (this.DirectoryName == "..")
            {
                var dir = Path.GetDirectoryName(Directory.GetCurrentDirectory());
                Directory.SetCurrentDirectory(dir);
                shell.Prompt = dir;
            }
            else if (Directory.Exists(this.DirectoryName) == true)
            {
                var dir = new DirectoryInfo(this.DirectoryName).FullName;
                Directory.SetCurrentDirectory(dir);
                shell.Prompt = dir;
            }
            else
            {
                throw new DirectoryNotFoundException(string.Format("'{0}'은(는) 존재하지 않는 경로입니다.", this.DirectoryName));
            }
        }
    }
}
