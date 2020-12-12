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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Library.Commands
{
    public abstract class CommandBase : ICommand, IExecutable, ICommandHost, ICommandCompletor, ICommandDescriptor, ICommandUsage
    {
        private ICommandNode node;

        protected CommandBase()
            : this(new string[] { })
        {
        }

        protected CommandBase(string[] aliases)
        {
            this.Name = CommandStringUtility.ToSpinalCase(this.GetType());
            this.Aliases = aliases ?? throw new ArgumentNullException(nameof(aliases));
        }

        protected CommandBase(string name)
            : this(name, new string[] { })
        {
        }

        protected CommandBase(string name, string[] aliases)
        {
            this.Name = name;
            this.Aliases = aliases ?? throw new ArgumentNullException(nameof(aliases));
        }

        public virtual string[] GetCompletions(CommandCompletionContext completionContext)
        {
            return null;
        }

        public string Name { get; }

        public string[] Aliases { get; }

        public virtual bool IsEnabled => true;

        public TextWriter Out => this.CommandContext.Out;

        public TextWriter Error => this.CommandContext.Error;

        public CommandContextBase CommandContext => this.node.CommandContext;

        protected abstract void OnExecute();

        protected CommandMemberDescriptor GetDescriptor(string propertyName)
        {
            return CommandDescriptor.GetMemberDescriptors(this)[propertyName];
        }

        protected CommandMemberDescriptor GetStaticDescriptor(Type type, string propertyName)
        {
            return CommandDescriptor.GetStaticMemberDescriptors(type)[propertyName];
        }

        protected virtual void PrintUsage(CommandUsage usage)
        {
            var descriptors = CommandDescriptor.GetMemberDescriptors(this);
            var printer = new CommandMemberUsagePrinter(this.Name, this, this.Aliases) { Usage = usage };
            printer.Print(this.Out, descriptors.ToArray());
        }

        #region ICommand

        void IExecutable.Execute()
        {
            this.OnExecute();
        }

        #endregion

        #region ICommandHost

        ICommandNode ICommandHost.Node
        {
            get => this.node;
            set => this.node = value;
        }

        #endregion

        #region ICommandDescriptor

        string ICommandDescriptor.Name => this.Name;

        string ICommandDescriptor.Summary => CommandDescriptor.GetUsageDescriptionProvider(this.GetType()).GetSummary(this);

        string ICommandDescriptor.Description => CommandDescriptor.GetUsageDescriptionProvider(this.GetType()).GetDescription(this);

        IEnumerable<CommandMemberDescriptor> ICommandDescriptor.Members => CommandDescriptor.GetMemberDescriptors(this);

        #endregion

        #region ICommandUsage

        void ICommandUsage.Print(CommandUsage usage)
        {
            this.PrintUsage(usage);
        }

        #endregion
    }

    public abstract class CommandAsyncBase : ICommand, IExecutableAsync, ICommandHost, ICommandDescriptor, ICommandUsage
    {
        private ICommandNode node;

        protected CommandAsyncBase()
            : this(new string[] { })
        {
        }

        protected CommandAsyncBase(string[] aliases)
        {
            this.Name = CommandStringUtility.ToSpinalCase(this.GetType());
            this.Aliases = aliases ?? throw new ArgumentNullException(nameof(aliases));
        }

        protected CommandAsyncBase(string name)
            : this(name, new string[] { })
        {
        }

        protected CommandAsyncBase(string name, string[] aliases)
        {
            this.Name = name;
            this.Aliases = aliases ?? throw new ArgumentNullException(nameof(aliases));
        }

        public virtual string[] GetCompletions(CommandCompletionContext completionContext)
        {
            return null;
        }

        public string Name { get; }

        public string[] Aliases { get; }

        public virtual bool IsEnabled => true;

        public TextWriter Out => this.CommandContext.Out;

        public TextWriter Error => this.CommandContext.Error;

        public CommandContextBase CommandContext => this.node.CommandContext;

        protected abstract Task OnExecuteAsync(CancellationToken cancellationToken);

        protected CommandMemberDescriptor GetDescriptor(string propertyName)
        {
            return CommandDescriptor.GetMemberDescriptors(this)[propertyName];
        }

        protected CommandMemberDescriptor GetStaticDescriptor(Type type, string propertyName)
        {
            return CommandDescriptor.GetStaticMemberDescriptors(type)[propertyName];
        }

        protected virtual void PrintUsage(CommandUsage usage)
        {
            var descriptors = CommandDescriptor.GetMemberDescriptors(this);
            var printer = new CommandMemberUsagePrinter(this.Name, this, this.Aliases) { Usage = usage };
            printer.Print(this.Out, descriptors.ToArray());
        }

        #region ICommand

        Task IExecutableAsync.ExecuteAsync(CancellationToken cancellationToken)
        {
            return this.OnExecuteAsync(cancellationToken);
        }

        #endregion

        #region ICommandHost

        ICommandNode ICommandHost.Node
        {
            get => this.node;
            set => this.node = value;
        }

        #endregion

        #region ICommandDescriptor

        string ICommandDescriptor.Name => this.Name;

        string ICommandDescriptor.Summary => CommandDescriptor.GetUsageDescriptionProvider(this.GetType()).GetSummary(this);

        string ICommandDescriptor.Description => CommandDescriptor.GetUsageDescriptionProvider(this.GetType()).GetDescription(this);

        IEnumerable<CommandMemberDescriptor> ICommandDescriptor.Members => CommandDescriptor.GetMemberDescriptors(this);

        #endregion

        #region ICommandUsage

        void ICommandUsage.Print(CommandUsage usage)
        {
            this.PrintUsage(usage);
        }

        #endregion
    }
}
