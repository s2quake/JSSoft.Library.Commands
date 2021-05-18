﻿// Released under the MIT License.
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Library.Commands
{
    public class CommandLineParser
    {
        private CommandMemberUsagePrinter commandUsagePrinter;
        private CommandMethodUsagePrinter methodUsagePrinter;
        private readonly FileVersionInfo versionInfo;
        private readonly string fullName;
        private readonly string filename;

        public CommandLineParser(object instance)
            : this(Assembly.GetEntryAssembly() ?? typeof(CommandLineParser).Assembly, instance)
        {
        }

        public CommandLineParser(Assembly assembly, object instance)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            this.Name = Path.GetFileNameWithoutExtension(assembly.Location);
            this.filename = Path.GetFileName(assembly.Location);
            this.fullName = assembly.Location;
            this.Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            this.versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            this.Version = this.versionInfo.ProductVersion;
        }

        public CommandLineParser(string name, object instance)
        {
            if (name == string.Empty)
                throw new ArgumentException(Resources.Exception_EmptyStringsAreNotAllowed);
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.fullName = name;
            this.filename = name;
            this.Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        public bool TryParseCommandLine(string commandLine)
        {
            var (name, args) = CommandStringUtility.SplitCommandLine(commandLine);
            if (this.VerifyName(name) == false)
                throw new ArgumentException(string.Format(Resources.Exception_InvalidCommandName_Format, name));
            return this.TryParse(args);
        }

        public bool TryParse(params string[] args)
        {
            try
            {
                this.Parse(args);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void ParseCommandLine(string commandLine)
        {
            var (name, args) = CommandStringUtility.SplitCommandLine(commandLine);
            if (this.VerifyName(name) == false)
                throw new ArgumentException(string.Format(Resources.Exception_InvalidCommandName_Format, name));
            this.Parse(args);
        }

        public void Parse(params string[] args)
        {
            var first = args.FirstOrDefault() ?? string.Empty;
            try
            {
                var descriptors = CommandDescriptor.GetMemberDescriptors(this.Instance).ToArray();
                var parser = new ParseDescriptor(descriptors, args);
                parser.SetValue(this.Instance);
            }
            catch (Exception e)
            {
                if (first == string.Empty)
                    throw new CommandParseException(CommandParseError.Empty, args, true, e);
                if (first == this.HelpName)
                    throw new CommandParseException(CommandParseError.Help, args, true, e);
                if (first == this.VersionName)
                    throw new CommandParseException(CommandParseError.Version, args, true, e);
                throw e;
            }
        }

        public void ParseArgumentLine(string argumentLine)
        {
            var args = CommandStringUtility.Split(argumentLine);
            this.Parse(args);
        }

        public bool TryInvoke(string[] args)
        {
            try
            {
                this.Invoke(args);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TryInvokeCommandLine(string commandLine)
        {
            var (name, args) = CommandStringUtility.SplitCommandLine(commandLine);
            if (this.VerifyName(name) == false)
                throw new ArgumentException(string.Format(Resources.Exception_InvalidCommandName_Format, name));
            return this.TryInvoke(args);
        }

        public bool TryInvokeArgumentLine(string argumentLine)
        {
            var args = CommandStringUtility.Split(argumentLine);
            return this.TryInvoke(args);
        }

        public void Invoke(params string[] args)
        {
            try
            {
                var first = args.FirstOrDefault() ?? string.Empty;
                var rest = args.Count() > 0 ? args.Skip(1).ToArray() : new string[] { };
                var instance = this.Instance;
                if (instance is ICommandHierarchy hierarchy && hierarchy.Commands.ContainsKey(first) == true)
                {
                    var command = hierarchy.Commands[first];
                    var parser = new CommandLineParser(first, command);
                    parser.Parse(rest);
                    if (command is IExecutable executable1)
                        this.Invoke(executable1);
                    else if (command is IExecutableAsync executable2)
                        throw new InvalidOperationException(Resources.Exception_InvokeAsyncInstead);
                }
                else if (instance is IExecutable executable1)
                {
                    this.Parse(args);
                    this.Invoke(executable1);
                }
                else if (instance is IExecutableAsync executable2)
                {
                    this.Parse(args);
                    throw new InvalidOperationException(Resources.Exception_InvokeAsyncInstead);
                }
                else if (CommandDescriptor.GetMethodDescriptor(instance.GetType(), first) is CommandMethodDescriptor descriptor)
                {
                    if (descriptor.IsAsync == true)
                    {
                        throw new InvalidOperationException(Resources.Exception_InvokeAsyncInstead);
                    }
                    else
                    {
                        this.Invoke(descriptor, instance, rest);
                    }
                }
            }
            catch (Exception e)
            {
                // if (this.VerifyName(name) == true)
                {
                    var first = args.FirstOrDefault() ?? string.Empty;
                    var rest = args.Count() > 0 ? args.Skip(1).ToArray() : new string[] { };
                    if (first == this.HelpName)
                    {
                        throw new CommandParseException(CommandParseError.Help, args, false, e);
                    }
                    else if (first == this.VersionName)
                    {
                        throw new CommandParseException(CommandParseError.Version, args, false, e);
                    }
                    else
                    {
                        throw new CommandParseException(CommandParseError.Empty, args, false, e);
                    }
                }
                throw;
            }
        }

        public void InvokeCommandLine(string commandLine)
        {
            var (name, args) = CommandStringUtility.SplitCommandLine(commandLine);
            if (this.VerifyName(name) == false)
                throw new ArgumentException(string.Format(Resources.Exception_InvalidCommandName_Format, name));
            this.Invoke(args);
        }

        public void InvokeArgumentLine(string argumentLine)
        {
            var args = CommandStringUtility.Split(argumentLine);
            this.Invoke(args);
        }

        public Task<bool> TryInvokeAsync(string[] args)
        {
            return this.TryInvokeAsync(args, new CancellationTokenSource().Token);
        }

        public async Task<bool> TryInvokeAsync(string[] args, CancellationToken cancellationToken)
        {
            try
            {
                await this.InvokeAsync(args, cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Task<bool> TryInvokeCommandLineAsync(string commandLine)
        {
            return this.TryInvokeCommandLineAsync(commandLine, new CancellationTokenSource().Token);
        }

        public Task<bool> TryInvokeCommandLineAsync(string commandLine, CancellationToken cancellationToken)
        {
            var (name, args) = CommandStringUtility.SplitCommandLine(commandLine);
            if (this.VerifyName(name) == false)
                throw new ArgumentException(string.Format(Resources.Exception_InvalidCommandName_Format, name));
            return this.TryInvokeAsync(args);
        }

        public Task<bool> TryInvokeArgumentLineAsync(string argumentLine)
        {
            return this.TryInvokeArgumentLineAsync(argumentLine, new CancellationTokenSource().Token);
        }

        public Task<bool> TryInvokeArgumentLineAsync(string argumentLine, CancellationToken cancellationToken)
        {
            var args = CommandStringUtility.Split(argumentLine);
            return this.TryInvokeAsync(args, cancellationToken);
        }

        public Task InvokeAsync(string[] args)
        {
            return this.InvokeAsync(args, new CancellationTokenSource().Token);
        }

        public async Task InvokeAsync(string[] args, CancellationToken cancellationToken)
        {
            // if (this.VerifyName(name) == false)
            //     throw new ArgumentException(string.Format(Resources.Exception_InvalidCommandName_Format, name));

            var first = args.FirstOrDefault() ?? string.Empty;
            var rest = args.Count() > 0 ? args.Skip(1).ToArray() : new string[] { };
            var instance = this.Instance;
            if (instance is ICommandHierarchy hierarchy && hierarchy.Commands.ContainsKey(first) == true)
            {
                var command = hierarchy.Commands[first];
                var parser = new CommandLineParser(first, command);
                parser.Parse(rest);
                if (command is IExecutable executable1)
                    this.Invoke(executable1);
                else if (command is IExecutableAsync executable2)
                    await this.InvokeAsync(executable2, cancellationToken);
            }
            else if (instance is IExecutable executable1)
            {
                this.Parse(args);
                this.Invoke(executable1);
            }
            else if (instance is IExecutableAsync executable2)
            {
                this.Parse(args);
                await this.InvokeAsync(executable2, cancellationToken);
            }
            else if (first != string.Empty && CommandDescriptor.GetMethodDescriptor(instance.GetType(), first) is CommandMethodDescriptor descriptor)
            {
                if (descriptor.IsAsync == true)
                    await this.InvokeAsync(descriptor, instance, rest);
                else
                    this.Invoke(descriptor, instance, rest);
            }
        }

        public Task InvokeCommandLineAsync(string commandLine)
        {
            return this.InvokeCommandLineAsync(commandLine, new CancellationTokenSource().Token);
        }

        public Task InvokeCommandLineAsync(string commandLine, CancellationToken cancellationToken)
        {
            var (name, args) = CommandStringUtility.SplitCommandLine(commandLine);
            if (this.VerifyName(name) == false)
                throw new ArgumentException(string.Format(Resources.Exception_InvalidCommandName_Format, name));
            return this.InvokeAsync(args, cancellationToken);
        }

        public Task InvokeArgumentLineAsync(string argumentLine)
        {
            return this.InvokeArgumentLineAsync(argumentLine, new CancellationTokenSource().Token);
        }

        public Task InvokeArgumentLineAsync(string argumentLine, CancellationToken cancellationToken)
        {
            var args = CommandStringUtility.Split(argumentLine);
            return this.InvokeAsync(args, cancellationToken);
        }

        public void PrintException(Exception e)
        {
            if (e is CommandParseException ex)
            {
                if (this.Out == null)
                    throw e;
                var commandLine = CommandStringUtility.Join(ex.Arguments);
                var isParse = ex.IsParse;
                switch (ex.Error)
                {
                    case CommandParseError.Empty:
                        {
                            this.PrintSummary(isParse);
                        }
                        break;
                    case CommandParseError.Help:
                        {
                            if (isParse == true)
                                this.PrintParseHelp(commandLine);
                            else
                                this.PrintInvokeHelp(commandLine);
                        }
                        break;
                    case CommandParseError.Version:
                        {
                            this.PrintVersion(commandLine);
                        }
                        break;
                }
            }
            else
            {
                if (this.Error != null)
                    this.Error.WriteLine(e.Message);
                else
                    throw e;
            }
        }

        public void PrintSummary(bool isParse)
        {
            var helpName = this.HelpName;
            var name = this.ExecutionName;
            var versionName = this.VersionName;
            {
                var parser = new CommandLineParser($"{name} {helpName}", isParse == true ? new HelpParseInstance() : new HelpInvokeInstance() as object)
                {
                    Out = this.Out
                };
                parser.PrintUsage(string.Empty, CommandUsage.None);
            }
            {
                var parser = new CommandLineParser($"{name} {versionName}", new VersionInstance())
                {
                    Out = this.Out
                };
                parser.PrintUsage(string.Empty, CommandUsage.None);
            }
            this.Out.WriteLine(Resources.Message_HelpUsage_Format, name, helpName);
            this.Out.WriteLine(Resources.Message_VersionUsage_Format, name, versionName);
            this.Out.WriteLine();
        }

        public void PrintVersion(bool isQuiet)
        {
            var name = this.ExecutionName;
            var version = this.Version;
            var versionInfo = this.versionInfo;
            var writer = this.Out;
            if (isQuiet == false)
            {
                writer.WriteLine($"{name} {version}");
                if (versionInfo != null)
                    writer.WriteLine(versionInfo.LegalCopyright);
            }
            else
            {
                writer.WriteLine($"{version}");
            }
        }

        public void PrintUsage(string memberName, CommandUsage usage)
        {
            if (memberName == null)
                throw new ArgumentNullException(nameof(memberName));
            var instance = this.Instance;
            var printer = this.MemberUsagePrinter;
            var writer = this.Out;
            var memberDescriptors = CommandDescriptor.GetMemberDescriptors(instance);
            printer.Usage = usage;
            if (memberName == string.Empty)
            {
                printer.Print(writer, memberDescriptors.ToArray());
            }
            else
            {
                var descriptor = CommandMemberDescriptor.Find(memberDescriptors, memberName);
                if (descriptor == null)
                    throw new InvalidOperationException(string.Format(Resources.Exception_MemberDoesNotExist_Format, memberName));
                printer.Print(writer, descriptor);
            }
        }

        public void PrintMethodUsage(string methodName, string memberName, CommandUsage usage)
        {
            if (methodName == null)
                throw new ArgumentNullException(nameof(methodName));
            if (memberName == null)
                throw new ArgumentNullException(nameof(memberName));
            var instance = this.Instance;
            var printer = this.MethodUsagePrinter;
            var writer = this.Out;
            var methodDescriptors = CommandDescriptor.GetMethodDescriptors(instance.GetType());
            printer.Usage = usage;
            if (methodName == string.Empty)
            {
                printer.Print(writer, methodDescriptors.ToArray());
            }
            else
            {
                var methodDescriptor = methodDescriptors.FirstOrDefault(item => item.Name == methodName);
                if (methodDescriptor == null)
                    throw new CommandNotFoundException(methodName);
                if (memberName == string.Empty)
                {
                    printer.Print(writer, methodDescriptor, methodDescriptor.Members);
                }
                else
                {
                    var memberDescriptor = CommandMemberDescriptor.Find(methodDescriptor.Members, memberName);
                    if (memberDescriptor == null)
                        throw new InvalidOperationException(string.Format(Resources.Exception_MemberDoesNotExist_Format, memberName));
                    printer.Print(writer, methodDescriptor, memberDescriptor);
                }
            }
        }

        public TextWriter Out { get; set; } = Console.Out;

        public TextWriter Error { get; set; } = Console.Error;

        public string Name { get; }

        public object Instance { get; }

        public string HelpName { get; set; } = "help";

        public string VersionName { get; set; } = "version";

        public string Version { get; set; } = $"{new Version(1, 0)}";

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

        protected virtual CommandMemberUsagePrinter CreateMemberUsagePrinter(string name, object instance)
        {
            return new CommandMemberUsagePrinter(name, instance);
        }

        protected virtual CommandMethodUsagePrinter CreateMethodUsagePrinter(string name, object instance)
        {
            return new CommandMethodUsagePrinter(name, instance);
        }

        private CommandMemberUsagePrinter MemberUsagePrinter
        {
            get
            {
                if (this.commandUsagePrinter == null)
                    this.commandUsagePrinter = this.CreateMemberUsagePrinter(this.ExecutionName, this.Instance);
                return this.commandUsagePrinter;
            }
        }

        private CommandMethodUsagePrinter MethodUsagePrinter
        {
            get
            {
                if (this.methodUsagePrinter == null)
                    this.methodUsagePrinter = this.CreateMethodUsagePrinter(this.ExecutionName, this.Instance);
                return this.methodUsagePrinter;
            }
        }

        private void PrintVersion(string commandLine)
        {
            var instance = new VersionInstance();
            var parser = new CommandLineParser(this.VersionName, instance)
            {
                Out = this.Out
            };
            parser.ParseCommandLine(commandLine);
            this.PrintVersion(instance.IsQuiet);
        }

        private void PrintParseHelp(string commandLine)
        {
            var instance = new HelpParseInstance();
            var parser = new CommandLineParser(this.HelpName, instance)
            {
                Out = this.Out
            };
            parser.ParseCommandLine(commandLine);
            this.PrintUsage(instance.OptionName, instance.Usage);
        }

        private void PrintInvokeHelp(string commandLine)
        {
            var (arg1, arg2) = CommandStringUtility.SplitCommandLine(commandLine);
            var instance = new HelpInvokeInstance();
            var parser = new CommandLineParser(this.HelpName, instance)
            {
                Out = this.Out
            };
            parser.ParseCommandLine(commandLine);
            this.PrintMethodUsage(instance.SubCommand, instance.OptionName, instance.Usage);
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

        private void Invoke(IExecutable executable)
        {
            executable.Execute();
        }

        private async Task InvokeAsync(IExecutableAsync executable, CancellationToken cancellationToken)
        {
            await executable.ExecuteAsync(cancellationToken);
        }

        private void Invoke(CommandMethodDescriptor descriptor, object instance, string[] args)
        {
            descriptor.Invoke(instance, args, descriptor.Members);
        }

        private async Task InvokeAsync(CommandMethodDescriptor descriptor, object instance, string[] args)
        {
            if (descriptor.Invoke(instance, args, descriptor.Members) is Task task)
            {
                await task;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
