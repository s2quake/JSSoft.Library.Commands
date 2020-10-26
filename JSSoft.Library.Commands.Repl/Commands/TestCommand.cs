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

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Library.Commands.Repl.Commands
{
    [Export(typeof(ICommand))]
    [CommandSummary("Test Command")]
    [CommandSummary("테스트 명령어", Locale = "ko-KR")]
    class TestCommand : CommandMethodBase
    {
        private Task task;
        private CancellationTokenSource cancellation;
        public TestCommand()
        {

        }

        [CommandMethod]
        [CommandSummary("Start async task")]
        [CommandSummary("비동기 작업을 시작합니다.", Locale = "ko-KR")]
        [CommandMethodStaticProperty(typeof(FilterProperties))]
        public void Start()
        {
            this.task = Task.Run(this.Test);
            this.cancellation = new CancellationTokenSource();
        }

        public bool CanStart => true;

        [CommandMethod]
        public async Task AsyncAsync(CancellationToken cancellationToken)
        {
            this.Out.WriteLine("type control+c to cancel");
            while (cancellationToken.IsCancellationRequested == false)
            {
                await Task.Delay(100);
            }
        }

        [CommandMethod]
        [CommandSummary("Stop async task")]
        [CommandSummary("비동기 작업을 멈춥니다..", Locale = "ko-KR")]
        [CommandExample("werwer")]
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            this.cancellation.Cancel();
            await this.task;
            this.cancellation = null;
            this.task = null;
        }

        [CommandMethod]
        public void PushMany(params string[] items)
        {
            foreach (var item in items)
            {
                Console.WriteLine(item);
            }
        }

        [CommandMethod("items", Aliases = new string[] { "ls" })]
        [CommandMethodProperty(nameof(IsReverse))]
        public void ShowItem()
        {
            var items = new string[] { "a", "b", "c" };
            if (this.IsReverse == false)
            {
                var i = 0;
                foreach (var item in items)
                {
                    Console.WriteLine($"{i++,2}: {item}");
                }
            }
            else
            {
                var i = items.Length - 1;
                foreach (var item in items.Reverse())
                {
                    Console.WriteLine($"{i--,2}: {item}");
                }
            }
        }

        public string[] CompleteShowItem(CommandMemberDescriptor descriptor, string find)
        {
            return null;
        }

        [CommandPropertySwitch("reverse", 'r')]
        public bool IsReverse
        {
            get; set;
        }

        private void Test()
        {
            while (!this.cancellation.IsCancellationRequested)
            {
                Console.WriteLine(DateTime.Now);
                Thread.Sleep(1000);
            }
        }

        protected override bool IsMethodEnabled(CommandMethodDescriptor descriptor)
        {
            if (descriptor.DescriptorName == nameof(Start))
            {
                return this.task == null;
            }
            else if (descriptor.DescriptorName == nameof(StopAsync))
            {
                return this.task != null && this.cancellation.IsCancellationRequested == false;
            }
            throw new NotImplementedException();
        }
    }
}
