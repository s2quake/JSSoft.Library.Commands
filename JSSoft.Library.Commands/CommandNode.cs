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

using JSSoft.Library.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace JSSoft.Library.Commands
{
    class CommandNode : ICommandNode
    {
        public CommandNode(CommandContextBase commandContext)
        {
            this.CommandContext = commandContext;
        }

        public override string ToString()
        {
            return this.Name;
        }

        public CommandNode Parent { get; set; }

        public CommandNodeCollection Childs { get; } = new CommandNodeCollection();

        public CommandAliasNodeCollection ChildsByAlias { get; } = new CommandAliasNodeCollection();

        public ICommand Command { get; set; }

        public ICommandDescriptor Descriptor => this.Command as ICommandDescriptor;

        public CommandContextBase CommandContext { get; }

        public List<ICommand> CommandList { get; } = new List<ICommand>();

        public string Name { get; set; } = string.Empty;

        public string[] Aliases => this.Command != null ? this.Command.Aliases : new string[] { };

        public bool IsEnabled => this.CommandList.Any(item => item.IsEnabled);

        #region ICommandNode

        IEnumerable<ICommand> ICommandNode.Commands => this.CommandList;

        ICommandNode ICommandNode.Parent => this.Parent;

        IContainer<ICommandNode> ICommandNode.Childs => this.Childs;

        IContainer<ICommandNode> ICommandNode.ChildsByAlias => this.ChildsByAlias;

        #endregion
    }
}
