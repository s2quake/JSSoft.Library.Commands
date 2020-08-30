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

namespace JSSoft.Library.Commands
{
    public class CommandContextTerminal : Terminal
    {
        private readonly CommandContextBase commandContext;
        private readonly ManualResetEvent resetEvent = new ManualResetEvent(false);
        private Exception exception;
        private string prompt;
        private string prefix;
        private string postfix;

        public CommandContextTerminal(CommandContextBase commandContext)
        {
            this.commandContext = commandContext;
            this.commandContext.Executing += CommandContext_Executing;
            this.commandContext.Executed += commandContext_Executed;
        }

        public new string Prompt
        {
            get => this.prompt ?? string.Empty;
            set
            {
                this.prompt = value;
                if (this.IsReading == true)
                {
                    this.SetPrompt(this.Prefix + this.Prompt + this.Postfix);
                }
            }
        }

        public string Prefix
        {
            get => this.prefix ?? string.Empty;
            set
            {
                this.prefix = value;
                if (this.IsReading == true)
                {
                    this.SetPrompt(this.Prefix + this.Prompt + this.Postfix);
                }
            }
        }

        public string Postfix
        {
            get => this.postfix ?? string.Empty;
            set
            {
                this.postfix = value;
                if (this.IsReading == true)
                {
                    this.SetPrompt(this.Prefix + this.Prompt + this.Postfix);
                }
            }
        }

        public new void Cancel()
        {
            this.IsCancellationRequested = true;
            base.Cancel();
        }

        public void Start()
        {
            string line;
            while ((line = this.ReadStringInternal(this.Prefix + this.Prompt + this.Postfix)) != null)
            {
                try
                {
                    this.exception = null;
                    this.resetEvent.Reset();
                    this.commandContext.Execute(this.commandContext.Name + " " + line);
                    this.resetEvent.WaitOne();
                    if (this.exception != null)
                        throw this.exception;
                }
                catch (TargetInvocationException e)
                {
                    this.WriteException(e.InnerException ?? e);
                }
                catch (AggregateException e)
                {
                    foreach (var item in e.InnerExceptions)
                    {
                        this.WriteException(item);
                    }
                }
                catch (Exception e)
                {
                    this.WriteException(e);
                }
                if (this.IsCancellationRequested == true)
                    break;
            }
        }

        public bool IsCancellationRequested { get; private set; }

        public bool DetailErrorMessage { get; set; }

        protected override string[] GetCompletion(string[] items, string find)
        {
            return this.commandContext.GetCompletionInternal(items, find);
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

        private void CommandContext_Executing(object sender, ExecuteEventArgs e)
        {
            if (e.Task is null)
            {
                this.resetEvent.Set();
            }
        }

        private void commandContext_Executed(object sender, ExecutedEventArgs e)
        {
            this.exception = e.Exception;
            this.resetEvent.Set();
        }
    }
}
