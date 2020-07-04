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

using Ntreev.Library.Commands.Properties;
using Ntreev.Library.ObjectModel;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace Ntreev.Library.Commands
{
    public abstract class CommandContextBase
    {
        private const string redirectionPattern = "(>{1,2}[^>]+)";
        private readonly CommandNode commandNode = new CommandNode();
        private readonly ICommand helpCommand;
        private readonly ICommand versionCommand;
        private FileVersionInfo versionInfo;
        private string fullName;

        protected CommandContextBase(IEnumerable<ICommand> commands)
            : this(Assembly.GetEntryAssembly(), commands)
        {

        }

        protected CommandContextBase(Assembly assembly, IEnumerable<ICommand> commands)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            this.Name = Path.GetFileName(assembly.Location);
            this.fullName = assembly.Location;
            this.versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            this.Version = new Version(this.versionInfo.ProductVersion);
            this.helpCommand = this.CreateHelpCommand();
            this.versionCommand = this.CreateVersionCommand();
            this.Initialize(this.commandNode, commands);
        }

        protected CommandContextBase(string name, IEnumerable<ICommand> commands)
        {
            if (name == string.Empty)
                throw new ArgumentException("empty string not allowed.");
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.fullName = name;
            this.helpCommand = this.CreateHelpCommand();
            this.versionCommand = this.CreateVersionCommand();
            this.Initialize(this.commandNode, commands);
        }

        public ICommand GetCommand(params string[] commandNames)
        {
            return this.GetCommand(this.commandNode, new List<string>(commandNames));
        }

        public void Execute(string commandLine)
        {
            var (name, arguments) = CommandStringUtility.Split(commandLine);
            this.Execute(name, arguments);
        }

        public void Execute(string name, string arguments)
        {
            if (this.Name != name)
                throw new ArgumentException(string.Format(Resources.InvalidCommandName_Format, name));

            this.ExecuteInternal(arguments);
        }

        public TextWriter Out { get; set; } = Console.Out;

        public TextWriter Error { get; set; } = Console.Error;

        public string Name { get; }

        public Version Version { get; set; } = new Version(1, 0);

        public ICommandNode Node => this.commandNode;

        public string BaseDirectory { get; set; } = Directory.GetCurrentDirectory();

        public event EventHandler Executed;

        protected virtual IEnumerable<ICommand> ValidateCommands(IEnumerable<ICommand> commands)
        {
            var query = from item in commands
                        where CommandSettings.IsConsoleMode == true || item.GetType().GetCustomAttribute<ConsoleModeOnlyAttribute>() == null
                        orderby item.Name
                        orderby item.GetType().GetCustomAttribute<PartialCommand>()
                        select item;

            yield return this.helpCommand;
            yield return this.versionCommand;
            foreach (var item in query)
            {
                yield return item;
            }
        }

        protected virtual ICommand CreateHelpCommand() => new HelpCommand();

        protected virtual ICommand CreateVersionCommand() => new VersionCommand();

        protected virtual void OnExecuted(EventArgs e)
        {
            this.Executed?.Invoke(this, e);
        }

        protected virtual string[] GetCompletion(string[] items, string find)
        {
            return this.GetCompletion(this.commandNode, new List<string>(items), find);
        }

        internal string[] GetCompletionInternal(string[] items, string find)
        {
            return this.GetCompletion(items, find);
        }

        private void Initialize(CommandNode node, IEnumerable<ICommand> commands)
        {
            this.CollectCommands(node, this.ValidateCommands(commands));
            this.InitializeCommand(node);
        }

        private void CollectCommands(CommandNode node, IEnumerable<ICommand> commands)
        {
            foreach (var item in commands)
            {
                var commandName = item.Name;
                if (node.Childs.ContainsKey(commandName) == true && item.GetType().GetCustomAttribute<PartialCommand>() == null)
                    throw new InvalidOperationException("command already exists.");

                if (node.Childs.ContainsKey(commandName) == false)
                {
                    node.Childs.Add(new CommandNode()
                    {
                        Parent = node,
                        Name = commandName,
                        Command = item
                    });
                }
                var commandNode = node.Childs[commandName];
                commandNode.CommandList.Add(item);

                if (item is ICommandHierarchy hierarchy)
                {
                    this.CollectCommands(commandNode, hierarchy.Commands);
                }
            }
        }

        private void InitializeCommand(CommandNode node)
        {
            foreach (var item in node.CommandList)
            {
                if (item is ICommandHost commandHost)
                {
                    commandHost.CommandContext = this;
                }
            }
            foreach (var item in node.Childs)
            {
                this.InitializeCommand(item);
            }
        }

        private string[] GetCompletion(CommandNode parent, IList<string> itemList, string find)
        {
            if (itemList.Count == 0)
            {
                var query = from item in parent.Childs
                            let name = item.Name
                            where item.IsEnabled
                            where name.StartsWith(find)
                            select name;
                return query.ToArray();
            }
            else
            {
                var commandName = itemList.First();
                var commandNode = parent.Childs[commandName];
                if (commandNode.Childs.Any() == true)
                {
                    itemList.RemoveAt(0);
                    return this.GetCompletion(commandNode, itemList, find);
                }
                else
                {
                    var args = itemList.Skip(1).ToArray();
                    foreach (var item in commandNode.CommandList)
                    {
                        if (this.GetCompletion(item, args, find) is string[] completions)
                            return completions;
                    }
                }
                return null;
            }
        }

        private string[] GetCompletion(ICommand item, string[] arguments, string find)
        {
            if (item is ICommandCompletor completor)
            {
                var memberList = new List<CommandMemberDescriptor>(CommandDescriptor.GetMemberDescriptors(item));
                var argList = new List<string>(arguments);
                var context = CommandCompletionContext.Create(item, memberList, argList, find);
                if (context is CommandCompletionContext completionContext)
                {
                    var completion = completor.GetCompletions(completionContext);
                    if (completion != null)
                        return completion;
                }
                else if (context is string[] completions)
                    return completions;
            }
            return null;
        }

        private void ExecuteInternal(string commandLine)
        {
            if (commandLine == string.Empty)
            {
                var sb = new StringBuilder();
                sb.AppendLine(string.Format(Resources.HelpMessage_Format, this.helpCommand.Name));
                sb.AppendLine(string.Format(Resources.VersionMessage_Format, this.versionCommand.Name));
                this.Out.Write(sb.ToString());
            }
            else
            {
                var arguments = CommandStringUtility.SplitAll(commandLine);
                var argumentList = new List<string>(arguments);
                var command = this.GetCommand(this.commandNode, argumentList);
                if (command != null)
                {
                    var parser = new CommandLineParser(command.Name, command);
                    var arg = string.Join(" ", argumentList);
                    parser.TryInvoke(command.Name, arg);
                }
                else
                {
                    throw new ArgumentException(string.Format("'{0}' does not existed command.", commandLine));
                }
            }
        }

        private ICommand GetCommand(CommandNode parent, List<string> argumentList)
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
                    if (argumentList.Count > 0)
                    {
                        return this.GetCommand(commandNode, argumentList);
                    }
                    return commandNode.CommandList.LastOrDefault();
                }
            }
            return null;
        }
    }
}
