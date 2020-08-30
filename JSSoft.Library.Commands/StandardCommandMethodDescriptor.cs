﻿// Released under the MIT License.
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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace JSSoft.Library.Commands
{
    class StandardCommandMethodDescriptor : CommandMethodDescriptor
    {
        private readonly CommandMemberDescriptor[] members;

        public StandardCommandMethodDescriptor(MethodInfo methodInfo)
            : base(methodInfo)
        {
            this.DescriptorName = methodInfo.Name;
            this.IsAsync = methodInfo.IsAsync();
            this.Name = methodInfo.GetName();
            this.DisplayName = methodInfo.GetDisplayName();
            this.Summary = methodInfo.GetSummary();
            this.Description = methodInfo.GetDescription();
            this.members = methodInfo.GetMemberDescriptors();
        }

        public override string DescriptorName { get; }

        public override string Name { get; }

        public override string DisplayName { get; }

        public override CommandMemberDescriptor[] Members => this.members.ToArray();

        public override string Summary { get; }

        public override string Description { get; }

        public override bool IsAsync { get; }

        protected override object OnInvoke(object instance, object[] parameters)
        {
            if (this.MethodInfo.DeclaringType.IsAbstract && this.MethodInfo.DeclaringType.IsSealed == true)
            {
                return this.MethodInfo.Invoke(null, parameters);
            }
            else
            {
                return this.MethodInfo.Invoke(instance, parameters);
            }
        }
    }
}
