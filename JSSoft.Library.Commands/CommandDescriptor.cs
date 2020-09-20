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
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace JSSoft.Library.Commands
{
    public static class CommandDescriptor
    {
        private static readonly Dictionary<Type, CommandMethodDescriptorCollection> typeToMethodDescriptors = new Dictionary<Type, CommandMethodDescriptorCollection>();
        private static readonly Dictionary<Type, CommandMemberDescriptorCollection> membersByType = new Dictionary<Type, CommandMemberDescriptorCollection>();
        private static readonly Dictionary<object, CommandMemberDescriptorCollection> membersByInstance = new Dictionary<object, CommandMemberDescriptorCollection>();
        private static readonly Dictionary<ICustomAttributeProvider, CommandMemberDescriptorCollection> providerToMemberDescriptors = new Dictionary<ICustomAttributeProvider, CommandMemberDescriptorCollection>();
        private static readonly Dictionary<ICustomAttributeProvider, CommandMethodDescriptorCollection> providerToMethodDescriptors = new Dictionary<ICustomAttributeProvider, CommandMethodDescriptorCollection>();
        private static readonly Dictionary<Type, IUsageDescriptionProvider> typeToUsageDescriptionProvider = new Dictionary<Type, IUsageDescriptionProvider>();

        public static IUsageDescriptionProvider GetUsageDescriptionProvider(Type type)
        {
            var attribute = type.GetCustomAttribute<UsageDescriptionProviderAttribute>();
            if (attribute == null)
                return UsageDescriptionProvider.Default;
            if (typeToUsageDescriptionProvider.ContainsKey(type) == false)
            {
                typeToUsageDescriptionProvider.Add(type, attribute.CreateInstanceInternal(type));
            }
            return typeToUsageDescriptionProvider[type];
        }

        public static CommandMethodDescriptor GetMethodDescriptor(Type type, string methodName)
        {
            return GetMethodDescriptors(type)[methodName];
        }

        public static CommandMethodDescriptorCollection GetMethodDescriptors(Type type)
        {
            if (typeToMethodDescriptors.ContainsKey(type) == false)
            {
                typeToMethodDescriptors.Add(type, CreateMethodDescriptors(type));
            }
            return typeToMethodDescriptors[type];
        }

        public static CommandMethodDescriptorCollection GetStaticMethodDescriptors(ICustomAttributeProvider provider)
        {
            if (providerToMethodDescriptors.ContainsKey(provider) == false)
            {
                providerToMethodDescriptors.Add(provider, CreateStaticMethodDescriptors(provider));
            }

            return providerToMethodDescriptors[provider];
        }

        public static CommandMemberDescriptorCollection GetStaticMemberDescriptors(ICustomAttributeProvider provider)
        {
            if (providerToMemberDescriptors.ContainsKey(provider) == false)
            {
                providerToMemberDescriptors.Add(provider, CreateStaticMemberDescriptors(provider));
            }

            return providerToMemberDescriptors[provider];
        }

        public static CommandMemberDescriptorCollection GetMemberDescriptors(object instance)
        {
            if (instance is IMemberDescriptorProvider provider)
            {
                if (membersByInstance.ContainsKey(provider) == false)
                {
                    membersByInstance.Add(provider, new CommandMemberDescriptorCollection(provider.Members));
                }
                return membersByInstance[provider];
            }
            else
            {
                var type = instance is Type t ? t : instance.GetType();
                if (membersByType.ContainsKey(type) == false)
                {
                    membersByType.Add(type, CreateMemberDescriptors(type));
                }
                return membersByType[type];
            }
        }

        public static CommandMemberDescriptorCollection CreateStaticMemberDescriptors(ICustomAttributeProvider provider)
        {
            var descriptors = new CommandMemberDescriptorCollection();
            var attrs = provider.GetCustomAttributes(typeof(CommandStaticPropertyAttribute), true);

            foreach (var item in attrs)
            {
                if (item is CommandStaticPropertyAttribute == false)
                    continue;
                var attr = item as CommandStaticPropertyAttribute;

                var staticDescriptors = CommandDescriptor.GetMemberDescriptors(attr.StaticType);
                descriptors.AddRange(Filter(staticDescriptors, attr.PropertyNames));
            }

            return descriptors;
        }

        public static CommandMethodDescriptorCollection CreateStaticMethodDescriptors(ICustomAttributeProvider provider)
        {
            var descriptors = new CommandMethodDescriptorCollection();
            var attrs = provider.GetCustomAttributes(typeof(CommandStaticMethodAttribute), true);

            foreach (var item in attrs)
            {
                if (item is CommandStaticMethodAttribute == false)
                    continue;
                var attr = item as CommandStaticMethodAttribute;
                var staticDescriptors = CommandDescriptor.GetMethodDescriptors(attr.StaticType);
                descriptors.AddRange(Filter(staticDescriptors, attr.MethodNames));
            }

            return descriptors;
        }

        public static CommandMethodDescriptorCollection CreateMethodDescriptors(Type type)
        {
            var descriptors = new CommandMethodDescriptorCollection();

            if (CommandSettings.IsConsoleMode == false && type.GetCustomAttribute<ConsoleModeOnlyAttribute>() != null)
                return descriptors;

            foreach (var item in type.GetMethods())
            {
                var attr = item.GetCustomAttribute<CommandMethodAttribute>();
                if (attr == null)
                    continue;
                if (CommandSettings.IsConsoleMode == false && item.GetCustomAttribute<ConsoleModeOnlyAttribute>() != null)
                    continue;
                if (item.GetCustomAttribute<BrowsableAttribute>() is BrowsableAttribute browsableAttribute && browsableAttribute.Browsable == false)
                    continue;
                descriptors.Add(new StandardCommandMethodDescriptor(item));
            }

            foreach (var item in GetStaticMethodDescriptors(type))
            {
                descriptors.Add(item);
            }

            return descriptors;
        }

        private static CommandMemberDescriptorCollection CreateMemberDescriptors(Type type)
        {
            var descriptors = new CommandMemberDescriptorCollection();
            var properties = type.GetProperties();

            foreach (var item in properties)
            {
                var attr = item.GetCustomAttribute<CommandPropertyBaseAttribute>();
                if (attr == null)
                    continue;
                if (CommandSettings.IsConsoleMode == false && item.GetCustomAttribute<ConsoleModeOnlyAttribute>() != null)
                    continue;
                if (item.GetCustomAttribute<BrowsableAttribute>() is BrowsableAttribute browsableAttribute && browsableAttribute.Browsable == false)
                    continue;
                if (item.CanWrite == false)
                    throw new Exception(string.Format(Resources.Exception_PropertyCannotUse_Format, item.Name));
                descriptors.Add(new CommandPropertyDescriptor(item));
            }

            foreach (var item in GetStaticMemberDescriptors(type))
            {
                descriptors.Add(item);
            }

            if (descriptors.Where(item => item.Usage == CommandPropertyUsage.Variables).Count() > 1)
                throw new InvalidOperationException(string.Format(Resources.Exception_VariablesCannotBeUsedAsMultiple_Format, nameof(CommandPropertyUsage.Variables)));

            descriptors.Sort();

            return descriptors;
        }

        private static IEnumerable<CommandMemberDescriptor> Filter(CommandMemberDescriptorCollection descriptors, params string[] propertyNames)
        {
            if (propertyNames.Any() == false)
            {
                foreach (var item in descriptors)
                {
                    yield return item;
                }
            }
            else
            {
                foreach (var item in propertyNames)
                {
                    yield return descriptors[item];
                }
            }
        }

        private static IEnumerable<CommandMethodDescriptor> Filter(CommandMethodDescriptorCollection descriptors, params string[] methodNames)
        {
            if (methodNames.Any() == false)
            {
                foreach (var item in descriptors)
                {
                    yield return item;
                }
            }
            else
            {
                foreach (var item in methodNames)
                {
                    yield return descriptors[item];
                }
            }
        }
    }
}
