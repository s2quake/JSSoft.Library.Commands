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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using Ntreev.Library.Commands.Extensions;

namespace Ntreev.Library.Commands
{
    public sealed class CommandPropertyArrayDescriptor : CommandMemberArrayDescriptor
    {
        private readonly PropertyInfo propertyInfo;

        public CommandPropertyArrayDescriptor(PropertyInfo propertyInfo)
            : base(propertyInfo.GetCommandPropertyAttribute(), propertyInfo.Name)
        {
            this.propertyInfo = propertyInfo;
            this.DisplayName = propertyInfo.GetDisplayName();
            this.MemberType = propertyInfo.PropertyType;
            this.Summary = propertyInfo.GetSummary();
            this.Description = propertyInfo.GetDescription();
            this.DefaultValue = propertyInfo.GetDefaultValue();
            this.Attributes = propertyInfo.GetCustomAttributes();
            this.IsExplicit = false;
        }

        public override string DisplayName { get; }

        public override Type MemberType { get; }

        public override string Summary { get; }

        public override string Description { get; }

        public override object DefaultValue { get; }

        public override IEnumerable<Attribute> Attributes { get; }

        public override bool IsExplicit { get; }

        protected override void SetValue(object instance, object value)
        {
            this.propertyInfo.SetValue(instance, value, null);
        }

        protected override object GetValue(object instance)
        {
            return this.propertyInfo.GetValue(instance, null);
        }
    }
}
