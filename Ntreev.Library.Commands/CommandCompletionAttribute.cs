using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ntreev.Library.Commands
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    public class CommandCompletionAttribute : Attribute
    {
        public CommandCompletionAttribute(string methodName)
        {
            this.MethodName = methodName;
        }

        public string MethodName { get; }

        public string TypeName
        {
            get => this.Type.AssemblyQualifiedName;
            set => this.Type = Type.GetType(value);
        }

        public Type Type { get; set; }
    }
}
