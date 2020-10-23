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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Library.Commands
{
    class SubCommandAsyncBase : ICommand, ICommandCompletor, IExecutableAsync, ICommandDescriptor, IMemberDescriptorProvider
    {
        private readonly CommandMethodBase command;
        private readonly CommandMethodDescriptor descriptor;

        public SubCommandAsyncBase(CommandMethodBase command, CommandMethodDescriptor descriptor)
        {
            this.command = command;
            this.descriptor = descriptor;
            this.Members = descriptor.Members.Select(item => new SubCommandPropertyDescriptor(command, item)).ToArray();
        }

        public string Name => this.descriptor.Name;

        public string Summary => this.descriptor.Summary;

        public string Description => this.descriptor.Description;

        public virtual bool IsEnabled => this.descriptor.CanExecute(this.command);

        public IEnumerable<CommandMemberDescriptor> Members { get; }

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return this.descriptor.InvokeAsync(this.command, this.descriptor.Members, cancellationToken);
        }

        public string[] GetCompletions(CommandCompletionContext completionContext)
        {
            return this.command.GetCompletions(this.descriptor, completionContext.MemberDescriptor, completionContext.Find);
        }
    }
}
