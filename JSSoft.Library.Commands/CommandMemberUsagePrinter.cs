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
    public class CommandMemberUsagePrinter
    {
        public CommandMemberUsagePrinter(string name, object instance)
            : this(name, instance, new string[] { })
        {
        }

        public CommandMemberUsagePrinter(string name, object instance, string[] aliases)
        {
            var provider = CommandDescriptor.GetUsageDescriptionProvider(instance.GetType());
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            this.Aliases = aliases ?? throw new ArgumentNullException(nameof(aliases));
            this.Summary = provider.GetSummary(instance);
            this.Description = provider.GetDescription(instance);
            this.Example = provider.GetExample(instance);
        }

        public virtual void Print(TextWriter writer, CommandMemberDescriptor[] descriptors)
        {
            using var tw = new CommandTextWriter(writer);
            this.Print(tw, descriptors);
        }

        public virtual void Print(TextWriter writer, CommandMemberDescriptor descriptor)
        {
            using var tw = new CommandTextWriter(writer);
            this.Print(tw, descriptor);
        }

        public string Name { get; }

        public object Instance { get; }

        public string[] Aliases { get; }

        public string Summary { get; }

        public string Description { get; }

        public string Example { get; }

        public CommandUsage Usage { get; set; }

        public bool IsDetail => this.Usage == CommandUsage.Detail;

        public bool IsSimple => this.Usage == CommandUsage.Simple;

        private void Print(CommandTextWriter writer, CommandMemberDescriptor[] descriptors)
        {
            this.PrintSummary(writer);
            this.PrintDescription(writer, descriptors);
            this.PrintUsage(writer, descriptors);
            this.PrintExample(writer);
            this.PrintRequirements(writer, descriptors);
            this.PrintVariables(writer, descriptors);
            this.PrintOptions(writer, descriptors);
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

        private void PrintUsage(CommandTextWriter writer, CommandMemberDescriptor[] descriptors)
        {
            var query = from item in descriptors
                        where item.IsRequired == true || item.IsVariables == true
                        select item;
            var maxWidth = writer.Width - (writer.TabString.Length * writer.Indent);
            var line = this.Name;

            writer.BeginGroup(Resources.Text_Usage);
            if (this.Aliases.Any() == true)
                line += $"({string.Join(",", this.Aliases)})";
            foreach (var item in query)
            {
                var text = this.GetString(item);
                if (line != string.Empty)
                    line += " ";
                if (line.Length + text.Length >= maxWidth)
                {
                    writer.WriteLine(line);
                    line = string.Empty.PadLeft(this.Name.Length + 1);
                }
                line += text;
            }
            writer.WriteLine(line);
            writer.EndGroup();
        }

        private void PrintDescription(CommandTextWriter writer, CommandMemberDescriptor[] _)
        {
            var description = this.Description;
            if (description != string.Empty && this.IsDetail == true)
            {
                writer.BeginGroup(Resources.Text_Description);
                writer.WriteMultiline(description);
                writer.EndGroup();
            }
        }

        private void PrintExample(CommandTextWriter writer)
        {
            var example = this.Example;
            if (example != string.Empty && this.Usage == CommandUsage.None)
            {
                writer.BeginGroup(Resources.Text_Example);
                writer.WriteMultiline(example);
                writer.EndGroup();
            }
        }

        private void PrintRequirements(CommandTextWriter writer, CommandMemberDescriptor[] descriptors)
        {
            var items = descriptors.Where(item => item.IsRequired == true);
            if (items.Any() == true && this.Usage != CommandUsage.Simple)
            {
                this.BeginGroup(writer, Resources.Text_Requirements);
                foreach (var item in items)
                {
                    this.PrintRequirement(writer, item, items.Last() == item);
                }
                this.EndGroup(writer);
            }
        }

        private void PrintVariables(CommandTextWriter writer, CommandMemberDescriptor[] descriptors)
        {
            var descriptor = descriptors.FirstOrDefault(item => item.Usage == CommandPropertyUsage.Variables);
            if (descriptor != null && this.Usage != CommandUsage.Simple)
            {
                this.BeginGroup(writer, Resources.Text_Variables);
                this.PrintVariables(writer, descriptor);
                this.EndGroup(writer);
            }
        }

        private void PrintOptions(CommandTextWriter writer, CommandMemberDescriptor[] descriptors)
        {
            var items = descriptors.Where(item => item.Usage == CommandPropertyUsage.General || item.Usage == CommandPropertyUsage.Switch);
            if (items.Any() == true && this.Usage != CommandUsage.Simple)
            {
                this.BeginGroup(writer, Resources.Text_Options);
                foreach (var item in items)
                {
                    this.PrintOption(writer, item, items.Last() == item);
                }
                this.EndGroup(writer);
            }
        }

        private void Print(CommandTextWriter writer, CommandMemberDescriptor descriptor)
        {
            this.PrintSummary(writer, descriptor);
            this.PrintUsage(writer, descriptor);
            this.PrintDescription(writer, descriptor);
        }

        private void PrintSummary(CommandTextWriter writer, CommandMemberDescriptor descriptor)
        {
            writer.BeginGroup(Resources.Text_Summary);
            writer.WriteLine(descriptor.Summary);
            writer.EndGroup();
        }

        private void PrintUsage(CommandTextWriter writer, CommandMemberDescriptor descriptor)
        {
            writer.BeginGroup(Resources.Text_Usage);
            writer.WriteLine(this.GetString(descriptor));
            writer.EndGroup();
        }

        private void PrintDescription(CommandTextWriter writer, CommandMemberDescriptor descriptor)
        {
            writer.BeginGroup(Resources.Text_Description);
            writer.WriteLine(descriptor.Description);
            writer.EndGroup();
        }

        private void PrintRequirement(CommandTextWriter writer, CommandMemberDescriptor descriptor, bool isLast)
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

        private void PrintVariables(CommandTextWriter writer, CommandMemberDescriptor descriptor)
        {
            writer.WriteLine(descriptor.DisplayName);
            if (descriptor.Summary != string.Empty)
            {
                writer.Indent++;
                writer.WriteMultiline(descriptor.Summary);
                writer.Indent--;
            }
        }

        private void PrintOption(CommandTextWriter writer, CommandMemberDescriptor descriptor, bool isLast)
        {
            if (descriptor.IsSwitch == true)
                writer.WriteLine(descriptor.DisplayName);
            else if (descriptor.DefaultValue == null)
                writer.WriteLine($"{descriptor.DisplayName} 'value' [default: null]");
            else if (descriptor.DefaultValue != DBNull.Value)
                writer.WriteLine($"{descriptor.DisplayName} 'value' [default: '{descriptor.DefaultValue}']");
            else
                writer.WriteLine($"{descriptor.DisplayName} 'value'");
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
                        return $"<{patternText} 'value'>";
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
