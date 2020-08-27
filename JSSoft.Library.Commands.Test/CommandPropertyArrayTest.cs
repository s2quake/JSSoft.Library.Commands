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

namespace JSSoft.Library.Commands.Test
{
    [TestClass]
    public class CommandPropertyArrayTest
    {
        [TestMethod]
        public void Test1()
        {
            var parser = new CommandLineParser(this);
            parser.ParseWith("get database=a port=123 userid=abc password=1234 comment=\"connect database to \\\"a\\\"\"");
        }

        [TestMethod]
        public void Test2()
        {
            var parser = new CommandLineParser(this);
            parser.Parse(parser.Name, "get \"database=a b c\"");

            CommandStringUtility.ArgumentsToDictionary(this.Arguments);
        }

        [TestMethod]
        public void Test3()
        {
            var parser = new CommandLineParser(this);
            parser.Parse(parser.Name, "get \"database=\\\"a b c\\\"\"");

            CommandStringUtility.ArgumentsToDictionary(this.Arguments);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ValueIncludedEqualsTest()
        {
            var parser = new CommandLineParser(this);
            parser.ParseWith("--value=0");
        }

        [TestMethod]
        public void ValueIncludedEqualsTest2()
        {
            var parser = new CommandLineParser(this);
            parser.Parse(parser.Name, "value=0");
        }

        [CommandPropertyRequired]
        public string Command
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
