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
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Library.Commands
{
    public abstract class CommandContextBase
    {
        private readonly CommandNode commandNode;
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
            this.helpCommand = commands.SingleOrDefault(item => item.GetType().GetCustomAttribute<HelpCommandAttribute>() != null) ?? new HelpCommand();
            this.versionCommand = commands.SingleOrDefault(item => item.GetType().GetCustomAttribute<VersionCommandAttribute>() != null) ?? new VersionCommand();
            this.commandNode = new CommandNode(this);
            this.Initialize(this.commandNode, commands);
        }

        protected CommandContextBase(string name, IEnumerable<ICommand> commands)
        {
            if (name == string.Empty)
                throw new ArgumentException(Resources.Exception_EmptyStringsAreNotAllowed);
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.fullName = name;
            this.filename = name;
            this.helpCommand = commands.SingleOrDefault(item => item.GetType().GetCustomAttribute<HelpCommandAttribute>() != null) ?? new HelpCommand();
            this.versionCommand = commands.SingleOrDefault(item => item.GetType().GetCustomAttribute<VersionCommandAttribute>() != null) ?? new VersionCommand();
            this.commandNode = new CommandNode(this);
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
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));
            var args = CommandStringUtility.SplitAll(arguments);
            var argList = new List<string>(args);
            return GetCommand(this.commandNode, argList);
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
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));
            this.ExecuteInternal(arguments);
            this.OnExecuted(EventArgs.Empty);
        }

        public void ExecuteWith(string arguments)
        {
            this.ExecuteInternal(arguments);
            this.OnExecuted(EventArgs.Empty);
        }

        public Task ExecuteAsync(string commandLine)
        {
            var cancellation = new CancellationTokenSource();
            return this.ExecuteAsync(commandLine, cancellation.Token);
        }

        public Task ExecuteAsync(string commandLine, CancellationToken cancellationToken)
        {
            var (name, arguments) = CommandStringUtility.Split(commandLine);
            return this.ExecuteAsync(name, arguments, cancellationToken);
        }

        public Task ExecuteAsync(string name, string arguments)
        {
            var cancellation = new CancellationTokenSource();
            return this.ExecuteAsync(name, arguments, cancellation.Token);
        }

        public async Task ExecuteAsync(string name, string arguments, CancellationToken cancellationToken)
        {
            if (this.VerifyName(name) == false)
                throw new ArgumentException(string.Format(Resources.Exception_InvalidCommandName_Format, name));
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));
            await this.ExecuteInternalAsync(arguments, cancellationToken);
            this.OnExecuted(EventArgs.Empty);
        }

        public Task ExecuteWithAsync(string arguments)
        {
            var cancellation = new CancellationTokenSource();
            return this.ExecuteWithAsync(arguments, cancellation.Token);
        }

        public async Task ExecuteWithAsync(string arguments, CancellationToken cancellationToken)
        {
            await this.ExecuteInternalAsync(arguments, cancellationToken);
            this.OnExecuted(EventArgs.Empty);
        }

        public static void PrintBaseUsage(CommandContextBase commandContext)
        {
            var helpCommand = commandContext.HelpCommand;
            var versionCommand = commandContext.VersionCommand;
            var helpName = helpCommand.Name;
            var name = commandContext.ExecutionName;
            var versionName = versionCommand.Name;
            var isNameVisible = commandContext.IsNameVisible;
            using var writer = new StringWriter();
            if (helpCommand is ICommandUsage helpUsage)
            {
                helpUsage.Print(CommandUsage.None);
            }
            if (versionCommand is ICommandUsage versionUsage)
            {
                versionUsage.Print(CommandUsage.None);
            }
            if (isNameVisible == true)
            {
                writer.WriteLine(Resources.Message_HelpUsage_Format, name, helpName);
                writer.WriteLine(Resources.Message_VersionUsage_Format, name, versionName);
            }
            else
            {
                writer.WriteLine(Resources.Message_Help_Format, helpName);
                writer.WriteLine(Resources.Message_Version_Format, versionName);
            }
            writer.WriteLine();
            commandContext.Out.Write(writer.ToString());
        }

        public TextWriter Out { get; set; } = Console.Out;

        public TextWriter Error { get; set; } = Console.Error;

        public string Name { get; }

        public string Version { get; set; } = $"{new Version(1, 0)}";

        public bool IsNameVisible { get; set; }

        public string ExecutionName
        {
            get
            {
#if NETCOREAPP
                if (this.filename != this.Name)
                {
                    return $"dotnet {this.filename}";
                }
#elif NETFRAMEWORK
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    return $"mono {this.filename}";
                }
#endif
                return this.Name;
            }
        }

        public ICommandNode Node => this.commandNode;

        public string BaseDirectory { get; set; } = Directory.GetCurrentDirectory();

        public Action<CommandContextBase> BaseUsage { get; set; } = PrintBaseUsage;

        public ICommand HelpCommand => this.helpCommand;

        public ICommand VersionCommand => this.versionCommand;

        public virtual bool IsAnsiSupported => false;

        public event EventHandler Executed;

        protected virtual IEnumerable<ICommand> ValidateCommands(IEnumerable<ICommand> commands)
        {
            var query = from item in commands
                        where CommandSettings.IsConsoleMode == true || item.GetType().GetCustomAttribute<ConsoleModeOnlyAttribute>() == null
                        orderby item.Name
                        orderby item.GetType().GetCustomAttribute<PartialCommandAttribute>()
                        select item;

            if (commands.Any(item => item.GetType().GetCustomAttribute<HelpCommandAttribute>() != null) == false)
                yield return this.helpCommand;
            if (commands.Any(item => item.GetType().GetCustomAttribute<VersionCommandAttribute>() != null) == false)
                yield return this.versionCommand;
            foreach (var item in query)
            {
                yield return item;
            }
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

        internal static ICommand GetCommand(ICommandNode parentNode, List<string> argumentList)
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
                        return GetCommand(commandNode, argumentList);
                    }
                    return commandNode.Command;
                }
                else if (parentNode.ChildsByAlias.ContainsKey(commandName) == true)
                {
                    var commandNode = parentNode.ChildsByAlias[commandName];
                    if (commandNode.IsEnabled == false)
                        return null;
                    argumentList.RemoveAt(0);
                    if (argumentList.Count > 0 && commandNode.Childs.Any())
                    {
                        return GetCommand(commandNode, argumentList);
                    }
                    return commandNode.Command;
                }
            }
            return null;
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
                CollectCommands(parentNode, item);
            }
        }

        private void CollectCommands(CommandNode parentNode, ICommand command)
        {
            var commandName = command.Name;
            var partialAttr = command.GetType().GetCustomAttribute<PartialCommandAttribute>();
            if (parentNode.Childs.ContainsKey(commandName) == true && partialAttr == null)
                throw new InvalidOperationException(string.Format(Resources.Exception_CommandAlreadyExists_Format, commandName));
            if (parentNode.Childs.ContainsKey(commandName) == false && partialAttr != null)
                throw new InvalidOperationException(string.Format(Resources.Exception_CommandDoesNotExists_Format, commandName));
            if (partialAttr != null && command.Aliases.Any() == true)
                throw new InvalidOperationException($"Partial command cannot have alias.: '{commandName}'");
            if (parentNode.Childs.ContainsKey(commandName) == false)
            {
                var commandNode = new CommandNode(this)
                {
                    Parent = parentNode,
                    Name = commandName,
                    Command = command
                };
                parentNode.Childs.Add(commandNode);
                foreach (var item in command.Aliases)
                {
                    parentNode.ChildsByAlias.Add(new CommandAliasNode(commandNode, item));
                }
            }
            {
                var commandNode = parentNode.Childs[commandName];
                commandNode.CommandList.Add(command);
                if (command is ICommandHost commandHost)
                    commandHost.Node = commandNode;
                if (command is ICommandHierarchy hierarchy)
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
            foreach (var item in commandNode.Childs)
            {
                this.InitializeCommand(item);
            }
        }

        private string[] GetCompletion(ICommandNode parentNode, IList<string> itemList, string find)
        {
            if (itemList.Count == 0)
            {
                var query = from item in parentNode.Childs
                            where item.IsEnabled
                            from name in new string[] { item.Name }.Concat(item.Aliases)
                            where name.StartsWith(find)
                            orderby name
                            select name;
                return query.ToArray();
            }
            else
            {
                var commandName = itemList.First();
                var commandNode = parentNode.Childs[commandName];
                if (commandNode == null)
                {
                    commandNode = parentNode.ChildsByAlias[commandName];
                    if (commandNode == null)
                        return null;
                }
                if (commandNode.Childs.Any() == true)
                {
                    itemList.RemoveAt(0);
                    return this.GetCompletion(commandNode, itemList, find);
                }
                else
                {
                    var args = itemList.Skip(1).ToArray();
                    foreach (var item in commandNode.Commands)
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
                this.BaseUsage?.Invoke(this);
            }
            else
            {
                var arguments = CommandStringUtility.SplitAll(commandLine);
                var argumentList = new List<string>(arguments);
                var command = GetCommand(this.commandNode, argumentList);
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

        private async Task ExecuteInternalAsync(string commandLine, CancellationToken cancellationToken)
        {
            if (commandLine == string.Empty)
            {
                this.BaseUsage?.Invoke(this);
            }
            else
            {
                var arguments = CommandStringUtility.SplitAll(commandLine);
                var argumentList = new List<string>(arguments);
                var command = GetCommand(this.commandNode, argumentList);
                if (command != null)
                {
                    var parser = new CommandLineParser(command.Name, command);
                    var arg = string.Join(" ", argumentList);
                    await parser.InvokeAsync(command.Name, arg, cancellationToken);
                }
                else
                {
                    throw new ArgumentException(string.Format(Resources.Exception_CommandDoesNotExists_Format, commandLine));
                }
            }
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
