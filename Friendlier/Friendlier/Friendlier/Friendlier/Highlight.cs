using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Microsoft.Xna.Framework;
using System.Runtime.Serialization;

namespace Xyglo
{
    /// <summary>
    /// A highlight is a piece of text, a position range, a colour and an optional indent
    /// </summary>
    [DataContractAttribute]
    public class Highlight : IComparable
    {

        public Highlight(int line, int startX, int endX, string text, Color colour, int indent = 0)
        {
            m_startHighlight = new FilePosition(startX, line);
            m_endHighlight = new FilePosition(endX, line);
            m_colour = colour;
            m_text = text;
        }

        /// <summary>
        /// Highlight start - line (Y) is included
        /// </summary>
        public FilePosition m_startHighlight { get; set; }

        /// <summary>
        /// Highlight end - line (Y) is included
        /// </summary>
        public FilePosition m_endHighlight { get; set; }

        /// <summary>
        /// Indent characters
        /// </summary>
        public int m_indent = 0;

        /// <summary>
        /// Colour of the highlight
        /// </summary>
        public Color m_colour { get; set; }

        // Text that is being highlighted
        //
        public string m_text;

        /// <summary>
        /// Must implement this for the IComparable interface so we can compares highlights
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        int IComparable.CompareTo(object x)
        {
            Highlight h1 = (Highlight)x;

            if (h1.m_startHighlight.Y <= this.m_startHighlight.Y && h1.m_startHighlight.X < this.m_startHighlight.X)
            {
                return -1;
            }
            else if (h1.m_startHighlight.Y == this.m_startHighlight.Y && h1.m_startHighlight.X == this.m_startHighlight.X)
            {
                return 0;
            }
            else // Greater than
            {
                return 1;
            }
        }
    }

    /// <summary>
    /// A HighlightList is a sorted (by start position) list of highlights for a file
    /// </summary>
    /*
    public class HighlightList : IComparable
    {
        private class HighlightListSorter : IComparer
        {
            // Determine the list position
            int IComparer.Compare(object x, object y)
            {
                Highlight h1 = (Highlight)x;
                Highlight h2 = (Highlight)y;

                if (h1.m_startHighlight.Y <= h2.m_startHighlight.Y && h1.m_startHighlight.X < h2.m_startHighlight.X)
                {
                    return -1;
                }
                else if (h1.m_startHighlight.Y == h2.m_startHighlight.Y && h1.m_startHighlight.X == h2.m_startHighlight.X)
                {
                    return 0;
                }
                else // Greater than
                {
                    return 1;
                }
            }
        }
    }
     * */
}
