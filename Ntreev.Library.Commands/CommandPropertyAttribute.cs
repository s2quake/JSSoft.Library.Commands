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
using Ntreev.Library.Commands.Properties;
using System.Text.RegularExpressions;

namespace Ntreev.Library.Commands
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class CommandPropertyAttribute : Attribute
    {
        public CommandPropertyAttribute()
        {
        }

        public CommandPropertyAttribute(string name)
            : this(name, char.MinValue)
        {

        }

        public CommandPropertyAttribute(string name, char shortName)
        { 
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length <= 1)
                throw new ArgumentException("name length must be greater than 1", nameof(name));
            CommandSettings.ValidateIdentifier(name);
            if (shortName != char.MinValue && Regex.IsMatch(shortName.ToString(), "[a-z]", RegexOptions.IgnoreCase) == false)
                throw new ArgumentException("shortName must be a alphabet character");
            this.Name = name;
            this.ShortName = shortName;
        }

        public CommandPropertyAttribute(char shortName)
        {
            if (shortName != char.MinValue && Regex.IsMatch(shortName.ToString(), "[a-z]", RegexOptions.IgnoreCase) == false)
                throw new ArgumentException("shortName must be a alphabet character", nameof(shortName));
            this.ShortName = shortName;
            this.AllowName = false;
        }

        public string Name { get; } = string.Empty;

        public char ShortName { get; }

        public bool AllowName { get; set; } = true;

        public CommandPropertyUsage Usage { get; set; } = CommandPropertyUsage.General;

        public object DefaultValue { get; set; } = DBNull.Value;

        protected virtual void Validate(object target)
        {
            if (this.Usage == CommandPropertyUsage.Variables)
                throw new InvalidOperationException($"use {nameof(CommandPropertyArrayAttribute)} instead.");
            if (this.IsExplicit == false && this.DefaultValue != DBNull.Value)
                throw new InvalidOperationException($"non explicit property does not have {nameof(DefaultValue)}: '{this.DefaultValue}'.");
        }

        internal void InvokeValidate(object target)
        {
            this.Validate(target);
        }

        internal string GetName(string descriptorName)
        {
            if (this.Name == string.Empty)
            {
                if (this.AllowName == true)
                    return CommandSettings.NameGenerator(descriptorName);
                return string.Empty;
            }
            return this.Name;
        }

        internal string InternalShortName => this.ShortName == char.MinValue ? string.Empty : this.ShortName.ToString();

        internal bool IsRequired => this.Usage == CommandPropertyUsage.Required || this.Usage == CommandPropertyUsage.ExplicitRequired;

        internal bool IsExplicit => this.Usage == CommandPropertyUsage.General || this.Usage == CommandPropertyUsage.ExplicitRequired;
    }
}