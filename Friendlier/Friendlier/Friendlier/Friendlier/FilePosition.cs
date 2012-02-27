using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Xyglo
{
    /// <summary>
    /// Define a line and character position in a file
    /// </summary>
    public struct FilePosition : ICloneable
    {
        public FilePosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public FilePosition(FilePosition p)
        {
            X = p.X;
            Y = p.Y;
        }

        public object Clone()
        {
            FilePosition copy = new FilePosition(this.X, this.Y);
            return copy;
        }

        public FilePosition(Vector2 vector)
        {
            X = Convert.ToInt16(vector.X);
            Y = Convert.ToInt16(vector.Y);
        }

        public static bool operator ==(FilePosition a, FilePosition b)
        {
            return ((a.X == b.X) && (a.Y == b.Y));
        }

        public static bool operator !=(FilePosition a, FilePosition b)
        {
            return ((a.X != b.X) || (a.Y != b.Y));
        }

        public static FilePosition operator +(FilePosition a, FilePosition b)
        {
            FilePosition result = a;
            a.X += b.X;
            a.Y += b.Y;
            return result;
        }

        /// <summary>
        /// Less than operator
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator <(FilePosition a, FilePosition b)
        {
            return (a.Y < b.Y || (a.Y == b.Y && a.X < b.X));
        }

        public static bool operator >(FilePosition a, FilePosition b)
        {
            return (a.Y > b.Y || (a.Y == b.Y && a.X > b.X));
        }

        // Needed to avoid warning
        //
        public override int GetHashCode()
        {
            return X ^ Y;
        }

        // Return a y offset FilePosition
        //
        public FilePosition yOffset(int offsetY)
        {
            return new FilePosition(X, Y + offsetY);
        }

        // Needed to avoid warning
        //
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public int X;
        public int Y;
    }

}
