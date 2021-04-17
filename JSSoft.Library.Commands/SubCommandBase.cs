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

using System.Collections.Generic;
using System.Linq;

namespace JSSoft.Library.Commands
{
    class SubCommandBase : ICommand, ICommandCompletor, IExecutable, ICommandDescriptor, IMemberDescriptorProvider, ICommandUsage
    {
        private readonly CommandMethodBase command;
        private readonly CommandMethodDescriptor descriptor;

        public SubCommandBase(CommandMethodBase command, CommandMethodDescriptor descriptor)
        {
            this.command = command;
            this.descriptor = descriptor;
            this.Members = descriptor.Members.Select(item => new SubCommandPropertyDescriptor(command, item)).ToArray();
        }

        public string Name => this.descriptor.Name;

        public string[] Aliases => this.descriptor.Aliases;

        public string ExecutionName
        {
            get
            {
                if (this.CommandContext.IsNameVisible == true)
                    return $"{this.CommandContext.ExecutionName} {this.UsageName}";
                return this.UsageName;
            }
        }

        public string Summary => this.descriptor.Summary;

        public string Description => this.descriptor.Description;

        public string Example => this.descriptor.Example;

        public virtual bool IsEnabled => this.descriptor.CanExecute(this.command);

        public CommandContextBase CommandContext => this.command.CommandContext;

        public IEnumerable<CommandMemberDescriptor> Members { get; }

        public void Execute()
        {
            this.descriptor.Invoke(this.command, this.descriptor.Members);
        }

        public string[] GetCompletions(CommandCompletionContext completionContext)
        {
            return this.command.GetCompletions(this.descriptor, completionContext.MemberDescriptor, completionContext.Find);
        }

        public bool IsAnsiSupported => this.CommandContext.IsAnsiSupported;

        protected virtual void PrintUsage(CommandUsage usage)
        {
            var descriptors = this.Members.ToArray();
            var printer = new CommandMethodUsagePrinter(this.ExecutionName, this.command, this.Aliases)
            {
                Usage = usage,
                IsAnsiSupported = this.IsAnsiSupported
            };
            printer.Print(this.command.Out, this.descriptor, descriptors);
        }

        private string UsageName
        {
            get
            {
                var name = this.command.Name;
                var aliases = this.command.Aliases;
                if (aliases.Any())
                    name += $"({string.Join(",", aliases)})";
                return name;
            }
        }

        #region ICommandUsage

        void ICommandUsage.Print(CommandUsage usage)
        {
            this.PrintUsage(usage);
        }

        #endregion
    }
}
