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
    public struct FilePosition : ICloneable, IComparable
    {
        /// <summary>
        /// Integer constructor
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public FilePosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="p"></param>
        public FilePosition(FilePosition p)
        {
            X = p.X;
            Y = p.Y;
        }

        /// <summary>
        /// ScreenPosition constructor
        /// </summary>
        /// <param name="p"></param>
        public FilePosition(ScreenPosition p)
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

        /// <summary>
        /// More than equals
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator >=(FilePosition a, FilePosition b)
        {
            return ((a.Y > b.Y) || (a.Y == b.Y && a.X >= b.X));
        }

        /// <summary>
        /// Less than equals
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator <=(FilePosition a, FilePosition b)
        {
            return ((a.Y < b.Y) || (a.Y == b.Y && a.X <= b.X));
        }

        /// <summary>
        /// Equality operator
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(FilePosition a, FilePosition b)
        {
            return ((a.X == b.X) && (a.Y == b.Y));
        }

        /// <summary>
        /// Inequality operator
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
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
        /// Implement for IComparable
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        int IComparable.CompareTo(object x)
        {
            FilePosition h1 = (FilePosition)x;

            if (h1.Y <= this.Y && h1.X < this.X)
            {
                return -1;
            }
            else if (h1.Y == this.Y && h1.X == this.X)
            {
                return 0;
            }
            else // Greater than
            {
                return 1;
            }
        }

        /// <summary>
        /// Return as a ScreenPosition
        /// </summary>
        /// <returns></returns>
        public ScreenPosition asScreenPosition()
        {
            return new ScreenPosition(this);
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

        /// <summary>
        /// Greater than
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Definition of a zero FilePosition for static use
        /// </summary>
        /// <returns></returns>
        public static FilePosition Zero()
        {
            return m_zero;
        }

        /// <summary>
        /// Global one-off definition
        /// </summary>
        static public FilePosition m_zero = new FilePosition(0, 0);

        // Our values
        //
        public int X;
        public int Y;
    }

}
