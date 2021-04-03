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
        private static TextWriter consoleOut = Console.Out;
        private static TextWriter consoleError = Console.Error;

        private readonly Dictionary<ConsoleKeyInfo, Action> actionMaps = new();
        private readonly List<string> histories = new();
        private readonly List<string> completions = new();

        private TerminalPoint pt1 = new(0, Console.CursorTop);
        private TerminalPoint pt2;
        private TerminalPoint pt3;
        private TerminalPoint ct1;
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

        private bool isHidden;
        private bool treatControlCAsInput;
        private bool isCancellationRequested;
        private ConsoleColor?[] foregroundColors = new ConsoleColor?[] { };
        private ConsoleColor?[] backgroundColors = new ConsoleColor?[] { };

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

        public string ReadString(string prompt, string command)
        {
            return this.ReadString(prompt, command, false);
        }

        public string ReadString(string prompt, string command, bool isHidden)
        {
            using var initializer = new Initializer(this)
            {
                Prompt = prompt,
                Command = command,
                IsHidden = isHidden,
            };
            return initializer.ReadLineImpl(i => true, false);
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
            using var initializer = new Initializer(this)
            {
                Prompt = prompt,
            };
            return initializer.ReadKeyImpl(filters);
        }

        public void NextHistory()
        {
            lock (LockedObject)
            {
                if (this.historyIndex + 1 < this.histories.Count)
                {
                    this.SetCommand(this.histories[this.historyIndex + 1]);
                    this.historyIndex++;
                }
            }
        }

        public void PrevHistory()
        {
            lock (LockedObject)
            {
                if (this.historyIndex > 0)
                {
                    this.SetCommand(this.histories[this.historyIndex - 1]);
                    this.historyIndex--;
                }
                else if (this.histories.Count == 1)
                {
                    this.SetCommand(this.histories[0]);
                    this.historyIndex = 0;
                }
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
                this.SetCommand(string.Empty);
            }
        }

        public void Delete()
        {
            lock (LockedObject)
            {
                if (this.cursorPosition < this.command.Length)
                {
                    this.DeleteImpl();
                }
            }
        }

        public void Home()
        {
            lock (LockedObject)
            {
                this.SetCursorPosition(0);
            }
        }

        public void End()
        {
            lock (LockedObject)
            {
                this.SetCursorPosition(this.command.Length);
            }
        }

        public void Left()
        {
            lock (LockedObject)
            {
                if (this.cursorPosition > 0)
                {
                    this.SetCursorPosition(this.cursorPosition - 1);
                }
            }
        }

        public void Right()
        {
            lock (LockedObject)
            {
                if (this.cursorPosition < this.command.Length)
                {
                    this.SetCursorPosition(this.cursorPosition + 1);
                }
            }
        }

        public void Backspace()
        {
            lock (LockedObject)
            {
                if (this.cursorPosition > 0)
                {
                    this.BackspaceImpl();
                }
            }
        }

        public void DeleteToEnd()
        {
            lock (LockedObject)
            {
                this.SetCommand(this.command.Substring(this.cursorPosition));
            }
        }

        public void DeleteToHome()
        {
            lock (LockedObject)
            {
                this.SetCommand(this.command.Remove(this.cursorPosition));
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
                if (value < 0 || value > this.command.Length)
                    throw new ArgumentOutOfRangeException(nameof(value));
                if (this.cursorPosition == value)
                    return;
                lock (LockedObject)
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
                    this.SetCommand(value);
                }
            }
        }

        public string Prompt
        {
            get => this.prompt;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (this.prompt == value)
                    return;
                lock (LockedObject)
                {
                    this.SetPrompt(value);
                }
            }
        }

        public bool IsReading { get; private set; }

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

        public static Terminal Current { get; private set; }

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

        protected virtual TextWriter Out => Console.Out;

        protected virtual TextWriter Error => Console.Error;

        public void Sync()
        {
            if (this.width != Console.BufferWidth)
            {
                int qwe = 0;
            }
        }

        private void InsertText(string text)
        {
            if (text == string.Empty)
                return;

            lock (LockedObject)
            {
                using var visible = TerminalCursorVisible.Set(false);
                var bufferWidth = this.width;
                var bufferHeight = this.height;
                var cursorPosition = this.cursorPosition + text.Length;
                var extra = this.command.Substring(this.cursorPosition);
                var command = this.command.Insert(this.cursorPosition, text);
                var pre = command.Substring(0, command.Length - extra.Length);
                var promptText = this.prompt + command;

                var pt1 = this.pt1;
                var pt2 = this.pt2;
                var pt3 = NextPosition(command, bufferWidth, pt2);
                var c1 = NextPosition(pre, bufferWidth, pt2);

                var s1 = this.pt2;
                var s2 = pt3;
                if (pt3.Y >= bufferHeight)
                {
                    var offset = pt3.Y - this.pt3.Y;
                    pt1.Y -= offset;
                    pt2.Y -= offset;
                    pt3.Y -= offset;
                    c1.Y -= offset;
                }

                this.cursorPosition = cursorPosition;
                this.command = command;
                this.promptText = this.prompt + command;
                this.inputText = command.Substring(0, cursorPosition);
                this.completion = string.Empty;
                this.pt1 = pt1;
                this.pt2 = pt2;
                this.pt3 = pt3;

                if (this.isHidden == false)
                {
                    Draw(s1, s2, c1, command, bufferHeight);
                }
                else
                {
                    SetCursorPosition(s1);
                }
            }
        }

        public static bool IsUnix = Environment.OSVersion.Platform == PlatformID.Unix;

        private static void Draw(TerminalPoint pt1, TerminalPoint pt2, TerminalPoint ct, string text, int bufferHeight)
        {
            using var stream = Console.OpenStandardOutput();
            using var writer = new StreamWriter(stream, Console.OutputEncoding);
            writer.AutoFlush = true;
            if (IsUnix == true)
            {
                writer.Write($"\x1b[{pt1.Y + 1};{pt1.X + 1}f");
            }
            if (IsEnd(pt2, bufferHeight) == true)
                writer.WriteLine(text);
            else
                writer.Write(text);
            if (IsUnix == true)
            {
                writer.Write($"\x1b[{ct.Y + 1};{ct.X + 1}f");
            }
        }

        private static string GetEscapeString(TerminalPoint pt)
        {
            return $"\x1b[{pt.Y + 1};{pt.X + 1}f";
        }

        // private TerminalPoint SetCursorPositionTest(int cursorPosition)
        // {
        //     var bufferWidth = this.width;
        //     var bufferHeight = this.height;
        //     var text = this.isHidden == true ? string.Empty : this.command.Substring(0, cursorPosition);
        //     var pt4 = NextPosition(text, bufferWidth, this.pt2);
        //     // pt2.Y = Math.Min(pt2.Y, bufferHeight - 1);
        //     if (Environment.OSVersion.Platform == PlatformID.Unix)
        //     {
        //         using (var stream = Console.OpenStandardOutput())
        //         using (var writer = new StreamWriter(stream, Console.OutputEncoding))
        //         {
        //             writer.Write(GetEscapeString(pt4));
        //         }
        //     }
        //     else
        //     {
        //         SetCursorPosition(pt2);
        //     }
        //     return pt2;
        // }

        private void BackspaceImpl()
        {
            var bufferWidth = this.width;
            var bufferHeight = this.height;
            var extra = this.command.Substring(this.cursorPosition);
            var command = this.command.Remove(this.cursorPosition - 1, 1);
            var pre = command.Substring(0, command.Length - extra.Length);
            var cursorPosition = this.cursorPosition - 1;
            var endPosition = this.command.Length;
            var pt2 = this.pt2;
            var pt3 = NextPosition(pre, bufferWidth, pt2);
            var pt4 = NextPosition(extra, bufferWidth, pt3);
            var pt5 = new TerminalPoint(bufferWidth - 1, pt4.Y);
            var text = GenerateString();

            this.command = command;
            this.promptText = this.prompt + this.command;
            this.cursorPosition = cursorPosition;
            this.pt3 = pt4;

            if (this.isHidden == false)
            {
                Draw(pt3, pt3, pt3, text, bufferHeight);
            }
            else
            {
                SetCursorPosition(pt2);
            }
            string GenerateString()
            {
                if (IsUnix == true)
                    return extra + "\x1b[K";
                return extra + PadString(pt4, pt5, bufferWidth);
            }
        }

        private void DeleteImpl()
        {
            var bufferWidth = this.width;
            var bufferHeight = this.height;
            var extra = this.command.Substring(this.cursorPosition + 1);
            var command = this.command.Remove(this.cursorPosition, 1);
            var pre = command.Substring(0, command.Length - extra.Length);
            var endPosition = this.command.Length;
            var pt2 = this.pt2;
            var pt3 = NextPosition(pre, bufferWidth, pt2);
            var pt4 = NextPosition(extra, bufferWidth, pt3);
            var pt5 = new TerminalPoint(bufferWidth - 1, pt4.Y);
            var text = GenerateString();

            this.command = command;
            this.promptText = this.prompt + this.command;
            this.pt3 = pt4;

            if (this.isHidden == false)
            {
                Draw(pt3, pt3, pt3, text, bufferHeight);
            }
            else
            {
                SetCursorPosition(pt2);
            }
            string GenerateString()
            {
                if (IsUnix == true)
                    return extra + "\x1b[K";
                return extra + PadString(pt4, pt5, bufferWidth);
            }
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
                this.SetCommand(command);
                this.completion = completion;
                this.inputText = inputText;
            }
        }

        private void SetPrompt(string prompt)
        {
            var bufferWidth = this.width;
            var bufferHeight = this.height;
            var command = this.command;
            var pre = command.Substring(0, this.cursorPosition);
            var pt1 = this.pt1;
            var pt2 = NextPosition(prompt, bufferWidth, pt1);
            var pt3 = NextPosition(command, bufferWidth, pt2);
            var pt4 = NextPosition(pre, bufferWidth, pt2);
            var text = prompt + command + PadString(pt3, this.pt3, bufferWidth);

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

            if (this.IsReading == true)
            {
                Draw(s1, pt3, pt4, text, bufferHeight);
            }
        }

        private void SetCommand(string value)
        {
            using var visible = TerminalCursorVisible.Set(false);
            var bufferWidth = this.width;
            var bufferHeight = this.height;
            var pt1 = this.pt1;
            var pt2 = this.pt2;
            var pt3 = NextPosition(value, bufferWidth, pt2);
            var text = value + PadString(pt3, this.pt3, bufferWidth);

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

            Draw(s1, pt3, pt3, text, bufferHeight);
        }

        private void SetCursorPosition(int cursorPosition)
        {
            var bufferWidth = this.width;
            var bufferHeight = this.height;
            var text = this.isHidden == true ? string.Empty : this.command.Substring(0, cursorPosition);
            var pt4 = NextPosition(text, bufferWidth, this.pt2);

            this.cursorPosition = cursorPosition;
            this.inputText = this.command.Substring(0, cursorPosition);
            this.completion = string.Empty;
            SetCursorPosition(pt4);
        }

        private object ReadNumber(string prompt, object value, Func<string, bool> validation)
        {
            using var initializer = new Initializer(this)
            {
                Prompt = prompt,
                Command = $"{value}"
            };
            return initializer.ReadLineImpl(validation, false);
        }

        private string ReadLineImpl(Func<string, bool> validation, bool recordHistory)
        {
            while (true)
            {
                Thread.Sleep(1);
                this.Sync();
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
                this.Sync();
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

        private void Initialize(string prompt, string command, bool isHidden)
        {
            lock (LockedObject)
            {
                var bufferWidth = Console.BufferWidth;
                var bufferHeight = Console.BufferHeight;

                var pt1 = new TerminalPoint(0, Console.CursorTop);
                var pt2 = NextPosition(prompt, bufferWidth, pt1);
                var pt3 = NextPosition(command, bufferWidth, pt2);
                var s1 = pt1;

                if (pt3.Y >= bufferHeight)
                {
                    var offset = (pt2.Y - pt1.Y);
                    pt1.Y -= offset;
                    pt2.Y -= offset;
                    pt3.Y -= offset;
                }

                this.treatControlCAsInput = Console.TreatControlCAsInput;
                this.width = bufferWidth;
                this.height = bufferHeight;
                this.command = command;
                this.prompt = prompt;
                this.promptText = prompt + command;
                this.cursorPosition = 0;
                this.isHidden = isHidden;
                this.inputText = command;
                this.completion = string.Empty;
                this.pt1 = pt1;
                this.pt2 = pt2;
                this.pt3 = pt3;
                this.ct1 = TerminalPoint.Zero;
                this.IsReading = true;

                Draw(s1, pt3, pt3, prompt + command, bufferHeight);
            }
        }

        private void Release()
        {
            lock (LockedObject)
            {
                using (var stream = Console.OpenStandardOutput())
                using (var writer = new StreamWriter(stream, Console.OutputEncoding))
                {
                    writer.WriteLine();
                }
                this.outputText.AppendLine(this.promptText);
                this.isHidden = false;
                this.IsReading = false;
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
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return text;
            }
            else
            {
                var lineBreak = text.EndsWith(Environment.NewLine) == true ? Environment.NewLine : string.Empty;
                var text2 = text.Substring(0, text.Length - lineBreak.Length);
                var items = text2.Split(Environment.NewLine, StringSplitOptions.None);
                return string.Join($"\x1b[K{Environment.NewLine}", items) + lineBreak;
            }
        }

        internal static string PadString(TerminalPoint pt1, TerminalPoint pt2, int bufferWidth)
        {
            var len = Math.Max(0, pt1.DistanceOf(pt2, bufferWidth) - 1);
            var text = string.Empty;
            if (Environment.OSVersion.Platform == PlatformID.Unix)
                return string.Empty.PadRight(len) + "\x1b[0K";
            return string.Empty.PadRight(len);
        }

        internal static void SetCursorPosition(TerminalPoint pt)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                using (var stream = Console.OpenStandardOutput())
                using (var writer = new StreamWriter(stream, Console.OutputEncoding))
                {
                    writer.Write(GetEscapeString(pt));
                }
            }
            else
            {
                Console.SetCursorPosition(pt.X, pt.Y);
            }
        }

        internal string ReadStringInternal(string prompt)
        {
            using var initializer = new Initializer(this)
            {
                Prompt = prompt,
            };
            return initializer.ReadLineImpl(i => true, true);
        }

        internal void Draw(TextWriter writer, string text)
        {
            var bufferWidth = this.width;
            var bufferHeight = this.height;
            var promptText = this.promptText;
            var prompt = this.prompt;
            var command = this.command;
            var pre = this.command.Substring(0, this.cursorPosition);
            var pt8 = this.pt1 + this.ct1;
            var ct1 = NextPosition(text, bufferWidth, pt8);
            var text1 = text.EndsWith(Environment.NewLine) == true || ct1.X == 0 ? text : text + Environment.NewLine;
            var pt9 = NextPosition(text1, bufferWidth, pt8);
            var pt1 = pt9.X == 0 ? new TerminalPoint(pt9.X, pt9.Y) : new TerminalPoint(0, pt9.Y + 1);
            var pt2 = NextPosition(prompt, bufferWidth, pt1);
            var pt3 = NextPosition(command, bufferWidth, pt2);
            var pt4 = NextPosition(pre, bufferWidth, pt2);
            var text6 = GetOverwrappedText(text1 + promptText, bufferWidth);

            var s1 = new TerminalPoint(pt8.X, pt8.Y);
            var s2 = new TerminalPoint(pt8.X, pt8.Y);
            var s3 = pt3;
            var len = s1.DistanceOf(pt3, bufferWidth);
            var text7 = PadString(s1, pt3, bufferWidth);
            if (pt3.Y >= bufferHeight)
            {
                var offset = pt3.Y + 1 - bufferHeight;
                pt1.Y -= offset;
                pt2.Y -= offset;
                pt3.Y -= offset;
                pt4.Y -= offset;
                s2.Y -= offset;
            }

            this.pt1 = pt1;
            this.pt2 = pt2;
            this.pt3 = pt3;
            this.ct1 = new TerminalPoint(ct1.X, ct1.X != 0 ? -1 : 0);
            this.outputText.Append(text);

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Draw(s1, pt3, pt4, text7 + text6, bufferHeight);
            }
            else
            {
                Draw(s1, s3, pt4, text6, bufferHeight);
            }
        }

        private static bool IsEnd(TerminalPoint pt, int bufferHeight)
        {
            if (pt.Y >= bufferHeight)
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    if (pt.X == 0 && Console.CursorLeft != 0)
                    {
                        return true;
                    }
                }
                else if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    if (pt.X == 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // private static void WriteEndLine(TextWriter writer, TerminalPoint pt, int bufferHeight)
        // {
        //     if (pt.Y >= bufferHeight)
        //     {
        //         if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        //         {
        //             if (pt.X == 0 && Console.CursorLeft != 0)
        //             {
        //                 writer.WriteLine();
        //             }
        //         }
        //         else if (Environment.OSVersion.Platform == PlatformID.Unix)
        //         {
        //             if (pt.X == 0)
        //             {
        //                 writer.WriteLine();
        //             }
        //         }
        //     }
        // }

        internal static object LockedObject { get; } = new object();

        #region Initializer

        class Initializer : IDisposable
        {
            private readonly Terminal terminal;

            public Initializer(Terminal terminal)
            {
                this.terminal = terminal;
            }

            public string Prompt { get; set; } = string.Empty;

            public string Command { get; set; } = string.Empty;

            public bool IsHidden { get; set; }

            public string ReadLineImpl(Func<string, bool> validation, bool recordHistory)
            {
                this.terminal.Initialize(this.Prompt, this.Command, this.IsHidden);
                return this.terminal.ReadLineImpl(validation, recordHistory);
            }

            public ConsoleKey ReadKeyImpl(params ConsoleKey[] filters)
            {
                this.terminal.Initialize(this.Prompt, this.Command, this.IsHidden);
                return this.terminal.ReadKeyImpl(filters);
            }

            public void Dispose()
            {
                this.terminal.Release();
            }
        }

        #endregion
    }
}
