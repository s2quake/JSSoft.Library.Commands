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
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Library.Commands.Repl.Commands
{
    [Export(typeof(ICommand))]
    class TestCommand : CommandMethodBase
    {
        private Task task;
        private CancellationTokenSource cancellation;
        public TestCommand()
        {

        }

        [CommandMethod]
        public void Start()
        {
            this.task = Task.Run(this.Test);
            this.cancellation = new CancellationTokenSource();
        }

        [CommandMethod]
        public async Task StopAsync()
        {
            this.cancellation.Cancel();
            await this.task;
            this.cancellation = null;
            this.task = null;
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
