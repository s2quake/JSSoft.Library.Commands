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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

namespace JSSoft.Library.Commands.Test
{
    [TestClass]
    public class RunScriptTest
    {
        private readonly CommandLineParser parser;

        public RunScriptTest()
        {
            this.parser = new CommandLineParser("run", this);
        }

        [TestMethod]
        public void TestMethod1()
        {
            this.parser.Parse("run --filename \"C:\\script.js\"");
            Assert.AreEqual(this.Filename, "C:\\script.js");
            Assert.AreEqual(this.Script, string.Empty);
            Assert.IsFalse(this.List);
            Assert.IsNotNull(this.Arguments);
            Assert.AreEqual(0, this.Arguments.Length);
        }

        [TestMethod]
        public void TestMethod2()
        {
            this.parser.Parse("run log(1);");
            Assert.AreEqual(this.Script, "log(1);");
            Assert.AreEqual(this.Filename, string.Empty);
            Assert.IsFalse(this.List);
            Assert.IsNotNull(this.Arguments);
            Assert.AreEqual(0, this.Arguments.Length);
        }

        [TestMethod]
        public void TestMethod3()
        {
            this.parser.Parse("run --list");
            Assert.IsTrue(this.List);
            Assert.AreEqual(this.Script, string.Empty);
            Assert.AreEqual(this.Filename, string.Empty);
            Assert.IsNotNull(this.Arguments);
            Assert.AreEqual(0, this.Arguments.Length);
        }

        [TestMethod]
        public void TestMethod4()
        {
            this.parser.Parse("run -l");
            Assert.AreEqual(this.Script, string.Empty);
            Assert.AreEqual(this.Filename, string.Empty);
            Assert.IsNotNull(this.Arguments);
            Assert.AreEqual(0, this.Arguments.Length);
        }

        [TestMethod]
        public void TestMethod4_With_Args()
        {
            this.parser.Parse("run -l -- db=string port=number async=boolean");
            Assert.IsTrue(this.List);
            Assert.AreEqual(this.Script, string.Empty);
            Assert.AreEqual(this.Filename, string.Empty);
            Assert.AreEqual(3, this.Arguments.Length);
            foreach (var item in this.Arguments)
            {
                Assert.IsTrue(Regex.IsMatch(item, ".+=.+"));
            }
        }

        [TestMethod]
        public void TestMethod5()
        {
            this.parser.Parse("run log(1); arg1=1 arg2=text");
            Assert.AreEqual(this.Script, "log(1);");
            Assert.AreEqual(2, this.Arguments.Length);
            foreach (var item in this.Arguments)
            {
                Assert.IsTrue(Regex.IsMatch(item, ".+=.+"));
            }
        }

        [CommandPropertyRequired(DefaultValue = "")]
        [CommandPropertyTrigger(nameof(Filename), "")]
        [CommandPropertyTrigger(nameof(List), false)]
        public string Script
        {
            get; set;
        }

        [CommandProperty]
        [CommandPropertyTrigger(nameof(Script), "")]
        [CommandPropertyTrigger(nameof(List), false)]
        public string Filename
        {
            get; set;
        }

        [CommandPropertySwitch("list", 'l')]
        [CommandPropertyTrigger(nameof(Script), "")]
        [CommandPropertyTrigger(nameof(Filename), "")]
        public bool List
        {
            get; set;
        }

        [CommandPropertyArray]
        public string[] Arguments
        {
            get; set;
        }
    }
}
