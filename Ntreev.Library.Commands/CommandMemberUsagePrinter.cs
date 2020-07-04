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
using System.IO;
using System.Reflection;
using Ntreev.Library.Commands.Properties;
using System.CodeDom.Compiler;

namespace Ntreev.Library.Commands
{
    public class CommandMemberUsagePrinter
    {
        public CommandMemberUsagePrinter(string name, object instance)
        {
            var provider = CommandDescriptor.GetUsageDescriptionProvider(instance.GetType());
            this.Name = name;
            this.Instance = instance;
            this.Summary = provider.GetSummary(instance);
            this.Description = provider.GetDescription(instance);
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

        public string Summary { get; }

        public string Description { get; }

        public bool IsDetailed { get; set; }

        private void Print(CommandTextWriter writer, CommandMemberDescriptor[] descriptors)
        {
            this.PrintSummary(writer, descriptors);
            this.PrintDescription(writer, descriptors);
            this.PrintUsage(writer, descriptors);
            this.PrintRequirements(writer, descriptors);
            this.PrintVariables(writer, descriptors);
            this.PrintOptions(writer, descriptors);
        }

        private void PrintSummary(CommandTextWriter writer, CommandMemberDescriptor[] descriptors)
        {
            var summary = this.Summary;
            if (summary != string.Empty)
            {
                writer.BeginGroup(Resources.Summary);
                writer.WriteLine(summary);
                writer.EndGroup();
            }
        }

        private void PrintUsage(CommandTextWriter writer, CommandMemberDescriptor[] descriptors)
        {
            var query = from item in descriptors
                        orderby item.IsRequired descending
                        select this.GetString(item);
            var maxWidth = writer.Width - (writer.TabString.Length * writer.Indent);
            var line = this.Name;

            writer.BeginGroup(Resources.Usage);
            foreach (var item in query)
            {
                if (line != string.Empty)
                    line += " ";
                if (line.Length + item.Length >= maxWidth)
                {
                    writer.WriteLine(line);
                    line = string.Empty.PadLeft(this.Name.Length + 1);
                }
                line += item;
            }
            writer.WriteLine(line);
            writer.EndGroup();
        }

        private void PrintDescription(CommandTextWriter writer, CommandMemberDescriptor[] descriptors)
        {
            var description = this.Description;
            if (description != string.Empty && this.IsDetailed == true)
            {
                writer.BeginGroup(Resources.Description);
                writer.WriteMultiline(description);
                writer.EndGroup();
            }
        }

        private void PrintRequirements(CommandTextWriter writer, CommandMemberDescriptor[] descriptors)
        {
            var items = descriptors.Where(item => item.IsRequired == true);
            if (items.Any() == true)
            {
                this.BeginGroup(writer, Resources.Requirements);
                foreach (var item in items)
                {
                    this.PrintRequirement(writer, item);
                }
                this.EndGroup(writer);
            }
        }

        private void PrintVariables(CommandTextWriter writer, CommandMemberDescriptor[] descriptors)
        {
            var descriptor = descriptors.FirstOrDefault(item => item is CommandMemberArrayDescriptor);
            if (descriptor != null)
            {
                this.BeginGroup(writer, Resources.Variables);
                this.PrintVariables(writer, descriptor);
                this.EndGroup(writer);
            }
        }

        private void PrintOptions(CommandTextWriter writer, CommandMemberDescriptor[] descriptors)
        {
            var items = descriptors.Where(item => item.IsRequired == false);
            if (items.Any() == true)
            {
                this.BeginGroup(writer, Resources.Options);
                foreach (var item in items)
                {
                    this.PrintOption(writer, item);
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
            writer.BeginGroup(Resources.Summary);
            writer.WriteLine(descriptor.Summary);
            writer.EndGroup();
        }

        private void PrintUsage(CommandTextWriter writer, CommandMemberDescriptor descriptor)
        {
            writer.BeginGroup(Resources.Usage);
            writer.WriteLine(this.GetString(descriptor));
            writer.EndGroup();
        }

        private void PrintDescription(CommandTextWriter writer, CommandMemberDescriptor descriptor)
        {
            writer.BeginGroup(Resources.Description);
            writer.WriteLine(descriptor.Description);
            writer.EndGroup();
        }

        private void PrintRequirement(CommandTextWriter writer, CommandMemberDescriptor descriptor)
        {
            writer.WriteLine(descriptor.DisplayName);
            if (descriptor.Summary != string.Empty)
            {
                writer.Indent++;
                writer.WriteMultiline(descriptor.Summary);
                writer.Indent--;
            }
            writer.WriteLine();
        }

        private void PrintVariables(CommandTextWriter writer, CommandMemberDescriptor descriptor)
        {
            writer.BeginGroup(descriptor.Name + " ...");
            writer.WriteMultiline(descriptor.Description);
            writer.EndGroup();
        }

        private void PrintOption(CommandTextWriter writer, CommandMemberDescriptor descriptor)
        {
            writer.WriteLine(descriptor.DisplayName);
            if (descriptor.Summary != string.Empty)
            {
                writer.Indent++;
                writer.WriteMultiline(descriptor.Summary);
                writer.Indent--;
            }
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

                if (descriptor.DefaultValue == DBNull.Value)
                {
                    if (descriptor.IsExplicit == true)
                        return $"<{patternText} {descriptorName}>";
                    else
                        return $"<{descriptorName}>";
                }
                else
                {
                    var value = descriptor.DefaultValue ?? "null";
                    if (descriptor.IsExplicit == true)
                        return $"<{patternText} {descriptorName}='{value}'>";
                    else
                        return $"<{descriptorName}='{value}'>";
                }
            }
            else if (descriptor is CommandMemberArrayDescriptor)
            {
                return $"[{descriptor.Name} ...]";
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
