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
        private const string escClearScreen = "\u001b[2J";
        private const string escCursorHome = "\u001b[H";
        private const string escEraseDown = "\u001b[J";
        private static byte[] charWidths;
        private const string passwordPattern = "[~`! @#$%^&*()_\\-+={[}\\]|\\\\:;\"'<,>.?/0-9a-zA-Z]";
        private static TerminalKeyBindingCollection keyBindings = TerminalKeyBindingCollection.Default;

        private readonly Dictionary<ConsoleKeyInfo, Func<string>> systemActions = new();
        private readonly List<string> histories = new();
        private readonly Queue<string> stringQueue = new Queue<string>();

        private TerminalPoint pt1 = new(0, Console.CursorTop);
        private TerminalPoint pt2;
        private TerminalPoint pt3;
        private TerminalPoint ct1;
        private StringBuilder outputText = new();
        private int width = Console.BufferWidth;
        private int height = Console.BufferHeight;
        private int historyIndex;
        private int cursorIndex;
        private TerminalString prompt = TerminalString.Empty;
        private TerminalString command = TerminalString.Empty;
        private TerminalString promptText = TerminalString.Empty;
        private string inputText = string.Empty;
        private string completion = string.Empty;

        private TerminalFlags flags;
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

            if (IsWin32NT == true)
            {
                TerminalWin32NT.Initialize();
            }
        }

        public Terminal()
        {
            if (Console.IsInputRedirected == true)
                throw new Exception("Terminal cannot use. Console.IsInputRedirected must be false");
            this.systemActions.Add(new ConsoleKeyInfo('\u0003', ConsoleKey.C, false, false, true), this.OnCancel);
            this.systemActions.Add(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false), this.OnEnter);
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

        public static int GetLength(string text)
        {
            var length = 0;
            foreach (var item in text)
            {
                length += charWidths[(int)item];
            }
            return length;
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

        public string ReadString(string prompt, bool isPassword)
        {
            return this.ReadString(prompt, string.Empty, isPassword);
        }

        public string ReadString(string prompt, string command)
        {
            return this.ReadString(prompt, command, false);
        }

        public string ReadString(string prompt, string command, bool isPassword)
        {
            using var initializer = new Initializer(this)
            {
                Prompt = prompt,
                Command = command,
                Flags = TerminalFlags.IsPassword
            };
            return initializer.ReadLineImpl(i => true);
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
                if (this.IsPassword == true)
                    throw new InvalidOperationException();
                if (this.historyIndex + 1 < this.histories.Count)
                {
                    this.SetHistoryIndex(this.historyIndex + 1);
                }
            }
        }

        public void PrevHistory()
        {
            lock (LockedObject)
            {
                if (this.IsPassword == true)
                    throw new InvalidOperationException();
                if (this.historyIndex > 0)
                {
                    this.SetHistoryIndex(this.historyIndex - 1);
                }
                else if (this.histories.Count == 1)
                {
                    this.SetHistoryIndex(0);
                }
            }
        }

        public IReadOnlyList<string> Histories => this.histories;

        public int HistoryIndex
        {
            get => this.historyIndex;
            set
            {
                if (value < 0 || value >= this.histories.Count)
                    throw new ArgumentOutOfRangeException(nameof(value));
                if (this.historyIndex == value)
                    return;
                lock (LockedObject)
                {
                    this.SetHistoryIndex(value);
                }
            }
        }

        public void Cancel()
        {
            this.isCancellationRequested = true;
        }

        public void Clear()
        {
            lock (LockedObject)
            {
                using var stream = Console.OpenStandardOutput();
                using var writer = new StreamWriter(stream, Console.OutputEncoding);
                var offset = new TerminalPoint(0, this.pt1.Y);
                var bufferWidth = this.width;
                var pre = this.command[..this.cursorIndex];
                var promptTextF = this.prompt.Format + this.command.Format;
                var st1 = NextPosition(pre, bufferWidth, this.pt2) - offset;
                this.pt1 -= offset;
                this.pt2 -= offset;
                this.pt3 -= offset;
                this.ct1 = TerminalPoint.Zero;
                writer.Write($"{escClearScreen}{escCursorHome}{promptTextF}{st1.CursorString}");
            }
        }

        public void Delete()
        {
            lock (LockedObject)
            {
                if (this.cursorIndex < this.command.Length)
                {
                    this.DeleteImpl();
                }
            }
        }

        public void MoveToFirst()
        {
            lock (LockedObject)
            {
                this.SetCursorIndex(0);
            }
        }

        public void MoveToLast()
        {
            lock (LockedObject)
            {
                this.SetCursorIndex(this.command.Length);
            }
        }

        public void Left()
        {
            lock (LockedObject)
            {
                if (this.cursorIndex > 0)
                {
                    this.SetCursorIndex(this.cursorIndex - 1);
                }
            }
        }

        public void Right()
        {
            lock (LockedObject)
            {
                if (this.cursorIndex < this.command.Length)
                {
                    this.SetCursorIndex(this.cursorIndex + 1);
                }
            }
        }

        public void Backspace()
        {
            lock (LockedObject)
            {
                if (this.cursorIndex > 0)
                {
                    this.BackspaceImpl();
                }
            }
        }

        public void NextCompletion()
        {
            lock (LockedObject)
            {
                if (this.IsPassword == true)
                    throw new InvalidOperationException();
                this.CompletionImpl(NextCompletion);
            }
        }

        public void PrevCompletion()
        {
            lock (LockedObject)
            {
                if (this.IsPassword == true)
                    throw new InvalidOperationException();
                this.CompletionImpl(PrevCompletion);
            }
        }

        public void EnqueueString(string text)
        {
            lock (Terminal.ExternalObject)
            {
                this.stringQueue.Enqueue(text);
            }
        }

        public int CursorIndex
        {
            get => this.cursorIndex;
            set
            {
                if (value < 0 || value > this.command.Length)
                    throw new ArgumentOutOfRangeException(nameof(value));
                if (this.cursorIndex == value)
                    return;
                lock (LockedObject)
                {
                    this.SetCursorIndex(value);
                }
            }
        }

        public string Command
        {
            get => this.command;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (this.IsPassword == true)
                    throw new InvalidOperationException();
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

        public bool IsEnabled { get; set; } = true;

        public bool IsReading => this.flags.HasFlag(TerminalFlags.IsReading);

        public bool IsPassword => this.flags.HasFlag(TerminalFlags.IsPassword);

        public static bool IsUnix => Environment.OSVersion.Platform == PlatformID.Unix;

        public static bool IsWin32NT => Environment.OSVersion.Platform == PlatformID.Win32NT;

        public static Terminal Current { get; private set; }

        public static char PasswordCharacter { get; set; } = '*';

        protected virtual string FormatPrompt(string prompt)
        {
            return TerminalStrings.Foreground(prompt, TerminalGraphic.Italic, TerminalColor.BrightBlue);
        }

        protected virtual string FormatCommand(string command)
        {
            return TerminalStrings.FromColor(command, TerminalColor.Red, TerminalColor.BrightGreen);
        }

        protected virtual string[] GetCompletion(string[] items, string find)
        {
            var query = from item in items
                        where item.StartsWith(find)
                        select item;
            return query.ToArray();
        }

        protected void UpdateLayout()
        {
            if (this.width != Console.BufferWidth)
            {
                var bufferWidth = Console.BufferWidth;
                var bufferHeight = Console.BufferHeight;
                var prompt = this.prompt;
                var command = this.command;
                var offset1 = this.pt3.Y - this.pt1.Y;
                var pre = this.command[..cursorIndex];
                var pt = new TerminalPoint(Console.CursorLeft, Console.CursorTop);
                var pt1 = new TerminalPoint(0, pt.Y - offset1);
                var pt2 = NextPosition(this.prompt, bufferWidth, pt1);
                var pt3 = NextPosition(this.command, bufferWidth, pt2);
                var ct1 = NextPosition(pre, bufferWidth, pt3);
                var offset2 = pt3.Y - pt1.Y;
                var nt1 = offset2 != offset1 ? new TerminalPoint(0, pt.Y - offset2) : pt1;
                var nt2 = NextPosition(prompt, bufferWidth, nt1);
                var nt3 = NextPosition(command, bufferWidth, nt2);
                var nt4 = NextPosition(pre, bufferWidth, nt3);
                var offset = nt3.Y >= bufferHeight ? new TerminalPoint(0, nt3.Y + 1 - bufferHeight) : TerminalPoint.Zero;
                var st1 = nt1 - offset;
                var st2 = nt3 - offset;
                var st3 = nt4 - offset;

                this.width = bufferWidth;
                this.height = bufferHeight;
                this.pt1 = nt1 - offset;
                this.pt2 = nt2 - offset;
                this.pt3 = nt3 - offset;
                RenderString(st1, st2, st3, bufferHeight, prompt, command);
            }
        }

        protected virtual TerminalKeyBindingCollection KeyBindings => keyBindings;

        private void InsertText(string text)
        {
            lock (LockedObject)
            {
                var bufferWidth = this.width;
                var bufferHeight = this.height;
                var cursorIndex = this.cursorIndex + text.Length;
                var extra = this.command[this.cursorIndex..];
                var command = this.command.Insert(this.cursorIndex, text, this.FormatCommand);
                var prompt = this.prompt;
                var pre = command[..(command.Length - extra.Length)];
                var pt1 = this.pt1;
                var pt2 = this.pt2;
                var pt3 = NextPosition(command, bufferWidth, pt2);
                var pt4 = NextPosition(pre, bufferWidth, pt2);
                var offset = pt3.Y >= bufferHeight ? new TerminalPoint(0, pt3.Y - this.pt3.Y) : TerminalPoint.Zero;
                var st1 = pt2;
                var st2 = pt3;
                var st3 = pt4 - offset; 

                this.cursorIndex = cursorIndex;
                this.command = command;
                this.promptText = prompt + command;
                this.inputText = command[..cursorIndex];
                this.completion = string.Empty;
                this.pt1 = pt1 - offset;
                this.pt2 = pt2 - offset;
                this.pt3 = pt3 - offset;

                RenderString(st1, st2, st3, bufferHeight, command);
            }
        }

        private static void RenderString(TerminalPoint pt1, TerminalPoint pt2, TerminalPoint ct1, int bufferHeight, params TerminalString[] items)
        {
            var capacity = items.Sum(item => item.Length) + 30;
            var sb = new StringBuilder(capacity);
            var last = items.Any() == true ? items.Last() : string.Empty;
            sb.Append(pt1.CursorString);
            sb.Append(escEraseDown);
            foreach (var item in items)
            {
                sb.Append(item.Format);
            }
            if (pt2.Y > pt1.Y && pt2.X == 0 && last.EndsWith(Environment.NewLine) == false)
                sb.Append(Environment.NewLine);
            sb.Append(escEraseDown);
            sb.Append(ct1.CursorString);
            RenderString(sb.ToString());
        }

        private static void RenderString(string text)
        {
            using var stream = Console.OpenStandardOutput();
            using var writer = new StreamWriter(stream, Console.OutputEncoding) { AutoFlush = true };
            writer.Write(text);
        }

        private static void SetCursorPosition(TerminalPoint pt)
        {
            using var stream = Console.OpenStandardOutput();
            using var writer = new StreamWriter(stream, Console.OutputEncoding);
            writer.Write(pt.CursorString);
        }

        private static TerminalPoint NextPosition(string text, int bufferWidth, TerminalPoint pt)
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

        private static string StripOff(string text)
        {
            return Regex.Replace(text, @"\e\[(\d+;)*(\d+)?[ABCDHJKfmsu]", string.Empty);
        }

        private void BackspaceImpl()
        {
            var bufferWidth = this.width;
            var bufferHeight = this.height;
            var extra = this.command[this.cursorIndex..];
            var command = this.command.Remove(this.cursorIndex - 1, 1, this.FormatCommand);
            var pre = command[..(command.Length - extra.Length)];
            var cursorIndex = this.cursorIndex - 1;
            var endPosition = this.command.Length;
            var pt2 = this.pt2;
            var pt3 = NextPosition(pre, bufferWidth, pt2);
            var pt4 = NextPosition(extra, bufferWidth, pt3);

            this.command = command;
            this.promptText = this.prompt + this.command;
            this.cursorIndex = cursorIndex;
            this.pt3 = pt4;

            RenderString(pt2, pt4, pt3, bufferHeight, command);
        }

        private void DeleteImpl()
        {
            var bufferWidth = this.width;
            var bufferHeight = this.height;
            var extra = this.command[(this.cursorIndex + 1)..];
            var command = this.command.Remove(this.cursorIndex, 1, this.FormatCommand);
            var pre = command[..(command.Length - extra.Length)];
            var endPosition = this.command.Length;
            var pt2 = this.pt2;
            var pt3 = NextPosition(pre, bufferWidth, pt2);
            var pt4 = NextPosition(extra, bufferWidth, pt3);

            this.command = command;
            this.promptText = this.prompt + this.command;
            this.pt3 = pt4;
            RenderString(pt2, pt4, pt3, bufferHeight, command);
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
                    matchText = matchText[1..];
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

        private void SetPrompt(string value)
        {
            var isPassword = this.IsPassword;
            var bufferWidth = this.width;
            var bufferHeight = this.height;
            var command = this.command;
            var prompt = new TerminalString(value, this.FormatPrompt);
            var pre = command[..this.cursorIndex];
            var pt1 = this.pt1;
            var pt2 = NextPosition(prompt, bufferWidth, pt1);
            var pt3 = NextPosition(command, bufferWidth, pt2);
            var pt4 = NextPosition(pre, bufferWidth, pt2);
            var offset = pt3.Y >= bufferHeight ? new TerminalPoint(0, pt3.Y - this.pt3.Y) : TerminalPoint.Zero;

            var st1 = pt1;
            var st2 = pt3 - offset;
            var st3 = pt4 - offset;

            this.prompt = prompt;
            this.promptText = prompt + command;
            this.pt1 = pt1 - offset;
            this.pt2 = pt2 - offset;
            this.pt3 = pt3 - offset;

            if (this.IsReading == true)
            {
                RenderString(st1, st2, st3, bufferHeight, prompt, command);
            }
        }

        private void SetCommand(string value)
        {
            var bufferWidth = this.width;
            var bufferHeight = this.height;
            var command = new TerminalString(value, this.FormatCommand) { IsPassword = this.IsPassword };
            var prompt = this.prompt;
            var pt1 = this.pt1;
            var pt2 = this.pt2;
            var pt3 = NextPosition(value, bufferWidth, pt2);
            var offset = pt3.Y >= bufferHeight ? new TerminalPoint(0, pt3.Y - this.pt3.Y) : TerminalPoint.Zero;

            var st1 = pt2;
            var st2 = pt3 - offset;
            var st3 = pt3 - offset;

            this.command = command;
            this.promptText = prompt + command;
            this.cursorIndex = command.Length;
            this.inputText = value;
            this.completion = string.Empty;
            this.pt1 = pt1 - offset;
            this.pt2 = pt2 - offset;
            this.pt3 = pt3 - offset;

            RenderString(st1, st2, st3, bufferHeight, command);
        }

        private void SetCursorIndex(int cursorIndex)
        {
            var bufferWidth = this.width;
            var bufferHeight = this.height;
            var pre = this.command[..cursorIndex];
            var pt4 = NextPosition(pre, bufferWidth, this.pt2);

            this.cursorIndex = cursorIndex;
            this.inputText = pre;
            this.completion = string.Empty;
            SetCursorPosition(pt4);
        }

        private void SetHistoryIndex(int index)
        {
            this.SetCommand(this.histories[index]);
            this.historyIndex = index;
        }

        private object ReadNumber(string prompt, object value, Func<string, bool> validation)
        {
            using var initializer = new Initializer(this)
            {
                Prompt = prompt,
                Command = $"{value}"
            };
            return initializer.ReadLineImpl(validation);
        }

        private string ReadLineImpl(Func<string, bool> validation)
        {
            while (true)
            {
                Thread.Sleep(1);
                this.Update();
                if (this.isCancellationRequested == true)
                    return null;
                if (this.IsEnabled == false)
                    continue;
                var keyChars = string.Empty;
                while (Console.KeyAvailable == true)
                {
                    var key = Console.ReadKey(true);
                    if (this.systemActions.ContainsKey(key) == true)
                    {
                        this.FlushKeyChars(validation, ref keyChars);
                        return this.systemActions[key]();
                    }
                    else if (this.KeyBindings.CanProcess(key) == true)
                    {
                        this.FlushKeyChars(validation, ref keyChars);
                        this.KeyBindings.Process(key, this);
                    }
                    else if (this.PreviewKeyChar(key.KeyChar) == true)
                    {
                        keyChars += key.KeyChar;
                    }
                }
                this.FlushKeyChars(validation, ref keyChars);
            }
        }

        private void FlushKeyChars(Func<string, bool> validation, ref string keyChars)
        {
            if (keyChars != string.Empty && validation(this.Command + keyChars) == true)
            {
                this.InsertText(keyChars);
            }
            keyChars = string.Empty;
        }

        private void RenderStringQueue()
        {
            var text = string.Empty;
            lock (ExternalObject)
            {
                if (this.stringQueue.Any() == true)
                {
                    var sb = new StringBuilder();
                    while (this.stringQueue.TryDequeue(out var item))
                    {
                        sb.Append(item);
                    }
                    text = sb.ToString();
                }
            }
            if (text != string.Empty)
                RenderOutput(text);
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

        private bool PreviewKeyChar(char ch)
        {
            if (ch != '\0')
            {
                if (this.IsPassword == true)
                    return Regex.IsMatch($"{ch}", passwordPattern);
                return true;
            }
            return false;
        }

        private void Initialize(string prompt, string command, TerminalFlags flags)
        {
            var bufferWidth = Console.BufferWidth;
            var bufferHeight = Console.BufferHeight;
            var pt1 = new TerminalPoint(0, Console.CursorTop);
            lock (LockedObject)
            {
                var isPassword = flags.HasFlag(TerminalFlags.IsPassword);
                var promptS = new TerminalString(prompt, this.FormatPrompt);
                var commandS = new TerminalString(command, this.FormatCommand) { IsPassword = isPassword };
                var pt2 = NextPosition(prompt, bufferWidth, pt1);
                var pt3 = NextPosition(command, bufferWidth, pt2);
                var offset = pt3.Y >= bufferHeight ? new TerminalPoint(0, pt2.Y - pt1.Y) : TerminalPoint.Zero;
                var st1 = pt1;
                var st2 = pt3 - offset;
                var st3 = pt3 - offset;

                this.width = bufferWidth;
                this.height = bufferHeight;
                this.prompt = promptS;
                this.command = commandS;
                this.promptText = promptS + commandS;
                this.cursorIndex = 0;
                this.inputText = command;
                this.completion = string.Empty;
                this.pt1 = pt1 - offset;
                this.pt2 = pt2 - offset;
                this.pt3 = pt3 - offset;
                this.ct1 = TerminalPoint.Zero;
                this.flags = flags | TerminalFlags.IsReading;

                RenderString(st1, st2, st3, bufferHeight, promptS, commandS);
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
                this.pt1 = new TerminalPoint(0, Console.CursorTop);
                this.pt2 = this.pt1;
                this.pt3 = this.pt1;
                this.prompt = TerminalString.Empty;
                this.command = TerminalString.Empty;
                this.promptText = TerminalString.Empty;
                this.cursorIndex = 0;
                this.inputText = string.Empty;
                this.completion = string.Empty;
                this.flags = TerminalFlags.None;
            }
        }

        private string OnEnter()
        {
            if (this.CanRecord == true)
            {
                this.RecordCommand(this.command);
            }
            return this.command.OriginText;
        }

        private string OnCancel()
        {
            return null;
        }

        private void RecordCommand(string command)
        {
            if (this.histories.Contains(command) == false)
            {
                this.histories.Add(command);
                this.historyIndex = this.histories.Count;
            }
            else
            {
                this.historyIndex = this.histories.LastIndexOf(command) + 1;
            }
        }

        private void RenderOutput(string textF)
        {
            var bufferWidth = this.width;
            var bufferHeight = this.height;
            var promptText = this.promptText;
            var prompt = this.prompt;
            var command = this.command;
            var text = StripOff(textF);
            var pre = this.command[..this.cursorIndex];
            var pt8 = this.pt1 + this.ct1;
            var ct1 = NextPosition(text, bufferWidth, pt8);
            var text1F = text.EndsWith(Environment.NewLine) == true || ct1 == TerminalPoint.Zero ? textF : textF + Environment.NewLine;
            var text1 = StripOff(text1F);
            var pt9 = NextPosition(text1, bufferWidth, pt8);
            var pt1 = pt9.X == 0 ? new TerminalPoint(pt9.X, pt9.Y) : new TerminalPoint(0, pt9.Y + 1);
            var pt2 = NextPosition(prompt, bufferWidth, pt1);
            var pt3 = NextPosition(command, bufferWidth, pt2);
            var pt4 = NextPosition(pre, bufferWidth, pt2);

            var offset = pt3.Y >= bufferHeight ? new TerminalPoint(0, pt3.Y + 1 - bufferHeight) : TerminalPoint.Zero;
            var st1 = new TerminalPoint(pt8.X, pt8.Y);
            var st2 = pt3 - offset;
            var st3 = pt4 - offset;

            this.pt1 = pt1 - offset;
            this.pt2 = pt2 - offset;
            this.pt3 = pt3 - offset;
            this.ct1 = new TerminalPoint(ct1.X, ct1.X != 0 ? -1 : 0);
            this.outputText.Append(text);

            RenderString(st1, st2, st3, bufferHeight, new TerminalString(text1, text1F), promptText);
        }

        private bool IsRecordable => this.flags.HasFlag(TerminalFlags.IsRecordable);

        private bool CanRecord => this.IsRecordable == true && this.IsPassword == false && this.command != string.Empty;

        internal string ReadStringInternal(string prompt)
        {
            using var initializer = new Initializer(this)
            {
                Prompt = prompt,
                Flags = TerminalFlags.IsRecordable
            };
            return initializer.ReadLineImpl(i => true);
        }

        internal void Update()
        {
            this.UpdateLayout();
            this.RenderStringQueue();
        }

        internal static object LockedObject { get; } = new object();

        internal static object ExternalObject { get; } = new object();

        #region Initializer

        class Initializer : IDisposable
        {
            private readonly Terminal terminal;
            private bool isControlC = Console.TreatControlCAsInput;

            public Initializer(Terminal terminal)
            {
                this.terminal = terminal;
                this.isControlC = Console.TreatControlCAsInput;
                Console.TreatControlCAsInput = true;
            }

            public string Prompt { get; set; } = string.Empty;

            public string Command { get; set; } = string.Empty;

            public TerminalFlags Flags { get; set; }

            public string ReadLineImpl(Func<string, bool> validation)
            {
                this.terminal.Initialize(this.Prompt, this.Command, this.Flags);
                return this.terminal.ReadLineImpl(validation);
            }

            public ConsoleKey ReadKeyImpl(params ConsoleKey[] filters)
            {
                this.terminal.Initialize(this.Prompt, this.Command, this.Flags);
                return this.terminal.ReadKeyImpl(filters);
            }

            public void Dispose()
            {
                this.terminal.Release();
                Console.TreatControlCAsInput = this.isControlC;
            }
        }

        #endregion
    }
}
