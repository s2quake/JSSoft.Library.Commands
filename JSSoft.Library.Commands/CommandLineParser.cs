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

        public bool TryParse(string commandLine)
        {
            var (name, arguments) = CommandStringUtility.Split(commandLine);
            return this.TryParse(name, arguments);
        }

        public bool TryParse(string name, string arguments)
        {
            try
            {
                this.Parse(name, arguments);
                return true;
            }
            catch (Exception e)
            {
                if (this.VerifyName(name) == true && this.Out != null)
                {
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
                }
                else if (this.VerifyName(name) == false && this.Error != null)
                {
                    this.Error.WriteLine(e.Message);
                }
                return false;
            }
        }

        public void Parse(string commandLine)
        {
            var (name, arguments) = CommandStringUtility.Split(commandLine);
            this.Parse(name, arguments);
        }

        public void Parse(string name, string arguments)
        {
            if (this.VerifyName(name) == false)
                throw new ArgumentException(string.Format(Resources.Exception_InvalidCommandName_Format, name));
            if (arguments == this.HelpName || arguments == this.VersionName)
                throw new ArgumentException();
            var descriptors = CommandDescriptor.GetMemberDescriptors(this.Instance).ToArray();
            var parser = new ParseDescriptor(descriptors, arguments);
            parser.SetValue(this.Instance);
        }

        public void ParseWith(string arguments)
        {
            this.Parse(this.Name, arguments);
        }

        public bool TryInvoke(string name, string arguments)
        {
            try
            {
                this.Invoke(name, arguments);
                return true;
            }
            catch (Exception e)
            {
                return this.PostTryInvoke(name, arguments, e);
            }
        }

        public bool TryInvoke(string commandLine)
        {
            var (name, arguments) = CommandStringUtility.Split(commandLine);
            return this.TryInvoke(name, arguments);
        }

        public bool TryInvokeWith(string arguments)
        {
            return this.TryInvoke(this.Name, arguments);
        }

        public void Invoke(string name, string arguments)
        {
            if (this.VerifyName(name) == false)
                throw new ArgumentException(string.Format(Resources.Exception_InvalidCommandName_Format, name));

            var (first, rest) = CommandStringUtility.Split(arguments);
            var instance = this.Instance;
            if (instance is ICommandHierarchy hierarchy && hierarchy.Commands.ContainsKey(first) == true)
            {
                var command = hierarchy.Commands[first];
                var parser = new CommandLineParser(first, arguments);
                var args = string.Join(" ", arguments);
                parser.Parse(args);
                if (command is IExecutable executable1)
                    this.Invoke(executable1);
                else if (command is IExecutableAsync executable2)
                    throw new InvalidOperationException("Asynchronous use in Invoke can pose a risk. Use InvokeAsync instead.");
            }
            else if (instance is IExecutable executable1)
            {
                this.Parse(name, arguments);
                this.Invoke(executable1);
            }
            else if (instance is IExecutableAsync executable2)
            {
                this.Parse(name, arguments);
                throw new InvalidOperationException("Asynchronous use in Invoke can pose a risk. Use InvokeAsync instead.");
            }
            else if (CommandDescriptor.GetMethodDescriptor(instance.GetType(), first) is CommandMethodDescriptor descriptor)
            {
                if (descriptor.IsAsync == true)
                {
                    throw new InvalidOperationException("Asynchronous use in Invoke can pose a risk. Use InvokeAsync instead.");
                }
                else
                {
                    this.Invoke(descriptor, instance, rest);
                }
            }
        }

        public void Invoke(string commandLine)
        {
            var (name, arguments) = CommandStringUtility.Split(commandLine);
            this.Invoke(name, arguments);
        }

        public void InvokeWith(string arguments)
        {
            this.Invoke(this.Name, arguments);
        }

        public Task<bool> TryInvokeAsync(string name, string arguments)
        {
            return this.TryInvokeAsync(name, arguments, new CancellationTokenRegistration().Token);
        }

        public async Task<bool> TryInvokeAsync(string name, string arguments, CancellationToken cancellationToken)
        {
            try
            {
                await this.InvokeAsync(name, arguments, cancellationToken);
                return true;
            }
            catch (Exception e)
            {
                return this.PostTryInvoke(name, arguments, e);
            }
        }

        public Task<bool> TryInvokeAsync(string commandLine)
        {
            return this.TryInvokeAsync(commandLine, new CancellationTokenSource().Token);
        }

        public Task<bool> TryInvokeAsync(string commandLine, CancellationToken cancellationToken)
        {
            var (name, arguments) = CommandStringUtility.Split(commandLine);
            return this.TryInvokeAsync(name, arguments);
        }

        public Task<bool> TryInvokeWithAsync(string arguments)
        {
            return this.TryInvokeWithAsync(arguments, new CancellationTokenSource().Token);
        }

        public Task<bool> TryInvokeWithAsync(string arguments, CancellationToken cancellationToken)
        {
            return this.TryInvokeAsync(this.Name, arguments, cancellationToken);
        }

        public Task InvokeAsync(string name, string arguments)
        {
            return this.InvokeAsync(name, arguments, new CancellationTokenSource().Token);
        }

        public async Task InvokeAsync(string name, string arguments, CancellationToken cancellationToken)
        {
            if (this.VerifyName(name) == false)
                throw new ArgumentException(string.Format(Resources.Exception_InvalidCommandName_Format, name));

            var (first, rest) = CommandStringUtility.Split(arguments);
            var instance = this.Instance;
            if (instance is ICommandHierarchy hierarchy && hierarchy.Commands.ContainsKey(first) == true)
            {
                var command = hierarchy.Commands[first];
                var parser = new CommandLineParser(first, arguments);
                var args = string.Join(" ", arguments);
                parser.Parse(args);
                if (command is IExecutable executable1)
                    this.Invoke(executable1);
                else if (command is IExecutableAsync executable2)
                    await this.InvokeAsync(executable2, cancellationToken);
            }
            else if (instance is IExecutable executable1)
            {
                this.Parse(name, arguments);
                this.Invoke(executable1);
            }
            else if (instance is IExecutableAsync executable2)
            {
                this.Parse(name, arguments);
                await this.InvokeAsync(executable2, cancellationToken);
            }
            else if (CommandDescriptor.GetMethodDescriptor(instance.GetType(), first) is CommandMethodDescriptor descriptor)
            {
                if (descriptor.IsAsync == true)
                    await this.InvokeAsync(descriptor, instance, rest);
                else
                    this.Invoke(descriptor, instance, rest);
            }
        }

        public Task InvokeAsync(string commandLine)
        {
            return this.InvokeAsync(commandLine, new CancellationTokenSource().Token);
        }

        public Task InvokeAsync(string commandLine, CancellationToken cancellationToken)
        {
            var (name, arguments) = CommandStringUtility.Split(commandLine);
            return this.InvokeAsync(name, arguments, cancellationToken);
        }

        public Task InvokeWithAsync(string arguments)
        {
            return this.InvokeWithAsync(arguments, new CancellationTokenSource().Token);
        }

        public Task InvokeWithAsync(string arguments, CancellationToken cancellationToken)
        {
            return this.InvokeAsync(this.Name, arguments, cancellationToken);
        }

        public void PrintSummary()
        {
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
                var descriptor = CommandMemberDescriptor.Find(memberDescriptors, memberName);
                if (descriptor == null)
                    throw new InvalidOperationException(string.Format(Resources.Exception_MemberDoesNotExist_Format, memberName));
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
            var methodDescriptors = CommandDescriptor.GetMethodDescriptors(instance.GetType());
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

        public string VersionName { get; set; } = "--version";

        public string Version { get; set; } = $"{new Version(1, 0)}";

        protected virtual void OnPrintSummary()
        {
            if (this.Out != null)
            {
                this.PrintSummary();
            }
        }

        protected virtual void OnPrintVersion()
        {
            if (this.Out != null)
            {
                this.PrintVersion();
            }
        }

        protected virtual void OnPrintUsage(string memberName)
        {
            if (this.Out != null)
            {
                this.PrintUsage(memberName);
            }
        }

        protected virtual void OnPrintMethodUsage(string methodName, string memberName)
        {
            if (this.Out != null)
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

        private bool PostTryInvoke(string name, string arguments, Exception e)
        {
            if (this.VerifyName(name) == true && this.Out != null)
            {
                var (first, rest) = CommandStringUtility.Split(arguments);
                if (first == this.HelpName)
                {
                    var (arg1, arg2) = CommandStringUtility.Split(rest);
                    this.OnPrintMethodUsage(arg1, arg2);
                }
                else if (first == this.VersionName)
                {
                    this.OnPrintVersion();
                }
                else
                {
                    this.OnPrintSummary();
                }

            }
            else if (this.VerifyName(name) == false && this.Error != null)
            {
                this.Error.WriteLine(e.Message);
            }
            return false;
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

        private void Invoke(CommandMethodDescriptor descriptor, object instance, string arguments)
        {
            descriptor.Invoke(instance, arguments, descriptor.Members);
        }

        private async Task InvokeAsync(CommandMethodDescriptor descriptor, object instance, string arguments)
        {
            if (descriptor.Invoke(instance, arguments, descriptor.Members) is Task task)
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
