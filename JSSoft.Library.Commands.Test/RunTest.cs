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
    public class RunTest
    {
        private readonly CommandLineParser parser;

        public RunTest()
        {
            this.parser = new CommandLineParser("run", this);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestMethod1()
        {
            try
            {
                this.parser.ParseCommandLine("run");
            }
            catch (CommandParseException e)
            {
                throw e.InnerException;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestMethod2()
        {
            this.parser.ParseCommandLine("run -l");
        }

        [TestMethod]
        public void TestMethod3()
        {
            this.parser.ParseCommandLine("run current_path");
            Assert.AreEqual("current_path", this.RepositoryPath);
            Assert.AreEqual(string.Empty, this.Authentication);
        }

        [TestMethod]
        public void TestMethod4()
        {
            this.parser.ParseCommandLine("run current_path -l");
            Assert.AreEqual("current_path", this.RepositoryPath);
            Assert.AreEqual("admin", this.Authentication);
        }

        [TestMethod]
        public void TestMethod5()
        {
            this.parser.ParseCommandLine("run current_path -l member");
            Assert.AreEqual("current_path", this.RepositoryPath);
            Assert.AreEqual("member", this.Authentication);
        }

        [CommandPropertyRequired]
        public string RepositoryPath
        {
            get; set;
        }

        [CommandProperty('l', DefaultValue = "admin")]
        public string Authentication
        {
            get; set;
        }
    }
}
