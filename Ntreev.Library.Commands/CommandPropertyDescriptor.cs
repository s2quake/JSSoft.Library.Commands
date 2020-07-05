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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using Ntreev.Library.Commands.Extensions;

namespace Ntreev.Library.Commands
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
            this.Attributes = propertyInfo.GetCustomAttributes();
        }

        public override string DisplayName
        {
            get
            {
                var displayName = propertyInfo.GetDisplayName();
                if (displayName != string.Empty)
                    return displayName;
                return base.DisplayName;
            }
        }

        public override Type MemberType { get; }

        public override string Summary { get; }

        public override string Description { get; }

        public override object DefaultValue
        {
            get
            {
                if (this.IsRequired == false && this.MemberType == typeof(bool))
                    return true;
                return this.propertyInfo.GetDefaultValue();
            }
        }

        public override bool IsExplicit => this.MemberType == typeof(bool) ? true : base.IsExplicit;

        public override IEnumerable<Attribute> Attributes { get; }

        public override TypeConverter Converter => this.propertyInfo.GetConverter();

        protected override void SetValue(object instance, object value)
        {
            this.propertyInfo.SetValue(instance, value, null);
        }

        protected override object GetValue(object instance)
        {
            return this.propertyInfo.GetValue(instance, null);
        }

        protected override void OnValidateTrigger(IDictionary<CommandMemberDescriptor, ParseDescriptorItem> descriptors)
        {
            if (this.triggers.Any() == false || descriptors[this].IsParsed == false)
                return;

            var query = from item in this.triggers
                        group item by item.Group into groups
                        select groups;

            var nameToDescriptor = descriptors.Keys.ToDictionary(item => item.DescriptorName);

            foreach (var items in query)
            {
                foreach (var item in items)
                {
                    if (nameToDescriptor.ContainsKey(item.PropertyName) == false)
                        throw new InvalidOperationException(string.Format("'{0}' property does not exists.", item.PropertyName));
                    var triggerDescriptor = nameToDescriptor[item.PropertyName];
                    if (triggerDescriptor is CommandPropertyDescriptor == false)
                        throw new InvalidOperationException(string.Format("'{0}' is not property", item.PropertyName));

                    var parseInfo = descriptors[triggerDescriptor];
                    if (parseInfo.IsParsed == false)
                        continue;
                    var value1 = parseInfo.Desiredvalue;
                    var value2 = GetDefaultValue(triggerDescriptor.MemberType, item.Value);

                    if (item.IsInequality == false)
                    {
                        if (object.Equals(value1, value2) == false)
                            throw new InvalidOperationException(string.Format("'{0}' can not use. '{1}' property value must be '{2}'", this.DisplayName, triggerDescriptor.DisplayName, value2));
                    }
                    else
                    {
                        if (object.Equals(value1, value2) == true)
                            throw new InvalidOperationException(string.Format("'{0}' can not use. '{1}' property value must be not '{2}'", this.DisplayName, triggerDescriptor.DisplayName, value2));
                    }
                }
            }
        }

        private static object GetDefaultValue(Type propertyType, object value)
        {
            if (value == DBNull.Value)
                return value;
            if (value == null)
            {
                if (propertyType.IsClass == false)
                    return DBNull.Value;
                return null;
            }
            if (value.GetType() == propertyType)
                return value;
            if (propertyType.IsArray == true)
                return Parser.ParseArray(propertyType, value.ToString());
            return TypeDescriptor.GetConverter(propertyType).ConvertFrom(value);
        }
    }
}
