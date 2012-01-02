using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;


namespace Xyglo
{
    /// <summary>
    /// A view on a buffer - can be independent from a FileBuffer but carries a reference if needed (undecided on this)
    /// </summary>
    public class BufferView
    {
        public enum BufferPosition
        {
            Above,
            Below,
            Left,
            Right
        };

        /// <summary>
        /// 3d position of the BufferView
        /// </summary>
        public Vector3 m_position;

        /// <summary>
        /// Where the buffer is showing from
        /// </summary>
        public int m_bufferShowStart = 0;

        /// <summary>
        /// Length of visible buffer
        /// </summary>
        public int m_bufferShowLength = 20;

        /// <summary>
        /// Current cursor coordinates in this BufferView
        /// </summary>
        public Vector2 m_cursorPosition;

        /// <summary>
        /// Text colour
        /// </summary>
        public Color m_textColour = Color.White;

        /// <summary>
        /// Cursor colour
        /// </summary>
        public Color m_cursorColour = Color.Yellow;

        /// <summary>
        /// Highlight colour
        /// </summary>
        public Color m_highlightColour = Color.PaleVioletRed;

        /// <summary>
        /// The FileBuffer associated with this BufferView
        /// </summary>
        public FileBuffer m_fileBuffer;

        public BufferView()
        {
        }

        public BufferView(FileBuffer buffer, Vector3 position, int bufferShowStart, int bufferShowLength)
        {
            m_position = position;
            m_fileBuffer = buffer;
            m_bufferShowStart = bufferShowStart;
            m_bufferShowLength = bufferShowLength;
        }

        public BufferView(BufferView rootBV, BufferPosition position)
        {
            m_fileBuffer = rootBV.m_fileBuffer;
            m_bufferShowStart = rootBV.m_bufferShowStart;
            m_bufferShowLength = rootBV.m_bufferShowLength;

            calculateRelativePosition(rootBV, position);
        }

        public void calculateRelativePosition(BufferView bV, BufferPosition position)
        {
            switch (position)
            {
                case BufferPosition.Above:
                    m_position = bV.m_position - (new Vector3(0.0f, bV.m_bufferShowLength * 10.0f, 0.0f));
                    break;

                case BufferPosition.Below:
                    m_position = bV.m_position + (new Vector3(0.0f, bV.m_bufferShowLength * 10.0f, 0.0f));
                    break;

                case BufferPosition.Left:
                    m_position = bV.m_position - (new Vector3(35 * 10.0f, 0.0f, 0.0f));
                    break;

                case BufferPosition.Right:
                    m_position = bV.m_position + (new Vector3(35 * 10.0f, 0.0f, 0.0f));
                    break;

                default:
                    throw new Exception("Unknown position parameter passed");
            }
        }
    }
}
