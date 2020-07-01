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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Ntreev.Library.Commands
{
    public class CommandContextTerminal : Terminal
    {
        private readonly static string[] emptyStrings = new string[] { };
        private readonly CommandContextBase commandContext;
        private string prompt;
        private string prefix;
        private string postfix;

        public CommandContextTerminal(CommandContextBase commandContext)
        {
            this.commandContext = commandContext;
        }

        public new string Prompt
        {
            get => this.prompt ?? string.Empty;
            set
            {
                this.prompt = value;
                if (this.IsReading == true)
                {
                    this.SetPrompt(this.Prefix + this.Prompt + this.Postfix);
                }
            }
        }

        public string Prefix
        {
            get => this.prefix ?? string.Empty;
            set
            {
                this.prefix = value;
                if (this.IsReading == true)
                {
                    this.SetPrompt(this.Prefix + this.Prompt + this.Postfix);
                }
            }
        }

        public string Postfix
        {
            get => this.postfix ?? string.Empty;
            set
            {
                this.postfix = value;
                if (this.IsReading == true)
                {
                    this.SetPrompt(this.Prefix + this.Prompt + this.Postfix);
                }
            }
        }

        public new void Cancel()
        {
            this.IsCancellationRequested = true;
            base.Cancel();
        }

        public void Start()
        {
            string line;
            while ((line = this.ReadStringInternal(this.Prefix + this.Prompt + this.Postfix)) != null)
            {
                try
                {
                    this.commandContext.Execute(this.commandContext.Name + " " + line);
                }
                catch (TargetInvocationException e)
                {
                    this.WriteException(e.InnerException != null ? e.InnerException : e);
                }
                catch (AggregateException e)
                {
                    foreach (var item in e.InnerExceptions)
                    {
                        this.WriteException(item);
                    }
                }
                catch (Exception e)
                {
                    this.WriteException(e);
                }
                if (this.IsCancellationRequested == true)
                    break;
            }
        }

        public bool IsCancellationRequested { get; private set; }

        public bool DetailErrorMessage { get; set; }

        protected override string[] GetCompletion(string[] items, string find)
        {
            return this.commandContext.GetCompletionInternal(items, find);
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
            var commandNames = this.commandContext.Commands.Select(item => item.Name).ToArray();
            if (commandNames.Contains(commandName) == true)
            {
                var command = this.commandContext.Commands[commandName];
                if (command.IsEnabled == true)
                    return command;
            }
            if (commandName == this.commandContext.HelpCommand.Name)
                return this.commandContext.HelpCommand;
            return null;
        }

        // private CommandMethodDescriptor GetMethodDescriptor(ICommand command, string methodName)
        // {
        //     if (command is IExecutable == true)
        //         return null;
        //     var descriptors = CommandDescriptor.GetMethodDescriptors(command);
        //     if (descriptors.Contains(methodName) == false)
        //         return null;
        //     var descriptor = descriptors[methodName];
        //     if (this.commandContext.IsMethodEnabled(command, descriptor) == false)
        //         return null;
        //     return descriptor;
        // }

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
            var query = from item in this.commandContext.CommandProviders
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
    
        private void WriteException(Exception e)
        {
            if (this.DetailErrorMessage == true)
            {
                this.commandContext.Error.WriteLine(e);
            }
            else
            {
                this.commandContext.Error.WriteLine(e.Message);
            }
        }
    }
}
