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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Library.Commands
{
    public class Terminal
    {
        private const string escClearScreen = "\u001b[2J";
        private const string escCursorHome = "\u001b[H";
        private const string escEraseDown = "\u001b[J";
        private const string escCursorInvisible = "\u001b[?25l";
        private const string escCursorVisible = "\u001b[?25h";
        private const string passwordPattern = "[~`! @#$%^&*()_\\-+={[}\\]|\\\\:;\"'<,>.?/0-9a-zA-Z]";
        private static readonly byte[] charWidths;
        private static readonly TerminalKeyBindingCollection keyBindings = TerminalKeyBindingCollection.Default;
        private static readonly TerminalString cursorVisible = new(escCursorVisible);

        private readonly List<string> histories = new();
        private readonly Queue<string> stringQueue = new();
        private readonly ManualResetEvent eventSet = new(false);
        private readonly StringBuilder outputText = new();

        private TerminalPoint pt1 = new(0, Console.IsOutputRedirected == true ? 0 : Console.CursorTop);
        private TerminalPoint pt2;
        private TerminalPoint pt3;
        private TerminalPoint pt4;
        private TerminalPoint ot1;
        private int width = Console.IsOutputRedirected == true ? int.MaxValue : Console.BufferWidth;
        private int height = Console.IsOutputRedirected == true ? int.MaxValue : Console.BufferHeight;
        private int historyIndex;
        private int cursorIndex;
        private TerminalPrompt prompt = TerminalPrompt.Empty;
        private TerminalCommand command = TerminalCommand.Empty;
        private string promptText = string.Empty;
        private string inputText = string.Empty;
        private string completion = string.Empty;
        private SecureString secureString;

        private TerminalFlags flags;
        private Func<string, bool> validator;

        static Terminal()
        {
            //System.Diagnostics.Debugger.Launch();
            var platformName = GetPlatformName(Environment.OSVersion.Platform);
            var name = $"{typeof(Terminal).Namespace}.{platformName}.dat";
            using var stream = typeof(Terminal).Assembly.GetManifestResourceStream(name);
            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            charWidths = buffer;

            static string GetPlatformName(PlatformID platformID)
            {
                return platformID switch
                {
                    PlatformID.Unix => $"{PlatformID.Unix}",
                    PlatformID.Win32NT => $"{PlatformID.Win32NT}",
                    _ => $"{PlatformID.Win32NT}",
                };
            }

            if (IsWin32NT == true && Console.IsOutputRedirected == false)
            {
                TerminalWin32NT.Initialize();
            }
        }

        public Terminal()
        {
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

        public string ReadString(string prompt, string command)
        {
            using var initializer = new Initializer(this)
            {
                Prompt = prompt,
                Command = command
            };
            return initializer.ReadLineImpl(i => true) as string;
        }

        public SecureString ReadSecureString(string prompt)
        {
            using var initializer = new Initializer(this)
            {
                Prompt = prompt,
                Command = command,
                Flags = TerminalFlags.IsPassword
            };
            return initializer.ReadLineImpl(i => true) as SecureString;
        }

        public ConsoleKey ReadKey(string prompt, params ConsoleKey[] filters)
        {
            using var initializer = new Initializer(this)
            {
                Prompt = prompt,
            };
            return initializer.ReadKeyImpl(filters);
        }

        public void InsertNewLine()
        {
            this.MoveToLast();
            this.InsertText(Environment.NewLine);
        }

        public void EndInput()
        {
            this.flags |= TerminalFlags.IsInputEnded;
        }

        public void CancelInput()
        {
            this.flags |= TerminalFlags.IsInputCancelled;
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

        public void Clear()
        {
            lock (LockedObject)
            {
                using var stream = Console.OpenStandardOutput();
                using var writer = new StreamWriter(stream, Console.OutputEncoding);
                var offset = new TerminalPoint(0, this.pt1.Y);
                var bufferWidth = this.width;
                var pre = this.command.Slice(0, this.cursorIndex);
                var promptTextF = this.prompt.FormattedText + this.command.FormattedText;
                var st1 = pre.Next(this.pt2, bufferWidth) - offset;
                this.pt1 -= offset;
                this.pt2 -= offset;
                this.pt3 -= offset;
                this.ot1 = TerminalPoint.Zero;
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
            lock (ExternalObject)
            {
                this.stringQueue.Enqueue(text);
            }
            this.RenderStringQueue();
        }

        public async Task EnqueueStringAsync(string text)
        {
            lock (ExternalObject)
            {
                this.eventSet.Reset();
                this.stringQueue.Enqueue(text);
            }
            await Task.Run(this.eventSet.WaitOne);
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
            get => this.command.Text;
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

        public bool IsReading => this.flags.HasFlag(TerminalFlags.IsReading);

        public bool IsPassword => this.flags.HasFlag(TerminalFlags.IsPassword);

        public bool IsEnabled { get; set; } = true;

        public static bool IsUnix => Environment.OSVersion.Platform == PlatformID.Unix;

        public static bool IsWin32NT => Environment.OSVersion.Platform == PlatformID.Win32NT;

        public static Terminal Current { get; private set; }

        public static char PasswordCharacter { get; set; } = '*';

        protected virtual string FormatPrompt(string prompt)
        {
            return prompt;
        }

        protected virtual string FormatCommand(string command)
        {
            return command;
        }

        protected virtual string[] GetCompletion(string[] items, string find)
        {
            var query = from item in items
                        where item.StartsWith(find)
                        select item;
            return query.ToArray();
        }

        protected void UpdateLayout(int bufferWidth, int bufferHeight)
        {
            var prompt = this.prompt;
            var command = this.command;
            var offsetY = this.pt4.Y - this.pt1.Y;
            var lt3 = this.pt3;
            var pre = command.Slice(0, cursorIndex);
            var cursor = new TerminalPoint(Console.IsOutputRedirected == true ? 0 : Console.CursorLeft, Console.IsOutputRedirected == true ? 0 : Console.CursorTop);
            var pt1 = new TerminalPoint(0, cursor.Y - offsetY);
            var nt1 = PrevPosition(prompt + pre, bufferWidth, cursor);
            if (nt1.X == 0)
            {
                var pt2 = NextPosition(prompt, bufferWidth, nt1);
                var pt3 = command.Next(pt2, bufferWidth);
                var pt4 = pre.Next(pt2, bufferWidth);
                this.width = bufferWidth;
                this.height = bufferHeight;
                this.pt1 = nt1;
                this.pt2 = pt2;
                this.pt3 = pt3;
                this.pt4 = pt4;
            }
            else
            {
                var pt2 = prompt.Next(pt1, bufferWidth);
                var pt3 = command.Next(pt2, bufferWidth);
                var pt4 = pre.Next(pt2, bufferWidth);
                var offset1 = pt3.Y >= bufferHeight ? new TerminalPoint(0, pt3.Y - lt3.Y) : TerminalPoint.Zero;
                var st1 = pt1;
                var st2 = pt3;
                var st3 = pt4 - offset1;

                this.width = bufferWidth;
                this.height = bufferHeight;
                this.pt1 = pt1 - offset1;
                this.pt2 = pt2 - offset1;
                this.pt3 = pt3 - offset1;
                this.pt4 = st3;

                RenderString(st1, st2, st3, prompt, command);
            }
        }

        protected virtual TerminalKeyBindingCollection KeyBindings => keyBindings;

        private void InsertText(string text)
        {
            lock (LockedObject)
            {
                var displayText = this.IsPassword == true ? ConvertToPassword(text) : text;
                var oldCursorIndex = this.cursorIndex;
                var bufferWidth = this.width;
                var bufferHeight = this.height;
                var newCursorIndex = oldCursorIndex + displayText.Length;
                var extra = this.command.Slice(oldCursorIndex);
                var command = this.command.Insert(oldCursorIndex, displayText);
                var prompt = this.prompt;
                var pt1 = this.pt1;
                var pt2 = this.pt2;
                var lt3 = this.pt3;

                var pre = command.Slice(0, command.Length - extra.Length);
                var pt3 = command.Next(pt2, bufferWidth);
                var pt4 = pre.Next(pt2, bufferWidth);
                var offset = pt3.Y >= bufferHeight ? new TerminalPoint(0, pt3.Y - lt3.Y) : TerminalPoint.Zero;
                var st1 = pt2;
                var st2 = pt3;
                var st3 = pt4 - offset;

                this.cursorIndex = newCursorIndex;
                this.command = command;
                this.promptText = prompt.FormattedText + command.FormattedText;
                this.inputText = pre.Text;
                this.completion = string.Empty;
                this.pt1 = pt1 - offset;
                this.pt2 = pt2 - offset;
                this.pt3 = pt3 - offset;
                this.pt4 = st3;
                this.secureString?.InsertAt(oldCursorIndex, text);

                RenderString(st1, st2, st3, command);
            }
        }

        private static void RenderString(TerminalPoint pt1, TerminalPoint pt2, TerminalPoint ct1, params ITerminalString[] items)
        {
            var capacity = items.Sum(item => item.Text.Length) + 30;
            var sb = new StringBuilder(capacity);
            var last = items.Any() == true ? items.Last().Text : string.Empty;
            sb.Append(pt1.CursorString);
            sb.Append(escEraseDown);
            foreach (var item in items)
            {
                sb.Append(item.Text);
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

        private static TerminalPoint PrevPosition(string text, int bufferWidth, TerminalPoint pt)
        {
            var x = pt.X;
            var y = pt.Y;
            for (var i = text.Length - 1; i >= 0; i--)
            {
                var ch = text[i];
                if (ch == '\r')
                {
                    x = bufferWidth;
                    continue;
                }
                else if (ch == '\n')
                {
                    x = bufferWidth;
                    y--;
                    continue;
                }

                var w = charWidths[(int)ch];
                if (x - w < 0)
                {
                    x = bufferWidth - w;
                    y--;
                }
                else
                {
                    x -= w;
                }
            }
            return new TerminalPoint(x, y);
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

        private static string StripOff(string text)
        {
            return Regex.Replace(text, @"\e\[(\d+;)*(\d+)?[ABCDHJKfmsu]", string.Empty);
        }

        private void BackspaceImpl()
        {
            var bufferWidth = this.width;
            var prompt = this.prompt;
            var extra = this.command.Slice(this.cursorIndex);
            var command = this.command.Remove(this.cursorIndex - 1, 1);
            var cursorIndex = this.cursorIndex - 1;
            var endPosition = this.command.Length;
            var pt2 = this.pt2;

            var pre = command.Slice(0, command.Length - extra.Length);
            var pt3 = pre.Next(pt2, bufferWidth);
            var pt4 = extra.Next(pt3, bufferWidth);

            this.command = command;
            this.promptText = prompt.FormattedText + command.FormattedText;
            this.cursorIndex = cursorIndex;
            this.inputText = pre;
            this.pt3 = pt4;
            this.pt4 = pt3;
            this.secureString?.RemoveAt(cursorIndex);

            RenderString(pt2, pt4, pt3, command);
        }

        private void DeleteImpl()
        {
            var bufferWidth = this.width;
            var prompt = this.prompt;
            var extra = this.command.Slice(this.cursorIndex + 1);
            var command = this.command.Remove(this.cursorIndex, 1);
            var endPosition = this.command.Length;
            var pt2 = this.pt2;

            var pre = command.Slice(0, command.Length - extra.Length);
            var pt3 = pre.Next(pt2, bufferWidth);
            var pt4 = extra.Next(pt3, bufferWidth);

            this.command = command;
            this.promptText = prompt.FormattedText + command.FormattedText;
            this.inputText = pre;
            this.pt3 = pt4;
            this.pt4 = pt3;
            this.secureString?.RemoveAt(this.cursorIndex);

            RenderString(pt2, pt4, pt3, command);
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
                var matchText = matches[i].Value.Trim();
                if (matchText != string.Empty)
                    argList.Add(matchText);
            }

            var completions = this.GetCompletion(argList.ToArray(), find);
            if (completions != null && completions.Any())
            {
                var completion = func(completions, this.completion);
                var inputText = this.inputText;
                var command = leftText + completion;
                if (prefix == true || postfix == true)
                {
                    command = leftText + "\"" + completion + "\"";
                }
                this.SetCommand(command);
                this.completion = completion;
                this.inputText = inputText;
            }
        }

        private void SetPrompt(string value)
        {
            var bufferWidth = this.width;
            var bufferHeight = this.height;
            var command = this.command;
            var prompt = new TerminalPrompt(value, this.FormatPrompt);
            var pt1 = this.pt1;
            var lt3 = this.pt3;
            var pre = command.Slice(0, this.cursorIndex);

            var pt2 = prompt.Next(pt1, bufferWidth);
            var pt3 = command.Next(pt2, bufferWidth);
            var pt4 = pre.Next(pt2, bufferWidth);
            var offset = pt3.Y >= bufferHeight ? new TerminalPoint(0, pt3.Y - lt3.Y) : TerminalPoint.Zero;
            var st1 = pt1;
            var st2 = pt3 - offset;
            var st3 = pt4 - offset;

            this.prompt = prompt;
            this.promptText = prompt.FormattedText + command.FormattedText;
            this.pt1 = pt1 - offset;
            this.pt2 = pt2 - offset;
            this.pt3 = pt3 - offset;
            this.pt4 = st3;

            if (this.IsReading == true)
            {
                RenderString(st1, st2, st3, prompt, command);
            }
        }

        private void SetCommand(string value)
        {
            var bufferHeight = this.height;
            var prompt = this.prompt;
            var pt1 = this.pt1;
            var pt2 = this.pt2;
            var lt3 = this.pt3;

            var pt3 = pt2;
            var offset = pt3.Y >= bufferHeight ? new TerminalPoint(0, pt3.Y - lt3.Y) : TerminalPoint.Zero;
            var st2 = pt3 - offset;
            var st3 = pt3 - offset;

            this.command = TerminalCommand.Empty;
            this.promptText = prompt.FormattedText;
            this.cursorIndex = command.Length;
            this.inputText = value;
            this.completion = string.Empty;
            this.pt1 = pt1 - offset;
            this.pt2 = pt2 - offset;
            this.pt3 = pt3 - offset;
            this.pt4 = st3;

            this.FlushKeyChars(ref value);
        }

        private void SetCursorIndex(int cursorIndex)
        {
            var bufferWidth = this.width;
            var pre = this.command.Slice(0, cursorIndex);
            var pt4 = pre.Next(this.pt2, bufferWidth);

            this.pt4 = pt4;
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

        private object ReadLineImpl(CancellationToken cancellation)
        {
            while (this.IsEnabled == true && cancellation.IsCancellationRequested == false)
            {
                var text = string.Empty;
                this.Update();
                if (Console.IsInputRedirected == false)
                {
                    while (Console.KeyAvailable == true)
                    {
                        var key = Console.ReadKey(true);
                        var ch = key.KeyChar;
                        if (text != string.Empty)
                            this.FlushKeyChars(ref text);
                        if (this.KeyBindings.CanProcess(key) == true)
                        {
                            this.KeyBindings.Process(key, this);
                        }
                        else if (this.PreviewKeyChar(key.KeyChar) == true && this.PreviewCommand(text + key.KeyChar) == true)
                        {
                            text += key.KeyChar;
                        }
                        if (this.IsInputEnded == true)
                            return this.OnInputEnd();
                        else if (this.IsInputCancelled == true)
                            return this.OnInputCancel();
                    }
                    if (text != string.Empty)
                        this.FlushKeyChars(ref text);
                    Thread.Sleep(1);
                }
                else
                {
                    if (Console.In.Peek() != -1)
                        return Console.ReadLine();
                }
            }
            return null;
        }

        private void FlushKeyChars(ref string keyChars)
        {
            this.InsertText(keyChars);
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
                    while (this.stringQueue.Any() == true)
                    {
                        var item = this.stringQueue.Dequeue();
                        sb.Append(item);
                    }
                    text = sb.ToString();
                }
            }
            if (text != string.Empty)
                RenderOutput(text);
            this.eventSet.Set();
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

        private bool PreviewCommand(string command)
        {
            if (this.validator != null)
                return this.validator.Invoke(command);
            return true;
        }

        private void Initialize(string prompt, TerminalFlags flags, Func<string, bool> validator)
        {
            var bufferWidth = Console.IsOutputRedirected == true ? int.MaxValue : Console.BufferWidth;
            var bufferHeight = Console.IsOutputRedirected == true ? int.MaxValue : Console.BufferHeight;
            var pt1 = new TerminalPoint(0, Console.IsOutputRedirected == true ? this.pt1.Y : Console.CursorTop);
            lock (LockedObject)
            {
                var isPassword = flags.HasFlag(TerminalFlags.IsPassword);
                var promptS = new TerminalPrompt(prompt, this.FormatPrompt);
                var pt2 = promptS.Next(pt1, bufferWidth);
                var pt3 = pt2;
                var offset = pt3.Y >= bufferHeight ? new TerminalPoint(0, pt3.Y - pt1.Y) : TerminalPoint.Zero;
                var st1 = pt1;
                var st2 = pt3 - offset;
                var st3 = pt3 - offset;

                this.width = bufferWidth;
                this.height = bufferHeight;
                this.prompt = promptS;
                this.command = new TerminalCommand(string.Empty, this.FormatCommand);
                this.promptText = promptS.FormattedText;
                this.cursorIndex = 0;
                this.inputText = command;
                this.completion = string.Empty;
                this.pt1 = pt1 - offset;
                this.pt2 = pt2 - offset;
                this.pt3 = pt3 - offset;
                this.pt4 = pt3 - offset;
                this.ot1 = TerminalPoint.Zero;
                this.flags = flags | TerminalFlags.IsReading;
                this.validator = validator;
                this.secureString = isPassword == true ? new SecureString() : null;
                RenderString(st1, st2, st3, cursorVisible, promptS);
            }
        }

        private void Release()
        {
            lock (LockedObject)
            {
                using (var stream = Console.OpenStandardOutput())
                using (var writer = new StreamWriter(stream, Console.OutputEncoding))
                {
                    writer.WriteLine(escCursorInvisible);
                }
                this.outputText.AppendLine(this.promptText);
                this.pt1 = new TerminalPoint(0, Console.IsOutputRedirected == true ? 0 : Console.CursorTop);
                this.pt2 = this.pt1;
                this.pt3 = this.pt1;
                this.pt4 = this.pt1;
                this.prompt = TerminalPrompt.Empty;
                this.command = TerminalCommand.Empty;
                this.promptText = string.Empty;
                this.cursorIndex = 0;
                this.inputText = string.Empty;
                this.completion = string.Empty;
                this.flags = TerminalFlags.None;
                this.secureString = null;
            }
        }

        private object OnInputEnd()
        {
            if (this.CanRecord == true)
                this.RecordCommand(this.command.Text);
            if (this.IsPassword == true)
                return this.secureString;
            var items = CommandStringUtility.Split(this.command.Text);
            return CommandStringUtility.Join(items);
        }

        private object OnInputCancel()
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
            var pre = this.command.Slice(0, this.cursorIndex);
            var pt8 = this.pt1 + this.ot1;

            var text = StripOff(textF);
            var ct1 = NextPosition(text, bufferWidth, pt8);
            var text1F = text.EndsWith(Environment.NewLine) == true || ct1 == TerminalPoint.Zero ? textF : textF + Environment.NewLine;
            var text1 = StripOff(text1F);
            var pt9 = NextPosition(text1, bufferWidth, pt8);
            var pt1 = pt9.X == 0 ? new TerminalPoint(pt9.X, pt9.Y) : new TerminalPoint(0, pt9.Y + 1);
            var pt2 = prompt.Next(pt1, bufferWidth);
            var pt3 = command.Next(pt2, bufferWidth);
            var pt4 = pre.Next(pt2, bufferWidth);

            var offset = pt3.Y >= bufferHeight ? new TerminalPoint(0, pt3.Y + 1 - bufferHeight) : TerminalPoint.Zero;
            var st1 = new TerminalPoint(pt8.X, pt8.Y);
            var st2 = pt3 - offset;
            var st3 = pt4 - offset;

            this.pt1 = pt1 - offset;
            this.pt2 = pt2 - offset;
            this.pt3 = pt3 - offset;
            this.pt4 = pt4 - offset;
            this.ot1 = new TerminalPoint(ct1.X, ct1.X != 0 ? -1 : 0);
            this.outputText.Append(text);

            RenderString(st1, st2, st3, new TerminalString(text1F + promptText));
        }

        private static string ConvertToPassword(string text)
        {
            return string.Empty.PadRight(text.Length, Terminal.PasswordCharacter);
        }

        private bool IsRecordable => this.flags.HasFlag(TerminalFlags.IsRecordable);

        private bool CanRecord => this.IsRecordable == true && this.IsPassword == false && this.command != string.Empty;

        private bool IsInputCancelled => this.flags.HasFlag(TerminalFlags.IsInputCancelled);

        private bool IsInputEnded => this.flags.HasFlag(TerminalFlags.IsInputEnded);

        internal string ReadStringInternal(string prompt, CancellationToken cancellation)
        {
            using var initializer = new Initializer(this)
            {
                Prompt = prompt,
                Flags = TerminalFlags.IsRecordable,
            };
            return initializer.ReadLineImpl(i => true, cancellation) as string;
        }

        internal void Update()
        {
            if (Console.IsOutputRedirected == false && this.width != Console.BufferWidth)
                this.UpdateLayout(Console.BufferWidth, Console.BufferHeight);
            this.RenderStringQueue();
        }

        internal static object LockedObject { get; } = new object();

        internal static object ExternalObject { get; } = new object();

        #region Initializer

        class Initializer : IDisposable
        {
            private readonly Terminal terminal;
            private readonly bool isControlC = Console.IsInputRedirected != true && Console.TreatControlCAsInput;

            public Initializer(Terminal terminal)
            {
                this.terminal = terminal;
                if (Console.IsInputRedirected == false)
                {
                    this.isControlC = Console.TreatControlCAsInput;
                    Console.TreatControlCAsInput = true;
                }
            }

            public string Prompt { get; set; } = string.Empty;

            public string Command { get; set; } = string.Empty;

            public TerminalFlags Flags { get; set; }

            public Func<string, bool> Validator { get; set; }

            public object ReadLineImpl(Func<string, bool> validation)
            {
                return this.ReadLineImpl(validation, CancellationToken.None);
            }

            public object ReadLineImpl(Func<string, bool> validation, CancellationToken cancellation)
            {
                this.terminal.Initialize(this.Prompt, this.Flags, this.Validator);
                this.terminal.SetCommand(this.Command);
                return this.terminal.ReadLineImpl(cancellation);
            }

            public ConsoleKey ReadKeyImpl(params ConsoleKey[] filters)
            {
                this.terminal.Initialize(this.Prompt, this.Flags, this.Validator);
                this.terminal.SetCommand(this.Command);
                return this.terminal.ReadKeyImpl(filters);
            }

            public void Dispose()
            {
                this.terminal.Release();
                if (Console.IsInputRedirected == false)
                {
                    Console.TreatControlCAsInput = this.isControlC;
                }
            }
        }

        #endregion
    }
}
