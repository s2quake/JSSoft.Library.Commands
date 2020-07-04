﻿//Released under the MIT License.
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
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ntreev.Library.Commands.Extensions
{
    static class PropertyInfoExtensions
    {
        public static CommandPropertyTriggerAttribute[] GetTriggerAttributes(this PropertyInfo propertyInfo)
        {
            var attrs = propertyInfo.GetCustomAttributes(typeof(CommandPropertyTriggerAttribute), true);
            var query = from item in attrs
                        where item is CommandPropertyTriggerAttribute
                        select item as CommandPropertyTriggerAttribute;
            return query.ToArray();
        }

        public static object GetDefaultValue(this PropertyInfo propertyInfo)
        {
            var attr = propertyInfo.GetCustomAttribute<DefaultValueAttribute>();
            if (attr == null)
                return DBNull.Value;
            var value = attr.Value;
            if (value == null)
            {
                if (propertyInfo.PropertyType.IsClass == false)
                    return DBNull.Value;
                return null;
            }
            if (value.GetType() == propertyInfo.PropertyType)
                return value;
            if (propertyInfo.PropertyType.IsArray == true)
                return Parser.ParseArray(propertyInfo.PropertyType, value.ToString());
            return propertyInfo.GetConverter().ConvertFrom(value);
        }

        public static TypeConverter GetConverter(this PropertyInfo propertyInfo)
        {
            return (propertyInfo as ICustomAttributeProvider).GetConverter(propertyInfo.PropertyType);
        }

        public static string GetSummary(this PropertyInfo propertyInfo)
        {
            return CommandDescriptor.GetUsageDescriptionProvider(propertyInfo.DeclaringType).GetSummary(propertyInfo);
        }

        public static string GetDescription(this PropertyInfo propertyInfo)
        {
            return CommandDescriptor.GetUsageDescriptionProvider(propertyInfo.DeclaringType).GetDescription(propertyInfo);
        }
    }
}
