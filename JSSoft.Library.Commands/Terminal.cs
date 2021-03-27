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
        private static readonly ConsoleKeyInfo cancelKeyInfo = new('\u0003', ConsoleKey.C, false, false, true);
        private static byte[] charWidths;
        private static int defaultBufferWidth = 80;

        private readonly Dictionary<ConsoleKeyInfo, Action> actionMaps = new();
        private readonly List<string> histories = new();
        private readonly List<string> completions = new();

        private TerminalPoint pt1 = new(0, Console.CursorTop);
        private TerminalPoint pt2;
        private TerminalPoint pt3;
        private StringBuilder outputText = new();
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
                if (this.CursorPosition < this.Command.Length)
                {
                    this.DeleteImpl();
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
                }
            }
        }

        public void DeleteToEnd()
        {
            this.Command = this.command.Substring(this.cursorPosition);
        }

        public void DeleteToHome()
        {
            this.Command = this.command.Remove(this.cursorPosition);
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
                if (value < 0 || value > this.command.Length)
                    throw new ArgumentOutOfRangeException(nameof(value));

                this.cursorPosition = value;
                this.inputText = this.command.Substring(0, value);
                this.completion = string.Empty;
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
                lock (LockedObject)
                {
                    if (this.command == value)
                        return;
                    using var visible = TerminalCursorVisible.Set(false);
                    var bufferWidth = this.width;
                    var bufferHeight = this.height;
                    var pt1 = this.pt1;
                    var pt2 = this.pt2;
                    var pt3 = NextPosition(value, bufferWidth, pt2);
                    var len = pt3.DistanceOf(this.pt3, bufferWidth);
                    var text = value + string.Empty.PadRight(Math.Max(0, len));

                    var s1 = pt2;
                    var s2 = pt3;
                    if (pt3.Y >= bufferHeight)
                    {
                        var offset = pt3.Y - this.pt3.Y;
                        s2.Y -= offset;
                        pt1.Y -= offset;
                        pt2.Y -= offset;
                        pt3.Y -= offset;
                    }

                    this.command = value;
                    this.promptText = this.prompt + this.command;
                    this.cursorPosition = this.command.Length;
                    this.inputText = value;
                    this.completion = string.Empty;
                    this.pt1 = pt1;
                    this.pt2 = pt2;
                    this.pt3 = pt3;

                    this.SetCursorPosition(s1);
                    writer.Write(text);
                    this.OnDrawEnd(writer, pt3, bufferHeight);
                    this.SetCursorPosition(s2);
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
                    this.pt1.Y = Console.CursorTop - (this.prompt.Length + this.cursorPosition) / Console.BufferWidth;
                    this.width = Console.BufferWidth;
                }
                return this.pt1.Y;
            }
            internal set => this.pt1.Y = value;
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
                var pt1 = this.pt1;
                var pt2 = NextPosition(prompt, bufferWidth, pt1);
                var pt3 = NextPosition(command, bufferWidth, pt2);
                var len = pt3.DistanceOf(this.pt3, bufferWidth);
                var text = prompt + command + string.Empty.PadRight(Math.Max(0, len));

                var s1 = pt1;
                var s2 = NextPosition(pre, bufferWidth, pt2);
                if (pt3.Y >= bufferHeight)
                {
                    var offset = pt3.Y - this.pt3.Y;
                    s2.Y -= offset;
                    pt1.Y -= offset;
                    pt2.Y -= offset;
                    pt3.Y -= offset;
                }

                this.prompt = prompt;
                this.promptText = this.prompt + this.command;
                this.pt1 = pt1;
                this.pt2 = pt2;
                this.pt3 = pt3;

                if (writer != null)
                {
                    this.SetCursorPosition(s1);
                    writer.Write(text);
                    this.OnDrawEnd(writer, pt3, bufferHeight);
                    this.SetCursorPosition(s2);
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
                this.promptText = this.prompt + command;
                this.inputText = command.Substring(0, cursorPosition);
                this.completion = string.Empty;
                if (this.isHidden == false)
                {
                    var bufferWidth = this.width;
                    var bufferHeight = this.height;
                    var pt1 = this.pt1;
                    var pt2 = this.pt2;
                    var pt3 = NextPosition(command, bufferWidth, pt2);

                    var s1 = this.pt2;
                    var s2 = pt3;
                    if (pt3.Y >= bufferHeight)
                    {
                        var offset = pt3.Y - this.pt3.Y;
                        pt1.Y -= offset;
                        pt2.Y -= offset;
                        pt3.Y -= offset;
                    }
                    this.pt1 = pt1;
                    this.pt2 = pt2;
                    this.pt3 = pt3;
                    this.SetCursorPosition(s1);
                    this.InvokeDrawCommand(writer, command);
                    this.OnDrawEnd(writer, s2, bufferHeight);
                }

                this.SetCursorPosition(this.cursorPosition);
            }
        }

        private TerminalPoint SetCursorPosition(int cursorPosition)
        {
            var bufferWidth = this.width;
            var bufferHeight = this.height;
            var position = this.isHidden == true ? 0 : cursorPosition;
            var pt1 = this.pt1;
            var index = this.prompt.Length + position;
            var text = this.promptText.Substring(0, index);
            var pt2 = NextPosition(text, bufferWidth, pt1);
            pt2.Y = Math.Min(pt2.Y, bufferHeight - 1);
            if (Environment.OSVersion.Platform == PlatformID.Unix)
                Console.SetCursorPosition(pt2.X, 0);
            this.SetCursorPosition(pt2);
            return pt2;
        }

        private void BackspaceImpl()
        {
            var bufferWidth = this.width;
            var extra = this.command.Substring(this.cursorPosition);
            var command = this.command.Remove(this.cursorPosition - 1, 1);
            var pre = command.Substring(0, command.Length - extra.Length);
            var cursorPosition = this.cursorPosition - 1;
            var endPosition = this.command.Length;
            var pt2 = this.pt2;
            var pt3 = NextPosition(pre, bufferWidth, pt2);
            var pt4 = NextPosition(extra, bufferWidth, pt3);
            var len = pt3.DistanceOf(this.pt3, bufferWidth);
            var text = extra + string.Empty.PadRight(len);

            this.command = command;
            this.promptText = this.prompt + this.command;
            this.cursorPosition = cursorPosition;
            this.pt3 = pt4;

            this.SetCursorPosition(pt3);
            writer.Write(text);
            this.SetCursorPosition(pt3);
        }

        private void DeleteImpl()
        {
            var bufferWidth = this.width;
            var extra = this.command.Substring(this.cursorPosition + 1);
            var command = this.command.Remove(this.cursorPosition, 1);
            var pre = command.Substring(0, command.Length - extra.Length);
            var endPosition = this.command.Length;
            var pt2 = this.pt2;
            var pt3 = NextPosition(pre, bufferWidth, pt2);
            var pt4 = NextPosition(extra, bufferWidth, pt3);
            var len = pt3.DistanceOf(this.pt3, bufferWidth);
            var text = extra + string.Empty.PadRight(len);

            this.command = command;
            this.promptText = this.prompt + this.command;
            this.pt3 = pt4;

            this.SetCursorPosition(pt3);
            writer.Write(text);
            this.SetCursorPosition(pt3);
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

        // private void UpdateInputText()
        // {
        //     this.inputText = this.command.Substring(0, this.cursorPosition);
        //     this.completion = string.Empty;
        // }

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

                this.pt1.Y = Console.CursorTop;
                this.width = Console.BufferWidth;
                this.isHidden = false;
                this.command = defaultText;
                this.prompt = prompt;
                this.promptText = prompt + defaultText;
                this.cursorPosition = 0;
                this.isHidden = isHidden;
                this.inputText = defaultText;
                this.pt2 = NextPosition(prompt, this.width, this.pt1);
                this.pt3 = NextPosition(command, this.width, this.pt2);
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

        internal static TerminalPoint NextPosition(string text, int bufferWidth, TerminalPoint pt)
        {
            var x = pt.X;
            var y = pt.Y;
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
            return new TerminalPoint(x, y);
        }

        internal static int GetStringLength(string text)
        {
            var pt = NextPosition(text, int.MaxValue, TerminalPoint.Zero);
            return pt.X;
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

        internal void SetCursorPosition(TerminalPoint pt) => Console.SetCursorPosition(pt.X, pt.Y);

        internal string ReadStringInternal(string prompt)
        {
            using var initializer = new Initializer(this, prompt, string.Empty, false);
            return this.ReadLineImpl(i => true, true);
        }

        internal void Draw()
        {
            this.SetCursorPosition(this.pt1);
            var bufferWidth = this.width;
            var bufferHeight = this.height;
            var command = this.isHidden == true ? string.Empty : this.command;
            var pt1 = this.pt1;
            var pt2 = NextPosition(prompt, bufferWidth, pt1);
            var pt3 = NextPosition(command, bufferWidth, pt2);
            if (pt3.Y >= Console.BufferHeight)
            {
                var offset = (pt2.Y - pt1.Y);
                this.pt1.Y -= (pt2.Y - pt1.Y);
                this.pt2.Y = this.pt1.Y + (pt2.Y - pt1.Y);
                this.pt3.Y = this.pt1.Y + (pt3.Y - pt1.Y);
            }
            this.InvokeDrawPrompt(this.writer, prompt);
            this.InvokeDrawCommand(this.writer, command);
            this.OnDrawEnd(this.writer, pt3, bufferHeight);
        }

        internal TerminalPoint Draw(TextWriter writer, string text, int x, int y)
        {
            var bufferWidth = this.width;
            var bufferHeight = this.height;
            var promptText = this.promptText;
            var prompt = this.prompt;
            var command = this.command;
            var pt8 = this.pt1;
            var pt9 = NextPosition(text, bufferWidth, pt8);
            var pt1 = pt9.X == 0 ? new TerminalPoint(pt9.X, pt9.Y) : new TerminalPoint(0, pt9.Y + 1);
            var pt2 = NextPosition(prompt, bufferWidth, pt1);
            var pt3 = NextPosition(command, bufferWidth, pt2);
            var text5 = GetOverwrappedText(text, bufferWidth);
            var text6 = GetOverwrappedText(promptText, bufferWidth);

            var s1 = new TerminalPoint(pt8.X, this.pt1.Y);
            var s2 = new TerminalPoint(pt8.X, this.pt1.Y);
            var h = (pt3.Y - pt8.Y + 1);
            if (pt3.Y >= bufferHeight)
            {
                this.pt1.Y = (pt1.Y + bufferHeight) - (pt3.Y + 1);
                this.pt2.Y = this.pt1.Y + pt2.Y - pt1.Y;
                this.pt3.Y = this.pt1.Y + pt3.Y - pt1.Y;
                s2.Y = this.pt1.Y - (pt9.Y - pt8.Y);
            }
            else
            {
                this.pt1.Y = pt1.Y;
                this.pt2.Y = pt1.Y + pt2.Y - pt1.Y;
                this.pt3.Y = pt1.Y + pt3.Y - pt1.Y;
            }
            this.outputText.Append(text);
            this.SetCursorPosition(s1);
            writer.Write(string.Empty.PadRight(h * bufferWidth - 1, ' '));
            this.SetCursorPosition(s2);
            writer.Write(text5 + text6);
            this.OnDrawEnd(writer, pt3, bufferHeight);
            this.SetCursorPosition(this.cursorPosition);
            return pt9;
        }

        private void OnDrawEnd(TextWriter writer, TerminalPoint pt, int bufferHeight)
        {
            if (pt.Y >= bufferHeight)
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    if (pt.X == 0 && Console.CursorLeft != 0)
                    {
                        writer.WriteLine();
                    }
                }
                else if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    if (pt.X == 0)
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
