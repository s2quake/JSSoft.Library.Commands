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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Library.Commands
{
    public class CommandContextTerminal : Terminal
    {
        private readonly CommandContextBase commandContext;
        // private string prompt1;
        // private string prefix;
        // private string postfix;

        public CommandContextTerminal(CommandContextBase commandContext)
        {
            this.commandContext = commandContext;
        }

        // public new string Prompt
        // {
        //     get => this.prompt1 ?? string.Empty;
        //     set
        //     {
        //         this.prompt1 = value;
        //         base.Prompt = this.Prefix + this.Prompt + this.Postfix;
        //     }
        // }

        // public string Prefix
        // {
        //     get => this.prefix ?? string.Empty;
        //     set
        //     {
        //         this.prefix = value;
        //         base.Prompt = this.Prefix + this.Prompt + this.Postfix;
        //     }
        // }

        // public string Postfix
        // {
        //     get => this.postfix ?? string.Empty;
        //     set
        //     {
        //         this.postfix = value;
        //         base.Prompt = this.Prefix + this.Prompt + this.Postfix;
        //     }
        // }

        public new void Cancel()
        {
            this.IsCancellationRequested = true;
            base.Cancel();
        }

        public void Start()
        {
            string line;
            Console.Clear();
            while ((line = this.ReadStringInternal(this.Prompt)) != null)
            {
                try
                {
                    if (this.OnPreviewExecute(line) == true)
                        continue;
                    this.commandContext.Execute(this.commandContext.Name + " " + line);
                    this.OnExecuted(null);
                }
                catch (TargetInvocationException e)
                {
                    this.OnExecuted(e);
                    this.WriteException(e.InnerException ?? e);
                }
                catch (AggregateException e)
                {
                    this.OnExecuted(e);
                    foreach (var item in e.InnerExceptions)
                    {
                        this.WriteException(item);
                    }
                }
                catch (Exception e)
                {
                    this.OnExecuted(e);
                    this.WriteException(e);
                }
                if (this.IsCancellationRequested == true)
                    break;
            }
        }

        public async Task StartAsync()
        {
            string line;
            CancellationTokenSource cancellation = null;

            var writer = Console.Out;
            var errorWriter = Console.Error;
            var treatControlCAsInput = Console.TreatControlCAsInput;

            this.commandContext.Out = Console.Out;
            this.commandContext.Error = Console.Error;
            Console.SetOut(new TerminalTextWriter(Console.Out, this, Console.OutputEncoding));
            Console.SetError(new TerminalTextWriter(Console.Error, this, Console.OutputEncoding));
            Console.TreatControlCAsInput = true;

            while ((line = this.ReadStringInternal(this.Prompt)) != null)
            {
                var oldTreatControlCAsInput = Console.TreatControlCAsInput;
                try
                {
                    Console.TreatControlCAsInput = false;
                    cancellation = new CancellationTokenSource();
                    Console.CancelKeyPress += ConsoleCancelEventHandler;
                    if (this.OnPreviewExecute(line) == true)
                        continue;
                    var task = this.commandContext.ExecuteAsync(this.commandContext.Name + " " + line, cancellation.Token);
                    while (task.IsCompleted == false)
                    {
                        await Task.Delay(1);
                        this.Sync();
                    }
                    if (task.Exception != null)
                        throw task.Exception;
                    this.OnExecuted(null);
                }
                catch (TargetInvocationException e)
                {
                    this.OnExecuted(e);
                    this.WriteException(e.InnerException ?? e);
                }
                catch (AggregateException e)
                {
                    this.OnExecuted(e);
                    foreach (var item in e.InnerExceptions)
                    {
                        this.WriteException(item);
                    }
                }
                catch (Exception e)
                {
                    this.OnExecuted(e);
                    this.WriteException(e);
                }
                finally
                {
                    Console.TreatControlCAsInput = oldTreatControlCAsInput;
                    Console.CancelKeyPress -= ConsoleCancelEventHandler;
                    cancellation = null;
                }
                if (this.IsCancellationRequested == true)
                    break;
            }

            Console.TreatControlCAsInput = treatControlCAsInput;
            Console.SetOut(writer);
            Console.SetError(errorWriter);
            void ConsoleCancelEventHandler(object sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                cancellation.Cancel();
            }
        }

        public bool IsCancellationRequested { get; private set; }

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
