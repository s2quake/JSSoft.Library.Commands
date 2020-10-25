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

using JSSoft.Library.Commands.Extensions;
using JSSoft.Library.Commands.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace JSSoft.Library.Commands
{
    public sealed class CommandPropertyDescriptor : CommandMemberDescriptor
    {
        private readonly PropertyInfo propertyInfo;
        private readonly CommandPropertyTriggerAttribute[] triggers;

        public CommandPropertyDescriptor(PropertyInfo propertyInfo)
            : base(propertyInfo.GetCommandPropertyAttribute(), propertyInfo.Name)
        {
            this.propertyInfo = propertyInfo;
            this.triggers = propertyInfo.GetTriggerAttributes();
            this.MemberType = propertyInfo.PropertyType;
            this.Summary = propertyInfo.GetSummary();
            this.Description = propertyInfo.GetDescription();
            if (this.Usage == CommandPropertyUsage.Variables && this.MemberType.IsArray == false)
                throw new InvalidOperationException(string.Format(Resources.Exception_VariablesPropertyMustBeAnArrayType_Format, nameof(CommandPropertyUsage.Variables)));
            if (this.Usage == CommandPropertyUsage.Switch && this.MemberType != typeof(bool))
                throw new InvalidOperationException("Only the bool type can be used as a switch.");
        }

        public override string DisplayName
        {
            get
            {
                var displayName = propertyInfo.GetDisplayName();
                if (displayName != string.Empty)
                    return displayName;
                if (Usage == CommandPropertyUsage.Variables)
                    return $"{base.DisplayName}...";
                return base.DisplayName;
            }
        }

        public override Type MemberType { get; }

        public override string Summary { get; }

        public override string Description { get; }

        protected override void SetValue(object instance, object value)
        {
            this.propertyInfo.SetValue(instance, value, null);
        }

        protected override object GetValue(object instance)
        {
            return this.propertyInfo.GetValue(instance, null);
        }

        protected override void OnValidateTrigger(ParseDescriptorItem[] parseItems)
        {
            if (this.triggers.Any() == false)
                return;

            var query = from item in this.triggers
                        group item by item.Group into groups
                        select groups;

            var descriptorByName = parseItems.ToDictionary(item => item.Descriptor.DescriptorName, item => item.Descriptor);
            var infoByDescriptor = parseItems.ToDictionary(item => item.Descriptor);

            foreach (var items in query)
            {
                foreach (var item in items)
                {
                    if (descriptorByName.ContainsKey(item.PropertyName) == false)
                        throw new InvalidOperationException(string.Format(Resources.Exception_PropertyDoesNotExists_Format, item.PropertyName));
                    var triggerDescriptor = descriptorByName[item.PropertyName];
                    if (triggerDescriptor is CommandPropertyDescriptor == false)
                        throw new InvalidOperationException(string.Format(Resources.Exception_NotProperty_Format, item.PropertyName));

                    var parseInfo = infoByDescriptor[triggerDescriptor];
                    var value1 = parseInfo.ActualValue;
                    var value2 = GetDefaultValue(triggerDescriptor.MemberType, item.Value);

                    if (item.IsInequality == false)
                    {
                        if (object.Equals(value1, value2) == false)
                            throw new InvalidOperationException(string.Format(Resources.Exception_Trigger_CannotUseProperty_Format, this.DisplayName, triggerDescriptor.DisplayName, value2));
                    }
                    else
                    {
                        if (object.Equals(value1, value2) == true)
                            throw new InvalidOperationException(string.Format(Resources.Exception_Trigger_CannotUsePropertyNot_Format, this.DisplayName, triggerDescriptor.DisplayName, value2));
                    }
                }
            }
        }

        private static object GetDefaultValue(Type propertyType, object value)
        {
            if (value == null)
            {
                if (propertyType.IsClass == false)
                    return DBNull.Value;
                return null;
            }
            if (value.GetType() == propertyType)
                return value;
            if (propertyType.IsArray == true)
            {
                if (value is IEnumerable enumerable)
                {
                    var itemList = new List<object>();
                    var elementType = propertyType.GetElementType();
                    var elementConverter = TypeDescriptor.GetConverter(elementType);
                    foreach (var item in enumerable)
                    {
                        itemList.Add(elementConverter.ConvertFrom(item));
                    }
                    return itemList.ToArray();
                }
            }
            return TypeDescriptor.GetConverter(propertyType).ConvertFrom(value);
        }

        private static object GetDefaultValue(PropertyInfo propertyInfo)
        {
            var attr = propertyInfo.GetCustomAttribute<DefaultValueAttribute>();
            var propertyType = propertyInfo.PropertyType;
            if (attr == null)
                return DBNull.Value;
            return GetDefaultValue(propertyType, attr.Value);
        }
    }
}
