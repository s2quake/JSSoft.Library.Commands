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

namespace JSSoft.Library.Commands
{
    public class CommandMemberDescriptorCollection : IEnumerable<CommandMemberDescriptor>
    {
        private readonly List<CommandMemberDescriptor> descriptors = new List<CommandMemberDescriptor>();

        internal CommandMemberDescriptorCollection()
        {

        }

        internal CommandMemberDescriptorCollection(ICommandDescriptor descriptor)
        {
            foreach (var item in descriptor.Members)
            {
                this.descriptors.Add(item);
            }
        }

        public CommandMemberDescriptor this[string name]
        {
            get
            {
                if (name is null)
                    throw new ArgumentNullException(nameof(name));
                var query = from item in this.descriptors
                            where item.DescriptorName == name
                            select item;
                if (query.Any() == false)
                    throw new KeyNotFoundException(string.Format(Resources.Exception_MemberDoesNotExist_Format, name));
                return query.First();
            }
        }

        public CommandMemberDescriptor this[int index] => this.descriptors[index];

        public int Count => this.descriptors.Count;

        internal void Sort()
        {
            var query = from item in this.descriptors
                        orderby item.InitValue == DBNull.Value descending
                        orderby item.Usage == CommandPropertyUsage.Required
                        orderby item.Usage == CommandPropertyUsage.Variables
                        orderby item.Usage == CommandPropertyUsage.ExplicitRequired
                        orderby item.Usage == CommandPropertyUsage.General
                        select item;
            var items = query.ToArray();
            this.descriptors.Clear();
            this.descriptors.AddRange(items);
        }

        internal void Add(CommandMemberDescriptor descriptor)
        {
            foreach (var item in this.descriptors)
            {
                if (item.Name != string.Empty && descriptor.Name != string.Empty && descriptor.Name == item.Name)
                {
                    throw new ArgumentException(string.Format(Resources.Exception_NameAlreadyExists_Format, descriptor.Name), nameof(descriptor));
                }

                if (item.ShortName != string.Empty && descriptor.ShortName != string.Empty && descriptor.ShortName == item.ShortName)
                {
                    throw new ArgumentException(string.Format(Resources.Exception_NameAlreadyExists_Format, descriptor.ShortName), nameof(descriptor));
                }
            }

            this.descriptors.Add(descriptor);
        }

        internal void AddRange(IEnumerable<CommandMemberDescriptor> descriptors)
        {
            foreach (var item in descriptors)
            {
                this.Add(item);
            }
        }

        #region IEnumerable

        IEnumerator<CommandMemberDescriptor> IEnumerable<CommandMemberDescriptor>.GetEnumerator()
        {
            return this.descriptors.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.descriptors.GetEnumerator();
        }

        #endregion
    }
}
