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

namespace Ntreev.Library.Commands
{
    class SubCommandBase : ICommand, ICommandCompletor, IExecutable, ICommandDescriptor
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

        public virtual bool IsEnabled => this.descriptor.CanExecute(this.command);

        public IEnumerable<CommandMemberDescriptor> Members { get; }

        public void Execute()
        {
            this.descriptor.Invoke(this.command, this.descriptor.Members);
        }

        public string[] GetCompletions(CommandCompletionContext completionContext)
        {
            return this.command.GetCompletions(this.descriptor, completionContext.MemberDescriptor, completionContext.Find);
        }
    }
}
