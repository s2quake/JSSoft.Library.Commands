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

namespace JSSoft.Library.Commands
{
    /// <summary>
    /// CommandMethod로 사용할 클래스에 추가로 사용될 CommandMethodAttribute가 정의되어 있는 static class 타입을 설정합니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class CommandStaticMethodAttribute : Attribute
    {
        public CommandStaticMethodAttribute(string typeName, params string[] methodNames)
            : this(Type.GetType(typeName), methodNames)
        {
        }

        public CommandStaticMethodAttribute(Type type, params string[] methodNames)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (type.GetConstructor(Type.EmptyTypes) == null && type.IsAbstract && type.IsSealed)
            {
                this.StaticType = type;
                this.TypeName = type.AssemblyQualifiedName;
            }
            else
            {
                throw new InvalidOperationException(Resources.Exception_TypeIsNotStaticClass);
            }
            this.MethodNames = methodNames;
        }

        public string TypeName { get; }

        public string[] MethodNames { get; }

        internal Type StaticType { get; }
    }
}
