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
        public HelpCommand()
        {
        }

        public override string[] GetCompletions(CommandCompletionContext completionContext)
        {
            var descriptor = completionContext.MemberDescriptor;
            var properties = completionContext.Properties;
            if (descriptor.DescriptorName == nameof(CommandNames))
            {
                var commandNames = properties[nameof(CommandNames)] as string[];
                return this.GetCommandNames(this.CommandContext.Node, commandNames, completionContext.Find);

            }
            return base.GetCompletions(completionContext);
        }

        // [DisplayName("commands ...")]
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
                var argumentList = new List<string>(this.CommandNames);
                var command = this.GetCommand(this.CommandContext.Node, argumentList);
                if (command != null)
                {
                    var commandName = string.Join(" ", this.CommandNames);
                    if (command is ICommandHierarchy)
                    {
                        var methodDescriptors = CommandDescriptor.GetMethodDescriptors(command);
                        var printer = new CommandMethodUsagePrinter(commandName, command) { IsDetailed = this.IsDetail };
                        printer.Print(this.Out, methodDescriptors.ToArray());
                    }
                    else
                    {
                        var memberDescriptors = CommandDescriptor.GetMemberDescriptors(command);
                        var printer = new CommandMemberUsagePrinter(commandName, command) { IsDetailed = this.IsDetail };
                        printer.Print(this.Out, memberDescriptors.ToArray());
                    }
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
            foreach (var item in this.CommandContext.Node.Childs)
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

        private ICommand GetCommand(ICommandNode parent, List<string> argumentList)
        {
            var commandName = argumentList.FirstOrDefault() ?? string.Empty;
            if (commandName != string.Empty)
            {
                if (parent.Childs.ContainsKey(commandName) == true)
                {
                    var commandNode = parent.Childs[commandName];
                    if (commandNode.IsEnabled == false)
                        return null;
                    argumentList.RemoveAt(0);
                    if (argumentList.Count > 0 && commandNode.Childs.Any())
                    {
                        return this.GetCommand(commandNode, argumentList);
                    }
                    return commandNode.Command;
                }
            }
            return null;
        }

        private string[] GetCommandNames(ICommandNode node, string[] commandNames, string find)
        {
            var commandName = commandNames.FirstOrDefault() ?? string.Empty;
            if (commandName == string.Empty)
            {
                var query = from item in node.Childs
                            where item.IsEnabled
                            where item.Name.StartsWith(find)
                            where item.Name != this.Name
                            orderby item.Name
                            select item.Name;
                return query.ToArray();
            }
            else if (node.Childs.ContainsKey(commandName) == true)
            {
                return this.GetCommandNames(node.Childs[commandName], commandNames.Skip(1).ToArray(), find);
            }
            return null;
        }
    }
}
