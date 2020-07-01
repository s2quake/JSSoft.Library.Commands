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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using Ntreev.Library.Commands.Properties;
using System.Reflection;
using System.Diagnostics;
using System.Collections;

namespace Ntreev.Library.Commands
{
    public class CommandLineParser
    {
        private TextWriter writer = Console.Out;
        private CommandMemberUsagePrinter commandUsagePrinter;
        private CommandMethodUsagePrinter methodUsagePrinter;
        private FileVersionInfo versionInfo;
        private string fullName;

        public CommandLineParser(object instance)
            : this(Assembly.GetEntryAssembly(), instance)
        {

        }

        public CommandLineParser(Assembly assembly, object instance)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            this.Name = Path.GetFileName(assembly.Location);
            this.fullName = assembly.Location;
            this.Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            this.versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            this.Version = new Version(this.versionInfo.ProductVersion);
        }

        public CommandLineParser(string name, object instance)
        {
            if (name == string.Empty)
                throw new ArgumentException("empty string not allowed.");
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.fullName = name;
            this.Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        public bool Parse(string commandLine)
        {
            var (name, arguments) = CommandStringUtility.Split(commandLine);
            return this.Parse(name, arguments);
        }

        public bool Parse(string name, string arguments)
        {
            if (this.Name != name && this.fullName != name)
                throw new ArgumentException(string.Format(Resources.InvalidCommandName_Format, name));

            var (first, rest) = CommandStringUtility.Split(arguments);
            if (first == string.Empty)
            {
                this.OnPrintSummary();
            }
            else if (first == this.HelpName)
            {
                this.OnPrintUsage(rest);
            }
            else if (first == this.VersionName)
            {
                this.OnPrintVersion();
            }
            else
            {
                var descriptors = CommandDescriptor.GetMemberDescriptors(this.Instance).ToArray();
                var parser = new ParseDescriptor(typeof(CommandPropertyDescriptor), descriptors, arguments, false);
                parser.SetValue(this.Instance);
                return true;
            }
            return false;
        }

        public bool Invoke(string name, string arguments)
        {
            if (this.Name != name && this.fullName != name)
                throw new ArgumentException(string.Format(Resources.InvalidCommandName_Format, name));

            var (first, rest) = CommandStringUtility.Split(arguments);
            var isSwitch = CommandStringUtility.IsSwitch(first);
            var instance = this.Instance;
            if (first == this.HelpName)
            {
                var (arg1, arg2) = CommandStringUtility.Split(rest);
                this.OnPrintMethodUsage(arg1, arg2);
            }
            else if (first == this.VersionName)
            {
                this.OnPrintVersion();
            }
            else if (instance is ICommandNode commandNode && commandNode.Commands.ContainsKey(first) == true)
            {
                var command = commandNode.Commands[first];
                var parser = new CommandLineParser(first, arguments);
                var args = string.Join(" ", arguments);
                if (parser.Parse(args) == false)
                    return false;
                if (command is IExecutable executable1)
                    executable1.Execute();
                else if (command is IExecutableAsync executable2)
                    executable2.ExecuteAsync().Wait();
            }
            else if (instance is IExecutable executable1)
            {
                if (arguments == string.Empty || this.Parse(name, arguments) == true)
                {
                    executable1.Execute();
                    return true;
                }
            }
            else if (instance is IExecutableAsync executable2)
            {
                if (arguments == string.Empty || this.Parse(name, arguments) == true)
                {
                    executable2.ExecuteAsync().Wait();
                    return true;
                }
            }
            else if (CommandDescriptor.GetMethodDescriptor(instance, first) is CommandMethodDescriptor descriptor)
            {
                if (descriptor is ExternalCommandMethodDescriptor externalDescriptor)
                    instance = externalDescriptor.Instance;
                var enabledDescriptors = descriptor.Members;
                descriptor.Invoke(instance, arguments, enabledDescriptors, false);
                return true;
            }
            else
            {
                this.OnPrintSummary();
            }
            return false;
        }

        public bool Invoke(string commandLine)
        {
            var (name, arguments) = CommandStringUtility.Split(commandLine);
            return this.Invoke(name, arguments);
        }

        public void PrintSummary()
        {
            // if (this.CommandContext != null)
            //     this.Out.WriteLine("Type '{0} {1}' for usage.", this.CommandContext.HelpCommand.Name, this.Name);
            // else
                this.Out.WriteLine("Type '{0} {1}' for usage.", this.Name, this.HelpName);
        }

        public void PrintVersion()
        {
            var name = this.Name;
            var version = this.Version;
            var versionInfo = this.versionInfo;
            var writer = this.Out;
            writer.WriteLine($"{name} {version}");
            if (versionInfo != null)
                writer.WriteLine(versionInfo.LegalCopyright);
        }

        public void PrintUsage(string memberName)
        {
            if (memberName == null)
                throw new ArgumentNullException(nameof(memberName));
            var instance = this.Instance;
            var printer = this.MemberUsagePrinter;
            var writer = this.Out;
            var memberDescriptors = CommandDescriptor.GetMemberDescriptors(instance);
            if (memberName == string.Empty)
            {
                printer.Print(writer, memberDescriptors.ToArray());
            }
            else
            {
                var descriptor = memberDescriptors.Find(memberName);
                if (descriptor == null)
                    throw new InvalidOperationException(string.Format(Resources.MemberDoesNotExist_Format, memberName));
                printer.Print(writer, descriptor);
            }
        }

        public void PrintMethodUsage(string methodName, string memberName)
        {
            if (methodName == null)
                throw new ArgumentNullException(nameof(methodName));
            if (memberName == null)
                throw new ArgumentNullException(nameof(memberName));
            var instance = this.Instance;
            var printer = this.MethodUsagePrinter;
            var writer = this.Out;
            var methodDescriptors = CommandDescriptor.GetMethodDescriptors(instance);
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
                    var memberDescriptor = methodDescriptor.Members.Find(memberName);
                    if (memberDescriptor == null)
                        throw new InvalidOperationException(string.Format(Resources.MemberDoesNotExist_Format, memberName));
                    printer.Print(writer, methodDescriptor, memberDescriptor);
                }
            }
        }

        public TextWriter Out
        {
            get => this.writer;
            set => this.writer = value;
        }

        public string Name { get; }

        public object Instance { get; }

        public string HelpName { get; set; } = "help";

        public string VersionName { get; set; } = "--version";

        public Version Version { get; set; } = new Version(1, 0);

        protected virtual void OnPrintSummary()
        {
            if (this.writer != null)
            {
                this.PrintSummary();
            }
        }

        protected virtual void OnPrintVersion()
        {
            if (this.writer != null)
            {
                this.PrintVersion();
            }
        }

        protected virtual void OnPrintUsage(string memberName)
        {
            if (this.writer != null)
            {
                this.PrintUsage(memberName);
            }
        }

        protected virtual void OnPrintMethodUsage(string methodName, string memberName)
        {
            if (this.writer != null)
            {
                this.PrintMethodUsage(methodName, memberName);
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
                    this.commandUsagePrinter = this.CreateMemberUsagePrinter(this.Name, this.Instance);
                return this.commandUsagePrinter;
            }
        }

        private CommandMethodUsagePrinter MethodUsagePrinter
        {
            get
            {
                if (this.methodUsagePrinter == null)
                    this.methodUsagePrinter = this.CreateMethodUsagePrinter(this.Name, this.Instance);
                return this.methodUsagePrinter;
            }
        }

        // internal CommandContextBase CommandContext { get; set; }
    }
}