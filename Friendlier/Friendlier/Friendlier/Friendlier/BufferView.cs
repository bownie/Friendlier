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
        /// What line the buffer is showing from
        /// </summary>
        protected int m_bufferShowStartY = 0;

        /// <summary>
        /// The BufferView remembers its own highlight positions
        /// </summary>
        FilePosition m_highlightStart;

        /// <summary>
        /// Get the start of the highlight
        /// </summary>
        /// <returns></returns>
        public FilePosition getHighlightStart()
        {
            return m_highlightStart;
        }

        /// <summary>
        /// Set beginning of the highlight
        /// </summary>
        /// <param name="fp"></param>
        protected void setHighlightStart(FilePosition fp)
        {
            m_highlightStart = fp;
            Logger.logMsg("BufferView::setHighlightStart() - starting at X = " + fp.X + ", Y = " + fp.Y);
        }

        /// <summary>
        /// The BufferView remembers its own highlight positions
        /// </summary>
        FilePosition m_highlightEnd;

        /// <summary>
        /// Get end point of the highlight
        /// </summary>
        /// <returns></returns>
        public FilePosition getHighlightEnd()
        {
            return m_highlightEnd;
        }

        /// <summary>
        /// Set end point of the highlight
        /// </summary>
        /// <param name="fp"></param>
        protected void setHighlightEnd(FilePosition fp)
        {
            m_highlightEnd = fp;
            Logger.logMsg("BufferView::setHighlightEnd() - ending at X = " + fp.X + ", Y = " + fp.Y);
        }

        /// <summary>
        /// Start highlighting
        /// </summary>
        public void startHighlight()
        {
            m_highlightStart = m_cursorPosition;
            m_highlightEnd = m_cursorPosition;
            Logger.logMsg("BufferView::startHighlight() - starting at X = " + m_cursorPosition.X + ", Y = " + m_cursorPosition.Y);
        }

        /// <summary>
        /// Where we set the highlight to when there isn't one
        /// </summary>
        static public FilePosition NoHighlightPosition = new FilePosition(-1 ,-1);

        /// <summary>
        /// No highlight position i
        /// </summary>
        public void noHighlight()
        {
            m_highlightStart = BufferView.NoHighlightPosition;
            m_highlightEnd = BufferView.NoHighlightPosition;
            //Logger.logMsg("BufferView::noHighlight()");
        }


        /// <summary>
        /// Extend an existing highlight to this position
        /// </summary>
        public void extendHighlight()
        {
            m_highlightEnd = m_cursorPosition;
            Logger.logMsg("BufferView::extendHighlight() - extending at X = " + m_cursorPosition.X + ", Y = " + m_cursorPosition.Y);
        }

        /// <summary>
        /// Get current buffer position
        /// </summary>
        /// <returns></returns>
        public int getBufferShowStartY()
        {
            return m_bufferShowStartY;
        }

        protected int m_bufferShowStartX = 0;

        /// <summary>
        /// Get current buffer view position X offset
        /// </summary>
        /// <returns></returns>
        public int getBufferShowStartX()
        {
            return m_bufferShowStartX;
        }

        /// <summary>
        /// Set current buffer view position X offset
        /// </summary>
        /// <param name="x"></param>
        public void setBufferShowStartX(int x)
        {
            m_bufferShowStartX = x;
        }

        // Store the cursor coordinates locally
        //
        protected Vector3 m_cursorCoordinates = new Vector3();

        /// <summary>
        /// Cursor coordinates in 3D
        /// </summary>
        public Vector3 getCursorCoordinates()
        {
            // Return the cursor coordinates taking into account paging using m_bufferShowStartY
            //
            m_cursorCoordinates.X = m_position.X + m_cursorPosition.X * m_charWidth;
            m_cursorCoordinates.Y = m_position.Y + ( m_cursorPosition.Y - m_bufferShowStartY ) * m_lineHeight;

            return m_cursorCoordinates;
        }

        /// <summary>
        /// Page up the BufferView
        /// </summary>
        public void pageUp()
        {
            // Page up the BufferView position
            //
            m_bufferShowStartY -= m_bufferShowLength;

            if (m_bufferShowStartY < 0)
            {
                m_bufferShowStartY = 0;
            }

            // Page up the cursor
            m_cursorPosition.Y -= m_bufferShowLength;

            if (m_cursorPosition.Y < 0)
            {
                m_cursorPosition.Y = 0;
            }
        }

        /// <summary>
        /// Page down the BufferView
        /// </summary>
        public void pageDown()
        {
            // Page down the buffer view position
            m_bufferShowStartY += m_bufferShowLength;

            if (m_bufferShowStartY > m_fileBuffer.getLineCount() - 1)
            {
                m_bufferShowStartY = m_fileBuffer.getLineCount() - 1;
            }

            // Page down the cursor
            //
            m_cursorPosition.Y += m_bufferShowLength;

            if (m_cursorPosition.Y > m_fileBuffer.getLineCount() - 1)
            {
                m_cursorPosition.Y = m_fileBuffer.getLineCount() - 1;
            }
        }

        /// <summary>
        /// Set position that we're showing the BufferView from
        /// </summary>
        /// <param name="bss"></param>
        /// <returns></returns>
        public void setBufferShowStartY(int bss)
        {
            if (bss < 0)
            {
                m_bufferShowStartY = 0;
            } else if (bss < m_fileBuffer.getLineCount())
            {
                m_bufferShowStartY = bss;
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
        /// Get the current cursor position relative to the page value (m_bufferShowStartY)
        /// </summary>
        /// <returns></returns>
        public FilePosition getRelativeCursorPosition()
        {
            return m_cursorPosition;
        }

        /// <summary>
        /// Cursor position is position in the FileBuffer - not in the visible BufferView
        /// </summary>
        /// <returns></returns>
        public FilePosition getCursorPosition()
        {
            return m_cursorPosition;
        }

        /// <summary>
        /// We have a highlight if the two endpoints are not equal
        /// </summary>
        /// <returns></returns>
        public bool gotHighlight()
        {
            return (m_highlightStart != m_highlightEnd);
        }

        /// <summary>
        /// Set the cursor position in this view
        /// </summary>
        /// <param name="fp"></param>
        public void setCursorPosition(FilePosition fp)
        {
            Logger.logMsg("BufferView::setCursorPosition() - fp.X = " + fp.X + ", fp.Y = " + fp.Y);
            if (m_fileBuffer.getLineCount() == 0)
            {
                return;
            }

            if (fp.Y + m_bufferShowStartY < m_fileBuffer.getLineCount())
            {
                m_cursorPosition = fp;
            }
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
            m_bufferShowStartY = rootBV.m_bufferShowStartY;
            m_bufferShowStartX = rootBV.m_bufferShowStartX;
            m_bufferShowLength = rootBV.m_bufferShowLength;
            m_bufferShowWidth = rootBV.m_bufferShowWidth;
            m_charWidth = rootBV.m_charWidth;
            m_lineHeight = rootBV.m_lineHeight;
            m_position = position;
        }

        public BufferView(FileBuffer buffer, Vector3 position, int bufferShowStartY, int bufferShowLength, float charWidth, float lineHeight)
        {
            m_position = position;
            m_fileBuffer = buffer;
            m_bufferShowStartY = bufferShowStartY;
            m_bufferShowStartX = 0;
            m_bufferShowLength = bufferShowLength;
            m_charWidth = charWidth;
            m_lineHeight = lineHeight;
        }

        public BufferView(BufferView rootBV, BufferPosition position)
        {
            m_fileBuffer = rootBV.m_fileBuffer;
            m_bufferShowStartY = rootBV.m_bufferShowStartY;
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
        /// Return the coordinates of the highlighted area use a list of bounding boxes
        /// </summary>
        /// <param name="highlightStart"></param>
        /// <param name="highlightEnd"></param>
        public List<BoundingBox> computeHighlight()
        {
            List<BoundingBox> bb = new List<BoundingBox>();

            // If we have no highlight then return an empty list
            //
            if (m_highlightStart == m_highlightEnd)
            {
                return bb;
            }

            Vector3 startPos = new Vector3();
            Vector3 endPos = new Vector3();

            if (m_highlightStart.Y == m_highlightEnd.Y)
            {
                // Set start position
                //
                startPos.X = m_position.X + m_highlightStart.X * m_charWidth;
                startPos.Y = m_position.Y + m_highlightStart.Y * m_lineHeight;

                // Set end position
                //
                endPos.X = m_position.X + m_highlightEnd.X * m_charWidth;
                endPos.Y = m_position.Y + (m_highlightEnd.Y + 1) * m_lineHeight;

                bb.Add(new BoundingBox(startPos, endPos));
                return bb;

            }
            else if (m_highlightStart < m_highlightEnd) // highlight down
            {
                // When we're highlighting down then the end point will always be on the screen
                // but the start point may not be so we have to test for this.
                //
                int minStart = m_highlightStart.Y;
                if (minStart < 0)
                {
                    minStart = 0;
                }

                bool fullLineAtStart = false;
                if (minStart < m_bufferShowStartY)
                {
                    minStart = m_bufferShowStartY;
                    fullLineAtStart = true;
                }

                for (int i = minStart; i < m_highlightEnd.Y + 1; i++)
                {
                    if (i == m_highlightStart.Y)
                    {
                        if (fullLineAtStart)
                        {
                            startPos.X = m_position.X;
                        }
                        else
                        {
                            startPos.X = m_position.X + m_highlightStart.X * m_charWidth;
                        }
                        endPos.X = m_position.X + m_fileBuffer.getLine(i).Length * m_charWidth;
                    }
                    else if (i == m_highlightEnd.Y)
                    {
                        startPos.X = m_position.X;
                        endPos.X = m_position.X + m_highlightEnd.X * m_charWidth;
                    }
                    else
                    {
                        startPos.X = m_position.X;
                        endPos.X = m_position.X + m_fileBuffer.getLine(i).Length * m_charWidth;
                    }

                    startPos.Y = ( m_position.Y + i  - m_bufferShowStartY) * m_lineHeight;
                    endPos.Y = (m_position.Y + i + 1 - m_bufferShowStartY) * m_lineHeight;

                    bb.Add(new BoundingBox(startPos, endPos));
                }
            }
            else // m_highlightStart > m_highlightEnd - highlighting up rather than down
            {
                bool fullLineAtStart = false;
                int minStart = m_highlightEnd.Y;
                if (minStart < m_bufferShowStartY)
                {
                    minStart = m_bufferShowStartY;
                    fullLineAtStart = true;
                }

                int maxStart = m_highlightStart.Y;
                if (maxStart > m_fileBuffer.getLineCount() - 1)
                {
                    maxStart = m_fileBuffer.getLineCount() - 1;
                }

                // Check if we're highlighting outside the screen size
                bool fullLineAtEnd = false;
                if (maxStart > m_bufferShowStartY + m_bufferShowLength - 1)
                {
                    maxStart = m_bufferShowStartY + m_bufferShowLength - 1;
                    fullLineAtEnd = true;
                }

                for (int i = m_highlightEnd.Y; i < maxStart + 1; i++)
                {
                    if (i == m_highlightEnd.Y)
                    {
                        if (fullLineAtStart)
                        {
                            startPos.X = m_position.X;
                        }
                        else
                        {
                            startPos.X = m_position.X + m_cursorPosition.X * m_charWidth;
                        }
                        endPos.X = m_position.X + m_fileBuffer.getLine(i).Length * m_charWidth;
                    }
                    else if (i == maxStart)
                    {
                        startPos.X = m_position.X;

                        if (fullLineAtEnd)
                        {
                            endPos.X = m_position.X + m_fileBuffer.getLine(i).Length * m_charWidth;
                        }
                        else
                        {
                            endPos.X = m_position.X + m_highlightStart.X * m_charWidth;
                        }
                    }
                    else
                    {
                        startPos.X = m_position.X;
                        endPos.X = m_position.X + m_fileBuffer.getLine(i).Length * m_charWidth;
                    }

                    startPos.Y = (m_position.Y + i - m_bufferShowStartY) * m_lineHeight;
                    endPos.Y = (m_position.Y + i + 1 - m_bufferShowStartY) * m_lineHeight;

                    bb.Add(new BoundingBox(startPos, endPos));
                }
            }

            return bb;
        }

        /// <summary>
        /// When moving up the cursor on this BufferView  
        /// </summary>
        /// <param name="leftCursor"></param>
        public void moveCursorUp(bool leftCursor)
        {
            if (m_cursorPosition.Y > 0)
            {
                m_cursorPosition.Y--;

                // Check for the top of the visible buffer 
                //
                if (m_cursorPosition.Y - m_bufferShowStartY < 0)
                {
                    m_bufferShowStartY--;
                    m_cursorPosition.Y = m_bufferShowStartY;
                }

                string line = m_fileBuffer.getLine(m_cursorPosition.Y);
                if (leftCursor || m_cursorPosition.X > line.Length)
                {
                    m_cursorPosition.X = line.Length;
                }
            }
            else
            {
                // Nudge up the buffer
                //
                if (m_bufferShowStartY > 0)
                {
                    m_bufferShowStartY--;
                }
            }
        }

        /// <summary>
        /// When we want to move the cursor down in the BufferView we're either doing this because the user
        /// wants to go down or wants to go off the end of the line (right)
        /// </summary>
        /// <param name="rightCursor"></param>
        public void moveCursorDown(bool rightCursor)
        {
            if (m_cursorPosition.Y + 1 < m_fileBuffer.getLineCount())
            {
                // Always increment cursor position
                m_cursorPosition.Y++;

                // Check to see if we've hit the bottom of the visible buffer and increment
                // accordingly
                //
                if (m_cursorPosition.Y - m_bufferShowStartY >= m_bufferShowLength)
                {
                    m_cursorPosition.Y = m_bufferShowStartY + m_bufferShowLength;
                    m_bufferShowStartY++;
                }

                // Sort out any X position irregularities
                //
                if (rightCursor)
                {
                    m_cursorPosition.X = 0;
                }
                else if (m_cursorPosition.X > m_fileBuffer.getLine(m_cursorPosition.Y).Length)
                {
                    m_cursorPosition.X = m_fileBuffer.getLine(m_cursorPosition.Y).Length;
                }
            }
            //Logger.logMsg("BufferView::moveCursorDown() - m_cursorPosition.Y = " + m_cursorPosition.Y);
            //Logger.logMsg("BufferView::moveCursorDown() - m_bufferShowStart.Y = " + m_bufferShowStartY);
        }

        /// <summary>
        /// Once we have a highlighted section we can delete it with this method
        /// </summary>
        public void deleteCurrentSelection()
        {
            m_fileBuffer.deleteSelection(m_highlightStart, m_highlightEnd);

            if (m_highlightStart < m_highlightEnd)
            {
                m_cursorPosition = m_highlightStart;
            }
            else
            {
                m_cursorPosition = m_highlightEnd;
            }

            // Cancel our highlight
            //
            noHighlight();
        }

        /// <summary>
        /// Delete a single character at the cursor
        /// </summary>
        public void deleteSingle()
        {
            m_fileBuffer.deleteSelection(m_cursorPosition, m_cursorPosition);
        }

        /// <summary>
        /// Replace a highlighted selection with some text (can be multi-line)
        /// </summary>
        /// <param name="text"></param>
        public void replaceCurrentSelection(string text)
        {
            m_cursorPosition = m_fileBuffer.replaceText(m_highlightStart, m_highlightEnd, text);

            if (m_cursorPosition.Y < m_bufferShowStartY)
            {
                m_bufferShowStartY = m_cursorPosition.Y;
            }
            else if (m_cursorPosition.Y > m_bufferShowStartY + m_bufferShowLength)
            {
                m_bufferShowStartY = m_cursorPosition.Y;
            }

            // Cancel the highlight
            noHighlight();
        }

        /// <summary>
        /// Insert some text into the BufferView
        /// </summary>
        /// <param name="text"></param>
        public void insertText(string text)
        {
            m_cursorPosition = m_fileBuffer.insertText(m_cursorPosition, text);
        }

        /// <summary>
        /// Insert a new line at the cursor
        /// </summary>
        public void insertNewLine()
        {
            m_cursorPosition = m_fileBuffer.insertNewLine(m_cursorPosition);
        }

        /// <summary>
        /// Return the text of the currently highlighted selection so we can put it in
        /// a cut and paste buffer for example.
        /// </summary>
        /// <returns></returns>
        public TextSnippet getSelection()
        {
            TextSnippet rS = new TextSnippet();

            FilePosition shiftStart = m_highlightStart;
            FilePosition shiftEnd = m_highlightEnd;

            // Swap the end points if start is greater than end.
            //
            if (shiftStart > shiftEnd)
            {
                shiftStart = m_highlightEnd;
                shiftEnd = m_highlightStart;
            }

            string line;

            // Are we deleting on the same line?
            //
            if (shiftStart.Y == shiftEnd.Y)
            {
                line = m_fileBuffer.getLine(shiftStart.Y).Substring(shiftStart.X, shiftEnd.X - shiftStart.X);
                rS.setSnippetSingle(line);
            }
            else  // Multi-line text
            {
                string newLine;

                for (int i = shiftStart.Y; i < shiftEnd.Y; i++)
                {
                    line = m_fileBuffer.getLine(i);

                    if (i == shiftStart.Y)
                    {
                        newLine = line.Substring(shiftStart.X, line.Length - shiftStart.X);
                    }
                    else if (i == shiftEnd.Y)
                    {
                        newLine = line.Substring(0, shiftEnd.X);
                    }
                    else
                    {
                        newLine = line;
                    }
                    rS.m_lines.Add(line);
                }
            }

            return rS;
        }
    }
}
