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
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JSSoft.Library.Commands
{
    class ParseDescriptor
    {
        private readonly Dictionary<string, string> unparsedArguments = new();

        /// <param name="members"></param>
        /// <param name="commandLine"></param>
        public ParseDescriptor(IEnumerable<CommandMemberDescriptor> members, string commandLine)
            : this(members, CommandStringUtility.EscapeString(commandLine))
        {
        }

        public ParseDescriptor(IEnumerable<CommandMemberDescriptor> members, string[] args)
        {
            var itemByDescriptor = members.ToDictionary(item => item, item => new ParseDescriptorItem(item));
            var unparsedArguments = new Dictionary<string, string>();
            var descriptors = ToDictionary(members);
            var variableList = new List<string>();
            var variablesDescriptor = members.Where(item => item.Usage == CommandPropertyUsage.Variables).FirstOrDefault();
            var arguments = CreateQueue(args);

            while (arguments.Any())
            {
                var arg = arguments.Dequeue();
                if (descriptors.ContainsKey(arg) == true)
                {
                    var descriptor = descriptors[arg];
                    if (descriptor.IsSwitch == true)
                    {
                        itemByDescriptor[descriptor].Value = true;
                        itemByDescriptor[descriptor].HasSwtich = true;
                    }
                    else
                    {
                        var nextArg = arguments.FirstOrDefault() ?? string.Empty;
                        var isValue = nextArg != string.Empty && CommandStringUtility.IsOption(nextArg) == false && nextArg != "--";
                        if (isValue == true)
                        {
                            var textValue = arguments.Dequeue();
                            itemByDescriptor[descriptor].Value = Parser.Parse(descriptor, textValue);
                        }
                        itemByDescriptor[descriptor].HasSwtich = true;
                    }
                }
                else if (arg == "--")
                {
                    if (variablesDescriptor != null)
                    {
                        foreach (var item in arguments)
                        {
                            variableList.Add(item);
                        }
                    }
                    else
                    {
                        foreach (var item in arguments)
                        {
                            unparsedArguments.Add(item, null);
                        }
                    }
                    arguments.Clear();
                }
                else if (CommandStringUtility.IsMultipleSwitch(arg))
                {
                    for (var i = 1; i < arg.Length; i++)
                    {
                        var s = arg[i];
                        var item = $"-{s}";
                        if (descriptors.ContainsKey(item) == false)
                            throw new InvalidOperationException($"unknown switch: '{s}'");
                        var descriptor = descriptors[item];
                        if (descriptor.MemberType != typeof(bool))
                            throw new InvalidOperationException($"unknown switch: '{s}'");
                        itemByDescriptor[descriptor].HasSwtich = true;
                    }
                }
                else if (CommandStringUtility.IsOption(arg) == true)
                {
                    unparsedArguments.Add(arg, null);
                }
                else if (arg.StartsWith("--") || arg.StartsWith("-"))
                {
                    unparsedArguments.Add(arg, null);
                }
                else if (CommandStringUtility.IsOption(arg) == false)
                {
                    var requiredDescriptor = itemByDescriptor.Where(item => item.Key.IsRequired == true && item.Key.IsExplicit == false && item.Value.IsParsed == false)
                                                          .Select(item => item.Key).FirstOrDefault();
                    if (requiredDescriptor != null)
                    {
                        var parseInfo = itemByDescriptor[requiredDescriptor];
                        var value = Parser.Parse(requiredDescriptor, arg);
                        parseInfo.Value = value;
                    }
                    else if (variablesDescriptor != null)
                    {
                        variableList.Add(arg);
                    }
                    else
                    {
                        var nextArg = arguments.FirstOrDefault();
                        if (nextArg != null && CommandStringUtility.IsOption(nextArg) == false)
                            unparsedArguments.Add(arg, arguments.Dequeue());
                        else
                            unparsedArguments.Add(arg, null);
                    }
                }
                else
                {
                    unparsedArguments.Add(arg, null);
                }
            }

            if (variableList.Any() == true)
            {
                itemByDescriptor[variablesDescriptor].Value = Parser.ParseArray(variablesDescriptor, variableList);
            }

            this.Items = itemByDescriptor.Values.ToArray();
            this.unparsedArguments = unparsedArguments;
        }

        private static Queue<string> CreateQueue(string[] arguments)
        {
            var queue = new Queue<string>(arguments.Length);
            foreach (var item in arguments)
            {
                queue.Enqueue(item);
            }
            return queue;
        }

        private static IDictionary<string, CommandMemberDescriptor> ToDictionary(IEnumerable<CommandMemberDescriptor> members)
        {
            var descriptors = new Dictionary<string, CommandMemberDescriptor>();
            foreach (var item in members)
            {
                if (item.IsExplicit == false)
                    continue;
                if (item.NamePattern != string.Empty)
                    descriptors.Add(item.NamePattern, item);
                if (item.ShortNamePattern != string.Empty)
                    descriptors.Add(item.ShortNamePattern, item);
            }

            return descriptors;
        }

        public void SetValue(object instance)
        {
            this.ValidateSetValue(instance);

            var items = this.Items;
            var initObj = instance as ISupportInitialize;
            foreach (var item in items)
            {
                var descriptor = item.Descriptor;
                if (item.IsParsed == false && item.HasSwtich == false)
                    continue;
                descriptor.ValidateTrigger(items);
            }
            initObj?.BeginInit();
            foreach (var item in items)
            {
                var descriptor = item.Descriptor;
                descriptor.SetValueInternal(instance, item.InitValue);
            }
            initObj?.EndInit();
            foreach (var item in items)
            {
                var descriptor = item.Descriptor;
                descriptor.SetValueInternal(instance, item.ActualValue);
            }
        }

        public ParseDescriptorItem[] Items { get; }

        private void ValidateSetValue(object _)
        {
            if (this.unparsedArguments.Any())
            {
                var items = new Dictionary<string, string>(this.unparsedArguments);
                if (items.Any() == true)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine(Resources.Message_UnprocessedArguments);
                    foreach (var item in items)
                    {
                        if (item.Value != null)
                        {
                            sb.AppendLine($"    {item.Key} {item.Value}");
                        }
                        else
                        {
                            sb.AppendLine($"    {item.Key}");
                        }
                    }
                    throw new ArgumentException(sb.ToString());
                }
            }

            foreach (var item in this.Items)
            {
                var descriptor = item.Descriptor;
                if (item.IsParsed == true)
                    continue;
                if (item.HasSwtich == true && item.Value == DBNull.Value)
                    throw new ArgumentException(string.Format(Resources.Exception_ValudIsNotSet_Format, descriptor.DisplayName));
                if (descriptor.IsRequired == true && item.Value == DBNull.Value)
                    throw new ArgumentException(string.Format(Resources.Exception_ValudIsNotSet_Format, descriptor.DisplayName));
            }
        }
    }
}
