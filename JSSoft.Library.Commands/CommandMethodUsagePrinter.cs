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
using System.IO;
using System.Linq;

namespace JSSoft.Library.Commands
{
    public class CommandMethodUsagePrinter
    {
        public CommandMethodUsagePrinter(string name, object instance)
        {
            var provider = CommandDescriptor.GetUsageDescriptionProvider(instance.GetType());
            this.Name = name;
            this.Instance = instance;
            this.Summary = provider.GetSummary(instance);
            this.Description = provider.GetDescription(instance);
            this.Example = provider.GetExample(instance);
        }

        public virtual void Print(TextWriter writer, CommandMethodDescriptor[] descriptors)
        {
            using var tw = new CommandTextWriter(writer);
            this.Print(tw, descriptors);
        }

        public virtual void Print(TextWriter writer, CommandMethodDescriptor descriptor, CommandMemberDescriptor[] memberDescriptors)
        {
            using var tw = new CommandTextWriter(writer);
            this.Print(tw, descriptor, memberDescriptors);
        }

        public virtual void Print(TextWriter writer, CommandMethodDescriptor descriptor, CommandMemberDescriptor memberDescriptor)
        {
            using var tw = new CommandTextWriter(writer);
            this.Print(tw, descriptor, memberDescriptor);
        }

        public string Name { get; }

        public object Instance { get; }

        public string Summary { get; }

        public string Description { get; }

        public string Example { get; }

        public bool IsDetailed { get; set; }

        private void Print(CommandTextWriter writer, CommandMethodDescriptor[] descriptors)
        {
            this.PrintSummary(writer);
            this.PrintDescription(writer, descriptors);
            this.PrintUsage(writer, descriptors);
            this.PrintExample(writer);
            this.PrintSubcommands(writer, descriptors);
        }

        private void Print(CommandTextWriter writer, CommandMethodDescriptor descriptor, CommandMemberDescriptor[] memberDescriptors)
        {
            this.PrintSummary(writer, descriptor, memberDescriptors);
            this.PrintDescription(writer, descriptor, memberDescriptors);
            this.PrintUsage(writer, descriptor, memberDescriptors);
            this.PrintExample(writer, descriptor, memberDescriptors);
            this.PrintRequirements(writer, descriptor, memberDescriptors);
            this.PrintVariables(writer, descriptor, memberDescriptors);
            this.PrintOptions(writer, descriptor, memberDescriptors);
        }

        private void Print(CommandTextWriter writer, CommandMethodDescriptor descriptor, CommandMemberDescriptor memberDescriptor)
        {
            this.PrintSummary(writer, descriptor, memberDescriptor);
            this.PrintDescription(writer, descriptor, memberDescriptor);
            this.PrintUsage(writer, descriptor, memberDescriptor);
        }

        private void PrintSummary(CommandTextWriter writer)
        {
            var summary = this.Summary;
            if (summary != string.Empty)
            {
                writer.BeginGroup(Resources.Text_Summary);
                writer.WriteMultiline(summary);
                writer.EndGroup();
            }
        }

        private void PrintSummary(CommandTextWriter writer, CommandMethodDescriptor descriptor, CommandMemberDescriptor[] _)
        {
            if (descriptor.Summary != string.Empty)
            {
                writer.BeginGroup(Resources.Text_Summary);
                writer.WriteMultiline(descriptor.Summary);
                writer.EndGroup();
            }
        }

        private void PrintSummary(CommandTextWriter writer, CommandMethodDescriptor _, CommandMemberDescriptor memberDescriptor)
        {
            writer.BeginGroup(Resources.Text_Summary);
            writer.WriteMultiline(memberDescriptor.Summary);
            writer.EndGroup();
        }

        private void PrintDescription(CommandTextWriter writer, CommandMethodDescriptor[] _)
        {
            var description = this.Description;
            if (description != string.Empty && this.IsDetailed == true)
            {
                writer.BeginGroup(Resources.Text_Description);
                writer.WriteMultiline(description);
                writer.EndGroup();
            }
        }

        private void PrintDescription(CommandTextWriter writer, CommandMethodDescriptor descriptor, CommandMemberDescriptor[] _)
        {
            if (descriptor.Description != string.Empty && this.IsDetailed == true)
            {
                writer.BeginGroup(Resources.Text_Description);
                writer.WriteMultiline(descriptor.Description);
                writer.EndGroup();
            }
        }

        private void PrintDescription(CommandTextWriter writer, CommandMethodDescriptor _, CommandMemberDescriptor memberDescriptor)
        {
            writer.BeginGroup(Resources.Text_Description);
            writer.WriteMultiline(memberDescriptor.Description);
            writer.EndGroup();
        }

        private void PrintExample(CommandTextWriter writer)
        {
            var example = this.Example;
            if (example != string.Empty)
            {
                writer.BeginGroup(Resources.Text_Example);
                writer.WriteMultiline(example);
                writer.EndGroup();
            }
        }

        private void PrintExample(CommandTextWriter writer, CommandMethodDescriptor descriptor, CommandMemberDescriptor[] _)
        {
            if (descriptor.Example != string.Empty)
            {
                writer.BeginGroup(Resources.Text_Example);
                writer.WriteMultiline(descriptor.Example);
                writer.EndGroup();
            }
        }

        private void PrintSubcommands(CommandTextWriter writer, CommandMethodDescriptor[] descriptors)
        {
            writer.BeginGroup(Resources.Text_Subcommands);
            foreach (var item in descriptors)
            {
                writer.WriteLine(item.Name);
                if (item.Summary != string.Empty)
                {
                    writer.Indent++;
                    writer.WriteMultiline(item.Summary);
                    writer.Indent--;
                }
                if (descriptors.Last() != item)
                    writer.WriteLine();
            }
            writer.EndGroup();
        }

