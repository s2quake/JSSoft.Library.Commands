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
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACTORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
// Forked from https://github.com/NtreevSoft/CommandLineParser
// Namespaces and files starting with "Ntreev" have been renamed to "JSSoft".

using System;

namespace JSSoft.Library.Commands
{
    public sealed class TerminalKeyBinding : TerminalKeyBindingBase
    {
        private readonly ConsoleKeyInfo key;
        private readonly Func<Terminal, bool> action;
        private readonly Func<Terminal, bool> verify;

        public TerminalKeyBinding(ConsoleKeyInfo key, Func<Terminal, bool> action)
            : this(key, action, (obj) => true)
        {
        }

        public TerminalKeyBinding(ConsoleKeyInfo key, Action<Terminal> action)
            : this(key, action, (obj) => true)
        {
        }

        public TerminalKeyBinding(ConsoleKeyInfo key, Action<Terminal> action, Func<Terminal, bool> verify)
        {
            this.key = key;
            this.action = ActionToFunc(action);
            this.verify = verify ?? throw new ArgumentNullException(nameof(verify));
        }

        public TerminalKeyBinding(ConsoleKeyInfo key, Func<Terminal, bool> action, Func<Terminal, bool> verify)
        {
            this.key = key;
            this.action = action ?? throw new ArgumentNullException(nameof(action));
            this.verify = verify ?? throw new ArgumentNullException(nameof(verify));
        }

        public override ConsoleKeyInfo Key => this.key;

        protected override bool OnVerify(Terminal terminal)
        {
            return this.verify(terminal);
        }

        protected override bool OnAction(Terminal terminal)
        {
            return this.action(terminal);
        }

        private static Func<Terminal, bool> ActionToFunc(Action<Terminal> action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
            return new Func<Terminal, bool>((t) =>
            {
                action(t);
                return true;
            });
        }
    }
}
