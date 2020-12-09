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

using System;
using System.Collections.Generic;
using System.Linq;

namespace JSSoft.Library.Commands
{
    public abstract class CommandMemberDescriptor
    {
        protected CommandMemberDescriptor(CommandPropertyBaseAttribute attribute, string descriptorName)
        {
            this.Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            this.DescriptorName = descriptorName ?? throw new ArgumentNullException(nameof(descriptorName));
            this.Name = attribute.GetName(descriptorName);
            this.ShortName = attribute.InternalShortName;
            this.IsRequired = attribute.IsRequiredProperty;
            this.IsExplicit = attribute.IsExplicitProperty;
            this.IsSwitch = attribute.IsSwitchProperty;
            this.DefaultValue = attribute.DefaultValueProperty;
            this.InitValue = attribute.InitValueProperty;
            this.Usage = attribute.GetUsage();
        }

        public override string ToString()
        {
            return $"{this.DescriptorName} [{this.DisplayName}]";
        }

        public static CommandMemberDescriptor Find(IEnumerable<CommandMemberDescriptor> descriptors, string displayName)
        {
            foreach (var item in descriptors)
            {
                if (item.DisplayName == displayName || item.NamePattern == displayName || item.ShortNamePattern == displayName)
                    return item;
            }
            return null;
        }

        public string Name { get; }

        public string ShortName { get; }

        public virtual string DisplayName
        {
            get
            {
                var items = this.IsExplicit == true ? new string[] { this.ShortNamePattern, this.NamePattern } : new string[] { this.ShortName, this.Name };
                var name = string.Join(" | ", items.Where(item => item != string.Empty).ToArray());
                if (name == string.Empty)
                    return this.Name;
                return name;
            }
        }

        public virtual string Summary { get; } = string.Empty;

        public virtual string Description { get; } = string.Empty;

        public virtual object InitValue { get; }

        public virtual object DefaultValue { get; }

        public virtual bool IsRequired { get; }

        public virtual bool IsExplicit { get; }

        public virtual bool IsSwitch { get; }

        public abstract Type MemberType { get; }

        public string DescriptorName { get; }

        public virtual CommandPropertyUsage Usage { get; }

        protected abstract void SetValue(object instance, object value);

        protected abstract object GetValue(object instance);

        protected virtual void OnValidateTrigger(ParseDescriptorItem[] parseItems)
        {
        }

        protected CommandPropertyBaseAttribute Attribute { get; }

        internal void Parse(object instance, List<string> arguments)
        {
            var arg = arguments.First();
            var value = Parser.Parse(this, arg);
            this.SetValue(instance, value);
            arguments.RemoveAt(0);
        }

        public virtual string NamePattern
        {
            get
            {
                if (this.Name == string.Empty)
                    return string.Empty;
                return CommandSettings.Delimiter + this.Name;
            }
        }

        public virtual string ShortNamePattern
        {
            get
            {
                if (this.ShortName == string.Empty)
                    return string.Empty;
                return CommandSettings.ShortDelimiter + this.ShortName;
            }
        }

        internal void SetValueInternal(object instance, object value)
        {
            this.SetValue(instance, value);
        }

        internal object GetValueInternal(object instance)
        {
            return this.GetValue(instance);
        }

        internal void ValidateTrigger(ParseDescriptorItem[] parseItems)
        {
            this.OnValidateTrigger(parseItems);
        }
    }
}
