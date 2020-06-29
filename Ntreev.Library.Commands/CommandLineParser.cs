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
        private TextWriter writer;
        private CommandMemberUsagePrinter commandUsagePrinter;
        private CommandMethodUsagePrinter methodUsagePrinter;

        public CommandLineParser(object instance)
            : this(Assembly.GetEntryAssembly(), instance)
        {

        }

        public CommandLineParser(Assembly assembly, object instance)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            this.Name = assembly.Location;
            this.Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            this.Version = new Version(FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion);
        }

        public CommandLineParser(string name, object instance)
        {
            if (name == string.Empty)
                throw new ArgumentException("empty string not allowed.");
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        public bool Parse(string commandLine)
        {
            var (name, command) = CommandStringUtility.Split(commandLine);
            return this.Parse(name, command);
        }

        public bool Parse(string name, string command)
        {
            if (this.Name != name)
                throw new ArgumentException(string.Format(Resources.InvalidCommandName_Format, name));

            var (first, rest) = CommandStringUtility.Split(command);
            if (first == this.HelpName)
            {
                if (rest == string.Empty)
                    this.PrintUsage(this.Out);
                else
                    this.PrintUsage(this.Out, rest);
                return false;
            }
            else if (first == this.VersionName)
            {
                this.PrintVersion(this.Out);
                return false;
            }
            else
            {
                var descriptors = CommandDescriptor.GetMemberDescriptors(this.Instance).ToArray();
                // var omitInitialize = (types & CommandParsingTypes.OmitInitialize) == CommandParsingTypes.OmitInitialize;
                var parser = new ParseDescriptor(typeof(CommandPropertyDescriptor), descriptors, command, false);
                parser.SetValue(this.Instance);
                return true;
            }
        }

        public bool Parse(string commandLine, CommandParsingTypes types)
        {
            return this.Parse(commandLine);
        }

        public bool Invoke(string name, string command)
        {
            if (this.Name != name)
                throw new ArgumentException(string.Format(Resources.InvalidCommandName_Format, name));

            var (method, arguments) = CommandStringUtility.Split(command);
            var isSwitch = CommandStringUtility.IsSwitch(method);

            if (method == string.Empty)
            {
                // if (this.Parse(commandLine) == true)
                // {
                //     if (this.Instance is IExecutable executable)
                //     {
                //         executable.Execute();
                //     }
                // }
                return false;
            }
            else if (method == this.HelpName)
            {
                var (first, rest) = CommandStringUtility.Split(arguments);
                if (rest == string.Empty)
                    this.PrintMethodUsage(this.Out);
                else if (rest == string.Empty)
                    this.PrintMethodUsage(this.Out, rest);
                else
                    this.PrintMethodUsage(this.Out, first, rest);
                return false;
            }
            else if (method == this.VersionName)
            {
                this.PrintVersion(this.Out);
                return false;
            }
            else
            {
                var instance = this.Instance;
                if (instance is ICommandNode commandNode && commandNode.Commands.ContainsKey(method) == true)
                {
                    var cmd = commandNode.Commands[method];
                    var parser = new CommandLineParser(method, command);
                    var args = string.Join(" ", arguments);
                    if (parser.Parse(args) == false)
                        return false;
                    if (cmd is IExecutable executable)
                        executable.Execute();
                }
                else if (instance is IExecutable executable1)
                {
                    if (this.Parse(command) == true)
                    {
                        executable1.Execute();
                        return true;
                    }
                }

                // var descriptor = CommandDescriptor.GetMethodDescriptor(this.Instance, method);
                // if (descriptor == null || this.IsMethodEnabled(descriptor) == false)
                //     throw new CommandNotFoundException(method);
                // if (descriptor is ExternalCommandMethodDescriptor externalDescriptor)
                //     instance = externalDescriptor.Instance;
                // var enabledDescriptors = descriptor.Members.Where(item => this.IsMemberEnabled(item));
                // var omitInitialize = (types & CommandParsingTypes.OmitInitialize) == CommandParsingTypes.OmitInitialize;
                // descriptor.Invoke(instance, arguments1[1], enabledDescriptors, omitInitialize == false);
                return true;
            }
        }

        public bool Invoke(string commandLine)
        {
            var (name, command) = CommandStringUtility.Split(commandLine);
            return this.Invoke(name, command);

        }

        public bool Invoke(string commandLine, CommandParsingTypes types)
        {
            // if ((types & CommandParsingTypes.OmitCommandName) == CommandParsingTypes.OmitCommandName)
            // {
            //     commandLine = $"{this.Name} {commandLine}";
            // }
            return this.Invoke(commandLine);
            
        }

        public virtual void PrintSummary(TextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (this.CommandContext != null)
                writer.WriteLine("Type '{0} {1}' for usage.", this.CommandContext.HelpCommand.Name, this.Name);
            else
                writer.WriteLine("Type '{0} {1}' for usage.", this.Name, this.HelpName);
        }

        public virtual void PrintUsage(TextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            var enabledDescriptors = CommandDescriptor.GetMemberDescriptors(this.Instance);
            this.MemberUsagePrinter.Print(writer, enabledDescriptors.ToArray());
        }

        public virtual void PrintUsage(TextWriter writer, string memberName)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (memberName == null)
                throw new ArgumentNullException(nameof(memberName));
            var descriptor = CommandDescriptor.GetMemberDescriptors(this.Instance)
                                              .FirstOrDefault(item => (item.IsRequired == true && memberName == item.Name) ||
                                                                       memberName == item.NamePattern ||
                                                                       memberName == item.ShortNamePattern);
            if (descriptor == null)
                throw new InvalidOperationException(string.Format(Resources.MemberDoesNotExist_Format, memberName));
            this.MemberUsagePrinter.Print(writer, descriptor);
        }

        public virtual void PrintVersion(TextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            var name = this.Name;
            var version = this.Version;
            // var info = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
            writer.WriteLine($"{name} {version}");
            // writer.WriteLine(info.LegalCopyright);
        }

        public virtual void PrintMethodUsage(TextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            var descriptors = CommandDescriptor.GetMethodDescriptors(this.Instance);
            this.MethodUsagePrinter.Print(writer, descriptors.ToArray());
        }

        public virtual void PrintMethodUsage(TextWriter writer, string methodName)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (methodName == null)
                throw new ArgumentNullException(nameof(methodName));
            var descriptors = CommandDescriptor.GetMethodDescriptors(this.Instance);
            var descriptor = descriptors.FirstOrDefault(item => item.Name == methodName);
            if (descriptor == null)
                throw new CommandNotFoundException(methodName);

            var enabledDescriptors = descriptor.Members.ToArray();
            this.MethodUsagePrinter.Print(writer, descriptor, enabledDescriptors);
        }

        public virtual void PrintMethodUsage(TextWriter writer, string methodName, string memberName)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (methodName == null)
                throw new ArgumentNullException(nameof(methodName));
            if (memberName == null)
                throw new ArgumentNullException(nameof(memberName));
            var descriptors = CommandDescriptor.GetMethodDescriptors(this.Instance);
            var descriptor = descriptors.FirstOrDefault(item => item.Name == methodName);
            if (descriptor == null)
                throw new CommandNotFoundException(methodName);

            var visibleDescriptor = descriptor.Members.FirstOrDefault(item => (item.IsRequired == true && memberName == item.Name) ||
                                                                               memberName == item.NamePattern ||
                                                                               memberName == item.ShortNamePattern);

            if (visibleDescriptor == null)
                throw new InvalidOperationException(string.Format(Resources.MemberDoesNotExist_Format, memberName));

            this.MethodUsagePrinter.Print(writer, descriptor, visibleDescriptor);
        }

        public TextWriter Out
        {
            get => this.writer ?? Console.Out;
            set => this.writer = value;
        }

        public string Name { get; }

        public object Instance { get; }

        public string HelpName { get; set; } = "help";

        public string VersionName { get; set; } = "--version";

        public Version Version {get;set;} = new Version(1, 0);

        // protected virtual bool IsMethodEnabled(CommandMethodDescriptor descriptor)
        // {
        //     if (descriptor.Attributes.FirstOrDefault(item => item is BrowsableAttribute) is BrowsableAttribute attr && attr.Browsable == false)
        //         return false;
        //     if (this.CommandContext != null)
        //         return this.CommandContext.IsMethodEnabled(this.Instance as ICommand, descriptor);
        //     return true;
        // }

        // protected virtual bool IsMemberEnabled(CommandMemberDescriptor descriptor)
        // {
        //     var attr = descriptor.Attributes.FirstOrDefault(item => item is BrowsableAttribute) as BrowsableAttribute;
        //     if (attr == null)
        //         return true;
        //     return attr.Browsable;
        // }

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

        // internal bool IsMethodVisible(string methodName)
        // {
        //     var descriptor = CommandDescriptor.GetMethodDescriptor(this.Instance, methodName);
        //     if (descriptor == null || this.IsMethodEnabled(descriptor) == false)
        //         return false;
        //     return true;
        // }

        internal CommandContextBase CommandContext { get; set; }
    }
}