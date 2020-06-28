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
        private Version version;
        private TextWriter writer;
        private CommandMemberUsagePrinter commandUsagePrinter;
        private CommandMethodUsagePrinter methodUsagePrinter;

        public CommandLineParser(object instance)
            : this(Path.GetFileName(Assembly.GetEntryAssembly().CodeBase), instance)
        {

        }

        public CommandLineParser(string name, object instance)
        {
            if (name == string.Empty)
                throw new ArgumentException("empty string not allowed.");
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            this.HelpName = "help";
            this.VersionName = "--version";
            this.Out = Console.Out;
        }

        public bool Parse(string commandLine)
        {
            return this.Parse(commandLine, CommandParsingTypes.None);
        }

        public bool Parse(string commandLine, CommandParsingTypes types)
        {
            if ((types & CommandParsingTypes.OmitCommandName) == CommandParsingTypes.OmitCommandName)
            {
                commandLine = $"{this.Name} {commandLine}";
            }

            var arguments = CommandStringUtility.Split(commandLine);
            var name = arguments[0];

            if (File.Exists(name) == true)
                name = Path.GetFileName(Assembly.GetEntryAssembly().CodeBase);
            if (this.Name != name)
                throw new ArgumentException(string.Format(Resources.InvalidCommandName_Format, name));

            var items = CommandStringUtility.Split(arguments[1]);

            if (items[0] == this.HelpName)
            {
                if (items[1] == string.Empty)
                    this.PrintUsage(this.Out);
                else
                    this.PrintUsage(this.Out, items[1]);
                return false;
            }
            else if (items[0] == this.VersionName)
            {
                this.PrintVersion(this.Out);
                return false;
            }
            else
            {
                var descriptors = CommandDescriptor.GetMemberDescriptors(this.Instance).Where(item => this.IsMemberEnabled(item));
                var omitInitialize = (types & CommandParsingTypes.OmitInitialize) == CommandParsingTypes.OmitInitialize;
                var parser = new ParseDescriptor(typeof(CommandPropertyDescriptor), descriptors, arguments[1], omitInitialize == false);
                parser.SetValue(this.Instance);
                return true;
            }
        }

        public bool Invoke(string commandLine)
        {
            return this.Invoke(commandLine, CommandParsingTypes.None);
        }

        public bool Invoke(string commandLine, CommandParsingTypes types)
        {
            if ((types & CommandParsingTypes.OmitCommandName) == CommandParsingTypes.OmitCommandName)
            {
                commandLine = $"{this.Name} {commandLine}";
            }

            var arguments = CommandStringUtility.Split(commandLine);
            var name = arguments[0];

            if (File.Exists(name) == true)
                name = Path.GetFileName(Assembly.GetEntryAssembly().CodeBase);
            if (this.Name != name)
                throw new ArgumentException(string.Format(Resources.InvalidCommandName_Format, name));

            var arguments1 = CommandStringUtility.Split(arguments[1]);
            var method = arguments1[0];

            if (string.IsNullOrEmpty(method) == true)
            {
                this.PrintSummary(this.Out);
                return false;
            }
            else if (method == this.HelpName)
            {
                var items = CommandStringUtility.Split(arguments1[1]);
                if (arguments1[1] == string.Empty)
                    this.PrintMethodUsage(this.Out);
                else if (items[1] == string.Empty)
                    this.PrintMethodUsage(this.Out, arguments1[1]);
                else
                    this.PrintMethodUsage(this.Out, items[0], items[1]);
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
                var descriptor = CommandDescriptor.GetMethodDescriptor(this.Instance, method);
                if (descriptor == null || this.IsMethodEnabled(descriptor) == false)
                    throw new CommandNotFoundException(method);
                if (descriptor is ExternalCommandMethodDescriptor externalDescriptor)
                    instance = externalDescriptor.Instance;
                var enabledDescriptors = descriptor.Members.Where(item => this.IsMemberEnabled(item));
                var omitInitialize = (types & CommandParsingTypes.OmitInitialize) == CommandParsingTypes.OmitInitialize;
                descriptor.Invoke(instance, arguments1[1], enabledDescriptors, omitInitialize == false);
                return true;
            }
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
            var enabledDescriptors = CommandDescriptor.GetMemberDescriptors(this.Instance).Where(item => this.IsMemberEnabled(item));
            this.MemberUsagePrinter.Print(writer, enabledDescriptors.ToArray());
        }

        public virtual void PrintUsage(TextWriter writer, string memberName)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (memberName == null)
                throw new ArgumentNullException(nameof(memberName));
            var descriptor = CommandDescriptor.GetMemberDescriptors(this.Instance)
                                              .Where(item => this.IsMemberEnabled(item))
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
            var info = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
            writer.WriteLine($"{name} {version}");
            writer.WriteLine(info.LegalCopyright);
        }

        public virtual void PrintMethodUsage(TextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            var descriptors = CommandDescriptor.GetMethodDescriptors(this.Instance).Where(item => this.IsMethodEnabled(item));
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
            if (descriptor == null || this.IsMethodEnabled(descriptor) == false)
                throw new CommandNotFoundException(methodName);

            var enabledDescriptors = descriptor.Members.Where(item => this.IsMemberEnabled(item)).ToArray();
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
            if (descriptor == null || this.IsMethodEnabled(descriptor) == false)
                throw new CommandNotFoundException(methodName);

            var visibleDescriptor = descriptor.Members.Where(item => this.IsMemberEnabled(item))
                                                      .FirstOrDefault(item => (item.IsRequired == true && memberName == item.Name) ||
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

        public string HelpName { get; set; }

        public string VersionName { get; set; }

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

        protected virtual bool IsMethodEnabled(CommandMethodDescriptor descriptor)
        {
            if (descriptor.Attributes.FirstOrDefault(item => item is BrowsableAttribute) is BrowsableAttribute attr && attr.Browsable == false)
                return false;
            if (this.CommandContext != null)
                return this.CommandContext.IsMethodEnabled(this.Instance as ICommand, descriptor);
            return true;
        }

        protected virtual bool IsMemberEnabled(CommandMemberDescriptor descriptor)
        {
            var attr = descriptor.Attributes.FirstOrDefault(item => item is BrowsableAttribute) as BrowsableAttribute;
            if (attr == null)
                return true;
            return attr.Browsable;
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

        internal bool IsMethodVisible(string methodName)
        {
            var descriptor = CommandDescriptor.GetMethodDescriptor(this.Instance, methodName);
            if (descriptor == null || this.IsMethodEnabled(descriptor) == false)
                return false;
            return true;
        }

        internal CommandContextBase CommandContext { get; set; }
    }
}