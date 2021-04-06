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

using System.Collections.Generic;

namespace JSSoft.Library.Commands
{
    public static class TerminalStrings
    {
        private static readonly Dictionary<TerminalColor, int> foregroundValues = new()
        {
            { TerminalColor.Black, 30 },
            { TerminalColor.Red, 31 },
            { TerminalColor.Green, 32 },
            { TerminalColor.Yellow, 33 },
            { TerminalColor.Blue, 34 },
            { TerminalColor.Magenta, 35 },
            { TerminalColor.Cyan, 36 },
            { TerminalColor.White, 37 },
            { TerminalColor.BrightBlack, 90 },
            { TerminalColor.BrightRed, 91 },
            { TerminalColor.BrightGreen, 92 },
            { TerminalColor.BrightYellow, 93 },
            { TerminalColor.BrightBlue, 94 },
            { TerminalColor.BrightMagenta, 95 },
            { TerminalColor.BrightCyan, 96 },
            { TerminalColor.BrightWhite, 97 },
        };

        private static readonly Dictionary<TerminalColor, int> backgroundValues = new()
        {
            { TerminalColor.Black, 40 },
            { TerminalColor.Red, 41 },
            { TerminalColor.Green, 42 },
            { TerminalColor.Yellow, 43 },
            { TerminalColor.Blue, 44 },
            { TerminalColor.Magenta, 45 },
            { TerminalColor.Cyan, 46 },
            { TerminalColor.White, 47 },
            { TerminalColor.BrightBlack, 100 },
            { TerminalColor.BrightRed, 101 },
            { TerminalColor.BrightGreen, 102 },
            { TerminalColor.BrightYellow, 103 },
            { TerminalColor.BrightBlue, 104 },
            { TerminalColor.BrightMagenta, 105 },
            { TerminalColor.BrightCyan, 106 },
            { TerminalColor.BrightWhite, 107 },
        };

        public static string Foreground(string text, TerminalColor foreground)
        {
            return $"\x1b[0;{foregroundValues[foreground]}m{text}\x1b[0m";
        }

        public static string Foreground(string text, TerminalGraphic graphic, TerminalColor foreground)
        {
            return $"\x1b[{(int)graphic};{foregroundValues[foreground]}m{text}\x1b[0m";
        }

        public static string Background(string text, TerminalColor background)
        {
            return $"\x1b[0;{backgroundValues[background]}m{text}\x1b[0m";
        }

        public static string Color(string text, TerminalColor foreground, TerminalColor background)
        {
            return $"\x1b[0;{foregroundValues[foreground]};{backgroundValues[background]}m{text}\x1b[0m";
        }
    }
}
