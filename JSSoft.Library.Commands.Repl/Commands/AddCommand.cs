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

using System;
using System.ComponentModel.Composition;

namespace JSSoft.Library.Commands.Repl.Commands
{
    [Export(typeof(ICommand))]
    [ResourceUsageDescription]
    [CommandStaticProperty(typeof(GlobalSettings))]
    class AddCommand : CommandBase
    {
        public AddCommand()
            : base("add")
        {
            this.Path = string.Empty;
        }

        [CommandPropertyRequired]
        public string Path
        {
            get; set;
        }

        [CommandPropertySwitch('n', AllowName = true)]
        public bool DryRun
        {
            get; set;
        }

        [CommandPropertySwitch('v', AllowName = true)]
        public bool Verbose
        {
            get; set;
        }

        [CommandPropertySwitch('f', AllowName = true)]
        public bool Force
        {
            get; set;
        }

        [CommandPropertySwitch('i', AllowName = true)]
        public bool Interactive
        {
            get; set;
        }

        [CommandPropertySwitch('P', AllowName = true)]
        public bool Patch
        {
            get; set;
        }

        protected override void OnExecute()
        {
            throw new NotImplementedException();
        }
    }
}
