﻿#region License
//Ntreev CommandLineParser for .Net 1.0.4548.25168
//https://github.com/NtreevSoft/CommandLineParser

//Released under the MIT License.

//Copyright (c) 2010 Ntreev Soft co., Ltd.

//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
//documentation files (the "Software"), to deal in the Software without restriction, including without limitation the 
//rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit 
//persons to whom the Software is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the 
//Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
//WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
//COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR 
//OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
#endregion


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;

using Trace = System.Diagnostics.Trace;
using Ntreev.Library.Properties;
using System.Reflection;

namespace Ntreev.Library
{
    /// <summary>
    /// 커맨드 라인을 분석해 메소드를 호출할 수 있는 방법을 제공합니다.
    /// </summary>
    public class CommandLineInvoker
    {
        internal const string defaultMethod = "default";
        private const string helpMethod = "help";

        private object instance;
        private MethodUsagePrinter usagePrinter;
        private string name;
        private string arguments;
        private string method;


        [Obsolete]
        public CommandLineInvoker()
        {

        }

        /// <summary>
        /// <seealso cref="CommandLineParser"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        public CommandLineInvoker(object instance)
            : this(Path.GetFileName(Assembly.GetEntryAssembly().Location), instance)
        {
            
        }

        public CommandLineInvoker(string name, object instance)
        {
            this.instance = instance;
            this.name = name;
            this.usagePrinter = this.CreateUsagePrinterCore(name, instance);
            this.TextWriter = Console.Out;
        }

        [Obsolete]
        public bool Invoke(object instance, string commandLine)
        {
            this.instance = instance;
            this.usagePrinter = this.CreateUsagePrinterCore(name, instance);
            return this.InvokeCore(commandLine);
        }

        [Obsolete]
        public bool Invoke(Type type, string commandLine)
        {
            this.instance = type;
            this.usagePrinter = this.CreateUsagePrinterCore(name, instance);
            return this.InvokeCore(commandLine);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandLine">
        /// command subcommand arguments
        /// </param>
        /// <param name="instance"></param>
        /// <param name="parsingOptions"></param>
        public bool Invoke(string commandLine)
        {
            return this.InvokeCore(commandLine);
        }

        /// <summary>
        /// 모든 스위치의 사용법을 출력합니다.
        /// </summary>
        public void PrintMethodUsage()
        {
            this.usagePrinter.PrintUsage(this.TextWriter);
        }

        public void PrintMethodUsage(string methodName)
        {
            this.usagePrinter.PrintUsage(this.TextWriter, methodName);
        }

        /// <summary>
        /// 분석과정중 생기는 다양한 정보를 출력할 수 있는 처리기를 지정합니다.
        /// </summary>
        public TextWriter TextWriter { get; set; }

        /// <summary>
        /// 사용방법을 출력하는 방법을 나타내는 인스턴를 가져옵니다.
        /// </summary>
        public MethodUsagePrinter Usage
        {
            get { return this.usagePrinter; }
        }

        public string Name
        {
            get { return this.name; }
        }

        public string Method
        {
            get { return this.method; }
        }

        public string Arguments
        {
            get { return this.arguments; }
        }

        protected virtual void PrintMethodHelp(object target, string commandName, string methodName)
        {
            if (string.IsNullOrEmpty(methodName) == true)
            {
                this.PrintMethodUsage();
            }
            else
            {
                MethodDescriptor descriptor = CommandDescriptor.GetMethodDescriptor(target, methodName);
                if (descriptor == null)
                {
                    this.TextWriter.WriteLine("{0} is not subcommand", methodName);
                }
                else
                {
                    this.PrintMethodUsage(methodName);
                }
            }
        }

        protected virtual void PrintSummary(object target)
        {
            this.TextWriter.WriteLine("Type '{0} help' for usage.", this.name);
        }

        protected virtual MethodUsagePrinter CreateUsagePrinterCore(string name, object instance)
        {
            return new MethodUsagePrinter(name, instance);
        }

        private bool InvokeCore(string commandLine)
        {
            //using (Tracer tracer = new Tracer("Inovking"))
            {
                string cmdLine = commandLine;

                Regex regex = new Regex(@"^((""[^""]*"")|(\S+))");
                Match match = regex.Match(cmdLine);
                this.name = match.Value.Trim(new char[] { '\"', });

                if (File.Exists(this.name) == true)
                    this.name = Path.GetFileNameWithoutExtension(this.name).ToLower();

                cmdLine = cmdLine.Substring(match.Length).Trim();
                match = regex.Match(cmdLine);
                this.method = match.Value;

                this.arguments = cmdLine.Substring(match.Length).Trim();
                this.arguments = this.arguments.Trim();

                if (string.IsNullOrEmpty(this.method) == true)
                {
                    this.PrintSummary(this.instance);
                    return false;
                }
                else if (this.method == CommandLineInvoker.helpMethod)
                {
                    this.PrintMethodHelp(this.instance, this.name, this.arguments);
                    return false;
                }
                else
                {
                    MethodDescriptor descriptor = CommandDescriptor.GetMethodDescriptor(this.instance, this.method);

                    if (descriptor == null)
                    {
                        throw new NotFoundMethodException(this.method);
                    }

                    try
                    {
                        descriptor.Invoke(this.instance, this.arguments);
                        return true;
                    }
                    catch (SwitchException e)
                    {
                        throw e;
                    }
                    catch (TargetInvocationException e)
                    {
                        if(e.InnerException != null)
                            throw new MethodInvokeException(this.method, e.InnerException);
                        throw e;
                    }
                    catch (Exception e)
                    {
                        throw new MethodInvokeException(this.method, e);
                    }
                }
            }
        }
    }


    public class InvokeEventArgs : EventArgs
    {
        private readonly string commandName;
        private readonly string methodName;
        private readonly object target;

        public InvokeEventArgs(object target, string commandName, string methodName)
        {
            this.target = target;
            this.commandName = commandName;
            this.methodName = methodName;
        }

        public object Target
        {
            get { return this.target; }
        }

        public string CommandName
        {
            get { return this.commandName; }
        }

        public string MethodName
        {
            get { return this.methodName; }
        }
    }
}