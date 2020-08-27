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
using System;
using System.Reflection;

namespace JSSoft.Library.Commands
{
    public sealed class CommandParameterDescriptor : CommandMemberDescriptor
    {
        private object value;

        public CommandParameterDescriptor(ParameterInfo parameterInfo)
            : base(new CommandPropertyRequiredAttribute(), parameterInfo.Name)
        {
            this.value = parameterInfo.DefaultValue;
            this.Summary = parameterInfo.GetSummary();
            this.Description = parameterInfo.GetDescription();
            this.InitValue = parameterInfo.DefaultValue;
            this.MemberType = parameterInfo.ParameterType;
        }

        public override string Summary { get; }

        public override string Description { get; }

        public override object InitValue { get; }

        public override Type MemberType { get; }

        protected override void SetValue(object instance, object value)
        {
            this.value = value;
        }

        protected override object GetValue(object instance)
        {
            return this.value;
        }
    }
}
