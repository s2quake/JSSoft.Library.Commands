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

using System.ComponentModel;
using System.Reflection;

namespace Ntreev.Library.Commands
{
    public class UsageDescriptionProvider : IUsageDescriptionProvider
    {
        public string GetDescription(PropertyInfo propertyInfo)
        {
            return ToDescription(propertyInfo);
        }

        public string GetDescription(ParameterInfo parameterInfo)
        {
            return ToDescription(parameterInfo);
        }

        public string GetDescription(object instance)
        {
            return ToDescription(instance.GetType());
        }

        public string GetDescription(MethodInfo methodInfo)
        {
            return ToDescription(methodInfo);
        }

        public string GetSummary(PropertyInfo propertyInfo)
        {
            return ToSummary(propertyInfo);
        }

        public string GetSummary(ParameterInfo parameterInfo)
        {
            return ToSummary(parameterInfo);
        }

        public string GetSummary(object instance)
        {
            return ToSummary(instance.GetType());
        }

        public string GetSummary(MethodInfo methodInfo)
        {
            return ToSummary(methodInfo);
        }

        public static readonly UsageDescriptionProvider Default = new UsageDescriptionProvider();

        public static string ToSummary(ICustomAttributeProvider customAttributeProvider)
        {
            var attribute = customAttributeProvider.GetCustomAttribute<SummaryAttribute>();
            if (attribute == null)
                return string.Empty;
            return attribute.Summary;
        }

        public static string ToDescription(ICustomAttributeProvider customAttributeProvider)
        {
            var attribute = customAttributeProvider.GetCustomAttribute<DescriptionAttribute>();
            if (attribute == null)
                return string.Empty;
            return attribute.Description;
        }
    }
}
