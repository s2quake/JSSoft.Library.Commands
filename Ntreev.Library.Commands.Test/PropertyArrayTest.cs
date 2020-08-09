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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.ComponentModel;
using System.Linq;

namespace Ntreev.Library.Commands.Test
{
    [TestClass]
    public class PropertyArrayTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var instance = new Instance1();
            var parser = new CommandLineParser("array", instance);
            parser.Parse("array");
            Assert.IsNotNull(instance.Arguments);
            Assert.AreEqual(0, instance.Arguments.Length);
        }

        class Instance1
        {
            [CommandPropertyArray]
            public string[] Arguments
            {
                get; set;
            }
        }

        [TestMethod]
        public void TestMethod2()
        {
            var instance = new Instance2();
            var parser = new CommandLineParser("array", instance);
            parser.Parse("array");
            Assert.IsNotNull(instance.Arguments);
            Assert.AreEqual(0, instance.Arguments.Length);
            Assert.IsTrue(new string[] { }.SequenceEqual(instance.Arguments));
        }

        class Instance2
        {
            [CommandPropertyArray]
            [DefaultValue(new string[] { })]
            public string[] Arguments
            {
                get; set;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestMethod3()
        {
            var instance = new Instance3();
            var parser = new CommandLineParser("array", instance);
            parser.Parse("array");
        }

        class Instance3
        {
            [CommandPropertyArray]
            public string[] Arguments1
            {
                get; set;
            }

            [CommandPropertyArray]
            public string[] Arguments2
            {
                get; set;
            }
        }

        [TestMethod]
        public void TestMethod4()
        {
            var instance = new Instance4();
            var parser = new CommandLineParser("array", instance);
            parser.Parse("array");
            Assert.IsNotNull(instance.Arguments);
            Assert.AreEqual(0, instance.Arguments.Length);
            Assert.IsTrue(new string[] { }.SequenceEqual(instance.Arguments));
        }

        class Instance4
        {
            [CommandPropertyArray(DefaultValue = new string[] { })]
            public string[] Arguments
            {
                get; set;
            }
        }
    }
}
