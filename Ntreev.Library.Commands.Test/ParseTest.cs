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

namespace Ntreev.Library.Commands.Test
{
    [TestClass]
    public class ParseTest : ISupportInitialize
    {
        private readonly CommandLineParser parser;

        public ParseTest()
        {
            this.parser = new CommandLineParser("parse", this);
        }

        [TestMethod]
        public void TestMethod1()
        {
            this.parser.Parse("parse --boolean false");
            Assert.IsFalse(this.Boolean);
        }

        [TestMethod]
        public void TestMethod2()
        {
            this.parser.Parse("parse --number 1");
            Assert.AreEqual(this.Number, 1);
        }

        [TestMethod]
        public void TestMethod3()
        {
            this.parser.Parse("parse --string qwer");
            Assert.AreEqual(this.String, "qwer");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestMethod4()
        {
            this.parser.Parse("parse --boolean ewe");
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestMethod5()
        {
            this.parser.Parse("parse --number ewe");
        }

        [CommandProperty]
        public bool Boolean
        {
            get; set;
        }

        [CommandProperty]
        public int Number
        {
            get; set;
        }

        [CommandProperty]
        public string String
        {
            get; set;
        }

        #region ISupportInitialize

        void ISupportInitialize.BeginInit()
        {

        }

        void ISupportInitialize.EndInit()
        {

        }

        #endregion
    }
}
