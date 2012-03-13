using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace Xyglo
{


    /// <summary>
    /// A view on a buffer - can be independent from a FileBuffer but carries a reference if needed (undecided on this)
    /// </summary>
    [DataContract(Name = "Friendlier", Namespace = "http://www.xyglo.com")]
    public class BufferView
    {
        /// <summary>
        /// BufferPosition is used to help determine positions of other BufferViews
        /// </summary>
        public enum BufferPosition
        {
            Above,
            Below,
            Left,
            Right
        };

        /// <summary>
        /// Which Quadrant of four BufferViews are we viewing from the current one - cycling
        /// through these possibilities makes it nine total screens we can view
        /// </summary>
        public enum ViewQuadrant
        {
            TopRight,
            BottomRight,
            BottomLeft,
            TopLeft
        }

        /// <summary>
        /// Which direction we're cycling through quadrant views
        /// </summary>
        public enum ViewCycleDirection
        {
            Clockwise,
            Anticlockwise
        }

        /// <summary>
        /// Which quadrant are we viewing
        /// </summary>
        protected ViewQuadrant m_viewQuadrant;

        /// <summary>
        /// Which direction are we cycling
        /// </summary>
        protected ViewCycleDirection m_cycleDirection;


        /// <summary>
        /// A little struct used to hold relative positions of BufferViews
        /// </summary>
        public struct BufferViewPosition
        {
            public BufferView rootBV { get; set; }
            public BufferPosition position { get; set; }
        }

        /// <summary>
        /// The FileBuffer associated with this BufferView
        /// </summary>
        protected FileBuffer m_fileBuffer;

        /// <summary>
        /// Index of the FileBuffer associated with this BufferView so we can recontruct the link
        /// </summary>
        [DataMember()]
        protected int m_fileBufferIndex = 0;

        /// <summary>
        /// 3d position of the BufferView
        /// </summary>
        [DataMember()]
        protected Vector3 m_position;

        /// <summary>
        /// What line the buffer is showing from
        /// </summary>
        [DataMember()]
        protected int m_bufferShowStartY = 0;

        /// <summary>
        /// The BufferView remembers its own highlight positions
        /// </summary>
        [DataMember()]
        protected FilePosition m_highlightStart;

        /// <summary>
        /// Store the BufferViewPosition relative to another
        /// </summary>
        [DataMember()]
        protected BufferViewPosition m_bufferViewPosition;

        /// <summary>
        /// The BufferView remembers its own highlight positions
        /// </summary>
        [DataMember()]
        protected FilePosition m_highlightEnd;

        /// <summary>
        /// Where we set the highlight to when there isn't one
        /// </summary>
        [XmlIgnore]
        static public FilePosition NoHighlightPosition = new FilePosition(-1, -1);

        [DataMember]
        protected int m_bufferShowStartX = 0;

        /// <summary>
        /// Store the cursor coordinates locally
        /// </summary>
        [DataMember]
        protected Vector3 m_cursorCoordinates = new Vector3();

        /// <summary>
        /// Length of visible buffer
        /// </summary>
        [DataMember]
        protected int m_bufferShowLength = 20;

        /// <summary>
        /// Number of characters to show in a BufferView line
        /// </summary>
        [DataMember]
        protected int m_bufferShowWidth = 80;

        /// <summary>
        /// Current cursor coordinates in this BufferView
        /// </summary>
        [DataMember]
        protected FilePosition m_cursorPosition;

        /// <summary>
        /// The position in the buffer at which this view is locked
        /// </summary>
        [DataMember]
        protected int m_viewLockPosition = 0;

        /// <summary>
        /// Is this view locked such that when we edit other views this one stays at the same relative position
        /// </summary>
        [DataMember]
        protected bool m_viewLocked = false;

        /// <summary>
        /// Text colour
        /// </summary>
        [DataMember]
        protected Color m_textColour = Color.White;

        /// <summary>
        /// Cursor colour
        /// </summary>
        [DataMember]
        protected Color m_cursorColour = Color.Yellow;

        /// <summary>
        /// Highlight colour
        /// </summary>
        [DataMember]
        protected Color m_highlightColour = Color.PaleVioletRed;

        /// <summary>
        /// Tailing colour
        /// </summary>
        [DataMember]
        protected Color m_tailColour = Color.LightBlue;

        /// <summary>
        /// Read only colour
        /// </summary>
        [DataMember()]
        protected Color m_readOnlyColour = Color.LightYellow;

        /// <summary>
        /// Width of a single character in the font that we're displaying in
        /// </summary>
        [DataMember()]
        protected float m_charWidth;

        /// <summary>
        /// Height of a line in the font we're displaying in
        /// </summary>
        [DataMember()]
        protected float m_lineHeight; // { get { return value;}  }

        /// <summary>
        /// Is this a non-editable BufferView?
        /// </summary>
        [DataMember()]
        protected bool m_readOnly = false;

        /// <summary>
        /// Are we tailing this File?
        /// </summary>
        [DataMember()]
        protected bool m_tailing = false;

        /////// CONSTRUCTORS /////////

        /// <summary>
        /// Default constructor for XML
        /// </summary>
        public BufferView()
        {
        }

        /// <summary>
        /// Specify just two element
        /// </summary>
        /// <param name="charWidth"></param>
        /// <param name="lineHeight"></param>
        public BufferView(float charWidth, float lineHeight, bool readOnly = false)
        {
            m_charWidth = charWidth;
            m_lineHeight = lineHeight;
            m_readOnly = readOnly;
        }

        /// <summary>
        /// Specify a root BufferView and an absolute position
        /// </summary>
        /// <param name="rootBV"></param>
        /// <param name="position"></param>
        public BufferView(BufferView rootBV, Vector3 position, bool readOnly = false)
        {
            m_fileBuffer = rootBV.m_fileBuffer;
            m_bufferShowStartY = rootBV.m_bufferShowStartY;
            m_bufferShowStartX = rootBV.m_bufferShowStartX;
            m_bufferShowLength = rootBV.m_bufferShowLength;
            m_bufferShowWidth = rootBV.m_bufferShowWidth;
            m_charWidth = rootBV.m_charWidth;
            m_lineHeight = rootBV.m_lineHeight;
            m_position = position;
            m_fileBufferIndex = rootBV.m_fileBufferIndex;
            m_readOnly = rootBV.m_readOnly;
        }

        /// <summary>
        /// Constructor specifying everything
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="position"></param>
        /// <param name="bufferShowStartY"></param>
        /// <param name="bufferShowLength"></param>
        /// <param name="charWidth"></param>
        /// <param name="lineHeight"></param>
        public BufferView(FileBuffer buffer, Vector3 position, int bufferShowStartY, int bufferShowLength, float charWidth, float lineHeight, int fileIndex, bool readOnly = false)
        {
            m_position = position;
            m_fileBuffer = buffer;
            m_bufferShowStartY = bufferShowStartY;
            m_bufferShowStartX = 0;
            m_bufferShowLength = bufferShowLength;
            m_charWidth = charWidth;
            m_lineHeight = lineHeight;
            m_fileBufferIndex = fileIndex;
            m_readOnly = readOnly;
        }

        /// <summary>
        /// Specify some but not all of the things we need to draw the BufferView - we still need
        /// charWidth and lineHeight from somewhere.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="position"></param>
        /// <param name="bufferShowStartY"></param>
        /// <param name="bufferShowLength"></param>
        public BufferView(FileBuffer buffer, Vector3 position, int bufferShowStartY, int bufferShowLength, int fileBufferIndex, bool readOnly = false)
        {
            m_position = position;
            m_fileBuffer = buffer;
            m_bufferShowStartY = bufferShowStartY;
            m_bufferShowStartX = 0;
            m_bufferShowLength = bufferShowLength;
            m_fileBufferIndex = fileBufferIndex;
            m_readOnly = readOnly;
        }

        /// <summary>
        /// Constructor based on an existing buffer view and a relative position
        /// </summary>
        /// <param name="rootBV"></param>
        /// <param name="position"></param>
        public BufferView(BufferView rootBV, BufferPosition position)
        {
            m_fileBuffer = rootBV.m_fileBuffer;
            m_bufferShowStartY = rootBV.m_bufferShowStartY;
            m_bufferShowLength = rootBV.m_bufferShowLength;
            m_bufferShowWidth = rootBV.m_bufferShowWidth;
            m_charWidth = rootBV.m_charWidth;
            m_lineHeight = rootBV.m_lineHeight;

            // Store the orientation from another bufferview
            //
            m_bufferViewPosition.position = position;
            m_bufferViewPosition.rootBV = rootBV;
            m_readOnly = rootBV.m_readOnly;

            // Only calculate a position if we have the information necessary to do it
            //
            if (m_charWidth != 0 && m_lineHeight != 0)
            {
                m_position = rootBV.calculateRelativePosition(position);
            }
        }



        //////// METHODS ///////

        /// <summary>
        /// Get text colour dependent on mode
        /// </summary>
        public Color getTextColour()
        {
            if (m_tailing)
            {
                return m_tailColour;
            }
            else if (m_readOnly)
            {
                return m_readOnlyColour;
            }
            else
            {
                return m_textColour;
            }
        }

        /// <summary>
        /// Return the highlight colour
        /// </summary>
        /// <returns></returns>
        public Color getHighlightColor()
        {
            return m_highlightColour;
        }


        /// <summary>
        /// Get the index of the FileBuffer associated with this BufferView
        /// </summary>
        /// <returns></returns>
        public int getFileBufferIndex()
        {
            return m_fileBufferIndex;
        }

        /// <summary>
        /// Set the index number of the FileBuffer associated with this BufferView
        /// </summary>
        /// <param name="index"></param>
        public void setFileBufferIndex(int index)
        {
            m_fileBufferIndex = index;
        }

        /// <summary>
        /// Get the position in 3D space
        /// </summary>
        public Vector3 getPosition() { return m_position; }

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

        /// <summary>
        /// Visible width of 'window' 
        /// </summary>
        /// <returns></returns>
        public float getVisibleWidth()
        {
            return (m_bufferShowWidth * m_charWidth);
        }

        /// <summary>
        /// Visible height of 'window'
        /// </summary>
        /// <returns></returns>
        public float getVisibleHeight()
        {
            return (m_bufferShowLength * m_lineHeight);
        }

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
        /// Get BufferShow length
        /// </summary>
        /// <returns></returns>
        public int getBufferShowLength()
        {
            return m_bufferShowLength;
        }

        /// <summary>
        /// Accessor for BufferShowWidth
        /// </summary>
        /// <returns></returns>
        public int getBufferShowWidth()
        {
            return m_bufferShowWidth;
        }

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
        /// Get the line height
        /// </summary>
        /// <returns></returns>
        public float getLineHeight()
        {
            return m_lineHeight;
        }

        /// <summary>
        /// Set the line height both in this BufferView and any rootBV
        /// </summary>
        /// <param name="height"></param>
        public void setLineHeight(float height)
        {
            m_lineHeight = height;

            if (m_bufferViewPosition.rootBV != null)
            {
                m_bufferViewPosition.rootBV.setLineHeight(height);
            }
        }

        /// <summary>
        /// Set the m_charWidth both in this BufferView and any rootBV
        /// </summary>
        /// <param name="width"></param>
        public void setCharWidth(float width)
        {
            m_charWidth = width;

            if (m_bufferViewPosition.rootBV != null)
            {
                m_bufferViewPosition.rootBV.setCharWidth(width);
            }
        }

        /// <summary>
        /// Get the associated FileBuffer
        /// </summary>
        /// <returns></returns>
        public FileBuffer getFileBuffer()
        {
            return m_fileBuffer;
        }

        /// <summary>
        /// Set the associated FileBuffer and index
        /// </summary>
        /// <param name="fb"></param>
        public void setFileBuffer(FileBuffer fb, int index = -1)
        {
            m_fileBuffer = fb;

            if (index != -1)
            {
                m_fileBufferIndex = index;
            }
        }


        /// <summary>
        /// Assuming we have valid m_charWidth and m_lineHeight and a valid
        /// m_bufferViewPosition then we can calculate the actual position of
        /// this BufferView using this.
        /// </summary>
        public void calculateMyRelativePosition()
        {
            if (m_charWidth == 0 || m_lineHeight == 0)
            {
                return;
            }

            // Reach through to the rootBV and calculate our position from there
            //
            if (m_bufferViewPosition.rootBV != null)
            {
                m_position = m_bufferViewPosition.rootBV.calculateRelativePosition(m_bufferViewPosition.position);
            }
        }

        /// <summary>
        /// Calculate the position of the next BufferView relative to us - these factors aren't constant
        /// and shouldn't be declared as such but for the moment they usually do.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Vector3 calculateRelativePosition(BufferPosition position)
        {
            if (m_lineHeight == 0 || m_charWidth == 0)
            {
                throw new Exception("BufferView::calculateRelativePosition() - some of our basic settings are zero - cannot calculate");
            }

            switch (position)
            {
                case BufferPosition.Above:
                    return m_position - (new Vector3(0.0f, (m_bufferShowLength + 10) * m_lineHeight, 0.0f));

                case BufferPosition.Below:
                    return m_position + (new Vector3(0.0f, (m_bufferShowLength + 10) * m_lineHeight, 0.0f));

                case BufferPosition.Left:
                    return m_position - (new Vector3(m_charWidth * (m_bufferShowWidth + 15), 0.0f, 0.0f));

                case BufferPosition.Right:
                    return m_position + (new Vector3(m_charWidth * (m_bufferShowWidth + 15), 0.0f, 0.0f));

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
        /// Rotate our BufferView at quadrant viewing height
        /// </summary>
        /// <param name="direction"></param>
        public void rotateQuadrant(BufferView.ViewCycleDirection direction)
        {
            // We probably don't need to store this
            //
            m_cycleDirection = direction;

            // First rotate the view
            //
            if (direction == ViewCycleDirection.Clockwise)
            {
                if (m_viewQuadrant == ViewQuadrant.TopLeft)
                {
                    m_viewQuadrant = ViewQuadrant.TopRight;
                }
                else
                {
                    m_viewQuadrant++;
                }
            }
            else
            {
                m_viewQuadrant--;
                if (m_viewQuadrant < 0)
                {
                    m_viewQuadrant += 4;
                }
            }

            Logger.logMsg("BufferView::rotateQuadrant() - quadrant is now " + m_viewQuadrant.ToString());

        }

        /// <summary>
        /// Return the eye vector of the centre of this BufferView for a given zoom level
        /// - at a certain height we ensure that we're using Quadrant view
        /// </summary>
        /// <returns></returns>
        public Vector3 getEyePosition(float zoomLevel)
        {
            Vector3 rV = m_position;

            rV.Y = -rV.Y; // invert Y
            rV.X += m_charWidth * m_bufferShowWidth / 2;
            rV.Y -= m_lineHeight * m_bufferShowLength / 2;
            rV.Z = zoomLevel;

            if (zoomLevel == 1000.0f)
            {

                switch (m_viewQuadrant)
                {
                    case ViewQuadrant.TopRight:
                        rV.X -= 3 * getVisibleWidth() / 4;
                        rV.Y -= getVisibleHeight();
                        break;

                    case ViewQuadrant.BottomRight:
                        rV.X -= 3 * getVisibleWidth() / 4;
                        rV.Y += getVisibleHeight();
                        break;

                    case ViewQuadrant.BottomLeft:
                        rV.X += 3 * getVisibleWidth() / 4;
                        rV.Y += getVisibleHeight();
                        break;

                    case ViewQuadrant.TopLeft:
                        rV.X += 3 * getVisibleWidth() / 4;
                        rV.Y -= getVisibleHeight();
                        break;

                    default:
                        Logger.logMsg("BufferView::getEyePosition - unknown orientation for quadrant");
                        break;
                }

            }

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
                startPos.Y = m_position.Y + (m_highlightStart.Y - m_bufferShowStartY) * m_lineHeight;

                // Set end position
                //
                endPos.X = m_position.X + m_highlightEnd.X * m_charWidth;
                endPos.Y = m_position.Y + (m_highlightEnd.Y + 1 - m_bufferShowStartY) * m_lineHeight;

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

                    startPos.Y = m_position.Y + (i  - m_bufferShowStartY) * m_lineHeight;
                    endPos.Y = m_position.Y + (i + 1 - m_bufferShowStartY) * m_lineHeight;

                    // If we have nothing highlighted in the line then indicate this with a
                    // half box line
                    //
                    if (startPos.X == endPos.X && endPos.X == m_position.X)
                    {
                        startPos.X += m_charWidth / 2;
                    }

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

                    startPos.Y = m_position.Y + (i - m_bufferShowStartY) * m_lineHeight;
                    endPos.Y = m_position.Y + (i + 1 - m_bufferShowStartY) * m_lineHeight;

                    // If we have nothing highlighted in the line then indicate this with a negative
                    // half box line
                    //
                    if (startPos.X == endPos.X && endPos.X == m_position.X)
                    {
                        startPos.X += m_charWidth / 2;
                    }

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

        /// <summary>
        /// Do we want to tail this file?
        /// </summary>
        /// <param name="tail"></param>
        /// <returns></returns>
        public void setTailing(bool tail)
        {
            m_tailing = tail;
        }

        /// <summary>
        /// Are we tailing this BufferView?
        /// </summary>
        /// <returns></returns>
        public bool isTailing()
        {
            return m_tailing;
        }

        /// <summary>
        /// Is this BufferView read only?
        /// </summary>
        /// <returns></returns>
        public bool isReadOnly()
        {
            return m_readOnly;
        }

        /// <summary>
        /// Verify that the cursor and the highlights are within bounds - used when we load
        /// a project and for example the underlying files have changed and we're still holding
        /// stale data about valid positions within these files.
        /// </summary>
        public void verifyBoundaries()
        {
            // Check for no FileBuffer
            //
            if (m_fileBuffer == null)
            {
                return;
            }
            
            // If cursor is less then zero or there are no rows in the file
            //
            if (m_cursorPosition.Y < 0 || m_fileBuffer.getLineCount() == 0)
            {
                m_cursorPosition.Y = 0;
                m_cursorPosition.X = 0;

                // Cancel highlight
                //
                m_highlightStart = m_cursorPosition;
                m_highlightEnd = m_cursorPosition;
            }
            else if (m_cursorPosition.Y >= m_fileBuffer.getLineCount()) // If the cursor position Y is greater than the linecount
            {
                if (m_fileBuffer.getLineCount() > 0)
                {
                    m_cursorPosition.Y = m_fileBuffer.getLineCount() - 1;
                    m_cursorPosition.X = m_fileBuffer.getLine(m_cursorPosition.Y).Length;

                    // Cancel highlight
                    //
                    m_highlightStart = m_cursorPosition;
                    m_highlightEnd = m_cursorPosition;
                }
            }
            else
            {
                // check the row length against cursor position
                string line = m_fileBuffer.getLine(m_cursorPosition.Y);
                if (m_cursorPosition.X > line.Length)
                {
                    m_cursorPosition.X = line.Length;

                    // Cancel highlight
                    //
                    m_highlightStart = m_cursorPosition;
                    m_highlightEnd = m_cursorPosition;
                }
            }
        }

        /// <summary>
        /// From the current cursor position jump a word to the left
        /// </summary>
        public void wordJumpCursorLeft()
        {
            FilePosition fp = m_cursorPosition;
            string line = m_fileBuffer.getLine(fp.Y);

            if (fp.X > 0)
            {
                // Find the nearest space to the current position to the left
                //
                int breakPos = 0;
                for (int i = 0; i < Math.Min(fp.X, line.Length - 1); i++)
                {
                    if (line[i] == ' ' || line[i] == '\t')
                    {
                        breakPos = i;
                    }
                }

                m_cursorPosition.X = breakPos;
            }
        }

        /// <summary>
        /// From the current cursor position jump a word to the right
        /// </summary>
        public void wordJumpCursorRight()
        {
            FilePosition fp = m_cursorPosition;
            string line = m_fileBuffer.getLine(fp.Y);

            if (fp.X < line.Length)
            {
                try
                {
                    int jumpPosition = line.IndexOf(' ', fp.X);

                    if (jumpPosition != -1)
                    {
                        if (fp.X == jumpPosition)
                        {
                            fp.X++;
                        }
                        else
                        {
                            fp.X = jumpPosition;
                        }
                    }
                    else
                    {
                        fp.X = line.Length;
                    }
                }
                catch (Exception /* e */)
                {
                    Logger.logMsg("Friendlier:: couldn't jump");
                    fp.X++;
                }
            }
            else
            {
                fp.X++;
            }
            m_cursorPosition = fp;
        }
    }
}
