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
using System.Text.RegularExpressions;

namespace JSSoft.Library.Commands
{
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class CommandPropertyBaseAttribute : Attribute
    {
        protected CommandPropertyBaseAttribute()
        {
        }

        protected CommandPropertyBaseAttribute(string name)
            : this(name, char.MinValue)
        {
        }

        protected CommandPropertyBaseAttribute(string name, char shortName)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length <= 1)
                throw new ArgumentException(Resources.Exception_NameLengthMustBeGreaterThanOne, nameof(name));
            CommandSettings.ValidateIdentifier(name);
            if (shortName != char.MinValue && Regex.IsMatch(shortName.ToString(), "[a-z]", RegexOptions.IgnoreCase) == false)
                throw new ArgumentException(Resources.Exception_ShortNameMustBe_AlphabetCharacter, nameof(shortName));
            this.Name = name;
            this.ShortName = shortName;
        }

        protected CommandPropertyBaseAttribute(char shortName)
        {
            if (shortName != char.MinValue && Regex.IsMatch(shortName.ToString(), "[a-z]", RegexOptions.IgnoreCase) == false)
                throw new ArgumentException(Resources.Exception_ShortNameMustBe_AlphabetCharacter, nameof(shortName));
            this.ShortName = shortName;
            this.AllowName = false;
        }

        public string Name { get; } = string.Empty;

        public char ShortName { get; }

        public bool AllowName { get; set; } = true;

        // public object DefaultValue { get; set; } = DBNull.Value;

        protected CommandPropertyUsage Usage { get; set; } = CommandPropertyUsage.General;

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

        internal CommandPropertyUsage GetUsage()
        {
            return this.Usage;
        }

        internal string InternalShortName => this.ShortName == char.MinValue ? string.Empty : this.ShortName.ToString();

        internal bool IsRequiredProperty => this.Usage == CommandPropertyUsage.Required || this.Usage == CommandPropertyUsage.ExplicitRequired;

        internal bool IsExplicitProperty => this.Usage == CommandPropertyUsage.General || this.Usage == CommandPropertyUsage.ExplicitRequired || this.Usage == CommandPropertyUsage.Switch;

        internal bool IsSwitchProperty => this.Usage == CommandPropertyUsage.Switch;

        internal virtual object DefaultValueProperty => DBNull.Value;

        internal virtual object InitValueProperty => DBNull.Value;
    }
}
