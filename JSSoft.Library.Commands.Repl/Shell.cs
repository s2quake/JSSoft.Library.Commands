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
using System.ComponentModel.Composition;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Library.Commands.Repl
{
    [Export(typeof(IShell))]
    class Shell : IShell
    {
        private readonly Lazy<ShellTerminal> terminal;
        private string currentDirectory = Directory.GetCurrentDirectory();
        private CancellationTokenSource cancellation;

        [ImportingConstructor]
        public Shell(Lazy<ShellTerminal> terminal)
        {
            this.terminal = terminal;
        }

        public void Cancel()
        {
            this.cancellation.Cancel();
        }

        public string ReadString(string prompt, string command, bool isHidden)
        {
            return this.Terminal.ReadString(prompt, command, isHidden);
        }

        public Task StartAsync()
        {
            this.cancellation = new ();
            return this.Terminal.StartAsync(this.cancellation.Token);
        }

        public string CurrentDirectory
        {
            get => this.currentDirectory;
            set
            {
                this.currentDirectory = value ?? throw new ArgumentNullException(nameof(value));
                this.OnDirectoryChanged(EventArgs.Empty);
            }
        }

        public event EventHandler DirectoryChanged;

        protected virtual void OnDirectoryChanged(EventArgs e)
        {
            this.DirectoryChanged?.Invoke(this, e);
        }

        private ShellTerminal Terminal => this.terminal.Value;
    }
}
