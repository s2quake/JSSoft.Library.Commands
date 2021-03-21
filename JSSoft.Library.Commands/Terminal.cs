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

using JSSoft.Library.Commands.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading;

namespace JSSoft.Library.Commands
{
    public class Terminal
    {
        private static readonly ConsoleKeyInfo cancelKeyInfo = new ConsoleKeyInfo('\u0003', ConsoleKey.C, false, false, true);
        private static readonly Dictionary<char, int> charToWidth = new Dictionary<char, int>(char.MaxValue);
        private static int bufferWidth = 80;

        private readonly Dictionary<ConsoleKeyInfo, Action> actionMaps = new Dictionary<ConsoleKeyInfo, Action>();
        private readonly List<string> histories = new List<string>();
        private readonly List<string> completions = new List<string>();

        private int y = Console.CursorTop;
        private int width = Console.BufferWidth;
        private int historyIndex;
        private string prompt = string.Empty;
        private string command = string.Empty;
        private string promptText = string.Empty;
        private string inputText;
        private string completion = string.Empty;
        private int cursorPosition;
        private TextWriter writer;
        private TextWriter errorWriter;
        private bool isHidden;
        private bool treatControlCAsInput;
        private bool isCancellationRequested;
        private int x2, y2;
        private int x3, y3;

        static Terminal()
        {
            {
                var name = $"{typeof(Terminal).Namespace}.{PlatformID.Win32NT}.dat";
                using var stream = typeof(Terminal).Assembly.GetManifestResourceStream(name);
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                for (var i = char.MinValue; i < char.MaxValue; i++)
                {
                    charToWidth.Add(i, buffer[i]);
                }
            }
        }

        public static bool IsOutputRedirected =>
#if !NET35
                Console.IsOutputRedirected;
#else
                return true;
#endif

        public static bool IsInputRedirected =>
#if !NET35
                Console.IsInputRedirected;
#else
                return true;
#endif


        public static int GetLength(string text)
        {
            var length = 0;
            foreach (var item in text)
            {
                length += charToWidth[item];
            }
            return length;
        }

