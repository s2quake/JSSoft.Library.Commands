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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace JSSoft.Library.Commands
{
    public class TerminalKeyBindingCollection : IEnumerable<TerminalKeyBindingBase>
    {
        private static readonly char[] multilineChars = new[] { '\"', '\'' };
        private readonly Dictionary<ConsoleKeyInfo, TerminalKeyBindingBase> itemByKey;

        static TerminalKeyBindingCollection()
        {
            if (Terminal.IsWin32NT == true)
                Default = Win32NT;
            else if (Terminal.IsUnix == true)
                Default = Unix;
        }

        public TerminalKeyBindingCollection(TerminalKeyBindingCollection bindings)
            : this(bindings, Enumerable.Empty<TerminalKeyBindingBase>())
        {
            this.itemByKey = new Dictionary<ConsoleKeyInfo, TerminalKeyBindingBase>();
            this.BaseBindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
        }

        public TerminalKeyBindingCollection(IEnumerable<TerminalKeyBindingBase> items)
            : this(null, items)
        {
        }

        public TerminalKeyBindingCollection(TerminalKeyBindingCollection bindings, IEnumerable<TerminalKeyBindingBase> items)
        {
            this.BaseBindings = bindings;
            this.itemByKey = (items ?? throw new ArgumentNullException(nameof(items))).ToDictionary(item => item.Key);
        }

        public bool CanProcess(ConsoleKeyInfo key)
        {
            if (this.itemByKey.ContainsKey(key) == true)
                return true;
            if (this.BaseBindings != null)
                return this.BaseBindings.CanProcess(key);
            return false;
        }

        public bool Process(ConsoleKeyInfo key, Terminal terminal)
        {
            if (this.itemByKey.ContainsKey(key) == true)
            {
                if (this.itemByKey[key].Invoke(terminal) == true)
                    return true;
            }
            if (this.BaseBindings != null)
                return this.BaseBindings.Process(key, terminal);
            return false;
        }

        public TerminalKeyBindingCollection BaseBindings { get; }

        public int Count => this.itemByKey.Count;

        public static TerminalKeyBindingCollection Common { get; } = new TerminalKeyBindingCollection(new TerminalKeyBindingBase[]
        {
            new TerminalKeyBinding(new ConsoleKeyInfo('\0', ConsoleKey.Delete, false, false, false), (t) => t.Delete()),
            new TerminalKeyBinding(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false), (t) => t.MoveToFirst()),
            new TerminalKeyBinding(new ConsoleKeyInfo('\0', ConsoleKey.End, false, false, false), (t) => t.MoveToLast()),
            new TerminalKeyBinding(new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false), (t) => t.PrevHistory(), (t) => !t.IsPassword),
            new TerminalKeyBinding(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false), (t) => t.NextHistory(), (t) => !t.IsPassword),
            new TerminalKeyBinding(new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false), (t) => t.Left()),
            new TerminalKeyBinding(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false), (t) => t.Right()),
        });

        public static TerminalKeyBindingCollection Win32NT { get; } = new TerminalKeyBindingCollection(Common, new TerminalKeyBindingBase[]
        {
            new TerminalKeyBinding(new ConsoleKeyInfo('\u001b', ConsoleKey.Escape, false, false, false), (t) => t.Command = string.Empty, (t) => !t.IsPassword),
            new TerminalKeyBinding(new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false), (t) => t.Backspace()),
            new TerminalKeyBinding(new ConsoleKeyInfo('\b', ConsoleKey.H, false, false, true), (t) => t.Backspace()),
            new TerminalKeyBinding(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, true), (t) => DeleteToFirst(t), (t) => !t.IsPassword),
            new TerminalKeyBinding(new ConsoleKeyInfo('\0', ConsoleKey.End, false, false, true), (t) => DeleteToLast(t), (t) => !t.IsPassword),
            new TerminalKeyBinding(new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false), (t) => t.NextCompletion(), (t) => !t.IsPassword),
            new TerminalKeyBinding(new ConsoleKeyInfo('\t', ConsoleKey.Tab, true, false, false), (t) => t.PrevCompletion(), (t) => !t.IsPassword),
            new TerminalKeyBinding(new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, true), (t) => PrevWord(t), (t) => !t.IsPassword),
            new TerminalKeyBinding(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, true), (t) => NextWord(t), (t) => !t.IsPassword),
        });

        public static TerminalKeyBindingCollection Unix { get; } = new TerminalKeyBindingCollection(Common, new TerminalKeyBindingBase[]
        {
            new TerminalKeyBinding(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false), (t) => InputEnterOnUnix(t)),
            new TerminalKeyBinding(new ConsoleKeyInfo('\u0003', ConsoleKey.C, false, false, true), (t) => t.CancelInput()),
            new TerminalKeyBinding(new ConsoleKeyInfo('\u001b', ConsoleKey.Escape, false, false, false), (t) => {}),
            new TerminalKeyBinding(new ConsoleKeyInfo('\u007f', ConsoleKey.Backspace, false, false, false), (t) => t.Backspace()),
            new TerminalKeyBinding(new ConsoleKeyInfo('\u0015', ConsoleKey.U, false, false, true), (t) => DeleteToFirst(t), (t) => !t.IsPassword),
            new TerminalKeyBinding(new ConsoleKeyInfo('\u000b', ConsoleKey.K, false, false, true), (t) => DeleteToLast(t), (t) => !t.IsPassword),
            new TerminalKeyBinding(new ConsoleKeyInfo('\u0005', ConsoleKey.E, false, false, true), (t) => t.MoveToLast()),
            new TerminalKeyBinding(new ConsoleKeyInfo('\u0001', ConsoleKey.A, false, false, true), (t) => t.MoveToFirst()),
            new TerminalKeyBinding(new ConsoleKeyInfo('\u0027', ConsoleKey.W, false, false, true), (t) => DeletePrevWord(t), (t) => !t.IsPassword),
            new TerminalKeyBinding(new ConsoleKeyInfo('\u000c', ConsoleKey.L, false, false, true), (t) => t.Clear(), (t) => !t.IsPassword),
            new TerminalKeyBinding(new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false), (t) => t.NextCompletion(), (t) => !t.IsPassword),
            new TerminalKeyBinding(new ConsoleKeyInfo('\0', ConsoleKey.Tab, true, false, false), (t) => t.PrevCompletion(), (t) => !t.IsPassword),
        });

        public static TerminalKeyBindingCollection Default { get; }

        private static int PrevWord(Terminal terminal)
        {
            if (terminal.CursorIndex > 0)
            {
                var index = terminal.CursorIndex - 1;
                var command = terminal.Command;
                var pattern = @"^\w|(?=\b)\w|$";
                var matches = Regex.Matches(command, pattern).Cast<Match>();
                var match = matches.Where(item => item.Index <= index).Last();
                terminal.CursorIndex = match.Index;
            }
            return terminal.CursorIndex;
        }

        private static int NextWord(Terminal terminal)
        {
            var command = terminal.Command;
            if (terminal.CursorIndex < command.Length)
            {
                var index = terminal.CursorIndex;
                var pattern = @"\w(?<=\b)|$";
                var matches = Regex.Matches(command, pattern).Cast<Match>();
                var match = matches.Where(item => item.Index > index).First();
                terminal.CursorIndex = Math.Min(command.Length, match.Index + 1);
            }
            return terminal.CursorIndex;
        }

        private static void DeleteToLast(Terminal terminal)
        {
            var index = terminal.CursorIndex;
            var command = terminal.Command;
            terminal.Command = command.Substring(0, index);
        }

        private static void DeleteToFirst(Terminal terminal)
        {
            var index = terminal.CursorIndex;
            var command = terminal.Command;
            terminal.Command = command.Remove(0, index);
            terminal.CursorIndex = 0;
        }

        private static void DeletePrevWord(Terminal terminal)
        {
            var index2 = terminal.CursorIndex;
            var command = terminal.Command;
            var index1 = PrevWord(terminal);
            var length = index2 - index1;
            terminal.Command = command.Remove(index1, length);
        }

        private static void InputEnterOnUnix(Terminal terminal)
        {
            var isMultiline = IsMultilineOnUnix(terminal);
            if (isMultiline == true)
                terminal.InsertNewLine();
            else
                terminal.EndInput();
        }

        private static bool IsMultilineOnUnix(Terminal terminal)
        {
            var command = terminal.Command;
            var index = command.IndexOfAny(multilineChars);
            var ch = index >= 0 ? command[index] : char.MinValue;
            var count = command.Count(item => item == ch);
            return count % 2 != 0 || command.LastOrDefault() == '\\';
        }

        #region IEnumerable

        IEnumerator<TerminalKeyBindingBase> IEnumerable<TerminalKeyBindingBase>.GetEnumerator()
        {
            foreach (var item in this.itemByKey)
            {
                yield return item.Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var item in this.itemByKey)
            {
                yield return item.Value;
            }
        }

        #endregion
    }
}
