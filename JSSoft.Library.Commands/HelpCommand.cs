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

using JSSoft.Library.Commands.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JSSoft.Library.Commands
{
    [ResourceUsageDescription]
    [HelpCommand]
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
                var commandNames = new string[] { };
                if (properties.TryGetValue(nameof(CommandNames), out var value) == true)
                {
                    commandNames = value as string[];
                }
                return this.GetCommandNames(this.CommandContext.Node, commandNames, completionContext.Find);
            }
            return base.GetCompletions(completionContext);
        }

        [CommandPropertyArray]
        public string[] CommandNames { get; set; } = new string[] { };

        [CommandPropertySwitch("detail")]
        [CommandPropertyTrigger(nameof(IsSimple), false)]
        public bool IsDetail { get; set; }

        [CommandPropertySwitch("simple")]
        [CommandPropertyTrigger(nameof(IsDetail), false)]
        public bool IsSimple { get; set; }

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
                var argList = new List<string>(this.CommandNames);
                var command = CommandContextBase.GetCommand(this.CommandContext.Node, argList);
                if (command != null && argList.Any() == false)
                {
                    if (command is ICommandUsage usage)
                        usage.Print(this.Usage);
                    else
                        throw new InvalidOperationException(Resources.Exception_NotProvideHelp);
                }
                else
                {
                    var commandName = CommandStringUtility.Join(this.CommandNames);
                    throw new InvalidOperationException(string.Format(Resources.Exception_CommandDoesNotExists_Format, commandName));
                }
            }
        }

        protected CommandUsage Usage
        {
            get
            {
                if (this.IsDetail == true)
                    return CommandUsage.Detail;
                else if (this.IsSimple == true)
                    return CommandUsage.Simple;
                return CommandUsage.None;
            }
        }

        private void PrintList()
        {
            using var writer = new CommandTextWriter() { IsAnsiSupported = this.IsAnsiSupported };
            var parser = new CommandLineParser(this.ExecutionName, this)
            {
                Out = writer
            };
            var query = from item in this.CommandContext.Node.Childs
                        where item.IsEnabled == true
                        orderby item.Name
                        select item;

            parser.PrintUsage(string.Empty, this.Usage);
            writer.WriteLine(Resources.Text_AvaliableCommands);
            writer.Indent++;

            foreach (var item in query)
            {
                var descriptor = item.Descriptor;
                var summary = descriptor != null ? descriptor.Summary : string.Empty;
                var name = GetCommandNames(item);
                writer.WriteLine(name);
                if (summary != string.Empty && this.Usage != CommandUsage.Simple)
                {
                    writer.Indent++;
                    writer.WriteMultiline(summary);
                    writer.Indent--;
                }
                if (query.Last() != item && this.Usage != CommandUsage.Simple)
                    writer.WriteLine();
            }
            writer.Indent--;
            this.Out.Write(writer.ToString());
        }

        private string[] GetCommandNames(ICommandNode node, string[] commandNames, string find)
        {
            var commandName = commandNames.FirstOrDefault() ?? string.Empty;
            if (commandName == string.Empty)
            {
                var query = from item in node.Childs
                            where item.IsEnabled
                            from name in new string[] { item.Name }.Concat(item.Aliases)
                            where name.StartsWith(find)
                            where name != this.Name
                            orderby name
                            select name;
                return query.ToArray();
            }
            else if (node.Childs.ContainsKey(commandName) == true)
            {
                return this.GetCommandNames(node.Childs[commandName], commandNames.Skip(1).ToArray(), find);
            }
            else if (node.ChildsByAlias.ContainsKey(commandName) == true)
            {
                return this.GetCommandNames(node.ChildsByAlias[commandName], commandNames.Skip(1).ToArray(), find);
            }
            return null;
        }

        private static string GetCommandNames(ICommandNode node)
        {
            var sb = new StringBuilder();
            sb.Append(node.Name);
            foreach (var item in node.Aliases)
            {
                sb.Append($", {item}");
            }
            return sb.ToString();
        }
    }
}
