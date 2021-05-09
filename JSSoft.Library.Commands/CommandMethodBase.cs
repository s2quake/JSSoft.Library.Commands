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

using JSSoft.Library.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JSSoft.Library.Commands
{
    public abstract class CommandMethodBase : ICommand, ICommandHost, ICommandHierarchy, ICommandCompletor, ICommandDescriptor, ICommandUsage
    {
        private readonly CommandCollection commands = new();
        private ICommandNode node;

        protected CommandMethodBase()
            : this(new string[] { })
        {
        }

        protected CommandMethodBase(string[] aliases)
        {
            this.Name = CommandStringUtility.ToSpinalCase(this.GetType());
            this.Aliases = aliases ?? throw new ArgumentNullException(nameof(aliases));

            foreach (var item in CommandDescriptor.GetMethodDescriptors(this.GetType()))
            {
                if (item.IsAsync == true)
                {
                    this.commands.Add(new SubCommandAsyncBase(this, item));
                }
                else
                {
                    this.commands.Add(new SubCommandBase(this, item));
                }
            }
        }

        protected CommandMethodBase(string name)
            : this(name, new string[] { })
        {
        }

        protected CommandMethodBase(string name, string[] aliases)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Aliases = aliases ?? throw new ArgumentNullException(nameof(aliases));

            foreach (var item in CommandDescriptor.GetMethodDescriptors(this.GetType()))
            {
                if (item.IsAsync == true)
                {
                    this.commands.Add(new SubCommandAsyncBase(this, item));
                }
                else
                {
                    this.commands.Add(new SubCommandBase(this, item));
                }
            }
        }

        public virtual string[] GetCompletions(CommandMethodDescriptor methodDescriptor, CommandMemberDescriptor memberDescriptor, string find)
        {
            return methodDescriptor.GetCompletionInternal(this, memberDescriptor, find);
        }

        public string Name { get; }

        public string[] Aliases { get; }

        public string ExecutionName
        {
            get
            {
                if (this.CommandContext.IsNameVisible == true)
                    return $"{this.CommandContext.ExecutionName} {this.Name}";
                return this.Name;
            }
        }

        public virtual bool IsEnabled => true;

        public TextWriter Out => this.CommandContext.Out;

        public TextWriter Error => this.CommandContext.Error;

        public CommandContextBase CommandContext => this.node.CommandContext;

        public bool IsAnsiSupported => this.CommandContext.IsAnsiSupported;

        protected virtual bool IsMethodEnabled(CommandMethodDescriptor descriptor)
        {
            return true;
        }

        protected virtual void PrintUsage(CommandUsage usage)
        {
            var query = from command in this.node.Commands
                        from item in CommandDescriptor.GetMethodDescriptors(command.GetType())
                        where item.CanExecute(this)
                        select item;
            var printer = new CommandMethodUsagePrinter(this.ExecutionName, this, this.Aliases)
            {
                Usage = usage,
                IsAnsiSupported = this.IsAnsiSupported
            };
            printer.Print(this.Out, query.ToArray());
        }

        internal bool InvokeIsMethodEnabled(CommandMethodDescriptor descriptor)
        {
            return this.IsMethodEnabled(descriptor);
        }

        #region ICommandHost

        ICommandNode ICommandHost.Node
        {
            get => this.node;
            set => this.node = value;
        }

        public IContainer<ICommand> Commands => this.commands;

        #endregion

        #region ICommandDescriptor

        string ICommandDescriptor.Summary => CommandDescriptor.GetUsageDescriptionProvider(this.GetType()).GetSummary(this);

        string ICommandDescriptor.Description => CommandDescriptor.GetUsageDescriptionProvider(this.GetType()).GetDescription(this);

        IEnumerable<CommandMemberDescriptor> ICommandDescriptor.Members => CommandDescriptor.GetMemberDescriptors(this);

        #endregion

        #region ICommandCompletor

        string[] ICommandCompletor.GetCompletions(CommandCompletionContext completionContext)
        {
            return null;
        }

        #endregion

        #region ICommandUsage

        void ICommandUsage.Print(CommandUsage usage)
        {
            this.PrintUsage(usage);
        }

        #endregion
    }
}
