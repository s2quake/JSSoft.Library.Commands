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
using Ntreev.Library.ObjectModel;

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
            var descriptor = completionContext.MemberDescriptor;
            var properties = completionContext.Properties;
            if (descriptor.DescriptorName == nameof(CommandNames))
            {
                var commandNames = properties[nameof(CommandNames)] as string[];
                return this.GetCommandNames(this.commandContext.Commands, commandNames, completionContext.Find);

            }
            return base.GetCompletions(completionContext);
        }

        [DisplayName("commands")]
        [DefaultValue("")]
        [CommandPropertyArray()]
        public string[] CommandNames { get; set; } = new string[] { };

        [CommandProperty("detail")]
        public bool IsDetail { get; set; }

        [CommandProperty("option")]
        public string OptionName { get; set; }

        protected override void OnExecute()
        {
            if (this.CommandNames.Length == 0)
            {
                this.PrintList();
            }
            else
            {
                var command = this.GetCommand(this.commandContext.Commands, this.CommandNames);
                if (command != null)
                {
                    var commandName = string.Join(" ", this.CommandNames);
                    var parser = new CommandLineParser(commandName, command);
                    if (command is ICommandNode)
                        parser.PrintMethodUsage(string.Empty, string.Empty);
                    else
                        parser.PrintUsage(string.Empty);
                }
            }
        }

        private void PrintList()
        {
            using var writer = new CommandTextWriter();
            var parser = new CommandLineParser(this.Name, this);
            parser.Out = writer;
            parser.PrintUsage(string.Empty);
            writer.WriteLine(Resources.AvaliableCommands);
            writer.Indent++;
            foreach (var item in this.commandContext.Commands)
            {
                if (item.IsEnabled == false)
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

        private ICommand GetCommand(IContainer<ICommand> commands, string[] commandNames)
        {
            var commandName = commandNames.FirstOrDefault() ?? string.Empty;
            if (commandName != string.Empty)
            {
                if (commands.ContainsKey(commandName) == true)
                {
                    var command = commands[commandName];
                    if (command.IsEnabled == false)
                        return null;
                    if (commandNames.Length > 1 && command is ICommandNode commandNode)
                    {
                        return this.GetCommand(commandNode.Commands, commandNames.Skip(1).ToArray());
                    }
                    return command;
                }
            }
            return null;
        }

        private string[] GetCommandNames(IContainer<ICommand> commands, string[] commandNames, string find)
        {
            var commandName = commandNames.FirstOrDefault() ?? string.Empty;
            if (commandName == string.Empty)
            {
                var query = from item in commands
                            where item.IsEnabled
                            where item.Name.StartsWith(find)
                            orderby item.Name
                            select item.Name;
                return query.ToArray();
            }
            else if (commands.ContainsKey(commandName) == true)
            {
                var command = commands[commandName];
                if (command is ICommandNode commandNode)
                {
                    return this.GetCommandNames(commandNode.Commands, commandNames.Skip(1).ToArray(), find);
                }
            }
            return null;
        }
    }
}
