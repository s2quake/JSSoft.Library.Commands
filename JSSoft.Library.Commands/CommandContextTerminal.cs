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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Library.Commands
{
    public class CommandContextTerminal : Terminal
    {
        private readonly CommandContextBase commandContext;
        private CancellationTokenSource cancellation = new();
        private string prompt = string.Empty;

        public CommandContextTerminal(CommandContextBase commandContext)
        {
            this.commandContext = commandContext;
        }

        public new string Prompt
        {
            get => this.prompt;
            set
            {
                this.prompt = value ?? throw new ArgumentNullException(nameof(value));
                base.Prompt = value;
            }
        }

        public new void Cancel()
        {
            base.Cancel();
            this.cancellation.Cancel();
        }

        public async Task StartAsync()
        {
            var consoleOut = Console.Out;
            var consoleError = Console.Error;
            var consoleControlC = Console.TreatControlCAsInput;

            var commnadOut = new TerminalTextWriter(this, Console.OutputEncoding);
            var commnadError = new TerminalTextWriter(this, Console.OutputEncoding) { Foreground = TerminalColor.BrightRed };

            Console.SetOut(commnadOut);
            Console.SetError(commnadError);
            this.commandContext.Out = commnadOut;
            this.commandContext.Error = commnadError;

            while (this.cancellation.IsCancellationRequested == false)
            {
                if (this.ReadStringInternal(this.Prompt) is string command)
                {
                    await this.ExecuteCommandAsync(command);
                }
            }

            Console.SetOut(consoleOut);
            Console.SetError(consoleError);
        }

        public bool DetailErrorMessage { get; set; }

        protected override string[] GetCompletion(string[] items, string find)
        {
            return this.commandContext.GetCompletionInternal(items, find);
        }

        protected virtual bool OnPreviewExecute(string command)
        {
            return false;
        }

        protected virtual void OnExecuted(Exception e)
        {

        }

        private async Task ExecuteCommandAsync(string line)
        {
            var consoleControlC = Console.TreatControlCAsInput;
            var cancellation = new CancellationTokenSource();
            try
            {
                Console.TreatControlCAsInput = false;
                Console.CancelKeyPress += ConsoleCancelEventHandler;
                if (this.OnPreviewExecute(line) == true)
                    return;
                var task = this.commandContext.ExecuteAsync(this.commandContext.Name + " " + line, cancellation.Token);
                while (task.IsCompleted == false)
                {
                    this.Update();
                    await Task.Delay(1);
                }
                if (task.Exception != null)
                    throw task.Exception;
                this.OnExecuted(null);
            }
            catch (Exception e)
            {
                this.OnExecuteWithException(e);
            }
            finally
            {
                Console.TreatControlCAsInput = consoleControlC;
                Console.CancelKeyPress -= ConsoleCancelEventHandler;
                cancellation = null;
            }

            void ConsoleCancelEventHandler(object sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                cancellation.Cancel();
            }
        }

        private void OnExecuteWithException(Exception e)
        {
            if (e is TargetInvocationException e1)
            {
                this.OnExecuted(e1);
                this.WriteException(e1.InnerException ?? e1);
            }
            else if (e is AggregateException e2)
            {
                this.OnExecuted(e2);
                foreach (var item in e2.InnerExceptions)
                {
                    this.WriteException(item);
                }
            }
            else
            {
                this.OnExecuted(e);
                this.WriteException(e);
            }
        }

        private void WriteException(Exception e)
        {
            if (this.DetailErrorMessage == true)
            {
                this.commandContext.Error.WriteLine(e);
            }
            else
            {
                this.commandContext.Error.WriteLine(e.Message);
            }
        }
    }
}
