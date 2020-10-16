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

        public bool IsDetailed { get; set; }

        private void Print(CommandTextWriter writer, CommandMethodDescriptor[] descriptors)
        {
            this.PrintSummary(writer, descriptors);
            this.PrintDescription(writer, descriptors);
            this.PrintUsage(writer, descriptors);
            this.PrintSubcommands(writer, descriptors);
        }

        private void Print(CommandTextWriter writer, CommandMethodDescriptor descriptor, CommandMemberDescriptor[] memberDescriptors)
        {
            this.PrintSummary(writer, descriptor, memberDescriptors);
            this.PrintDescription(writer, descriptor, memberDescriptors);
            this.PrintUsage(writer, descriptor, memberDescriptors);
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

        private void PrintSummary(CommandTextWriter writer, CommandMethodDescriptor[] _)
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
            this.PrintOption(writer, memberDescriptor);
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
            var items = memberDescriptors.Where(item => item.Usage != CommandPropertyUsage.Variables)
                                .Where(item => item.IsRequired == false)
                                .ToArray();
            if (items.Any() == false)
                return;

            writer.BeginGroup(Resources.Text_Options);
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                this.PrintOption(writer, item);
                if (i + 1 < items.Length)
                    writer.WriteLine();
            }
            writer.EndGroup();
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

        private void PrintOption(CommandTextWriter writer, CommandMemberDescriptor descriptor)
        {
            if (descriptor.ShortNamePattern != string.Empty)
                writer.WriteLine(descriptor.ShortNamePattern);
            if (descriptor.NamePattern != string.Empty)
                writer.WriteLine(descriptor.NamePattern);

            var description = descriptor.Summary != string.Empty ? descriptor.Summary : descriptor.Description;
            if (description != string.Empty)
            {
                writer.Indent++;
                writer.WriteMultiline(description);
                writer.Indent--;
            }
        }

        private string GetString(CommandMemberDescriptor descriptor)
        {
            if (descriptor.IsRequired == true)
            {
                var name = descriptor.DisplayName;
                var value = $"{descriptor.InitValue ?? "null"}";
                if (descriptor.InitValue == DBNull.Value)
                    return $"<{name}>";
                return $"<{name}={value}>";
            }
            else
            {
                return $"[{descriptor.DisplayName}]";
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
