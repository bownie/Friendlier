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
        protected Vector3 m_position;

        /// <summary>
        /// Get the position in 3D space
        /// </summary>
        public Vector3 getPosition() { return m_position; }

        public void moveX(float x) { m_position.X += x; }
        public void moveY(float y) { m_position.Y += y; }
        public void moveZ(float z) { m_position.Z += z; }

        /// <summary>
        /// Where the buffer is showing from
        /// </summary>
        protected int m_bufferShowStart = 0;

        /// <summary>
        /// Get current buffer position
        /// </summary>
        /// <returns></returns>
        public int getBufferShowStart()
        {
            return m_bufferShowStart;
        }

        /// <summary>
        /// Set current buffer position
        /// </summary>
        /// <param name="bss"></param>
        /// <returns></returns>
        public void setBufferShowStart(int bss)
        {
            if (bss < m_fileBuffer.getLineCount())
            {
                m_bufferShowStart = bss;
            }
            else
            {
                System.Exception ex = new System.Exception("Cannot set buffershowstart to greater than buffer length (" + m_fileBuffer.getLineCount() + ")");
                throw ex;
            }
        }

        /// <summary>
        /// Length of visible buffer
        /// </summary>
        protected int m_bufferShowLength = 20;

        /// <summary>
        /// Get BufferShow length
        /// </summary>
        /// <returns></returns>
        public int getBufferShowLength()
        {
            return m_bufferShowLength;
        }

        /// <summary>
        /// Number of characters to show in a BufferView line
        /// </summary>
        protected int m_bufferShowWidth = 80;

        /// <summary>
        /// Accessor for BufferShowWidth
        /// </summary>
        /// <returns></returns>
        public int getBufferShowWidth()
        {
            return m_bufferShowWidth;
        }

        /// <summary>
        /// Current cursor coordinates in this BufferView
        /// </summary>
        protected FilePosition m_cursorPosition;

        /// <summary>
        /// The position in the buffer at which this view is locked
        /// </summary>
        protected int m_viewLockPosition = 0;

        /// <summary>
        /// Is this view locked such that when we edit other views this one stays at the same relative position
        /// </summary>
        protected bool m_viewLocked = false;

        public bool isLocked()
        {
            return m_viewLocked;
        }

        /// <summary>
        /// Set the lock on this view at a position
        /// </summary>
        /// <param name="locked"></param>
        /// <param name="lockedPosition"></param>
        public void setLock(bool locked, int lockedPosition)
        {
            m_viewLocked = locked;
            m_viewLockPosition = lockedPosition;
        }
        
        /// <summary>
        /// Get the current cursor position
        /// </summary>
        /// <returns></returns>
        public FilePosition getCursorPosition()
        {
            return m_cursorPosition;
        }

        /// <summary>
        /// Set the cursor position in this view
        /// </summary>
        /// <param name="fp"></param>
        public void setCursorPosition(FilePosition fp)
        {
            //Logger.logMsg("setCursorPosition = " + fp.Y);

            if (m_fileBuffer.getLineCount() == 0)
            {
                return;
            }

            if (fp.Y + m_bufferShowStart < m_fileBuffer.getLineCount())
            {
                m_cursorPosition = fp;
            }

/*
            else
            {
                System.Exception ret = new System.Exception("Cannot set cursor past end of buffer");
                throw ret;
            }
 * */
        }

        /// <summary>
        /// Set cursor position from a Vector2
        /// </summary>
        /// <param name="vfp"></param>
        public void setCursorPosition(Vector2 vfp)
        {
            int x = Convert.ToInt16(vfp.X);
            int y = Convert.ToInt16(vfp.Y);
            m_cursorPosition.X = x;
            m_cursorPosition.Y = y;
        }

        /// <summary>
        /// Text colour
        /// </summary>
        public Color m_textColour = Color.LawnGreen;

        /// <summary>
        /// Cursor colour
        /// </summary>
        public Color m_cursorColour = Color.Yellow;

        /// <summary>
        /// Highlight colour
        /// </summary>
        public Color m_highlightColour = Color.PaleVioletRed;

        /// <summary>
        /// Width of a single character in the font that we're displaying in
        /// </summary>
        protected float m_charWidth;

        /// <summary>
        /// Height of a line in the font we're displaying in
        /// </summary>
        protected float m_lineHeight;

        public float getLineHeight()
        {
            return m_lineHeight;
        }


        /// <summary>
        /// The FileBuffer associated with this BufferView
        /// </summary>
        protected FileBuffer m_fileBuffer;

        /// <summary>
        /// Get the associated FileBuffer
        /// </summary>
        /// <returns></returns>
        public FileBuffer getFileBuffer()
        {
            return m_fileBuffer;
        }

        public BufferView(float charWidth, float lineHeight)
        {
            m_charWidth = charWidth;
            m_lineHeight = lineHeight;
        }

        public BufferView(BufferView rootBV, Vector3 position)
        {
            m_fileBuffer = rootBV.m_fileBuffer;
            m_bufferShowStart = rootBV.m_bufferShowStart;
            m_bufferShowLength = rootBV.m_bufferShowLength;
            m_bufferShowWidth = rootBV.m_bufferShowWidth;
            m_charWidth = rootBV.m_charWidth;
            m_lineHeight = rootBV.m_lineHeight;
            m_position = position;
        }

        public BufferView(FileBuffer buffer, Vector3 position, int bufferShowStart, int bufferShowLength, float charWidth, float lineHeight)
        {
            m_position = position;
            m_fileBuffer = buffer;
            m_bufferShowStart = bufferShowStart;
            m_bufferShowLength = bufferShowLength;
            m_charWidth = charWidth;
            m_lineHeight = lineHeight;
        }

        public BufferView(BufferView rootBV, BufferPosition position)
        {
            m_fileBuffer = rootBV.m_fileBuffer;
            m_bufferShowStart = rootBV.m_bufferShowStart;
            m_bufferShowLength = rootBV.m_bufferShowLength;
            m_bufferShowWidth = rootBV.m_bufferShowWidth;
            m_charWidth = rootBV.m_charWidth;
            m_lineHeight = rootBV.m_lineHeight;

            m_position = rootBV.calculateRelativePosition(position);
        }

        public Vector3 calculateRelativePosition(BufferPosition position)
        {
            switch (position)
            {
                case BufferPosition.Above:
                    return m_position - (new Vector3(0.0f, (m_bufferShowLength + 5) * m_lineHeight, 0.0f));

                case BufferPosition.Below:
                    return m_position + (new Vector3(0.0f, (m_bufferShowLength + 5) * m_lineHeight, 0.0f));

                case BufferPosition.Left:
                    return m_position - (new Vector3(m_charWidth * (m_bufferShowWidth + 10), 0.0f, 0.0f));

                case BufferPosition.Right:
                    return m_position + (new Vector3(m_charWidth * (m_bufferShowWidth + 10), 0.0f, 0.0f));

                default:
                    throw new Exception("Unknown position parameter passed");
            }
        }

        /// <summary>
        /// Return the vector of the centre of this BufferView
        /// </summary>
        /// <returns></returns>
        public Vector3 getLookPosition()
        {
            Vector3 rV = m_position;
            rV.Y = -rV.Y; // insert Y
            rV.X += m_charWidth * m_bufferShowWidth / 2;
            rV.Y -= m_lineHeight * m_bufferShowLength / 2;
            rV.Z = 0.0f;
            return rV;
        }

        /// <summary>
        /// Return the eye vector of the centre of this BufferView
        /// </summary>
        /// <returns></returns>
        public Vector3 getEyePosition()
        {
            Vector3 rV = m_position;
            rV.Y = -rV.Y; // invert Y
            rV.X += m_charWidth * m_bufferShowWidth / 2;
            rV.Y -= m_lineHeight * m_bufferShowLength / 2;
            rV.Z += 600.0f;
            return rV;
        }

        /// <summary>
        /// If we're changinging aspect ratio we might want to scale position and text sizes to
        /// ensure that everything appears in the right place
        /// </summary>
        /// <param name="scaleFactor"></param>
        public void scale(float scaleFactor)
        {
            m_position = m_position * scaleFactor;
            m_charWidth = m_charWidth * scaleFactor;
            m_lineHeight = m_lineHeight * scaleFactor;
        }
    }
}
