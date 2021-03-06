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

namespace JSSoft.Library.Commands
{
    public struct TerminalPoint : IEquatable<TerminalPoint>, IComparable
    {
        public TerminalPoint(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public override bool Equals(object obj)
        {
            if (obj is TerminalPoint point)
            {
                return this.X == point.X && this.Y == point.Y;
            }
            return base.Equals(obj);
        }

        public int DistanceOf(TerminalPoint point, int bufferWidth)
        {
            if (this == point)
                return 1;
            var (s1, s2, op) = this < point ? (this, point, 1) : (point, this, -1);
            var x = s1.X;
            var y = s1.Y;
            var c = 0;
            for (; y <= s2.Y; y++)
            {
                var count = y == s2.Y ? s2.X : bufferWidth;
                if (count >= bufferWidth)
                {
                    count = bufferWidth - 1;
                }
                for (; x <= count; x++)
                {
                    c++;
                }
                x = 0;
            }
            return c * op;
        }

        public IEnumerable<TerminalPoint> EnumerateTo(TerminalPoint point, int bufferWidth)
        {
            var (s1, s2) = this < point ? (this, point) : (point, this);
            var x = s1.X;
            for (var y = s1.Y; y <= s2.Y; y++)
            {
                var count = y == s2.Y ? s2.X : bufferWidth;
                for (; x < count; x++)
                {
                    yield return new TerminalPoint(x, y);
                }
                x = 0;
            }
        }

        public override int GetHashCode()
        {
            return this.X ^ this.Y;
        }

        public override string ToString()
        {
            return $"{this.X}, {this.Y}";
        }

        public int X { get; set; }

        public int Y { get; set; }

        public static bool operator >(TerminalPoint pt1, TerminalPoint pt2)
        {
            return pt1.Y > pt2.Y || (pt1.Y == pt2.Y && pt1.X > pt2.X);
        }

        public static bool operator >=(TerminalPoint pt1, TerminalPoint pt2)
        {
            return pt1 > pt2 || pt1 == pt2;
        }

        public static bool operator <(TerminalPoint pt1, TerminalPoint pt2)
        {
            return pt1.Y < pt2.Y || (pt1.Y == pt2.Y && pt1.X < pt2.X);
        }

        public static bool operator <=(TerminalPoint pt1, TerminalPoint pt2)
        {
            return pt1 < pt2 || pt1 == pt2;
        }

        public static bool operator ==(TerminalPoint pt1, TerminalPoint pt2)
        {
            return pt1.Y == pt2.Y && pt1.X == pt2.X;
        }

        public static bool operator !=(TerminalPoint pt1, TerminalPoint pt2)
        {
            return pt1.Y != pt2.Y || pt1.X != pt2.X;
        }

        public static TerminalPoint operator +(TerminalPoint pt1, TerminalPoint pt2)
        {
            return new TerminalPoint(pt1.X + pt2.X, pt1.Y + pt2.Y);
        }

        public static TerminalPoint operator -(TerminalPoint pt1, TerminalPoint pt2)
        {
            return new TerminalPoint(pt1.X - pt2.X, pt1.Y - pt2.Y);
        }

        public static readonly TerminalPoint Zero = new(0, 0);

        public static readonly TerminalPoint Invalid = new(-1, -1);

        internal string CursorString => $"\u001b[{this.Y + 1};{this.X + 1}f";

        #region implementations

        bool IEquatable<TerminalPoint>.Equals(TerminalPoint other)
        {
            return this.X == other.X && this.Y == other.Y;
        }

        int IComparable.CompareTo(object obj)
        {
            if (obj == null)
                return 1;
            if (obj is TerminalPoint point)
            {
                if (this < point)
                    return -1;
                else if (this > point)
                    return 1;
                return 0;
            }
            throw new ArgumentException("invalid object", nameof(obj));
        }

        #endregion
    }
}
