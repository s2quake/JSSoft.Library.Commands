//Released under the MIT License.
//
//Copyright (c) 2018 Ntreev Soft co., Ltd.
//
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
//documentation files (the "Software"), to deal in the Software without restriction, including without limitation the 
//rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit 
//persons to whom the Software is furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the 
//Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
//WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
//COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR 
//OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ntreev.Library.Commands
{
    public abstract class CommandBase : ICommand, IExecutable, ICommandHost
    {
        private CommandContextBase commandContext;

        protected CommandBase()
        {
            this.Name = CommandStringUtility.ToSpinalCase(this.GetType());
        }

        protected CommandBase(string name)
        {
            this.Name = name;
        }

        public virtual string[] GetCompletions(CommandCompletionContext completionContext)
        {
            return null;
        }

        public string Name { get; }

        public virtual bool IsEnabled => true;

        public TextWriter Out => this.commandContext.Out;

        public TextWriter Error => this.commandContext.Error;

        protected abstract void OnExecute();

        protected CommandMemberDescriptor GetDescriptor(string propertyName)
        {
            return CommandDescriptor.GetMemberDescriptors(this)[propertyName];
        }

        protected CommandMemberDescriptor GetStaticDescriptor(Type type, string propertyName)
        {
            return CommandDescriptor.GetStaticMemberDescriptors(type)[propertyName];
        }

        #region ICommand

        void IExecutable.Execute()
        {
            this.OnExecute();
        }

        #endregion

        #region ICommandHost

        CommandContextBase ICommandHost.CommandContext
        {
            get => this.commandContext;
            set => this.commandContext = value;
        }

        #endregion
    }

    public abstract class CommandAsyncBase : ICommand, IExecutableAsync, ICommandHost
    {
        private CommandContextBase commandContext;

        protected CommandAsyncBase()
        {
            this.Name = CommandStringUtility.ToSpinalCase(this.GetType());
        }

        protected CommandAsyncBase(string name)
        {
            this.Name = name;
        }

        public virtual string[] GetCompletions(CommandCompletionContext completionContext)
        {
            return null;
        }

        public string Name { get; }

        public virtual bool IsEnabled => true;

        public TextWriter Out => this.commandContext.Out;

        public TextWriter Error => this.commandContext.Error;

        protected abstract Task OnExecuteAsync();

        protected CommandMemberDescriptor GetDescriptor(string propertyName)
        {
            return CommandDescriptor.GetMemberDescriptors(this)[propertyName];
        }

        protected CommandMemberDescriptor GetStaticDescriptor(Type type, string propertyName)
        {
            return CommandDescriptor.GetStaticMemberDescriptors(type)[propertyName];
        }

        #region ICommand

        Task IExecutableAsync.ExecuteAsync()
        {
            return this.OnExecuteAsync();
        }

        #endregion

        #region ICommandHost

        CommandContextBase ICommandHost.CommandContext
        {
            get => this.commandContext;
            set => this.commandContext = value;
        }

        #endregion
    }
}
