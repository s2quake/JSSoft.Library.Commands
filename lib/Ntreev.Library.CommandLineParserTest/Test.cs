﻿#region License
//Ntreev CommandLineParser for .Net 1.0.4548.25168
//https://github.com/NtreevSoft/CommandLineParser

//Released under the MIT License.

//Copyright (c) 2010 Ntreev Soft co., Ltd.

//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
//documentation files (the "Software"), to deal in the Software without restriction, including without limitation the 
//rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit 
//persons to whom the Software is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the 
//Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
//WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
//COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR 
//OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
#endregion


using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ntreev;
using System.IO;
using CommandLineParserTest.Options;
using CommandLineParserTest.Library;
using System.Net;
using Ntreev.Library;

namespace CommandLineParserTest
{
    /// <summary>
    /// Test의 요약 설명
    /// </summary>
    [TestClass]
    public class Test
    {
        public Test()
        {
            //
            // TODO: 여기에 생성자 논리를 추가합니다.
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///현재 테스트 실행에 대한 정보 및 기능을
        ///제공하는 테스트 컨텍스트를 가져오거나 설정합니다.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region 추가 테스트 특성
        //
        // 테스트를 작성할 때 다음 추가 특성을 사용할 수 있습니다.
        //
        // ClassInitialize를 사용하여 클래스의 첫 번째 테스트를 실행하기 전에 코드를 실행합니다.
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // ClassCleanup을 사용하여 클래스의 테스트를 모두 실행한 후에 코드를 실행합니다.
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // 테스트를 작성할 때 다음 추가 특성을 사용할 수 있습니다. 
         [TestInitialize()]
         public void MyTestInitialize() 
         {
             Assert.AreEqual('/', CommandSwitchAttribute.SwitchDelimiter);
         }
        //
        // TestInitialize를 사용하여 각 테스트를 실행하기 전에 코드를 실행합니다.
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void BaseTypeSwitchesTest()
        {
            BaseTypeSwitches options = new BaseTypeSwitches();
            CommandLineParser parser = new CommandLineParser();

            string path = Path.GetTempFileName();
            parser.Parse(options, @"Test.exe /Text ""this is a string"" /Number 5 /Boolean /Path """ + path + @""" /AttributeTargets ""Assembly,Constructor""");

            Assert.AreEqual("this is a string", options.Text);
            Assert.AreEqual(5, options.Number);
            Assert.AreEqual(true, options.Boolean);
            Assert.AreEqual(path, options.Path.FullName);
            Assert.AreEqual(AttributeTargets.Assembly | AttributeTargets.Constructor, options.AttributeTargets);
        }

        [TestMethod]
        public void SwitchDelimiterTest()
        {
            try
            {
                CommandSwitchAttribute.SwitchDelimiter = "a";
                Assert.Inconclusive();
            }
            catch (Exception)
            {

            }

            try
            {
                CommandSwitchAttribute.SwitchDelimiter = "1";
                Assert.Inconclusive();
            }
            catch (Exception)
            {

            }

            try
            {
                CommandSwitchAttribute.SwitchDelimiter = " ";
                Assert.Inconclusive();
            }
            catch (Exception)
            {

            }

            try
            {
                CommandSwitchAttribute.SwitchDelimiter = "\"";
                Assert.Inconclusive();
            }
            catch (Exception)
            {

            }

            CommandSwitchAttribute.SwitchDelimiter = "/";
        }

        [TestMethod]
        public void RequiredSwitchesTest()
        {
            RequiredSwitches options = new RequiredSwitches();
            StringBuilder arg = new StringBuilder();
            CommandLineParser parser = new CommandLineParser();

            arg.Append("Test.exe");

            // 인자가 하나도 없을때의 예외 테스트
            try
            {
                parser.Parse(options, arg.ToString());
                Assert.Inconclusive("예외가 발생하지 않았습니다.");
            }
            catch (ArgumentException)
            {
                
            }
            arg.Append(" /Index 5");    

            // text 인자 테스트
            try
            {
                parser.Parse(options, arg.ToString());
                Assert.Inconclusive("예외가 발생하지 않았습니다.");
            }
            catch (MissingSwitchException e)
            {
                Assert.AreEqual("Text", e.SwitchName);
            }
            arg.Append(@" /Text ""this is a string""");

            // number 인자 테스트
            try
            {
                parser.Parse(options, arg.ToString());
                Assert.Inconclusive("예외가 발생하지 않았습니다.");
            }
            catch (MissingSwitchException e)
            {
                Assert.AreEqual("Number", e.SwitchName);
            }

            // 최종적으로 필요한 인자를 모두 추가후 마지막 테스트
            arg.Append(" /Number 4");
            try
            {
                parser.Parse(options, arg.ToString());
            }
            catch (Exception)
            {
                Assert.Inconclusive("예외가 발생하지 말아야 합니다.");
            }

            // 마지막 값 비교 테스트
            Assert.AreEqual(5, options.Index);
            Assert.AreEqual("this is a string", options.Text);
            Assert.AreEqual(4, options.Number);
        }

        [TestMethod]
        public void ArgSeperatorSwitchesTest()
        {
            ArgSeperatorSwitches options = new ArgSeperatorSwitches();

            CommandLineParser parser = new CommandLineParser();

            parser.Parse(options, "Test.exe /Level5 /IsAlive:true");

            Assert.AreEqual(5, options.Level);
            Assert.AreEqual(true, options.IsAlive);

            parser.Parse(options, "Test.exe /Level-1 /IsAlive:false");

            Assert.AreEqual(-1, options.Level);
            Assert.AreEqual(false, options.IsAlive);
        }

        [TestMethod]
        public void DuplicatedOptionsTest()
        {
            DuplicatedOptions options = new DuplicatedOptions();
            CommandLineParser parser = new CommandLineParser();

            try
            {
                parser.Parse(options, "Test.exe /index 5");
                Assert.Inconclusive("예외가 발생하지 않았습니다.");
            }
            catch (SwitchException e)
            {
                Assert.AreEqual("index", e.SwitchName);
            }
        }

        [TestMethod]
        public void SwitchTypeArgTest()
        {

        }

        [TestMethod]
        public void ListSwitchesTest()
        {
            ListSwitches options = new ListSwitches();
            CommandLineParser parser = new CommandLineParser();

            CommandLineBuilder cmdBuilder = new CommandLineBuilder();

            cmdBuilder.AddSwitch("IPs", "255.255.255.255", "1.1.1.1");
            cmdBuilder.AddSwitch("InternalNumbers", 1, 2, 3, 4, 5);
            cmdBuilder.AddSwitch("Numbers", 9, 8, 7, 6, 5, 4);
            cmdBuilder.AddSwitch("Texts", "command", "line", "parse");
            cmdBuilder.AddSwitch("PathList", '|', @"d:\license directory\license.txt", @"d:\sourceCode.txt");

            parser.Parse(options, cmdBuilder.ToString());

            Assert.AreEqual(options.IPs.Count, cmdBuilder.GetArgCount("IPs"));
            for (int i = 0; i < options.IPs.Count; i++)
            {
                Assert.AreEqual(options.IPs[i].ToString(), cmdBuilder["IPs", i].ToString());
            }

            Assert.AreEqual(options.InternalNumbers.Count, cmdBuilder.GetArgCount("InternalNumbers"));
            for (int i = 0; i < options.InternalNumbers.Count; i++)
            {
                Assert.AreEqual(options.InternalNumbers[i].ToString(), cmdBuilder["InternalNumbers", i].ToString());
            }

            Assert.AreEqual(options.Numbers.Count, cmdBuilder.GetArgCount("Numbers"));
            for (int i = 0; i < options.Numbers.Count; i++)
            {
                Assert.AreEqual(options.Numbers[i].ToString(), cmdBuilder["Numbers", i].ToString());
            }

            Assert.AreEqual(options.Texts.Count, cmdBuilder.GetArgCount("Texts"));
            for (int i = 0; i < options.Texts.Count; i++)
            {
                Assert.AreEqual(options.Texts[i].ToString(), cmdBuilder["Texts", i].ToString());
            }
        }
    }
}