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

namespace JSSoft.Library.Commands
{
    [ResourceUsageDescription(nameof(HelpCommand))]
    class HelpInvokeInstance
    {
        [CommandPropertyRequired(DefaultValue = "")]
        public string SubCommand { get; set; }

        [CommandPropertySwitch("detail")]
        [CommandPropertyTrigger(nameof(IsSimple), false)]
        public bool IsDetail { get; set; }

        [CommandPropertySwitch("simple")]
        [CommandPropertyTrigger(nameof(IsDetail), false)]
        public bool IsSimple { get; set; }

        [CommandProperty("option")]
        public string OptionName { get; set; }

        public CommandUsage Usage
        {
            get
            {
                if (this.IsDetail == true)
                    return CommandUsage.Detail;
                else if (this.IsSimple == true)
                    return CommandUsage.Simple;
                return CommandUsage.None;
            }
        }
    }
}
