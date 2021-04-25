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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Library.Commands.Extensions
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

        public static string GetExample(this MethodInfo methodInfo)
        {
            return CommandDescriptor.GetUsageDescriptionProvider(methodInfo.DeclaringType).GetExample(methodInfo);
        }

        public static bool IsAsync(this MethodInfo methodInfo)
        {
            return methodInfo.ReturnType.IsAssignableFrom(typeof(Task));
        }

        public static string GetName(this MethodInfo methodInfo)
        {
            var methodName = GetPureName(methodInfo);
            var attribute = methodInfo.GetCommandMethodAttribute();
            return attribute.Name != string.Empty ? attribute.Name : CommandSettings.NameGenerator(methodName);
        }

        public static string[] GetAliases(this MethodInfo methodInfo)
        {
            var attribute = methodInfo.GetCommandMethodAttribute();
            return attribute.Aliases;
        }

        public static string GetPureName(this MethodInfo methodInfo)
        {
            var isAsync = methodInfo.IsAsync();
            var methodName = methodInfo.Name;
            if (isAsync == true && methodName.EndsWith("Async") == true)
                methodName = methodName.Substring(0, methodName.Length - "Async".Length);
            return methodName;
        }

        public static PropertyInfo GetCanExecutableProperty(this MethodInfo methodInfo)
        {
            if (methodInfo.GetCustomAttribute<CommandMethodValidationAttribute>() is CommandMethodValidationAttribute attribute)
            {
                var instanceType = attribute.Type ?? methodInfo.DeclaringType;
                var propertyName = attribute.PropertyName;
                var bindingFlags1 = attribute.Type != null ? BindingFlags.Static : BindingFlags.Instance;
                var bindingFlags2 = BindingFlags.Public | BindingFlags.NonPublic | bindingFlags1;
                return instanceType.GetProperty(propertyName, bindingFlags2);
            }
            else
            {
                var instanceType = methodInfo.DeclaringType;
                var propertyName = $"Can{GetPureName(methodInfo)}";
                var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                return instanceType.GetProperty(propertyName, bindingFlags);
            }
        }

        public static MethodInfo GetCompletionMethod(this MethodInfo methodInfo)
        {
            var instanceType = methodInfo.DeclaringType;
            var isAsync = methodInfo.IsAsync();
            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
            if (isAsync == true)
            {
                var asyncName = $"Complete{GetPureName(methodInfo)}Async";
                var asyncMethod = instanceType.GetMethod(asyncName, bindingFlags);
                if (IsCompletionAsyncMethod(asyncMethod) == true)
                    return asyncMethod;
            }
            var name = $"Complete{GetPureName(methodInfo)}";
            var method = instanceType.GetMethod(name, bindingFlags);
            if (IsCompletionMethod(method) == true)
                return method;
            return null;
        }

        public static CommandMemberDescriptor[] GetMemberDescriptors(this MethodInfo methodInfo)
        {
            var memberList = new List<CommandMemberDescriptor>();
            foreach (var item in methodInfo.GetParameters())
            {
                if (item.ParameterType == typeof(CancellationToken))
                    continue;
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
                        throw new ArgumentException(string.Format(Resources.Exception_AttributeDoesNotExists_Format, item));
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
                        orderby item.DefaultValue != DBNull.Value
                        orderby item.Usage
                        select item;

            return query.ToArray();
        }

        private static bool IsCompletionMethod(MethodInfo methodInfo)
        {
            if (methodInfo != null && methodInfo.ReturnType == typeof(string[]))
            {
                var parameters = methodInfo.GetParameters();
                if (parameters.Length == 2 && parameters[0].ParameterType == typeof(CommandMemberDescriptor) && parameters[1].ParameterType == typeof(string))
                    return true;
            }
            return false;
        }

        private static bool IsCompletionAsyncMethod(MethodInfo methodInfo)
        {
            if (methodInfo != null && methodInfo.ReturnType == typeof(Task<string[]>))
            {
                var parameters = methodInfo.GetParameters();
                if (parameters.Length == 2 && parameters[0].ParameterType == typeof(CommandMemberDescriptor) && parameters[1].ParameterType == typeof(string))
                    return true;
            }
            return false;
        }
    }
}
