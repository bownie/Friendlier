using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Xyglo
{
    /// <summary>
    /// Define a line and character position in on the screen - currently very similar to FilePosition
    /// but we draw a distinction to make sure our commands are clear on what they're accepting and
    /// what they are producing.
    /// </summary>
    public struct ScreenPosition : ICloneable, IComparable
    {
        /*
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public ScreenPosition()
        {
            X = -1;
            Y = -1;
        }*/

        /// <summary>
        /// Construct from integers
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public ScreenPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="p"></param>
        public ScreenPosition(ScreenPosition p)
        {
            X = p.X;
            Y = p.Y;
        }

        /// <summary>
        /// Construct from a FilePosition
        /// </summary>
        /// <param name="p"></param>
        public ScreenPosition(FilePosition p)
        {
            X = p.X;
            Y = p.Y;
        }

        /// <summary>
        /// Close this object
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            ScreenPosition copy = new ScreenPosition(this.X, this.Y);
            return copy;
        }

        /// <summary>
        /// Build from a Vector2
        /// </summary>
        /// <param name="vector"></param>
        public ScreenPosition(Vector2 vector)
        {
            X = Convert.ToInt16(vector.X);
            Y = Convert.ToInt16(vector.Y);
        }

        /// <summary>
        /// Equality operator
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(ScreenPosition a, ScreenPosition b)
        {
            return ((a.X == b.X) && (a.Y == b.Y));
        }

        /// <summary>
        /// Not equals
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(ScreenPosition a, ScreenPosition b)
        {
            return ((a.X != b.X) || (a.Y != b.Y));
        }

        public static ScreenPosition operator +(ScreenPosition a, ScreenPosition b)
        {
            ScreenPosition result = a;
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
            ScreenPosition h1 = (ScreenPosition)x;

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
        /// Less than operator
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator <(ScreenPosition a, ScreenPosition b)
        {
            return (a.Y < b.Y || (a.Y == b.Y && a.X < b.X));
        }

        /// <summary>
        /// Greater than
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator >(ScreenPosition a, ScreenPosition b)
        {
            return (a.Y > b.Y || (a.Y == b.Y && a.X > b.X));
        }

        // Needed to avoid warning
        //
        public override int GetHashCode()
        {
            return X ^ Y;
        }

        // Return a y offset ScreenPosition
        //
        public ScreenPosition yOffset(int offsetY)
        {
            return new ScreenPosition(X, Y + offsetY);
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
        public static ScreenPosition Zero()
        {
            return m_zero;
        }

        /// <summary>
        /// Global one-off definition
        /// </summary>
        static public ScreenPosition m_zero = new ScreenPosition(0, 0);

        // Our values
        //
        public int X;
        public int Y;
    }

}
