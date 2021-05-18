﻿// Released under the MIT License.
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
using System;

namespace JSSoft.Library.Commands.Test
{
    [TestClass]
    public class LockTest
    {
        private readonly CommandLineParser parser;

        public LockTest()
        {
            this.parser = new CommandLineParser("lock", this);
        }

        [TestMethod]
        public void TestMethod1()
        {
            this.parser.ParseCommandLine("lock");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestMethod2()
        {
            this.parser.ParseCommandLine("lock -m");
        }

        [TestMethod]
        public void TestMethod3()
        {
            this.parser.ParseCommandLine("lock -m 123");
            Assert.AreEqual("", this.Path);
            Assert.AreEqual("123", this.Comment);
        }

        [TestMethod]
        public void TestMethod4()
        {
            this.parser.ParseCommandLine("lock current_path");
        }

        [TestMethod]
        public void TestMethod5()
        {
            this.parser.ParseCommandLine("lock current_path -m 123");
            Assert.AreEqual("current_path", this.Path);
            Assert.AreEqual("123", this.Comment);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestMethod6()
        {
            this.parser.ParseCommandLine("lock current_path -m 123 -i");
        }

        [CommandPropertyRequired(DefaultValue = "")]
        public string Path
        {
            get; set;
        }

        [CommandProperty('m')]
        [CommandPropertyTrigger(nameof(Information), false)]
        public string Comment
        {
            get; set;
        }

        [CommandPropertySwitch('i')]
        [CommandPropertyTrigger(nameof(Comment), null)]
        public bool Information
        {
            get; set;
        }

        [CommandProperty("format", DefaultValue = "xml")]
        [CommandPropertyTrigger(nameof(Information), true)]
        public string FormatType
        {
            get; set;
        }
    }
}
