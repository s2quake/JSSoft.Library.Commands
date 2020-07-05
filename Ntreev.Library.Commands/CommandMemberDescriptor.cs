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
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Reflection;
using Ntreev.Library.Commands.Properties;

namespace Ntreev.Library.Commands
{
    public abstract class CommandMemberDescriptor
    {
        protected CommandMemberDescriptor(CommandPropertyAttribute attribute, string descriptorName)
        {
            if (attribute == null)
                throw new ArgumentNullException(nameof(attribute));
            attribute.InvokeValidate(this);
            this.DescriptorName = descriptorName ?? throw new ArgumentNullException(nameof(descriptorName));
            this.Name = attribute.GetName(descriptorName);
            this.ShortName = attribute.InternalShortName;
            this.IsRequired = attribute.IsRequired;
            this.IsExplicit = attribute.IsRequired == false ? true : attribute.IsExplicit;
            this.ExplicitValue = attribute.ExplicitValue;
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

        public virtual object DefaultValue { get; } = DBNull.Value;

        public virtual object ExplicitValue { get; }

        public virtual bool IsRequired { get; }

        public virtual bool IsExplicit { get; }

        public abstract Type MemberType { get; }

        public virtual TypeConverter Converter => TypeDescriptor.GetConverter(this.MemberType);

        //public virtual IEnumerable<Attribute> Attributes { get { yield break; } }

        public string DescriptorName { get; }

        protected abstract void SetValue(object instance, object value);

        protected abstract object GetValue(object instance);

        protected virtual void OnValidateTrigger(IDictionary<CommandMemberDescriptor, ParseDescriptorItem> descriptors)
        {

        }

        internal void Parse(object instance, List<string> arguments)
        {
            if (this.MemberType == typeof(bool))
            {
                this.SetValue(instance, true);
            }
            else
            {
                var arg = arguments.First();
                var value = Parser.Parse(this, arg);
                this.SetValue(instance, value);
                arguments.RemoveAt(0);
            }
        }

        //internal string[] GetCompletion(object target)
        //{
        //    var memberType = this.MemberType;
        //    var attributes = this.Attributes;
        //    if (memberType.IsEnum == true)
        //    {
        //        return Enum.GetNames(memberType).Select(item => CommandSettings.NameGenerator(item)).ToArray();
        //    }
        //    else if (attributes.FirstOrDefault(item => item is CommandCompletionAttribute) is CommandCompletionAttribute attr)
        //    {
        //        if (attr.Type == null)
        //        {
        //            var methodInfo = target.GetType().GetMethod(attr.MethodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new Type[] { }, null);
        //            return methodInfo.Invoke(target, null) as string[];
        //        }
        //        else
        //        {
        //            var methodInfo = attr.Type.GetMethod(attr.MethodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, new Type[] { }, null);
        //            return methodInfo.Invoke(null, null) as string[];
        //        }
        //    }
        //    return null;
        //}

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

        internal void ValidateTrigger(IDictionary<CommandMemberDescriptor, ParseDescriptorItem> descriptors)
        {
            this.OnValidateTrigger(descriptors);
        }
    }
}
