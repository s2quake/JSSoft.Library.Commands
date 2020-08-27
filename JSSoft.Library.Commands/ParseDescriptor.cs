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
        private readonly Dictionary<string, string> unparsedArguments = new Dictionary<string, string>();

        private readonly Dictionary<CommandMemberDescriptor, ParseDescriptorItem> itemByDescriptor = new Dictionary<CommandMemberDescriptor, ParseDescriptorItem>();

        /// <param name="members"></param>
        /// <param name="commandLine"></param>
        public ParseDescriptor(IEnumerable<CommandMemberDescriptor> members, string commandLine)
            : this(members, CommandStringUtility.SplitAll(commandLine))
        {
        }

        public ParseDescriptor(IEnumerable<CommandMemberDescriptor> members, string[] args)
        {
            foreach (var item in members)
            {
                this.itemByDescriptor.Add(item, new ParseDescriptorItem(item));
            }
            this.Items = this.itemByDescriptor.Values.ToArray();

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

            var variableList = new List<string>();
            var variablesDescriptor = members.Where(item => item.Usage == CommandPropertyUsage.Variables).FirstOrDefault();
            var arguments = new Queue<string>(args);

            while (arguments.Any())
            {
                var arg = arguments.Dequeue();
                if (descriptors.ContainsKey(arg) == true)
                {
                    var descriptor = descriptors[arg];
                    var nextArg = arguments.FirstOrDefault() ?? string.Empty;
                    var isValue = nextArg != string.Empty && CommandStringUtility.IsSwitch(nextArg) == false && nextArg != "--";
                    if (isValue == true)
                    {
                        var textValue = arguments.Dequeue();
                        if (CommandStringUtility.IsWrappedOfQuote(textValue) == true)
                            textValue = CommandStringUtility.TrimQuot(textValue);
                        this.itemByDescriptor[descriptor].Value = Parser.Parse(descriptor, textValue);
                    }
                    this.itemByDescriptor[descriptor].HasSwtich = true;
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
                            this.unparsedArguments.Add(item, null);
                        }
                    }
                    arguments.Clear();
                }
                else if (CommandStringUtility.IsSwitch(arg) == true)
                {
                    this.unparsedArguments.Add(arg, null);
                }
                else if (arg.StartsWith("--") || arg.StartsWith("-"))
                {
                    this.unparsedArguments.Add(arg, null);
                }
                else if (CommandStringUtility.IsSwitch(arg) == false)
                {
                    var requiredDescriptor = this.itemByDescriptor.Where(item => item.Key.IsRequired == true && item.Key.IsExplicit == false && item.Value.IsParsed == false)
                                                          .Select(item => item.Key).FirstOrDefault();
                    if (requiredDescriptor != null)
                    {
                        var parseInfo = this.itemByDescriptor[requiredDescriptor];
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
                        if (nextArg != null && CommandStringUtility.IsSwitch(nextArg) == false)
                            this.unparsedArguments.Add(arg, arguments.Dequeue());
                        else
                            this.unparsedArguments.Add(arg, null);
                    }
                }
                else
                {
                    this.unparsedArguments.Add(arg, null);
                }
            }

            if (variableList.Any() == true)
            {
                this.itemByDescriptor[variablesDescriptor].Value = Parser.ParseArray(variablesDescriptor, variableList);
            }
        }

        public void SetValue(object instance)
        {
            this.ValidateSetValue(instance);

            var items = this.Items;
            var initObj = instance as ISupportInitialize;
            foreach (var item in items)
            {
                var descriptor = item.Descriptor;
                if (item.IsParsed == false)
                    continue;
                descriptor.ValidateTrigger(items);
            }

            if (initObj != null)
            {
                initObj.BeginInit();
            }
            foreach (var item in items)
            {
                var descriptor = item.Descriptor;
                descriptor.SetValueInternal(instance, item.InitValue);
            }
            if (initObj != null)
            {
                initObj.EndInit();
            }

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
                    sb.AppendLine("처리되지 않은 인자가 포함되어 있습니다.");
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
