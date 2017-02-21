﻿using Ntreev.Library.Commands.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ntreev.Library.Commands
{
    public class CommandMethodDescriptorCollection : IReadOnlyList<CommandMethodDescriptor>
    {
        private readonly List<CommandMethodDescriptor> descriptors = new List<CommandMethodDescriptor>();

        internal CommandMethodDescriptorCollection()
        {

        }

        public CommandMethodDescriptor this[string name]
        {
            get
            {
                var query = from item in descriptors
                            where item.Name == name
                            select item;

                if (query.Any() == false)
                    throw new KeyNotFoundException(string.Format(Resources.MethodDoesNotExist_Format, name));

                return query.First();
            }
        }

        public CommandMethodDescriptor this[int index]
        {
            get { return this.descriptors[index]; }
        }

        public int Count
        {
            get { return this.descriptors.Count; }
        }

        internal void Add(CommandMethodDescriptor item)
        {
            this.descriptors.Add(item);
        }

        internal void AddRange(IEnumerable<CommandMethodDescriptor> descriptors)
        {
            foreach (var item in descriptors)
            {
                this.Add(item);
            }
        }

        #region IEnumerable

        IEnumerator<CommandMethodDescriptor> IEnumerable<CommandMethodDescriptor>.GetEnumerator()
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