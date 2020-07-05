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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel;

namespace Ntreev.Library.Commands.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var settings = new Settings();
            var parser = new CommandLineParser(settings);
            parser.ParseWith("--list -c");

            Assert.AreEqual("", settings.List);
            Assert.AreEqual(true, settings.IsCancel);
            Assert.AreEqual(5005, settings.Port);
        }

        [TestMethod]
        public void TestMethod2()
        {
            var settings = new Settings();
            var parser = new CommandLineParser(settings);
            parser.ParseWith("--list wer -c");

            Assert.AreEqual("wer", settings.List);
            Assert.AreEqual(true, settings.IsCancel);
            Assert.AreEqual(5005, settings.Port);
        }

        [TestMethod]
        public void TestMethod3()
        {
            var settings = new Settings();
            var parser = new CommandLineParser(settings);
            parser.ParseWith("--list \"a \\\"b\\\" c\" -c");

            Assert.AreEqual("a \"b\" c", settings.List);
            Assert.AreEqual(true, settings.IsCancel);
            Assert.AreEqual(5005, settings.Port);
        }

        [TestMethod]
        public void TestMethod4()
        {
            var commands = new Commands();
            var parser = new CommandLineParser(commands);
            parser.InvokeWith("test a -m wow");
        }

        class Settings
        {
            [CommandProperty]
            [DefaultValue("")]
            public string List { get; set; }

            [CommandProperty('c')]
            public bool IsCancel { get; set; }

            [CommandProperty]
            [DefaultValue(5005)]
            public int Port { get; set; }
        }

        class Commands
        {
            [CommandMethod]
            [CommandMethodProperty(nameof(Message))]
            public void Test(string target1, string target2 = null)
            {
                Assert.AreEqual("a", target1);
                Assert.AreEqual(null, target2);
                Assert.AreEqual("wow", this.Message);
            }

            [CommandProperty('m', Usage = CommandPropertyUsage.ExplicitRequired)]
            public string Message
            {
                get; set;
            }
        }
    }
}
