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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace JSSoft.Library.Commands
{
    public abstract class CommandMethodDescriptor
    {
        protected CommandMethodDescriptor(MethodInfo methodInfo)
        {
            this.MethodInfo = methodInfo;
        }

        public abstract string DescriptorName { get; }

        public abstract string Name { get; }

        public abstract string DisplayName { get; }

        public abstract CommandMemberDescriptor[] Members { get; }

        public abstract string Summary { get; }

        public abstract string Description { get; }

        public abstract bool IsAsync { get; }

        public MethodInfo MethodInfo { get; }

        protected abstract void OnInvoke(object instance, object[] parameters);

        protected virtual bool OnCanExecute(object instance)
        {
            return true;
        }

        internal bool CanExecute(object instance)
        {
            return this.OnCanExecute(instance);
        }

        internal void Invoke(object instance, string arguments, IEnumerable<CommandMemberDescriptor> descriptors)
        {
            var parser = new ParseDescriptor(descriptors, arguments);
            var values = new ArrayList();
            var nameToDescriptors = descriptors.ToDictionary(item => item.DescriptorName);
            var parameters = this.MethodInfo.GetParameters();
            parser.SetValue(instance);
            foreach (var item in parameters)
            {
                var descriptor = nameToDescriptors[item.Name];
                var value = descriptor.GetValueInternal(instance);
                values.Add(value);
            }
            this.OnInvoke(instance, values.ToArray());
        }

        internal void Invoke(object instance, IEnumerable<CommandMemberDescriptor> descriptors)
        {
            var values = new ArrayList();
            var nameToDescriptors = descriptors.ToDictionary(item => item.DescriptorName);
            var parameters = this.MethodInfo.GetParameters();
            foreach (var item in parameters)
            {
                var descriptor = nameToDescriptors[item.Name];
                var value = descriptor.GetValueInternal(instance);
                values.Add(value);
            }
            this.OnInvoke(instance, values.ToArray());
        }
    }
}
