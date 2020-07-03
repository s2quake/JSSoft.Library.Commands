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
        private readonly static TextWriter defaultWriter = new ConsoleTextWriter();
        private readonly CommandCollection commands = new CommandCollection();
        private readonly ICommand helpCommand;
        private readonly ICommand versionCommand;
        private string name;
        private Version version;
        private TextWriter writer;
        private TextWriter error;
        private string baseDirectory;

        protected CommandContextBase(IEnumerable<ICommand> commands)
            : this(commands, new ICommandProvider[] { })
        {

        }

        protected CommandContextBase(IEnumerable<ICommand> commands, IEnumerable<ICommandProvider> commandProviders)
        {
            this.Out = defaultWriter;
            this.CommandProviders = commandProviders.ToArray();
            this.helpCommand = this.CreateHelpCommand();
            this.versionCommand = this.CreateVersionCommand();

            var commands2 = commands.Concat(new ICommand[] { this.helpCommand, this.versionCommand });
            foreach (var item in commands2)
            {
                if (CommandSettings.IsConsoleMode == false && item.GetType().GetCustomAttribute<ConsoleModeOnlyAttribute>() != null)
                    continue;
                this.commands.Add(item);
                if (item is ICommandHost commandHost)
                {
                    commandHost.CommandContext = this;
                }
            }

            foreach (var item in commandProviders)
            {
                if (CommandSettings.IsConsoleMode == false && item.GetType().GetCustomAttribute<ConsoleModeOnlyAttribute>() != null)
                    continue;
                var command = commands.FirstOrDefault(i => i.Name == item.CommandName);
                if (command == null)
                    throw new CommandNotFoundException(item.CommandName);

                var descriptors = CommandDescriptor.GetMethodDescriptors(command);
                descriptors.AddRange(this.GetExternalMethodDescriptors(item));
                if (item is ICommandHost commandHost)
                {
                    commandHost.CommandContext = this;
                }
            }
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

            arguments = this.InitializeRedirection(arguments);

            try
            {
                this.ExecuteInternal(arguments.Trim());
            }
            finally
            {
                if (this.Out is RedirectionTextWriter == true)
                {
                    (this.Out as RedirectionTextWriter).Dispose();
                    this.Out = defaultWriter;
                }
            }
        }

        public TextWriter Out
        {
            get => this.writer ?? Console.Out;
            set => this.writer = value;
        }

        public TextWriter Error
        {
            get => this.error ?? Console.Error;
            set => this.error = value;
        }

        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(this.name) == true)
                    return Path.GetFileName(Assembly.GetEntryAssembly().CodeBase); ;
                return this.name;
            }
            set => this.name = value;
        }

        public Version Version
        {
            get
            {
                if (this.version == null)
                {
                    return new Version(FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).ProductVersion);
                }
                return this.version;
            }
            set => this.version = value;
        }

        public IContainer<ICommand> Commands => this.commands;

        public ICommandProvider[] CommandProviders { get; private set; }

        public string BaseDirectory
        {
            get => this.baseDirectory ?? Directory.GetCurrentDirectory();
            set => this.baseDirectory = value;
        }

        public event EventHandler Executed;

        protected virtual ICommand CreateHelpCommand() => new HelpCommand(this);

        protected virtual ICommand CreateVersionCommand() => new VersionCommand(this);

        protected virtual void OnExecuted(EventArgs e)
        {
            this.Executed?.Invoke(this, e);
        }

        protected virtual string[] GetCompletion(string[] items, string find)
        {
            return this.GetCompletion(this.commands, new List<string>(items), find);
        }

        internal string[] GetCompletionInternal(string[] items, string find)
        {
            return this.GetCompletion(items, find);
        }

        private string[] GetCompletion(IContainer<ICommand> commands, IList<string> itemList, string find)
        {
            if (itemList.Count == 0)
            {
                var query = from item in commands
                            let name = item.Name
                            where item.IsEnabled
                            where name.StartsWith(find)
                            select name;
                return query.ToArray();
            }
            else
            {
                var commandName = itemList.First();
                var command = commands[commandName];
                if (command is ICommandNode commandNode)
                {
                    itemList.RemoveAt(0);
                    return this.GetCompletion(commandNode.Commands, itemList, find);
                }
                else if (command is ICommandCompletor completor)
                {
                    var memberList = new List<CommandMemberDescriptor>(CommandDescriptor.GetMemberDescriptors(command));
                    var argList = new List<string>(itemList.Skip(1));
                    var context = CommandCompletionContext.Create(command, memberList, argList, find);
                    if (context is CommandCompletionContext completionContext)
                        return completor.GetCompletions(completionContext);
                    else if (context is string[] completions)
                        return completions;
                }
                return null;
            }
        }

        private object GetCommandTarget(ICommand command, CommandMethodDescriptor methodDescriptor)
        {
            var methodInfo = methodDescriptor.MethodInfo;
            if (methodInfo.DeclaringType == command.GetType())
                return command;
            var query = from item in this.CommandProviders
                        where item.CommandName == command.Name
                        where item.GetType() == methodInfo.DeclaringType
                        select item;

            return query.First();
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
                var command = this.GetCommand(this.commands, argumentList);
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

        private ICommand GetCommand(IContainer<ICommand> commands, List<string> argumentList)
        {
            var commandName = argumentList.FirstOrDefault() ?? string.Empty;
            if (commandName != string.Empty)
            {
                if (commands.ContainsKey(commandName) == true)
                {
                    var command = commands[commandName];
                    if (command.IsEnabled == false)
                        return null;
                    argumentList.RemoveAt(0);
                    if (argumentList.Count > 0 && command is ICommandNode commandNode)
                    {
                        return this.GetCommand(commandNode.Commands, argumentList);
                    }
                    return command;
                }
            }
            return null;
        }

        private IEnumerable<CommandMethodDescriptor> GetExternalMethodDescriptors(ICommandProvider commandProvider)
        {
            foreach (var item in CommandDescriptor.GetMethodDescriptors(commandProvider))
            {
                yield return new ExternalCommandMethodDescriptor(commandProvider, item);
            }
        }

        private string InitializeRedirection(string arguments)
        {
            var args = CommandStringUtility.SplitAll(arguments, false);
            var argList = new List<string>(args.Length);

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                var isWrpped = arg.Length > 1 && arg.First() == '\"' && arg.Last() == '\"';

                if (args[i] == ">")
                {
                    i++;
                    this.AddRedirection(args[i], false);
                }
                else if (args[i].StartsWith(">>"))
                {
                    this.AddRedirection(args[i].Substring(2), true);
                }
                else if (args[i].StartsWith(">"))
                {
                    this.AddRedirection(args[i].Substring(1), false);
                }
                else
                {
                    if (isWrpped == false)
                    {
                        arg = this.InitializeRedirectionFromArgument(arg);
                    }
                    argList.Add(arg);
                }
            }

            return string.Join(" ", argList.ToArray());
        }

        private string InitializeRedirectionFromArgument(string input)
        {
            var match = Regex.Match(input, redirectionPattern);
            var matchList = new List<Match>();
            while (match.Success)
            {
                matchList.Insert(0, match);
                if (match.Value.StartsWith(">>"))
                {
                    this.AddRedirection(match.Value.Substring(2), true);
                }
                else if (match.Value.StartsWith(">"))
                {
                    this.AddRedirection(match.Value.Substring(1), false);
                }
                match = match.NextMatch();
            }

            foreach (var item in matchList)
            {
                input = input.Remove(item.Index, item.Length);
            }
            return input;
        }

        private void AddRedirection(string filename, bool appendMode)
        {
            if (this.Out is RedirectionTextWriter == false)
            {
                this.Out = new RedirectionTextWriter(this.BaseDirectory, this.Out.Encoding);
            }

            (this.Out as RedirectionTextWriter).Add(CommandStringUtility.TrimQuot(filename), false);
        }

        #region classes

        class ConsoleTextWriter : TextWriter
        {
            public override Encoding Encoding
            {
                get { return Console.OutputEncoding; }
            }

            public override void Write(char value)
            {
                Console.Write(value);
            }

            public override void Write(string value)
            {
                Console.Write(value);
            }

            public override void WriteLine(string value)
            {
                Console.WriteLine(value);
            }
        }

        class RedirectionTextWriter : StringWriter
        {
            private readonly List<string> writeList = new List<string>();
            private readonly List<string> appendList = new List<string>();
            private readonly string baseDirectory;
            private readonly Encoding encoding;

            public RedirectionTextWriter(string baseDirectory, Encoding encoding)
            {
                this.baseDirectory = baseDirectory;
                this.encoding = encoding;
            }

            public void Add(string filename, bool appendMode)
            {
                if (appendMode == true)
                    this.appendList.Add(filename);
                else
                    this.writeList.Add(filename);
            }

            public override Encoding Encoding => this.encoding;

            protected override void Dispose(bool disposing)
            {
                var directory = Directory.GetCurrentDirectory();
                try
                {
                    if (Directory.Exists(baseDirectory) == false)
                        Directory.CreateDirectory(baseDirectory);
                    Directory.SetCurrentDirectory(baseDirectory);
                    foreach (var item in this.writeList)
                    {
                        File.WriteAllText(item, this.ToString());
                    }
                    foreach (var item in this.appendList)
                    {
                        File.AppendAllText(item, this.ToString());
                    }
                }
                finally
                {
                    Directory.SetCurrentDirectory(directory);
                    this.writeList.Clear();
                    this.appendList.Clear();
                }

                base.Dispose(disposing);
            }
        }

        #endregion
    }
}