        public Terminal()
        {
            if (Terminal.IsInputRedirected == true)
                throw new Exception("Terminal cannot use. Console.IsInputRedirected must be false");
            this.actionMaps.Add(new ConsoleKeyInfo('\u001b', ConsoleKey.Escape, false, false, false), this.Clear);
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                this.actionMaps.Add(new ConsoleKeyInfo('\u007f', ConsoleKey.Backspace, false, false, false), this.Backspace);
            }
            else
            {
                this.actionMaps.Add(new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false), this.Backspace);
            }
            this.actionMaps.Add(new ConsoleKeyInfo('\0', ConsoleKey.Delete, false, false, false), this.Delete);
            this.actionMaps.Add(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false), this.Home);
            this.actionMaps.Add(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, true), this.DeleteToHome);
            this.actionMaps.Add(new ConsoleKeyInfo('\0', ConsoleKey.End, false, false, false), this.End);
            this.actionMaps.Add(new ConsoleKeyInfo('\0', ConsoleKey.End, false, false, true), this.DeleteToEnd);
            this.actionMaps.Add(new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false), this.PrevHistory);
            this.actionMaps.Add(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false), this.NextHistory);
            this.actionMaps.Add(new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false), this.Left);
            this.actionMaps.Add(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false), this.Right);
            this.actionMaps.Add(new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false), this.NextCompletion);
            if (Environment.OSVersion.Platform == PlatformID.Unix)
                this.actionMaps.Add(new ConsoleKeyInfo('\0', ConsoleKey.Tab, true, false, false), this.PrevCompletion);
            else
                this.actionMaps.Add(new ConsoleKeyInfo('\t', ConsoleKey.Tab, true, false, false), this.PrevCompletion);
        }

        public long? ReadLong(string prompt)
        {
            var result = this.ReadNumber(prompt, null, i => long.TryParse(i, out long v));
            if (result is long value)
            {
                return value;
            }
            return null;
        }

        public long? ReadLong(string prompt, long defaultValue)
        {
            var result = this.ReadNumber(prompt, defaultValue, i => long.TryParse(i, out long v));
            if (result is long value)
            {
                return value;
            }
            return null;
        }

        public double? ReadDouble(string prompt)
        {
            var result = this.ReadNumber(prompt, null, i => double.TryParse(i, out double v));
            if (result is double value)
            {
                return value;
            }
            return null;
        }

        public double? ReadDouble(string prompt, double defaultValue)
        {
            var result = this.ReadNumber(prompt, defaultValue, i => double.TryParse(i, out double v));
            if (result is double value)
            {
                return value;
            }
            return null;
        }

        public string ReadString(string prompt)
        {
            return ReadString(prompt, string.Empty);
        }

        public string ReadString(string prompt, bool isHidden)
        {
            return this.ReadString(prompt, string.Empty, isHidden);
        }

        public string ReadString(string prompt, string defaultText)
        {
            return this.ReadString(prompt, defaultText, false);
        }

        public string ReadString(string prompt, string defaultText, bool isHidden)
        {
            this.Initialize(prompt, defaultText, isHidden);

            try
            {
                return ReadLineImpl(i => true, false);
            }
            finally
            {
                this.Release();
            }
        }

        public SecureString ReadSecureString(string prompt)
        {
            var text = this.ReadString(prompt, true);
            var secureString = new SecureString();
            foreach (var item in text)
            {
                secureString.AppendChar(item);
            }
            return secureString;
        }

        public ConsoleKey ReadKey(string prompt, params ConsoleKey[] filters)
        {
            this.Initialize(prompt, string.Empty, false);
            try
            {
                return ReadKeyImpl(filters);
            }
            finally
            {
                this.Release();
            }
        }

        public void NextHistory()
        {
            if (this.historyIndex + 1 < this.histories.Count)
            {
                this.Command = this.histories[this.historyIndex + 1];
                this.historyIndex++;
            }
        }

        public void PrevHistory()
        {
            if (this.historyIndex > 0)
            {
                this.Command = this.histories[this.historyIndex - 1];
                this.historyIndex--;
            }
            else if (this.histories.Count == 1)
            {
                this.Command = this.histories[0];
                this.historyIndex = 0;
            }
        }

        public IList<string> Histories => this.histories;

        public IList<string> Completions => this.completions;

        public void Cancel()
        {
            this.isCancellationRequested = true;
        }

        public void Clear()
        {
            lock (LockedObject)
            {
                this.Command = string.Empty;
            }
        }

        public void Delete()
        {
            lock (LockedObject)
            {
                if (this.cursorPosition < this.command.Length)
                {
                    using (TerminalCursorVisible.Set(false))
                    {
                        this.CursorPosition++;
                        this.Backspace();
                        this.UpdateInputText();
                    }
                }
            }
        }

        public void Home()
        {
            lock (LockedObject)
            {
                this.CursorPosition = 0;
            }
        }

        public void End()
        {
            lock (LockedObject)
            {
                this.CursorPosition = this.command.Length;
            }
        }

        public void Left()
        {
            lock (LockedObject)
            {
                if (this.CursorPosition > 0)
                {
                    this.CursorPosition--;
                    this.UpdateInputText();
                }
            }
        }

        public void Right()
        {
            lock (LockedObject)
            {
                if (this.CursorPosition < this.command.Length)
                {
                    this.CursorPosition++;
                    this.UpdateInputText();
                }
            }
        }

        public void Backspace()
        {
            lock (LockedObject)
            {
                if (this.CursorPosition > 0)
                {
                    this.BackspaceImpl();
                    this.UpdateInputText();
                }
            }
        }

        public void DeleteToEnd()
        {
            lock (LockedObject)
            {
                this.command = this.command.Substring(this.cursorPosition);
                this.inputText = this.command;
                this.completion = string.Empty;
                this.promptText = this.prompt + this.command;
                if (this.writer != null)
                    this.Draw();
            }
        }

        public void DeleteToHome()
        {
            lock (LockedObject)
            {
                this.command = this.command.Remove(this.cursorPosition);
                this.inputText = this.command;
                this.completion = string.Empty;
                this.promptText = this.prompt + this.command;
                if (this.writer != null)
                    this.Draw();
            }
        }

        public void NextCompletion()
        {
            lock (LockedObject)
            {
                this.CompletionImpl(NextCompletion);
            }
        }

        public void PrevCompletion()
        {
            lock (LockedObject)
            {
                this.CompletionImpl(PrevCompletion);
            }
        }

        [Obsolete]
        public int Index
        {
            get => this.CursorPosition;
            set => this.CursorPosition = value;
        }

        public int CursorPosition
        {
            get => this.cursorPosition;
            set
            {
                this.cursorPosition = value;
                if (this.isHidden == false)
                {
                    using var visible = TerminalCursorVisible.Set(false);
                    var index = this.prompt.Length + value;
                    var text = this.promptText.Substring(0, index);
                    var (x1, y1) = (0, this.Top);
                    var (x2, y2) = NextPosition(text, x1, y1);
                    y2 = Math.Min(y2, Console.BufferHeight - 1);
                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                        Console.SetCursorPosition(x2, 0);
                    Console.SetCursorPosition(x2, y2);
                }
            }
        }

        public string Text => this.command;

        public string Command
        {
            get => this.command;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (this.command != value)
                {
                    this.command = value;
                    this.promptText = this.prompt + this.command;
                    this.cursorPosition = this.command.Length;
                    if (this.writer != null)
                    {
                        this.Erase(this.x2, this.y2, this.x3, this.y3);
                        this.Draw();
                    }
                }
            }
        }

        public string Prompt => this.prompt;

        public bool IsReading => this.writer != null;

        public int Top
        {
            get
            {
                if (this.width != Console.BufferWidth)
                {
                    this.y = Console.CursorTop - (this.prompt.Length + this.cursorPosition) / Console.BufferWidth;
                    this.width = Console.BufferWidth;
                }
                return this.y;
            }
            internal set => this.y = value;
        }

        public bool IsEnabled { get; set; } = true;

        public static int BufferWidth
        {
            get
            {
                if (Terminal.IsOutputRedirected == false)
                    return Console.BufferWidth;
                return bufferWidth;
            }
            set
            {
                if (Terminal.IsOutputRedirected == false)
                    throw new InvalidOperationException(Resources.Exception_BufferWidthCannotSet);
                if (value <= 0)
                    throw new ArgumentOutOfRangeException();
                bufferWidth = value;
            }
        }

        public static string NextCompletion(string[] completions, string text)
        {
            completions = completions.OrderBy(item => item)
                .ToArray();
            if (completions.Contains(text) == true)
            {
                for (var i = 0; i < completions.Length; i++)
                {
                    var r = string.Compare(text, completions[i], true);
                    if (r == 0)
                    {
                        if (i + 1 < completions.Length)
                            return completions[i + 1];
                        else
                            return completions.First();
                    }
                }
            }
            else
            {
                for (var i = 0; i < completions.Length; i++)
                {
                    var r = string.Compare(text, completions[i], true);
                    if (r < 0)
                    {
                        return completions[i];
                    }
                }
            }
            return text;
        }

        public static string PrevCompletion(string[] completions, string text)
        {
            completions = completions.OrderBy(item => item)
                .ToArray();
            if (completions.Contains(text) == true)
            {
                for (var i = completions.Length - 1; i >= 0; i--)
                {
                    var r = string.Compare(text, completions[i], true);
                    if (r == 0)
                    {
                        if (i - 1 >= 0)
                            return completions[i - 1];
                        else
                            return completions.Last();
                    }
                }
            }
            else
            {
                for (var i = completions.Length - 1; i >= 0; i--)
                {
                    var r = string.Compare(text, completions[i], true);
                    if (r < 0)
                    {
                        return completions[i];
                    }
                }
            }
            return text;
        }

        public void SetPrompt(string prompt)
        {
            lock (LockedObject)
            {
                // using (TerminalCursorVisible.Set(false))
                {
                    this.prompt = prompt;
                    this.promptText = this.prompt + this.command;
                    if (this.writer != null)
                    {
                        this.Erase(0, this.y, this.x3, this.y3);
                        this.Draw();
                    }
                }
            }
        }

        public event TerminalCancelEventHandler CancelKeyPress;

        public event EventHandler Cancelled;

        protected virtual void OnCancelKeyPress(TerminalCancelEventArgs e)
        {
            this.CancelKeyPress?.Invoke(this, e);
        }

        protected virtual void OnCancelled(EventArgs e)
        {
            this.Cancelled?.Invoke(this, e);
        }

        protected virtual string[] GetCompletion(string[] items, string find)
        {
            var query = from item in this.completions
                        where item.StartsWith(find)
                        select item;
            return query.ToArray();
        }

        protected virtual void OnDrawPrompt(TextWriter writer, string prompt)
        {
            writer.Write(prompt);
        }

        protected virtual void OnDrawText(TextWriter writer, string text)
        {
            writer.Write(text);
        }

        private void InsertText(string text)
        {
            if (text == string.Empty)
                return;

            lock (LockedObject)
            {
                var writer = this.writer;
                var cursorPosition = this.cursorPosition;
                var extra = this.command.Substring(this.cursorPosition);
                var command = this.command.Insert(this.cursorPosition, text);
                var promptText = this.prompt + command;

                if (this.isHidden == true)
                {
                    cursorPosition = 0;
                }
                else
                {
                    cursorPosition += text.Length;
                    using (TerminalCursorVisible.Set(false))
                    {
                        var (x1, y1) = (0, this.y);
                        var (x2, y2) = this.SetCursorPosition(0);
                        var (x3, y3) = NextPosition(command, x2, y2);
                        this.InvokeDrawCommand(writer, command);

                        if (y3 >= Console.BufferHeight)
                        {
                            if (Environment.OSVersion.Platform == PlatformID.Unix && x3 == 0)
                            {
                                writer.WriteLine();
                            }
                            this.y--;
                            this.y2 -= (y3 - y2);
                            this.y3 = this.y2 + (y3 - y2);
                        }
                    }
                }
                this.cursorPosition = cursorPosition;
                this.command = command;
                this.promptText = this.prompt + this.command;
                this.SetCursorPosition(this.cursorPosition);
            }
        }

        private (int x, int y) SetCursorPosition(int cursorPosition)
        {
            var position = this.isHidden == true ? 0 : cursorPosition;
            var (x1, y1) = (0, this.y);
            var index = this.prompt.Length + position;
            var text = this.promptText.Substring(0, index);
            var (x2, y2) = NextPosition(text, x1, y1);
            y2 = Math.Min(y2, Console.BufferHeight - 1);
            Console.SetCursorPosition(x2, 0);
            Console.SetCursorPosition(x2, y2);
            return (x2, y2);
        }

        private void BackspaceImpl()
        {
            var extra = this.command.Substring(this.cursorPosition);
            var cursorPosition = this.cursorPosition;
            var command = this.command.Remove(this.cursorPosition - 1, 1);
            var endPosition = this.command.Length;

            if (this.isHidden == false)
            {
                this.SetCursorPosition(endPosition);
                if (Console.CursorLeft == 0)
                {
                    this.SetCursorPosition(--endPosition);
                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                        this.writer.Write(" ");
                    else
                        this.writer.Write("\0");
                    this.SetCursorPosition(--cursorPosition);
                }
                else
                {
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                        this.writer.Write("\b \b");
                    else
                        this.writer.Write("\b \b");
                    this.SetCursorPosition(--cursorPosition);
                }
            }

            if (this.isHidden == false)
            {
                this.writer.Write(extra);
            }
            this.SetCursorPosition(cursorPosition);
            this.command = command;
            this.promptText = this.prompt + this.command;
            this.cursorPosition = cursorPosition;
        }

        private void CompletionImpl(Func<string[], string, string> func)
        {
            var matches = new List<Match>(CommandStringUtility.MatchCompletion(this.inputText));
            var find = string.Empty;
            var prefix = false;
            var postfix = false;
            var leftText = this.inputText;
            if (matches.Count > 0)
            {
                var match = matches.Last();
                var matchText = match.Value;
                if (matchText.Length > 0 && matchText.First() == '\"')
                {
                    prefix = true;
                    matchText = matchText.Substring(1);
                }
                if (matchText.Length > 1 && matchText.Last() == '\"')
                {
                    postfix = true;
                    matchText = matchText.Remove(matchText.Length - 1);
                }
                if (matchText == string.Empty || matchText.Trim() != string.Empty)
                {
                    find = matchText;
                    matches.RemoveAt(matches.Count - 1);
                    leftText = this.inputText.Remove(match.Index);
                }
            }

            var argList = new List<string>();
            for (var i = 0; i < matches.Count; i++)
            {
                var matchText = CommandStringUtility.TrimQuot(matches[i].Value).Trim();
                if (matchText != string.Empty)
                    argList.Add(matchText);
            }

            var completions = this.GetCompletion(argList.ToArray(), find);
            using var visible = TerminalCursorVisible.Set(false);
            if (completions != null && completions.Any())
            {
                var completion = func(completions, this.completion);
                var inputText = this.inputText;
                var command = string.Empty;
                if (prefix == true || postfix == true)
                {
                    command = leftText + "\"" + completion + "\"";
                }
                else
                {
                    command = leftText + completion;
                }
                this.completion = completion;
                this.Command = command;
                this.inputText = inputText;
            }
        }

        private void UpdateInputText()
        {
            this.inputText = this.command.Substring(0, this.cursorPosition);
            this.completion = string.Empty;
        }

        private object ReadNumber(string prompt, object defaultValue, Func<string, bool> validation)
        {
            this.Initialize(prompt, $"{defaultValue}", false);
            try
            {
                return ReadLineImpl(validation, false);
            }
            finally
            {
                this.Release();
            }
        }

        private string ReadLineImpl(Func<string, bool> validation, bool recordHistory)
        {
            while (true)
            {
                Thread.Sleep(1);
                if (this.isCancellationRequested == true)
                    return null;
                if (this.IsEnabled == false)
                    continue;
                var keys = this.ReadKeys().ToArray();
                if (this.isCancellationRequested == true)
                    return null;

                var keyChars = string.Empty;
                foreach (var key in keys)
                {
                    if (key == cancelKeyInfo)
                    {
                        var args = new TerminalCancelEventArgs(ConsoleSpecialKey.ControlC);
                        this.OnCancelKeyPress(args);
                        if (args.Cancel == false)
                        {
                            this.OnCancelled(EventArgs.Empty);
                            throw new OperationCanceledException(Resources.Exception_ReadOnlyCanceled);
                        }
                    }
                    else if (this.actionMaps.ContainsKey(key) == true)
                    {
                        this.actionMaps[key]();
                    }
                    else if (key.Key == ConsoleKey.Enter)
                    {
                        var text = this.Text;
                        this.command = string.Empty;
                        this.promptText = this.prompt + this.command;
                        this.cursorPosition = 0;

                        if (recordHistory == true)
                        {
                            if (this.isHidden == false && text != string.Empty)
                            {
                                if (this.histories.Contains(text) == false)
                                {
                                    this.histories.Add(text);
                                    this.historyIndex = this.histories.Count;
                                }
                                else
                                {
                                    this.historyIndex = this.histories.LastIndexOf(text) + 1;
                                }
                            }
                        }
                        return text;
                    }
                    else if (key.KeyChar != '\0')
                    {
                        keyChars += key.KeyChar;
                    }
                }

                if (keyChars != string.Empty && validation(this.Text + keyChars) == true)
                {
                    this.InsertText(keyChars);
                    this.UpdateInputText();
                }
            }
        }

        private IEnumerable<ConsoleKeyInfo> ReadKeys()
        {
            while (this.isCancellationRequested == false)
            {
                if (Console.KeyAvailable == true)
                {
                    yield return Console.ReadKey(true);
                    yield break;
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        private ConsoleKey ReadKeyImpl(params ConsoleKey[] filters)
        {
            while (true)
            {
                var key = Console.ReadKey(true);

                if ((int)key.Modifiers != 0)
                    continue;

                if (filters.Any() == false || filters.Any(item => item == key.Key) == true)
                {
                    this.InsertText(key.Key.ToString());
                    return key.Key;
                }
            }
        }

        private void Initialize(string prompt, string defaultText, bool isHidden)
        {
            lock (LockedObject)
            {
                this.writer = Console.Out;
                this.errorWriter = Console.Error;
                Console.SetOut(new TerminalTextWriter(Console.Out, this, Console.OutputEncoding));
                Console.SetError(new TerminalTextWriter(Console.Error, this, Console.OutputEncoding));
                this.treatControlCAsInput = Console.TreatControlCAsInput;
                Console.TreatControlCAsInput = true;

                this.y = Console.CursorTop;
                this.width = Console.BufferWidth;
                this.isHidden = false;
                this.command = defaultText;
                this.prompt = prompt;
                this.promptText = prompt + defaultText;
                this.cursorPosition = 0;
                this.isHidden = isHidden;
                this.inputText = defaultText;
                this.Draw();
            }
        }

        private void Release()
        {
            lock (LockedObject)
            {
                Console.TreatControlCAsInput = this.treatControlCAsInput;
                Console.SetOut(this.writer);
                Console.SetError(this.errorWriter);
                Console.WriteLine();
                this.writer = null;
                this.isHidden = false;
            }
        }

        private void InvokeDrawPrompt(TextWriter writer, string prompt)
        {
            this.OnDrawPrompt(writer, prompt);
            this.x2 = Console.CursorLeft;
            this.y2 = Console.CursorTop;
        }

        private void InvokeDrawCommand(TextWriter writer, string command)
        {
            this.OnDrawText(writer, command);
            this.x3 = Console.CursorLeft;
            this.y3 = Console.CursorTop;
        }

        internal static (int x, int y) NextPosition(string text, int x, int y)
        {
            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (ch == '\r')
                {
                    x = 0;
                    continue;
                }
                else if (ch == '\n')
                {
                    x = 0;
                    y++;
                    continue;
                }

                var w = charToWidth[ch];
                if (x + w >= Console.BufferWidth)
                {
                    x = x + w - Console.BufferWidth;
                    y++;
                }
                else
                {
                    x += w;
                }
            }
            return (x, y);
        }

        // internal static void NextPosition(string text, ref int x, ref int y)
        // {
        //     for (var i = 0; i < text.Length; i++)
        //     {
        //         var ch = text[i];
        //         if (ch == '\r')
        //         {
        //             x = 0;
        //             continue;
        //         }
        //         else if (ch == '\n')
        //         {
        //             x = 0;
        //             y++;
        //             continue;
        //         }

        //         var w = charToWidth[ch];
        //         if (x + w >= Console.BufferWidth)
        //         {
        //             x = x + w - Console.BufferWidth;
        //             y++;
        //         }
        //         else
        //         {
        //             x += w;
        //         }
        //     }
        // }

        internal string ReadStringInternal(string prompt)
        {
            this.Initialize(prompt, string.Empty, false);
            try
            {
                return ReadLineImpl(i => true, true);
            }
            finally
            {
                this.Release();
            }
        }

        internal void Erase()
        {
            var (x, y) = (Console.CursorLeft, Console.CursorTop);
            var (x1, y1) = (0, this.y);
            var (x2, y2) = NextPosition(this.promptText, x1, y1);
            var text = this.isHidden == true ? string.Empty : this.command;
            var writer = this.writer;
            for (var i = y1; i <= y2; i++)
            {
                Console.SetCursorPosition(1, i);
                Console.SetCursorPosition(0, i);
                if (Environment.OSVersion.Platform != PlatformID.Unix)
                {
                    Console.MoveBufferArea(Console.BufferWidth - 1, i, 1, 1, 0, i);
                    writer?.Write($"\r{new string('\0', Console.BufferWidth - 1)}\r");
                }
                else
                {
                    writer?.Write("\r" + new string(' ', Console.BufferWidth) + "\r");
                }
            }
            Console.SetCursorPosition(x, y);
        }

        internal void Erase(int x1, int y1, int x2, int y2)
        {
            var (x, y) = (Console.CursorLeft, Console.CursorTop);
            var writer = this.writer;
            for (var i = y1; i <= y2; i++)
            {
                Console.SetCursorPosition(1, i);
                Console.SetCursorPosition(0, i);
                if (Environment.OSVersion.Platform != PlatformID.Unix)
                {
                    Console.MoveBufferArea(Console.BufferWidth - 1, i, 1, 1, 0, i);
                    writer?.Write($"\r{new string('\0', Console.BufferWidth - 1)}\r");
                }
                else
                {
                    writer?.Write("\r" + new string(' ', Console.BufferWidth) + "\r");
                }
            }
            Console.SetCursorPosition(x, y);
        }

        internal void Draw()
        {
            Console.SetCursorPosition(0, this.y);
            var command = this.isHidden == true ? string.Empty : this.command;
            var (x1, y1) = (0, Console.CursorTop);
            var (x2, y2) = NextPosition(prompt, x1, y1);
            var (x3, y3) = NextPosition(command, x2, y2);
            this.InvokeDrawPrompt(this.writer, prompt);
            this.InvokeDrawCommand(this.writer, command);
            var clear = new string(' ', Console.BufferWidth - Console.CursorLeft) + "".PadRight(Console.BufferWidth - Console.CursorLeft - 1, '\b');
            this.writer.Write(clear);

            if (y3 >= Console.BufferHeight)
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix && x3 == 0)
                {
                    writer.WriteLine();
                }
                this.y -= (y2 - y1);
                this.y2 = this.y + (y2 - y1);
                this.y3 = this.y + (y3 - y1);
            }
        }

        internal (int x, int y) Draw(TextWriter writer, string text, int x, int y)
        {
            Console.SetCursorPosition(x, y);
            var clear = "\r" + new string(' ', Console.BufferWidth) + "\r";
            var promptText = this.promptText;
            var (x1, y1) = (0, Console.CursorTop);
            var (x2, y2) = NextPosition(text, x1, y1);
            var (x3, y3) = x2 == 0 ? (x2, y2) : (0, y2 + 1);
            var (x4, y4) = NextPosition(promptText, x3, y3);
            var text2 = x2 == 0 ? text : text + Environment.NewLine;
            var text3 = text2 + prompt + command;
            var text4 = clear + text3;
            var text5 = text4.Replace("\n", "\n" + clear);

            writer.Write(text5);
            // this.InvokeDrawPrompt(writer, prompt);
            // this.InvokeDrawCommand(writer, command);

            if (y4 >= Console.BufferHeight)
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix && x4 == 0)
                {
                    writer.WriteLine();
                    this.y -= (y4 - y3);
                    this.y2 = this.y + y2 - y1;
                    this.y3 = this.y + y3 - y1;
                }
            }
            return (x2, y2);
        }

        internal static object LockedObject { get; } = new object();
    }
}