        private void PrintUsage(CommandTextWriter writer, CommandMethodDescriptor[] _)
        {
            writer.BeginGroup(Resources.Text_Usage);
            writer.WriteLine("{0} <sub-command> [options...]", this.Name);
            writer.EndGroup();
        }

        private void PrintUsage(CommandTextWriter writer, CommandMethodDescriptor descriptor, CommandMemberDescriptor[] memberDescriptors)
        {
            this.BeginGroup(writer, Resources.Text_Usage);
            this.PrintMethodUsage(writer, descriptor, memberDescriptors);
            this.EndGroup(writer);
        }

        private void PrintUsage(CommandTextWriter writer, CommandMethodDescriptor _, CommandMemberDescriptor memberDescriptor)
        {
            this.BeginGroup(writer, Resources.Text_Usage);
            this.PrintOption(writer, memberDescriptor, false);
            this.EndGroup(writer);
        }

        private void PrintMethodUsage(CommandTextWriter writer, CommandMethodDescriptor descriptor, CommandMemberDescriptor[] memberDescriptors)
        {
            var indent = writer.Indent;
            var query = from item in memberDescriptors
                        orderby item.IsRequired descending
                        select this.GetString(item);
            var maxWidth = writer.Width - (writer.TabString.Length * writer.Indent);
            var line = this.Name + " " + descriptor.Name;
            foreach (var item in query)
            {
                if (line != string.Empty)
                    line += " ";
                if (line.Length + item.Length >= maxWidth)
                {
                    writer.WriteLine(line);
                    line = string.Empty.PadLeft(descriptor.Name.Length + 1);
                }
                line += item;
            }
            writer.WriteLine(line);
            writer.Indent = indent;
        }

        private void PrintRequirements(CommandTextWriter writer, CommandMethodDescriptor _, CommandMemberDescriptor[] memberDescriptors)
        {
            var items = memberDescriptors.Where(item => item.IsRequired == true).ToArray();
            if (items.Any() == false)
                return;

            writer.BeginGroup(Resources.Text_Requirements);
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                this.PrintRequirement(writer, item);
                if (i + 1 < items.Length)
                    writer.WriteLine();
            }
            writer.EndGroup();
        }

        private void PrintVariables(CommandTextWriter writer, CommandMethodDescriptor _, CommandMemberDescriptor[] memberDescriptors)
        {
            var variables = memberDescriptors.FirstOrDefault(item => item.Usage == CommandPropertyUsage.Variables);
            if (variables != null)
            {
                this.BeginGroup(writer, Resources.Text_Variables);
                this.PrintVariables(writer, variables);
                this.EndGroup(writer);
            }
        }

        private void PrintOptions(CommandTextWriter writer, CommandMethodDescriptor _, CommandMemberDescriptor[] memberDescriptors)
        {
            var items = memberDescriptors.Where(item => item.Usage == CommandPropertyUsage.General || item.Usage == CommandPropertyUsage.General);
            if (items.Any() == true)
            {
                this.BeginGroup(writer, Resources.Text_Options);
                foreach (var item in items)
                {
                    this.PrintOption(writer, item, items.Last() == item);
                }
                this.EndGroup(writer);
            }
        }

        private void PrintRequirement(CommandTextWriter writer, CommandMemberDescriptor descriptor)
        {
            if (descriptor.IsRequired == true)
            {
                writer.WriteLine(descriptor.DisplayName);
            }
            else
            {
                writer.WriteLine(descriptor.DisplayName);
            }

            var description = descriptor.Summary != string.Empty ? descriptor.Summary : descriptor.Description;
            if (description != string.Empty)
            {
                writer.Indent++;
                writer.WriteMultiline(description);
                writer.Indent--;
            }
        }

        private void PrintVariables(CommandTextWriter writer, CommandMemberDescriptor descriptor)
        {
            writer.BeginGroup(descriptor.Name + " ...");
            writer.WriteMultiline(descriptor.Description);
            writer.EndGroup();
        }

        private void PrintOption(CommandTextWriter writer, CommandMemberDescriptor descriptor, bool isLast)
        {
            writer.WriteLine(descriptor.DisplayName);
            if (descriptor.Summary != string.Empty)
            {
                writer.Indent++;
                writer.WriteMultiline(descriptor.Summary);
                writer.Indent--;
            }
            if (isLast == false)
                writer.WriteLine();
        }

        private string GetString(CommandMemberDescriptor descriptor)
        {
            var patternText = descriptor.DisplayName;
            if (descriptor.IsRequired == true)
            {
                var descriptorName = descriptor.DisplayName;
                if (descriptorName == string.Empty)
                    descriptorName = CommandSettings.NameGenerator(descriptor.DescriptorName);

                if (descriptor.InitValue == DBNull.Value)
                {
                    if (descriptor.IsExplicit == true)
                        return $"<{patternText} {descriptorName}>";
                    else
                        return $"<{descriptorName}>";
                }
                else
                {
                    var value = descriptor.InitValue ?? "null";
                    if (descriptor.IsExplicit == true)
                        return $"<{patternText} {descriptorName}, default='{value}'>";
                    else
                        return $"<{descriptorName}, default='{value}'>";
                }
            }
            else if (descriptor.IsSwitch == false)
            {
                return $"[{patternText} 'value']";
            }
            else
            {
                return $"[{patternText}]";
            }
        }

        private void BeginGroup(CommandTextWriter writer, string text)
        {
            writer.BeginGroup(text);
        }

        private void EndGroup(CommandTextWriter writer)
        {
            writer.EndGroup();
        }
    }
}
