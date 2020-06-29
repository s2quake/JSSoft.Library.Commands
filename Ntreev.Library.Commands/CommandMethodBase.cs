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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Ntreev.Library.ObjectModel;

namespace Ntreev.Library.Commands
{
    public abstract class CommandMethodBase : ICommand, ICommandHost, ICommandNode
    {
        private CommandContextBase commandContext;
        private readonly CommandCollection commands = new CommandCollection();

        protected CommandMethodBase()
        {
            this.Name = CommandStringUtility.ToSpinalCase(this.GetType());

            foreach (var item in CommandDescriptor.GetMethodDescriptors(this))
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
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));

            foreach (var item in CommandDescriptor.GetMethodDescriptors(this))
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

        public string Name { get; }

        public virtual bool IsEnabled => true;

        public TextWriter Out => this.commandContext.Out;

        public TextWriter Error => this.commandContext.Error;

        protected virtual bool IsMethodEnabled(CommandMethodDescriptor descriptor)
        {
            return true;
        }

        public virtual string[] GetCompletions(CommandMethodDescriptor methodDescriptor, CommandMemberDescriptor memberDescriptor, string find)
        {
            return null;
        }

        internal bool InvokeIsMethodEnabled(CommandMethodDescriptor descriptor)
        {
            return this.IsMethodEnabled(descriptor);
        }

        #region ICommandHost

        CommandContextBase ICommandHost.CommandContext
        {
            get => this.commandContext;
            set => this.commandContext = value;
        }

        public IContainer<ICommand> Commands => this.commands;

        #endregion
    }
}
