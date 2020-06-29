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

using Ntreev.Library;
using Ntreev.Library.Linq;
using Ntreev.Library.Commands.Properties;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Ntreev.Library.Commands
{
    [UsageDescriptionProvider(typeof(ResourceUsageDescriptionProvider))]
    class HelpCommand : CommandBase
    {
        private readonly CommandContextBase commandContext;

        public HelpCommand(CommandContextBase commandContext)
        {
            this.commandContext = commandContext;
        }

        public override string[] GetCompletions(CommandCompletionContext completionContext)
        {
            var arguments = completionContext.Arguments;
            if (arguments.IsEmpty())
            {
                return this.GetCommandNames();
            }
            else if (arguments.IsSingle() == true)
            {
                return this.GetCommandMethodNames(arguments.First());
            }
            return base.GetCompletions(completionContext);
        }

        [CommandProperty(IsRequired = true)]
        [DisplayName("command")]
        [DefaultValue("")]
        public string CommandName { get; set; } = string.Empty;

        [CommandProperty("sub-command", IsRequired = true)]
        [DefaultValue("")]
        public string MethodName { get; set; } = string.Empty;

        [CommandProperty("detail")]
        public bool IsDetail { get; set; }

        protected override void OnExecute()
        {
            try
            {
                var commandName = this.CommandName;
                if (commandName == string.Empty)
                {
                    this.PrintList();
                }
                else
                {
                    var command = this.commandContext.Commands[commandName];
                    if (command == null || this.commandContext.IsCommandEnabled(command) == false)
                        throw new CommandNotFoundException(commandName);

                    var parser = this.commandContext.Parsers[command];
                    parser.Out = this.commandContext.Out;
                    this.PrintUsage(command, parser);
                }
            }
            finally
            {
                this.CommandName = string.Empty;
            }
        }

        protected virtual void PrintUsage(ICommand command, CommandLineParser parser)
        {
            using var sw = new StringWriter();
            var methodName = this.MethodName;
            if (command is IExecutable == false && command is IExecutableAsync == false)
            {
                if (methodName != string.Empty)
                    parser.PrintMethodUsage(sw, methodName);
                else
                    parser.PrintMethodUsage(sw);
            }
            else
            {
                parser.PrintUsage(sw);
            }
            this.Out.Write(sw.ToString());
        }

        private void PrintList()
        {
            using var writer = new CommandTextWriter();
            var parser = this.commandContext.Parsers[this];

            parser.PrintUsage(writer.InnerWriter);
            writer.WriteLine(Resources.AvaliableCommands);
            writer.Indent++;
            foreach (var item in this.commandContext.Commands)
            {
                if (this.commandContext.IsCommandEnabled(item) == false)
                    continue;
                var summary = CommandDescriptor.GetUsageDescriptionProvider(item.GetType()).GetSummary(item);

                writer.WriteLine(item.Name);
                writer.Indent++;
                writer.WriteMultiline(summary);
                if (summary != string.Empty)
                    writer.WriteLine();
                writer.Indent--;
            }
            writer.Indent--;
            this.Out.Write(writer.ToString());
        }

        private string[] GetCommandNames()
        {
            var commands = this.commandContext.Commands;
            var query = from item in commands
                        where item.IsEnabled
                        orderby item.Name
                        select item.Name;
            return query.ToArray();
        }

        private string[] GetCommandMethodNames(string commandName)
        {
            var commands = this.commandContext.Commands;
            if (this.commandContext.Commands.ContainsKey(commandName) == false)
                return null;
            var command = this.commandContext.Commands[commandName];
            if (command is IExecutable == true)
                return null;

            var descriptors = CommandDescriptor.GetMethodDescriptors(command);
            var query = from item in descriptors
                        where this.commandContext.IsMethodEnabled(command, item)
                        orderby item.Name
                        select item.Name;
            return query.ToArray();
        }
    }
}
