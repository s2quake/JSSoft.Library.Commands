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
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ntreev.Library.Commands
{
    static class MethodInfoExtensions
    {
        public static string GetSummary(this MethodInfo methodInfo)
        {
            return CommandDescriptor.GetUsageDescriptionProvider(methodInfo.DeclaringType).GetSummary(methodInfo);
        }

        public static string GetDescription(this MethodInfo methodInfo)
        {
            return CommandDescriptor.GetUsageDescriptionProvider(methodInfo.DeclaringType).GetDescription(methodInfo);
        }

        public static bool IsAsync(this MethodInfo methodInfo)
        {
            return methodInfo.ReturnType.IsAssignableFrom(typeof(Task));
        }

        public static string GetName(this MethodInfo methodInfo)
        {
            var isAsync = methodInfo.IsAsync();
            var methodName = methodInfo.Name;
            if (isAsync == true && methodName.EndsWith("Async") == true)
                methodName = methodName.Substring(0, methodName.Length - "Async".Length);
            var attribute = methodInfo.GetCommandMethodAttribute();
            return attribute.Name != string.Empty ? attribute.Name : CommandSettings.NameGenerator(methodName);
        }

        public static CommandMemberDescriptor[] GetMemberDescriptors(this MethodInfo methodInfo)
        {
            var memberList = new List<CommandMemberDescriptor>();

            foreach (var item in methodInfo.GetParameters())
            {
                if (item.GetCustomAttribute<ParamArrayAttribute>() != null)
                {
                    memberList.Add(new CommandParameterArrayDescriptor(item));
                }
                else
                {
                    memberList.Add(new CommandParameterDescriptor(item));
                }
            }

            var methodAttr = methodInfo.GetCustomAttribute<CommandMethodPropertyAttribute>();
            if (methodAttr != null)
            {
                foreach (var item in methodAttr.PropertyNames)
                {
                    var memberDescriptor = CommandDescriptor.GetMemberDescriptors(methodInfo.DeclaringType)[item];
                    if (memberDescriptor == null)
                        throw new ArgumentException(string.Format("'{0}' attribute does not existed .", item));
                    memberList.Add(memberDescriptor);
                }
            }

            var staticAttrs = methodInfo.GetCustomAttributes(typeof(CommandMethodStaticPropertyAttribute), true);
            foreach (var item in staticAttrs)
            {
                if (item is CommandMethodStaticPropertyAttribute attr)
                {
                    var memberDescriptors = CommandDescriptor.GetMemberDescriptors(attr.StaticType);
                    memberList.AddRange(memberDescriptors);
                }
            }

            var query = from item in memberList
                        orderby item.IsRequired == false
                        orderby item.DefaultValue != DBNull.Value
                        orderby item is CommandMemberArrayDescriptor
                        select item;

            return query.ToArray();
        }
    }
}
