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

namespace JSSoft.Library.Commands.Invoke
{
    [CommandStaticMethod(typeof(StaticCommand))]
    class Commands
    {
        public Commands()
        {
            this.Message = string.Empty;
        }

        [CommandMethod("init")]
        [CommandMethodStaticProperty(typeof(GlobalSettings))]
        [CommandMethodProperty(nameof(Message))]
        public void Initialize(string path)
        {
            Console.WriteLine("{0} initialized.", path);
        }

        [CommandMethod]
        [CommandMethodProperty(nameof(Message))]
        public void Add(params string[] paths)
        {
            foreach (var item in paths)
            {
                Console.WriteLine("{0} added.", item);
            }
        }

        [CommandMethod]
        public void Update(string path)
        {
            Console.WriteLine("{0} updated.", path);
        }

        [CommandMethod]
        public void Delete(params string[] paths)
        {
            foreach (var item in paths)
            {
                Console.WriteLine("{0} deleted.", item);
            }
        }

        [CommandMethod]
        [CommandMethodProperty(nameof(Message))]
        public void Commit(string path)
        {
            if (this.Message == string.Empty)
                Console.WriteLine("{0} committed.", path);
            else
                Console.WriteLine("{0} committed. : {1}", path, this.Message);
        }

        [CommandProperty('m', AllowName = true, Usage = CommandPropertyUsage.ExplicitRequired)]
        [ConsoleModeOnly]
        public string Message
        {
            get; set;
        }
    }
}
