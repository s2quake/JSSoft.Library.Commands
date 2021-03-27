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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace JSSoft.Library.Commands
{
    public class Terminal
    {
        private static readonly ConsoleKeyInfo cancelKeyInfo = new ConsoleKeyInfo('\u0003', ConsoleKey.C, false, false, true);
        private static byte[] charWidths;
        private static int defaultBufferWidth = 80;

        private readonly Dictionary<ConsoleKeyInfo, Action> actionMaps = new Dictionary<ConsoleKeyInfo, Action>();
        private readonly List<string> histories = new List<string>();
        private readonly List<string> completions = new List<string>();

        private int x1 = 0;
        private int y1 = Console.CursorTop;
        private int width = Console.BufferWidth;
        private int height = Console.BufferHeight;
        private int historyIndex;
        private string prompt = string.Empty;
        private string command = string.Empty;
        private string promptText = string.Empty;
        private string inputText = string.Empty;
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
            var platformName = GetPlatformName(Environment.OSVersion.Platform);
            var name = $"{typeof(Terminal).Namespace}.{platformName}.dat";
            using var stream = typeof(Terminal).Assembly.GetManifestResourceStream(name);
            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            charWidths = buffer;

            string GetPlatformName(PlatformID platformID) => platformID switch
            {
                PlatformID.Unix => $"{PlatformID.Unix}",
                PlatformID.Win32NT => $"{PlatformID.Win32NT}",
                _ => $"{PlatformID.Win32NT}",
            };
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
                length += charWidths[(int)item];
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
            return this.ReadString(prompt, string.Empty);
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
            using var initializer = new Initializer(this, prompt, defaultText, isHidden);
            return this.ReadLineImpl(i => true, false);
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
            using var initializer = new Initializer(this, prompt, string.Empty, false);
            return this.ReadKeyImpl(filters);
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

        public int CursorPosition
        {
            get => this.cursorPosition;
            set
            {
                this.cursorPosition = value;
                this.Sync();
                if (this.isHidden == false)
                {
                    this.SetCursorPosition(value);
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
                if (this.command == value)
                    return;
                lock (LockedObject)
                {
                    using var visible = TerminalCursorVisible.Set(false);
                    var bufferWidth = this.width;
                    var bufferHeight = this.height;
                    var (x1, y1) = (this.x1, this.y1);
                    var (x2, y2) = (this.x2, this.y2);
                    var (x3, y3) = NextPosition(value, bufferWidth, x2, y2);
                    var len = (this.y3 - y3) * bufferWidth - x3 + this.x3;
                    var text = value + string.Empty.PadRight(Math.Max(0, len));

                    var (sx1, sy1) = (x2, y2);
                    var (sx2, sy2) = (x3, y3);
                    if (y3 >= bufferHeight)
                    {
                        var offset = y3 - this.y3;
                        sy2 -= offset;
                        y1 -= offset;
                        y2 -= offset;
                        y3 -= offset;
                    }

                    this.command = value;
                    this.promptText = this.prompt + this.command;
                    this.cursorPosition = this.command.Length;
                    (this.x1, this.y1) = (x1, y1);
                    (this.x2, this.y2) = (x2, y2);
                    (this.x3, this.y3) = (x3, y3);

                    Console.SetCursorPosition(sx1, sy1);
                    writer.Write(text);
                    this.OnDrawEnd(writer, x3, y3, bufferHeight);
                    Console.SetCursorPosition(sx2, sy2);
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
                    this.y1 = Console.CursorTop - (this.prompt.Length + this.cursorPosition) / Console.BufferWidth;
                    this.width = Console.BufferWidth;
                }
                return this.y1;
            }
            internal set => this.y1 = value;
        }

        public bool IsEnabled { get; set; } = true;

        public static int BufferWidth
        {
            get
            {
                if (Terminal.IsOutputRedirected == false)
                    return Console.BufferWidth;
                return defaultBufferWidth;
            }
            set
            {
                if (Terminal.IsOutputRedirected == false)
                    throw new InvalidOperationException(Resources.Exception_BufferWidthCannotSet);
                if (value <= 0)
                    throw new ArgumentOutOfRangeException();
                defaultBufferWidth = value;
            }
        }

        public static string NextCompletion(string[] completions, string text)
        {
            completions = completions.OrderBy(item => item).ToArray();
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
            completions = completions.OrderBy(item => item).ToArray();
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
                var bufferWidth = this.width;
                var bufferHeight = this.height;
                var command = this.isHidden == true ? string.Empty : this.command;
                var pre = command.Substring(0, this.cursorPosition);
                var (x1, y1) = (this.x1, this.y1);
                var (x2, y2) = NextPosition(prompt, bufferWidth, x1, y1);
                var (x3, y3) = NextPosition(command, bufferWidth, x2, y2);
                var len = (this.y3 - y3) * bufferWidth - x3 + this.x3;
                var text = prompt + command + string.Empty.PadRight(Math.Max(0, len));

                var (sx1, sy1) = (x1, y1);
                var (sx2, sy2) = NextPosition(pre, bufferWidth, x2, y2);
                if (y3 >= bufferHeight)
                {
                    var offset = y3 - this.y3;
                    sy2 -= offset;
                    y1 -= offset;
                    y2 -= offset;
                    y3 -= offset;
                }

                this.prompt = prompt;
                this.promptText = this.prompt + this.command;
                (this.x1, this.y1) = (x1, y1);
                (this.x2, this.y2) = (x2, y2);
                (this.x3, this.y3) = (x3, y3);

                if (writer != null)
                {
                    Console.SetCursorPosition(sx1, sy1);
                    writer.Write(text);
                    this.OnDrawEnd(writer, x3, y3, bufferHeight);
                    Console.SetCursorPosition(sx2, sy2);
                    // this.SetCursorPosition(this.cursorPosition);
                }

                // if (y3 >= Console.BufferHeight)
                // {
                //     this.y1 -= (y2 - y1);
                //     this.y2 = this.y1 + (y2 - y1);
                //     this.y3 = this.y1 + (y3 - y1);
                // }

                // this.prompt = prompt;
                // this.promptText = this.prompt + this.command;
                // if (this.writer != null)
                // {
                //     this.Erase(this.writer, this.x1, this.y1, this.x3, this.y3);
                //     this.Draw();
                // }
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

        protected virtual void OnDrawCommand(TextWriter writer, string command)
        {
            writer.Write(command);
        }

        private void InsertText(string text)
        {
            if (text == string.Empty)
                return;

            lock (LockedObject)
            {
                using var visible = TerminalCursorVisible.Set(false);
                var writer = this.writer;
                var cursorPosition = this.isHidden == true ? 0 : this.cursorPosition + text.Length;
                var extra = this.command.Substring(this.cursorPosition);
                var command = this.command.Insert(this.cursorPosition, text);
                var promptText = this.prompt + command;

                this.cursorPosition = cursorPosition;
                this.command = command;
                this.promptText = this.prompt + this.command;
                if (this.isHidden == false)
                {
                    var bufferWidth = this.width;
                    var bufferHeight = this.height;
                    var (x1, y1) = (this.x1, this.y1);
                    var (x2, y2) = (this.x2, this.y2);
                    var (x3, y3) = NextPosition(command, bufferWidth, x2, y2);

                    var (sx1, sy1) = (this.x2, this.y2);
                    var (sx2, sy2) = (x3, y3);
                    if (y3 >= bufferHeight)
                    {
                        var offset = y3 - this.y3;
                        y1 -= offset;
                        y2 -= offset;
                        y3 -= offset;
                    }
                    (this.x1, this.y1) = (x1, y1);
                    (this.x2, this.y2) = (x2, y2);
                    (this.x3, this.y3) = (x3, y3);
                    Console.SetCursorPosition(sx1, sy1);
                    this.InvokeDrawCommand(writer, command);
                    this.OnDrawEnd(writer, sx2, sy2, bufferHeight);
                }

                this.SetCursorPosition(this.cursorPosition);
            }
        }

        private (int x, int y) SetCursorPosition(int cursorPosition)
        {
            var bufferWidth = this.width;
            var bufferHeight = this.height;
            var position = this.isHidden == true ? 0 : cursorPosition;
            var (x1, y1) = (this.x1, this.y1);
            var index = this.prompt.Length + position;
            var text = this.promptText.Substring(0, index);
            var (x2, y2) = NextPosition(text, bufferWidth, x1, y1);
            y2 = Math.Min(y2, bufferHeight - 1);
            if (Environment.OSVersion.Platform == PlatformID.Unix)
                Console.SetCursorPosition(x2, 0);
            Console.SetCursorPosition(x2, y2);
            return (x2, y2);
        }

        private void BackspaceImpl()
        {
            var bufferWidth = this.width;
            var extra = this.command.Substring(this.cursorPosition);
            var command = this.command.Remove(this.cursorPosition - 1, 1);
            var pre = command.Substring(0, command.Length - extra.Length);
            var cursorPosition = this.cursorPosition - 1;
            var endPosition = this.command.Length;
            var (x2, y2) = (this.x2, this.y2);
            var (x3, y3) = NextPosition(pre, bufferWidth, x2, y2);
            var (x4, y4) = NextPosition(extra, bufferWidth, x3, y3);
            var len = (this.y3 - y3) * bufferWidth - x3 + this.x3;
            var text = extra + string.Empty.PadRight(len);

            this.command = command;
            this.promptText = this.prompt + this.command;
            this.cursorPosition = cursorPosition;
            (this.x3, this.y3) = (x4, y4);

            Console.SetCursorPosition(x3, y3);
            writer.Write(text);
            Console.SetCursorPosition(x3, y3);
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
            using var initializer = new Initializer(this, prompt, $"{defaultValue}", false);
            return this.ReadLineImpl(validation, false);
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

                this.y1 = Console.CursorTop;
                this.width = Console.BufferWidth;
                this.isHidden = false;
                this.command = defaultText;
                this.prompt = prompt;
                this.promptText = prompt + defaultText;
                this.cursorPosition = 0;
                this.isHidden = isHidden;
                this.inputText = defaultText;
                (this.x2, this.y2) = NextPosition(prompt, this.width, this.x1, this.y1);
                (this.x3, this.y3) = NextPosition(command, this.width, this.x2, this.y2);
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
        }

        private void InvokeDrawCommand(TextWriter writer, string command)
        {
            this.OnDrawCommand(writer, command);
        }

        internal static int[] Split(string text, int bufferWidth)
        {
            var lineList = new List<int>((text.Length / bufferWidth) + 1);
            var x = 0;
            var y = 0;
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
                    lineList.Add(x);
                    x = 0;
                    y++;
                    continue;
                }

                var w = charWidths[(int)ch];
                if (x + w >= bufferWidth)
                {
                    lineList.Add(x);
                    x = x + w - bufferWidth;
                    y++;
                }
                else
                {
                    x += w;
                }
            }
            lineList.Add(x);
            return lineList.ToArray();
        }

        internal static (int x, int y) NextPosition(string text, int bufferWidth, int x, int y)
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

                var w = charWidths[(int)ch];
                if (x + w >= bufferWidth)
                {
                    x = x + w - bufferWidth;
                    y++;
                }
                else
                {
                    x += w;
                }
            }
            return (x, y);
        }

        internal static int GetStringLength(string text)
        {
            var (x, y) = NextPosition(text, int.MaxValue, 0, 0);
            return x;
        }

        internal static string GetOverwrappedText(string text, int bufferWidth)
        {
            var lineBreak = text.EndsWith(Environment.NewLine) == true ? Environment.NewLine : string.Empty;
            var text2 = text.Substring(0, text.Length - lineBreak.Length);
            var items = text2.Split(Environment.NewLine, StringSplitOptions.None);
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return text;
            }
            else
            {
                var itemList = new List<string>(items.Length);
                foreach (var item in items)
                {
                    itemList.Add(item + "\x1b[K");
                }
                return string.Join(Environment.NewLine, itemList) + lineBreak;
            }
        }

        internal string ReadStringInternal(string prompt)
        {
            using var initializer = new Initializer(this, prompt, string.Empty, false);
            return this.ReadLineImpl(i => true, true);
        }

        internal void Erase(TextWriter writer, int x1, int y1, int x2, int y2)
        {
            var bufferWidth = this.width;
            var (sx, sy) = (Console.CursorLeft, Console.CursorTop);
            var x = x1;
            var len = (y2 - y1) * bufferWidth - x1 + x2;
            var text = string.Empty.PadRight(len);
            Console.SetCursorPosition(x1, y1);
            writer.Write(text);
            Console.SetCursorPosition(sx, sy);
        }

        internal void Draw()
        {
            Console.SetCursorPosition(this.x1, this.y1);
            var bufferWidth = this.width;
            var bufferHeight = this.height;
            var command = this.isHidden == true ? string.Empty : this.command;
            var (x1, y1) = (this.x1, this.y1);
            var (x2, y2) = NextPosition(prompt, bufferWidth, x1, y1);
            var (x3, y3) = NextPosition(command, bufferWidth, x2, y2);
            if (y3 >= Console.BufferHeight)
            {
                this.y1 -= (y2 - y1);
                this.y2 = this.y1 + (y2 - y1);
                this.y3 = this.y1 + (y3 - y1);
            }
            this.InvokeDrawPrompt(this.writer, prompt);
            this.InvokeDrawCommand(this.writer, command);
            this.OnDrawEnd(this.writer, x3, y3, bufferHeight);
        }

        internal (int x, int y) Draw(TextWriter writer, string text, int x, int y)
        {
            var bufferWidth = this.width;
            var bufferHeight = this.height;
            var promptText = this.promptText;
            var prompt = this.prompt;
            var command = this.command;
            var (x8, y8) = (this.x1, this.y1);
            var (x9, y9) = NextPosition(text, bufferWidth, x8, y8);
            var (x1, y1) = x9 == 0 ? (x9, y9) : (0, y9 + 1);
            var (x2, y2) = NextPosition(prompt, bufferWidth, x1, y1);
            var (x3, y3) = NextPosition(command, bufferWidth, x2, y2);
            var text5 = GetOverwrappedText(text, bufferWidth);
            var text6 = GetOverwrappedText(promptText, bufferWidth);

            var (sx1, sy1) = (x8, this.y1);
            var (sx2, sy2) = (x8, this.y1);
            var h = (y3 - y8 + 1);
            if (y3 >= bufferHeight)
            {
                this.y1 = (y1 + bufferHeight) - (y3 + 1);
                this.y2 = this.y1 + y2 - y1;
                this.y3 = this.y1 + y3 - y1;
                sy2 = this.y1 - (y9 - y8);
            }
            else
            {
                this.y1 = y1;
                this.y2 = y1 + y2 - y1;
                this.y3 = y1 + y3 - y1;
            }
            Console.SetCursorPosition(sx1, sy1);
            writer.Write(string.Empty.PadRight(h * bufferWidth - 1, ' '));
            Console.SetCursorPosition(sx2, sy2);
            writer.Write(text5 + text6);
            this.OnDrawEnd(writer, x3, y3, bufferHeight);
            this.SetCursorPosition(this.cursorPosition);
            return (x9, y9);
        }

        private void OnDrawEnd(TextWriter writer, int x, int y, int bufferHeight)
        {
            if (y >= bufferHeight)
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    if (x == 0 && Console.CursorLeft != 0)
                    {
                        writer.WriteLine();
                    }
                }
                else if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    if (x == 0)
                    {
                        writer.WriteLine();
                    }
                }
            }
        }

        private void Sync()
        {
            if (this.width != Console.BufferWidth)
            {
                int qwer = 0;
            }
        }

        internal static object LockedObject { get; } = new object();

        #region Initializer

        class Initializer : IDisposable
        {
            private readonly Terminal terminal;

            public Initializer(Terminal terminal, string prompt, string defaultText, bool isHidden)
            {
                this.terminal = terminal;
                this.terminal.Initialize(prompt, defaultText, isHidden);
            }

            public void Dispose()
            {
                this.terminal.Release();
            }
        }

        #endregion
    }
}
