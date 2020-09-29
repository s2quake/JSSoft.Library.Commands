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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JSSoft.Library.Commands
{
    public abstract class CommandContextBase
    {
        private readonly CommandNode commandNode = new CommandNode();
        private readonly ICommand helpCommand;
        private readonly ICommand versionCommand;
        private readonly FileVersionInfo versionInfo;
        private readonly string fullName;
        private readonly string filename;

        protected CommandContextBase(IEnumerable<ICommand> commands)
            : this(Assembly.GetEntryAssembly(), commands)
        {

        }

        protected CommandContextBase(Assembly assembly, IEnumerable<ICommand> commands)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            this.Name = Path.GetFileNameWithoutExtension(assembly.Location);
            this.filename = Path.GetFileName(assembly.Location);
            this.fullName = assembly.Location;
            this.versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            this.Version = this.versionInfo.ProductVersion;
            this.helpCommand = this.CreateHelpCommand();
            this.versionCommand = this.CreateVersionCommand();
            this.Initialize(this.commandNode, commands);
        }

        protected CommandContextBase(string name, IEnumerable<ICommand> commands)
        {
            if (name == string.Empty)
                throw new ArgumentException(Resources.Exception_EmptyStringsAreNotAllowed);
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.fullName = name;
            this.filename = name;
            this.helpCommand = this.CreateHelpCommand();
            this.versionCommand = this.CreateVersionCommand();
            this.Initialize(this.commandNode, commands);
        }

        public ICommand GetCommand(string commandLine)
        {
            var (name, arguments) = CommandStringUtility.Split(commandLine);
            return this.GetCommand(name, arguments);
        }

        public ICommand GetCommand(string name, string arguments)
        {
            if (this.VerifyName(name) == false)
                return null;
            var args = CommandStringUtility.SplitAll(arguments);
            var argList = new List<string>(args);
            return this.GetCommand(this.commandNode, argList);
        }

        public void Execute(string commandLine)
        {
            var (name, arguments) = CommandStringUtility.Split(commandLine);
            this.Execute(name, arguments);
        }

        public void Execute(string name, string arguments)
        {
            if (this.VerifyName(name) == false)
                throw new ArgumentException(string.Format(Resources.Exception_InvalidCommandName_Format, name));
            this.ExecuteInternal(arguments);
        }

        public Task ExecuteAsync(string commandLine)
        {
            var (name, arguments) = CommandStringUtility.Split(commandLine);
            return this.ExecuteAsync(name, arguments);
        }

        public Task ExecuteAsync(string name, string arguments)
        {
            if (this.VerifyName(name) == false)
                throw new ArgumentException(string.Format(Resources.Exception_InvalidCommandName_Format, name));
            return this.ExecuteInternalAsync(arguments);
        }

        public TextWriter Out { get; set; } = Console.Out;

        public TextWriter Error { get; set; } = Console.Error;

        public string Name { get; }

        public string Version { get; set; } = $"{new Version(1, 0)}";

        public ICommandNode Node => this.commandNode;

        public string BaseDirectory { get; set; } = Directory.GetCurrentDirectory();

        public event EventHandler Executed;

        protected virtual IEnumerable<ICommand> ValidateCommands(IEnumerable<ICommand> commands)
        {
            var query = from item in commands
                        where CommandSettings.IsConsoleMode == true || item.GetType().GetCustomAttribute<ConsoleModeOnlyAttribute>() == null
                        orderby item.Name
                        orderby item.GetType().GetCustomAttribute<PartialCommandAttribute>()
                        select item;

            yield return this.helpCommand;
            yield return this.versionCommand;
            foreach (var item in query)
            {
                yield return item;
            }
        }

        protected virtual ICommand CreateHelpCommand()
        {
            return new HelpCommand();
        }

        protected virtual ICommand CreateVersionCommand()
        {
            return new VersionCommand();
        }

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

        private void Initialize(CommandNode commandNode, IEnumerable<ICommand> commands)
        {
            this.CollectCommands(commandNode, this.ValidateCommands(commands));
            this.InitializeCommand(commandNode);
        }

        private void CollectCommands(CommandNode parentNode, IEnumerable<ICommand> commands)
        {
            foreach (var item in commands)
            {
                var commandName = item.Name;
                if (parentNode.Childs.ContainsKey(commandName) == true && item.GetType().GetCustomAttribute<PartialCommandAttribute>() == null)
                    throw new InvalidOperationException(string.Format(Resources.Exception_CommandAlreadyExists_Format, commandName));
                if (parentNode.Childs.ContainsKey(commandName) == false && item.GetType().GetCustomAttribute<PartialCommandAttribute>() != null)
                    throw new InvalidOperationException(string.Format(Resources.Exception_CommandDoesNotExists_Format, commandName));
                if (parentNode.Childs.ContainsKey(commandName) == false)
                {
                    parentNode.Childs.Add(new CommandNode()
                    {
                        Parent = parentNode,
                        Name = commandName,
                        Command = item
                    });
                }
                var commandNode = parentNode.Childs[commandName];
                commandNode.CommandList.Add(item);
                if (item is ICommandHierarchy hierarchy)
                {
                    this.CollectCommands(commandNode, hierarchy.Commands);
                }
            }
        }

        private void InitializeCommand(CommandNode commandNode)
        {
            var query = from item in commandNode.CommandList
                        where item is ICommandHost commandHost
                        select item as ICommandHost;
            foreach (var item in query)
            {
                item.CommandContext = this;
            }
            foreach (var item in commandNode.Childs)
            {
                this.InitializeCommand(item);
            }
        }

        private string[] GetCompletion(CommandNode parentNode, IList<string> itemList, string find)
        {
            if (itemList.Count == 0)
            {
                var query = from item in parentNode.Childs
                            let name = item.Name
                            where item.IsEnabled
                            where name.StartsWith(find)
                            orderby name
                            select name;
                return query.ToArray();
            }
            else
            {
                var commandName = itemList.First();
                var commandNode = parentNode.Childs[commandName];
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
                var members = CommandDescriptor.GetMemberDescriptors(item);
                var context = CommandCompletionContext.Create(item, members, arguments, find);
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
                sb.AppendLine(string.Format(Resources.Message_Help_Format, this.helpCommand.Name));
                sb.AppendLine(string.Format(Resources.Message_Version_Format, this.versionCommand.Name));
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
                    parser.Invoke(command.Name, arg);
                }
                else
                {
                    throw new ArgumentException(string.Format(Resources.Exception_CommandDoesNotExists_Format, commandLine));
                }
            }
        }

        private async Task ExecuteInternalAsync(string commandLine)
        {
            if (commandLine == string.Empty)
            {
                var sb = new StringBuilder();
                sb.AppendLine(string.Format(Resources.Message_Help_Format, this.helpCommand.Name));
                sb.AppendLine(string.Format(Resources.Message_Version_Format, this.versionCommand.Name));
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
                    await parser.InvokeAsync(command.Name, arg);
                }
                else
                {
                    throw new ArgumentException(string.Format(Resources.Exception_CommandDoesNotExists_Format, commandLine));
                }
            }
        }

        private ICommand GetCommand(CommandNode parentNode, List<string> argumentList)
        {
            var commandName = argumentList.FirstOrDefault() ?? string.Empty;
            if (commandName != string.Empty)
            {
                if (parentNode.Childs.ContainsKey(commandName) == true)
                {
                    var commandNode = parentNode.Childs[commandName];
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

        private bool VerifyName(string name)
        {
            if (this.Name == name)
                return true;
            if (this.fullName == name)
                return true;
            if (this.filename == name)
                return true;
            return false;
        }
    }
}
