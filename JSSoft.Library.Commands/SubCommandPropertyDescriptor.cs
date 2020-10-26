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

namespace JSSoft.Library.Commands
{
    sealed class SubCommandPropertyDescriptor : CommandMemberDescriptor
    {
        private readonly object instance;
        private readonly CommandMemberDescriptor descriptor;

        public SubCommandPropertyDescriptor(object instance, CommandMemberDescriptor descriptor)
            : base(new CommandPropertyAttribute(), descriptor.DescriptorName)
        {
            this.instance = instance;
            this.descriptor = descriptor;
        }

        public override string DisplayName => this.descriptor.DisplayName;

        public override Type MemberType => this.descriptor.MemberType;

        public override string Summary => this.descriptor.Summary;

        public override string Description => this.descriptor.Description;

        public override object InitValue => this.descriptor.InitValue;

        public override object DefaultValue => this.descriptor.DefaultValue;

        public override bool IsRequired => this.descriptor.IsRequired;

        public override bool IsExplicit => this.descriptor.IsExplicit;

        public override bool IsSwitch => this.descriptor.IsSwitch;

        public override string NamePattern => this.descriptor.NamePattern;

        public override string ShortNamePattern => this.descriptor.ShortNamePattern;

        public override CommandPropertyUsage Usage => this.descriptor.Usage;

        protected override void SetValue(object instance, object value)
        {
            this.descriptor.SetValueInternal(this.instance, value);
        }

        protected override object GetValue(object instance)
        {
            return this.descriptor.GetValueInternal(this.instance);
        }

        protected override void OnValidateTrigger(ParseDescriptorItem[] parseItems)
        {
            this.descriptor.ValidateTrigger(parseItems);
        }
    }
}
