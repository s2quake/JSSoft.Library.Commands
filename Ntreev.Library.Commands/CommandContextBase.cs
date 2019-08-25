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
        private readonly CommandLineParserCollection parsers = new CommandLineParserCollection();
        private readonly CommandCollection commands = new CommandCollection();
        private readonly ICommandProvider[] commandProviders;
        private string name;
        private Version version;
        private TextWriter writer;
        private TextWriter errorWriter;
        private ICommand helpCommand;
        private ICommand versionCommand;
        private string category;
        private string baseDirectory;

        protected CommandContextBase(IEnumerable<ICommand> commands)
            : this(commands, new ICommandProvider[] { })
        {

        }

        protected CommandContextBase(IEnumerable<ICommand> commands, IEnumerable<ICommandProvider> commandProviders)
        {
            this.VerifyName = true;
            this.Out = defaultWriter;
            this.commandProviders = commandProviders.ToArray();

            foreach (var item in commands)
            {
                if (CommandSettings.IsConsoleMode == false && item.GetType().GetCustomAttribute<ConsoleModeOnlyAttribute>() != null)
                    continue;
                this.commands.Add(item);
                this.parsers.Add(item, this.CreateInstance(this, item));
            }
            this.parsers.Add(this.HelpCommand, this.CreateInstance(this, this.HelpCommand));
            this.parsers.Add(this.VersionCommand, this.CreateInstance(this, this.versionCommand));

            foreach (var item in commandProviders)
            {
                if (CommandSettings.IsConsoleMode == false && item.GetType().GetCustomAttribute<ConsoleModeOnlyAttribute>() != null)
                    continue;
                var command = commands.FirstOrDefault(i => i.Name == item.CommandName);
                if (command == null)
                    throw new CommandNotFoundException(item.CommandName);

                var descriptors = CommandDescriptor.GetMethodDescriptors(command);
                descriptors.AddRange(this.GetExternalMethodDescriptors(item));
            }

            foreach (var item in this.parsers)
            {
                item.VersionName = null;
                item.HelpName = null;
            }
        }

        public void Execute(string commandLine)
        {
            var segments = CommandStringUtility.Split(commandLine);

            var name = segments[0];
            var arguments = segments[1];

            if (File.Exists(name) == true)
                name = Process.GetCurrentProcess().ProcessName;

            if (this.VerifyName == true && this.Name != name)
                throw new ArgumentException(string.Format(Resources.InvalidCommandName_Format, name));

            arguments = this.InitializeRedirection(arguments);
            try
            {
                this.Execute(CommandStringUtility.Split(arguments));
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

        public virtual bool IsCommandEnabled(ICommand command)
        {
            if (this.HelpCommand == command)
                return false;
            if (this.VersionCommand == command)
                return false;
            if (this.parsers.Contains(command) == false)
                return false;

            var attr = command.GetType().GetCustomAttribute<CategoryAttribute>();
            var category = attr == null ? string.Empty : attr.Category;
            if (this.Category != category)
                return false;

            return command.IsEnabled;
        }

        public virtual bool IsMethodEnabled(ICommand command, CommandMethodDescriptor descriptor)
        {
            if (command is IExecutable == true)
                return false;
            if (this.IsCommandEnabled(command) == false)
                return false;
            if (command is CommandMethodBase commandMethod)
                return commandMethod.InvokeIsMethodEnabled(descriptor);
            return true;
        }

        public virtual string ReadString(string title)
        {
            var terminal = new Terminal();
            return terminal.ReadString(title);
        }

        public virtual SecureString ReadSecureString(string title)
        {
            var terminal = new Terminal();
            return terminal.ReadSecureString(title);
        }

        public void WriteList(string[] items)
        {
            if (this.Out is RedirectionTextWriter)
            {
                this.Out.WriteLine(string.Join(Environment.NewLine, items));
            }
            else
            {
                this.Out.Print(items);
            }
        }

        public void WriteList(TerminalTextItem[] items)
        {
            if (this.Out is RedirectionTextWriter)
            {
                var texts = items.Select(item => item.ToString()).ToArray();
                this.Out.WriteLine(string.Join(Environment.NewLine, texts));
            }
            else
            {
                this.Out.Print(items, (i, w, s) => i.Draw(w, s), (i) => i.ToString());
            }
        }

        public void WriteLine(string value)
        {
            this.Out.WriteLine(value);
        }

        public void WriteLine()
        {
            this.Out.WriteLine();
        }

        public void Write(string value)
        {
            this.Out.Write(value);
        }

        public TextWriter Out
        {
            get { return this.writer ?? Console.Out; }
            set
            {
                this.writer = value;
                foreach (var item in this.parsers)
                {
                    item.Out = value;
                }
            }
        }

        public TextWriter Error
        {
            get { return this.errorWriter ?? Console.Error; }
            set
            {
                this.errorWriter = value;
            }
        }

        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(this.name) == true)
                    return System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                return this.name;
            }
            set
            {
                this.name = value;
            }
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
            set
            {
                this.version = value;
            }
        }

        public CommandCollection Commands
        {
            get { return this.commands; }
        }

        public CommandLineParserCollection Parsers
        {
            get { return this.parsers; }
        }

        public ICommandProvider[] CommandProviders
        {
            get { return this.commandProviders; }
        }

        public virtual ICommand HelpCommand
        {
            get
            {
                if (this.helpCommand == null)
                    this.helpCommand = new HelpCommand(this);
                return this.helpCommand;
            }
        }

        public virtual ICommand VersionCommand
        {
            get
            {
                if (this.versionCommand == null)
                    this.versionCommand = new VersionCommand(this);
                return this.versionCommand;
            }
        }

        public bool VerifyName { get; set; }

        public string Category
        {
            get { return this.category ?? string.Empty; }
            set { this.category = value; }
        }

        public string BaseDirectory
        {
            get { return this.baseDirectory ?? Directory.GetCurrentDirectory(); }
            set { this.baseDirectory = value; }
        }

        public event EventHandler Executed;

        protected virtual CommandLineParser CreateInstance(ICommand command)
        {
            return new CommandLineParser(command.Name, command) { Out = this.Out };
        }

        protected virtual bool OnExecute(ICommand command, string arguments)
        {
            var parser = this.parsers[command];
            if (command is IExecutable == true)
            {
                if (parser.Parse(command.Name + " " + arguments) == false)
                {
                    return false;
                }

                (command as IExecutable).Execute();
            }
            else if (command is IExecutableAsync == true)
            {
                if (parser.Parse(command.Name + " " + arguments) == false)
                {
                    return false;
                }
                (command as IExecutableAsync).ExecuteAsync().Wait();
            }
            else
            {
                if (parser.Invoke(parser.Name + " " + arguments) == false)
                {
                    return false;
                }
            }
            this.OnExecuted(EventArgs.Empty);
            return true;
        }

        protected virtual void OnExecuted(EventArgs e)
        {
            this.Executed?.Invoke(this, e);
        }

        protected virtual string[] GetCompletion(string[] items, string find)
        {
            if (items.Length == 0)
            {
                var query = from item in this.Commands
                            let name = item.Name
                            where this.IsCommandEnabled(item)
                            where name.StartsWith(find)
                            select name;
                return query.ToArray();
            }
            else if (items.Length == 1)
            {
                var commandName = items[0];
                var command = this.GetCommand(commandName);
                if (command == null)
                    return null;
                if (command is IExecutable == false)
                {
                    var query = from item in CommandDescriptor.GetMethodDescriptors(command)
                                let name = item.Name
                                where this.IsMethodEnabled(command, item)
                                where name.StartsWith(find)
                                select name;
                    return query.ToArray();
                }
                else
                {
                    var memberList = new List<CommandMemberDescriptor>(CommandDescriptor.GetMemberDescriptors(command));
                    var argList = new List<string>(items.Skip(1));
                    var completionContext = new CommandCompletionContext(command, memberList, argList, find);
                    return this.GetCompletions(completionContext);
                }
            }
            else if (items.Length >= 2)
            {
                var commandName = items[0];
                var command = this.GetCommand(commandName);
                if (command == null)
                    return null;

                if (command is IExecutable == true)
                {
                    var memberList = new List<CommandMemberDescriptor>(CommandDescriptor.GetMemberDescriptors(command));
                    var argList = new List<string>(items.Skip(1));
                    var completionContext = new CommandCompletionContext(command, memberList, argList, find);
                    return this.GetCompletions(completionContext);
                }
                else
                {
                    var methodName = items[1];
                    var methodDescriptor = this.GetMethodDescriptor(command, methodName);
                    if (methodDescriptor == null)
                        return null;

                    if (methodDescriptor.Members.Length == 0)
                        return null;

                    var commandTarget = this.GetCommandTarget(command, methodDescriptor);
                    var memberList = new List<CommandMemberDescriptor>(methodDescriptor.Members);
                    var argList = new List<string>(items.Skip(2));
                    var completionContext = new CommandCompletionContext(command, methodDescriptor, memberList, argList, find);
                    if (completionContext.MemberDescriptor == null && find != string.Empty)
                        return this.GetCompletions(memberList, find);

                    return this.GetCompletions(completionContext);
                }
            }

            return null;
        }

        protected virtual string[] GetCompletions(CommandCompletionContext completionContext)
        {
            var query = from item in GetCompletionsCore() ?? new string[] { }
                        where item.StartsWith(completionContext.Find)
                        select item;
            return query.ToArray();

            string[] GetCompletionsCore()
            {
                if (completionContext.Command is CommandBase commandBase)
                {
                    return commandBase.GetCompletions(completionContext);
                }
                else if (completionContext.Command is CommandMethodBase commandMethodBase)
                {
                    return commandMethodBase.GetCompletions(completionContext.MethodDescriptor, completionContext.MemberDescriptor, completionContext.Find);
                }
                else if (completionContext.Command is CommandProviderBase consoleCommandProvider)
                {
                    return consoleCommandProvider.GetCompletions(completionContext.MethodDescriptor, completionContext.MemberDescriptor);
                }
                return null;
            }
        }

        private CommandMemberDescriptor FindMemberDescriptor(List<string> argList, List<CommandMemberDescriptor> memberList)
        {
            if (argList.Any())
            {
                var arg = argList.Last();
                var descriptor = this.FindMemberDescriptor(memberList, arg);
                if (descriptor != null)
                {
                    return descriptor;
                }
            }

            for (var i = 0; i < argList.Count; i++)
            {
                if (memberList.Any() == false)
                    break;
                var arg = argList[i];
                var member = memberList.First();
                if (member.IsRequired == true)
                    memberList.RemoveAt(0);
            }

            if (memberList.Any() == true && memberList.First().IsRequired == true)
            {
                return memberList.First();
            }
            return null;
        }

        private ICommand GetCommand(string commandName)
        {
            var commandNames = this.Commands.Select(item => item.Name).ToArray();
            if (Enumerable.Contains(commandNames, commandName) == true)
            {
                var command = this.Commands[commandName];
                if (this.IsCommandEnabled(command) == true)
                    return command;
            }
            if (commandName == this.HelpCommand.Name)
                return this.HelpCommand;
            return null;
        }

        private CommandMethodDescriptor GetMethodDescriptor(ICommand command, string methodName)
        {
            if (command is IExecutable == true)
                return null;
            var descriptors = CommandDescriptor.GetMethodDescriptors(command);
            if (descriptors.Contains(methodName) == false)
                return null;
            var descriptor = descriptors[methodName];
            if (this.IsMethodEnabled(command, descriptor) == false)
                return null;
            return descriptor;
        }

        private CommandMemberDescriptor FindMemberDescriptor(IEnumerable<CommandMemberDescriptor> descriptors, string argument)
        {
            foreach (var item in descriptors)
            {
                if (item.NamePattern == argument || item.ShortNamePattern == argument)
                {
                    return item;
                }
            }
            return null;
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

        private string[] GetCompletions(IEnumerable<CommandMemberDescriptor> descriptors, string find)
        {
            var patternList = new List<string>();
            foreach (var item in descriptors)
            {
                if (item.IsRequired == false)
                {
                    if (item.NamePattern != string.Empty)
                        patternList.Add(item.NamePattern);
                    if (item.ShortNamePattern != string.Empty)
                        patternList.Add(item.ShortNamePattern);
                }
            }
            return patternList.Where(item => item.StartsWith(find)).ToArray();
        }

        private bool Execute(string[] args)
        {
            var commandName = args[0];
            var arguments = args[1];

            if (commandName == string.Empty)
            {
                this.Out.WriteLine(Resources.HelpMessage_Format, string.Join(" ", new string[] { this.HelpCommand.Name }.Where(i => i != string.Empty).ToArray()));
                this.Out.WriteLine(Resources.VersionMessage_Format, string.Join(" ", new string[] { this.VersionCommand.Name }.Where(i => i != string.Empty).ToArray()));
                return false;
            }
            else if (commandName == this.HelpCommand.Name)
            {
                return this.OnExecute(this.HelpCommand, arguments);
            }
            else if (commandName == this.VersionCommand.Name)
            {
                return this.OnExecute(this.VersionCommand, arguments);
            }
            else if (this.commands.Contains(commandName) == true)
            {
                var command = this.commands[commandName];
                if (this.IsCommandEnabled(command) == true)
                    return this.OnExecute(command, arguments);
            }

            throw new ArgumentException(string.Format("'{0}' does not exsited command.", commandName));
        }

        private CommandLineParser CreateInstance(CommandContextBase commandContext, ICommand command)
        {
            var parser = this.CreateInstance(command);
            parser.CommandContext = commandContext;
            return parser;
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
