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
using System.Reflection;
using System.Resources;

namespace JSSoft.Library.Commands
{
    public class ResourceUsageDescriptionProvider : IUsageDescriptionProvider
    {
        public const string ReferencePrefix = "&";
        public const string DescriptionPrefix = "d:";
        public const string ExamplePrefix = "e:";

        private const string extension = ".resources";
        private readonly static Dictionary<string, ResourceManager> resourceManagers = new();
        private readonly string resourceName;

        static ResourceUsageDescriptionProvider()
        {
        }

        public ResourceUsageDescriptionProvider()
            : this(string.Empty)
        {
        }

        public ResourceUsageDescriptionProvider(string resourceName)
        {
            this.resourceName = resourceName ?? string.Empty;
        }

        public static string GetString(Assembly assembly, string resourceName, string name)
        {
            var resourceSet = GetResourceSet(resourceName, assembly);
            if (resourceSet == null)
                return null;
            return resourceSet.GetString(name);
        }

        public string GetDescription(PropertyInfo propertyInfo)
        {
            var type = propertyInfo.DeclaringType;
            var name = propertyInfo.Name;
            var id = $"{type.Name}.{name}";
            if (type.DeclaringType != null)
                id = $"{type.DeclaringType.Name}.{id}";
            var description = this.GetResourceDescription(propertyInfo.DeclaringType, id);
            if (description != null)
                return description;
            return UsageDescriptionProvider.Default.GetDescription(propertyInfo);
        }

        public string GetDescription(ParameterInfo parameterInfo)
        {
            var method = parameterInfo.Member;
            var type = method.DeclaringType;
            var name = parameterInfo.Name;
            var id = $"{type.Name}.{method.Name}.{name}";
            var description = this.GetResourceDescription(parameterInfo.Member.DeclaringType, id);
            if (description != null)
                return description;
            return UsageDescriptionProvider.Default.GetDescription(parameterInfo);
        }

        public string GetDescription(object instance)
        {
            var id = instance.GetType().Name;
            var description = this.GetResourceDescription(instance.GetType(), id);
            if (description != null)
                return description;
            return UsageDescriptionProvider.Default.GetDescription(instance);
        }

        public string GetDescription(MethodInfo methodInfo)
        {
            var type = methodInfo.DeclaringType;
            var name = methodInfo.Name;
            var id = $"{type.Name}.{name}";
            if (type.DeclaringType != null)
                id = $"{type.DeclaringType.Name}.{id}";
            var description = this.GetResourceDescription(methodInfo.DeclaringType, id);
            if (description != null)
                return description;
            return UsageDescriptionProvider.Default.GetDescription(methodInfo);
        }

        public string GetSummary(PropertyInfo propertyInfo)
        {
            var type = propertyInfo.DeclaringType;
            var name = propertyInfo.Name;
            var id = $"{type.Name}.{name}";
            if (type.DeclaringType != null)
                id = $"{type.DeclaringType.Name}.{id}";
            var summary = this.GetResourceSummary(propertyInfo.DeclaringType, id);
            if (summary != null)
                return summary;
            return UsageDescriptionProvider.Default.GetSummary(propertyInfo);
        }

        public string GetSummary(ParameterInfo parameterInfo)
        {
            var method = parameterInfo.Member;
            var type = method.DeclaringType;
            var name = parameterInfo.Name;
            var id = $"{type.Name}.{method.Name}.{name}";
            var summary = this.GetResourceSummary(parameterInfo.Member.DeclaringType, id);
            if (summary != null)
                return summary;
            return UsageDescriptionProvider.Default.GetSummary(parameterInfo);
        }

        public string GetSummary(object instance)
        {
            var id = instance.GetType().Name;
            var summary = this.GetResourceSummary(instance.GetType(), id);
            if (summary != null)
                return summary;
            return UsageDescriptionProvider.Default.GetSummary(instance);
        }

        public string GetSummary(MethodInfo methodInfo)
        {
            var type = methodInfo.DeclaringType;
            var name = methodInfo.Name;
            var id = $"{type.Name}.{name}";
            if (type.DeclaringType != null)
                id = $"{type.DeclaringType.Name}.{id}";
            var summary = this.GetResourceSummary(methodInfo.DeclaringType, id);
            if (summary != null)
                return summary;
            return UsageDescriptionProvider.Default.GetSummary(methodInfo);
        }

        public string GetExample(object instance)
        {
            var id = instance.GetType().Name;
            var example = this.GetResourceExample(instance.GetType(), id);
            if (example != null)
                return example;
            return UsageDescriptionProvider.Default.GetExample(instance);
        }

        public string GetExample(MethodInfo methodInfo)
        {
            var type = methodInfo.DeclaringType;
            var name = methodInfo.Name;
            var id = $"{type.Name}.{name}";
            if (type.DeclaringType != null)
                id = $"{type.DeclaringType.Name}.{id}";
            var example = this.GetResourceExample(methodInfo.DeclaringType, id);
            if (example != null)
                return example;
            return UsageDescriptionProvider.Default.GetExample(methodInfo);
        }

        private string GetResourceDescription(Type type, string name)
        {
            var resourceManager = GetResourceSet(this.resourceName, type);
            if (resourceManager == null)
                return null;
            return GetString(resourceManager, $"{DescriptionPrefix}{name}");
        }

        private string GetResourceSummary(Type type, string name)
        {
            var resourceManager = GetResourceSet(this.resourceName, type);
            if (resourceManager == null)
                return null;
            return GetString(resourceManager, name);
        }

        private string GetResourceExample(Type type, string name)
        {
            var resourceManager = GetResourceSet(this.resourceName, type);
            if (resourceManager == null)
                return null;
            return GetString(resourceManager, $"{ExamplePrefix}{name}");
        }

        private static ResourceManager GetResourceSet(string resourceName, Type type)
        {
            var resourceNames = type.Assembly.GetManifestResourceNames();
            var baseName = resourceName == string.Empty ? type.FullName : resourceName;

            if (resourceNames.Contains(baseName + extension) == false)
                return null;

            if (resourceManagers.ContainsKey(baseName) == false)
                resourceManagers.Add(baseName, new ResourceManager(baseName, type.Assembly));

            return resourceManagers[baseName];
        }

        private static ResourceManager GetResourceSet(string resourceName, Assembly assembly)
        {
            var resourceNames = assembly.GetManifestResourceNames();
            var baseName = resourceName;

            if (resourceNames.Contains(baseName + extension) == false)
                return null;

            if (resourceManagers.ContainsKey(baseName) == false)
                resourceManagers.Add(baseName, new ResourceManager(baseName, assembly));

            return resourceManagers[baseName];
        }

        private static string GetString(ResourceManager resourceManager, string id)
        {
            var text = resourceManager.GetString(id);
            if (text != null && text.StartsWith(ReferencePrefix))
            {
                return resourceManager.GetString(text.Substring(ReferencePrefix.Length));
            }
            return text;
        }
    }
}
