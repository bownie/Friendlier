using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace Xyglo
{
    /// <summary>
    /// A view on a buffer - can be independent from a FileBuffer but carries a reference if needed (undecided on this)
    /// </summary>
    [DataContract(Name = "Friendlier", Namespace = "http://www.xyglo.com")]
    public class BufferView : XygloView
    {

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
            public ViewPosition position { get; set; }
        }

        /// <summary>
        /// The FileBuffer associated with this BufferView
        /// </summary>
        [NonSerialized]
        protected FileBuffer m_fileBuffer;

        /// <summary>
        /// Index of the FileBuffer associated with this BufferView so we can reconstruct the link
        /// </summary>
        [DataMember]
        protected int m_fileBufferIndex = 0;

        /// <summary>
        /// What line the buffer is showing from
        /// </summary>
        [DataMember]
        protected int m_bufferShowStartY = 0;

        /// <summary>
        /// The BufferView remembers its own highlight positions
        /// </summary>
        [DataMember]
        protected ScreenPosition m_highlightStart;

        /// <summary>
        /// Store the BufferViewPosition relative to another
        /// </summary>
        [DataMember]
        protected BufferViewPosition m_bufferViewPosition;

        /// <summary>
        /// The BufferView remembers its own highlight positions
        /// </summary>
        [DataMember]
        protected ScreenPosition m_highlightEnd;

        /// <summary>
        /// Where we set the highlight to when there isn't one
        /// </summary>
        [XmlIgnore]
        static public ScreenPosition NoHighlightPosition = new ScreenPosition(-1, -1);

        [DataMember]
        protected int m_bufferShowStartX = 0;

        /// <summary>
        /// Store the cursor coordinates locally
        /// </summary>
        [DataMember]
        protected Vector3 m_cursorCoordinates = new Vector3();

        /// <summary>
        /// Current cursor coordinates in this BufferView
        /// </summary>
        [DataMember]
        protected ScreenPosition m_cursorPosition = new ScreenPosition(0, 0);

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
        /// Tailing colour
        /// </summary>
        [DataMember]
        protected Color m_tailColour = Color.LightBlue;

        /// <summary>
        /// Read only colour
        /// </summary>
        [DataMember]
        protected Color m_readOnlyColour = Color.LightYellow;

        /// <summary>
        /// Is this a non-editable BufferView?
        /// </summary>
        [DataMember]
        protected bool m_readOnly = false;

        /// <summary>
        /// Are we tailing this File?
        /// </summary>
        [DataMember]
        protected bool m_tailing = false;

        /// <summary>
        /// Background colour for this bufferview
        /// </summary>
        [DataMember]
        protected Color m_backgroundColour = Color.Black;  //new Color(0, 191, 255, 10);//Color.DeepSkyBlue;

        /// <summary>
        /// Store the search text per BufferView
        /// </summary>
        [DataMember]
        protected string m_searchText = "";

        /// <summary>
        /// Get my search locations in the BufferView
        /// </summary>
        protected List<int> m_searchLocations = new List<int>();

        /////// CONSTRUCTORS /////////

        /// <summary>
        /// Default constructor for XML
        /// </summary>
        public BufferView()
        {
            if (m_searchText == null)
            {
                m_searchText = "";
            }
        }
       
        /// <summary>
        /// Constructor with just FontManager
        /// </summary>
        /// <param name="fontManager"></param>
        public BufferView(FontManager fontManager)
        {
            if (m_searchText == null)
            {
                m_searchText = "";
            }

            m_fontManager = fontManager;
        }
       
        /// <summary>
        /// Specify a root BufferView and an absolute position
        /// </summary>
        /// <param name="rootBV"></param>
        /// <param name="position"></param>
        public BufferView(FontManager fontManager, BufferView rootBV, Vector3 position, bool readOnly = false)
        {
            m_fileBuffer = rootBV.m_fileBuffer;
            m_bufferShowStartY = rootBV.m_bufferShowStartY;
            m_bufferShowStartX = rootBV.m_bufferShowStartX;
            m_bufferShowLength = rootBV.m_bufferShowLength;
            m_bufferShowWidth = rootBV.m_bufferShowWidth;
            m_position = position;
            m_fileBufferIndex = rootBV.m_fileBufferIndex;
            m_readOnly = rootBV.m_readOnly;
            m_fontManager = fontManager;
        }

        /// <summary>
        /// Constructor specifying everything
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="position"></param>
        /// <param name="bufferShowStartY"></param>
        /// <param name="bufferShowLength"></param>
        public BufferView(FontManager fontManager, FileBuffer buffer, Vector3 position, int bufferShowStartY, int bufferShowLength, int fileIndex, bool readOnly = false)
        {
            m_position = position;
            m_fileBuffer = buffer;
            m_bufferShowStartY = bufferShowStartY;
            m_bufferShowStartX = 0;
            m_bufferShowLength = bufferShowLength;
            m_fileBufferIndex = fileIndex;
            m_readOnly = readOnly;
            m_fontManager = fontManager;
        }

        /// <summary>
        /// Constructor based on an existing buffer view and a relative position
        /// </summary>
        /// <param name="rootBV"></param>
        /// <param name="position"></param>
        public BufferView(FontManager fontManager, BufferView rootBV, ViewPosition position)
        {
            m_fileBuffer = rootBV.m_fileBuffer;
            m_bufferShowStartY = rootBV.m_bufferShowStartY;
            m_bufferShowLength = rootBV.m_bufferShowLength;
            m_bufferShowWidth = rootBV.m_bufferShowWidth;
            m_bufferShowStartX = rootBV.m_bufferShowStartX;
            m_bufferShowStartY = rootBV.m_bufferShowStartY;

            // Store the orientation from another bufferview
            //
            m_bufferViewPosition.position = position;
            m_bufferViewPosition.rootBV = rootBV;
            m_readOnly = rootBV.m_readOnly;

            m_highlightStart = rootBV.m_highlightStart;
            m_highlightEnd = rootBV.m_highlightEnd;

            m_cursorPosition = rootBV.m_cursorPosition;

            m_fontManager = fontManager;

            // Only calculate a position if we have the information necessary to do it
            //
            if (m_fontManager.getCharWidth() != 0 && m_fontManager.getLineSpacing() != 0)
            {
                m_position = rootBV.calculateRelativePositionVector(position);
            }
        }



        //////// METHODS ///////

        /// <summary>
        /// Set our fontmanager
        /// </summary>
        /// <param name="fontManager"></param>
        public void setFontManager(FontManager fontManager)
        {
            m_fontManager = fontManager;
        }


        /// <summary>
        /// When we initialise the project (post deserialisation) we can use this method to reset
        /// any defaults which we don't like any more.  Useful for backwards compatiability and
        /// enforcing new features or colours etc.
        /// </summary>
        public void setDefaults()
        {
            /*
            if (m_backgroundColour == null || m_backgroundColour == Color.Black)
            {
                m_backgroundColour = new Color(0, 191, 255, 190);//Color.DeepSkyBlue;
            }

            m_backgroundColour = Color.Black;
            */


            if (m_searchText == null)
            {
                m_searchText = "";
            }
        }

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
        /// Set the TextColour
        /// </summary>
        /// <param name="colour"></param>
        public void setTextColour(Color colour)
        {
            m_textColour = colour;
        }

        /// <summary>
        /// Set the tailing colour
        /// </summary>
        /// <param name="colour"></param>
        public void setTailColour(Color colour)
        {
            m_tailColour = colour;
        }

        /// <summary>
        /// Set the read only colour
        /// </summary>
        /// <param name="colour"></param>
        public void setReadOnlyColour(Color colour)
        {
            m_readOnlyColour = colour;
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
        public ScreenPosition getHighlightStart()
        {
            return m_highlightStart;
        }

        /// <summary>
        /// Set beginning of the highlight
        /// </summary>
        /// <param name="fp"></param>
        protected void setHighlightStart(ScreenPosition fp)
        {
            m_highlightStart = fp;
            Logger.logMsg("BufferView::setHighlightStart() - starting at X = " + fp.X + ", Y = " + fp.Y);
        }

        /// <summary>
        /// Get end point of the highlight
        /// </summary>
        /// <returns></returns>
        public ScreenPosition getHighlightEnd()
        {
            return m_highlightEnd;
        }

        public void mouseCursorTo(bool shiftDown, ScreenPosition endPosition)
        {
            if (shiftDown)
            {
                if (m_highlightStart == m_highlightEnd)
                {
                    m_highlightStart = m_cursorPosition;
                    m_highlightEnd = endPosition;
                }
                else // Existing highlight
                {
                    if (endPosition > m_highlightEnd)
                    {
                        m_highlightEnd = endPosition;
                    }
                    else
                    {
                        m_highlightStart = endPosition;
                    }
                }
            }
            else
            {
                noHighlight();
            }

            m_cursorPosition = endPosition;
        }
        /// <summary>
        /// Set end point of the highlight
        /// </summary>
        /// <param name="fp"></param>
        protected void setHighlightEnd(ScreenPosition fp)
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
#if DEBUG_HIGHLIGHT
            Logger.logMsg("BufferView::startHighlight() - starting at X = " + m_cursorPosition.X + ", Y = " + m_cursorPosition.Y);
#endif
        }

        /// <summary>
        /// No highlight position i
        /// </summary>
        public void noHighlight()
        {
            m_highlightStart = BufferView.NoHighlightPosition;
            m_highlightEnd = BufferView.NoHighlightPosition;
#if DEBUG_HIGHLIGHT
            Logger.logMsg("BufferView::noHighlight()");
#endif
        }

        /// <summary>
        /// Extend an existing highlight to this position
        /// </summary>
        public void extendHighlight()
        {
            m_highlightEnd = m_cursorPosition;
#if DEBUG_HIGHLIGHT
            Logger.logMsg("BufferView::extendHighlight() - extending at X = " + m_cursorPosition.X + ", Y = " + m_cursorPosition.Y);
#endif
        }

        /// <summary>
        /// Set our highlight
        /// </summary>
        /// <param name="h1"></param>
        /// <param name="h2"></param>
        public void setHighlight(ScreenPosition h1, ScreenPosition h2)
        {
            m_highlightStart = h1;
            m_highlightEnd = h2;
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
            return (m_bufferShowWidth * m_fontManager.getCharWidth());
        }

        /// <summary>
        /// Visible height of 'window'
        /// </summary>
        /// <returns></returns>
        public float getVisibleHeight()
        {
            return (m_bufferShowLength * m_fontManager.getLineSpacing());
        }

        /// <summary>
        /// Cursor coordinates in 3D - adjust for offsetting with the buffershowStart values
        /// </summary>
        public Vector3 getCursorCoordinates()
        {
            // Return the cursor coordinates taking into account paging using m_bufferShowStartY
            //
            m_cursorCoordinates.X = m_position.X + (m_cursorPosition.X - m_bufferShowStartX) * m_fontManager.getCharWidth();
            m_cursorCoordinates.Y = m_position.Y + (m_cursorPosition.Y - m_bufferShowStartY) * m_fontManager.getLineSpacing();

            return m_cursorCoordinates;
        }

        /// <summary>
        /// Get the Space coordinates from the 
        /// </summary>
        /// <param name="sp"></param>
        /// <returns></returns>
        public Vector3 getSpaceCoordinates(ScreenPosition sp)
        {
            Vector3 rV = new Vector3(m_position.X + sp.X * m_fontManager.getCharWidth(), m_position.Y + sp.Y * m_fontManager.getLineSpacing(), 0);
            return rV;
        }

        /// <summary>
        /// Page up the BufferView
        /// </summary>
        public void pageUp(Project project)
        {
            // Page up the BufferView position
            //
            m_bufferShowStartY -= m_bufferShowLength;

            if (m_bufferShowStartY < 0)
            {
                m_bufferShowStartY = 0;
            }

            // Handle tailing without cursor
            //
            if (m_tailing)
            {
                return;
            }

            // Page up the cursor
            m_cursorPosition.Y -= m_bufferShowLength;

            if (m_cursorPosition.Y < 0)
            {
                m_cursorPosition.Y = 0;
            }

            string line = m_fileBuffer.getLine(m_cursorPosition.Y).Replace("\t", project.getTab());
            if (m_cursorPosition.X > line.Length)
            {
                m_cursorPosition.X = line.Length;
            }
        }

        /// <summary>
        /// Page down the BufferView
        /// </summary>
        public void pageDown(Project project)
        {
            // Page down the buffer view position
            m_bufferShowStartY += m_bufferShowLength;

            if (m_bufferShowStartY > m_fileBuffer.getLineCount() - 1)
            {
                m_bufferShowStartY = m_fileBuffer.getLineCount() - 1;
            }

            // Handle tailing without cursor
            //
            if (m_tailing)
            {
                return;
            }

            // Page down the cursor
            //
            m_cursorPosition.Y += m_bufferShowLength;

            if (m_cursorPosition.Y > m_fileBuffer.getLineCount() - 1)
            {
                m_cursorPosition.Y = m_fileBuffer.getLineCount() - 1;
            }

            string line = m_fileBuffer.getLine(m_cursorPosition.Y).Replace("\t", project.getTab());
            if (m_cursorPosition.X > line.Length)
            {
                m_cursorPosition.X = line.Length;
            }

        }

        /// <summary>
        /// Set position that we're showing the BufferView from.
        /// </summary>
        /// <param name="bss"></param>
        /// <returns></returns>
        public void setBufferShowStartY(int bss)
        {
            if (bss < 0)
            {
                m_bufferShowStartY = 0;
            }
            else if (bss < m_fileBuffer.getLineCount())
            {
                m_bufferShowStartY = bss;
            }

            // Ensure that the cursor is still on the page
            //
            keepVisible();
        }

        /// <summary>
        /// Is this view locked?
        /// </summary>
        /// <returns></returns>
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
        public ScreenPosition getRelativeCursorPosition()
        {
            return m_cursorPosition;
        }

        /// <summary>
        /// Cursor position is position in the FileBuffer - not in the visible BufferView
        /// </summary>
        /// <returns></returns>
        public ScreenPosition getCursorPosition()
        {
            return m_cursorPosition;
        }

        /// <summary>
        /// Return the text of the current line we are on
        /// </summary>
        /// <returns></returns>
        public string getCurrentLine()
        {
            string rs = "";

            if (m_fileBuffer != null)
            {
                try
                {
                    rs = m_fileBuffer.getLine(m_cursorPosition.Y);
                }
                catch (Exception e)
                {
                    Logger.logMsg("BufferView::getCurrentLine() - can't get line " + m_cursorPosition.Y + "(" + e.Message + ")");
                }
            }

            return rs;
        }

        /// <summary>
        /// Get the first non-space on a given line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public int getFirstNonSpace(Project project, int lineNumber)
        {
            if (lineNumber < 0 || lineNumber >= m_fileBuffer.getLineCount())
            {
                return -1;
            }

            string line = m_fileBuffer.getLine(lineNumber).Replace("\t", project.getTab());

            int pos = 0;
            while (pos < line.Length)
            {
                if (!Char.IsWhiteSpace(line[pos]))
                {
                    break;
                }
                pos++;
            }

            if (pos == line.Length)
            {
                return -1;
            }

            return pos;
        }

        /// <summary>
        /// Return the position of the first non space character on the current line
        /// </summary>
        /// <returns></returns>
        public ScreenPosition getFirstNonSpace(Project project)
        {
            string line = m_fileBuffer.getLine(m_cursorPosition.Y).Replace("\t", project.getTab());

            int pos = 0;
            while (pos < m_cursorPosition.X)
            {
                if (!Char.IsWhiteSpace(line[pos]))
                {
                    break;
                }
                pos++;
            }

            // If from the start of line to cursor position is white space then send to beginning of line (second
            // tap on the home button).
            //
            if (pos == m_cursorPosition.X)
            {
                pos = 0;
            }

            return new ScreenPosition(pos, m_cursorPosition.Y);
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
        /// Is a given FilePosition valid in this BufferView?  i.e. is there any text there?
        /// </summary>
        /// <param name="fp"></param>
        public bool testCursorPosition(ScreenPosition fp)
        {
            if (fp.Y >= 0 && fp.Y < m_fileBuffer.getLineCount())
            {
                string line = m_fileBuffer.getLine(fp.Y);

                if (fp.X <= line.Length)
                {
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// Set the cursor position in this view
        /// </summary>
        /// <param name="fp"></param>
        public void setCursorPosition(ScreenPosition fp)
        {
            // Do nothing if tailing
            //
            if (m_tailing)
            {
                return;
            }

            if (fp.Y >= 0 && fp.Y < m_fileBuffer.getLineCount() || m_fileBuffer.getLineCount() == 0)
            {
                m_cursorPosition = fp;
                keepVisible();

                Logger.logMsg("BufferView::setCursorPosition() - fp.X = " + fp.X + ", fp.Y = " + fp.Y);
            }
            else
            {
                throw new Exception("BufferView::setCursorPosition() - can't set the cursor position");
            }
        }

        /// <summary>
        /// Set cursor position from a Vector2
        /// </summary>
        /// <param name="vfp"></param>
        public void setCursorPosition(Vector2 vfp)
        {
            // Do nothing if tailing
            //
            if (m_tailing)
            {
                return;
            }

            int x = Convert.ToInt16(vfp.X);
            int y = Convert.ToInt16(vfp.Y);
            m_cursorPosition.X = x;
            m_cursorPosition.Y = y;

            // Keep the cursor visible
            //
            keepVisible();
        }

        /// <summary>
        /// Get the line height
        /// </summary>
        /// <returns></returns>
        public float getLineSpacing()
        {
            return m_fontManager.getLineSpacing();
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
            if (m_fontManager.getCharWidth() == 0 || m_fontManager.getLineSpacing() == 0)
            {
                return;
            }

            // Reach through to the rootBV and calculate our position from there
            //
            if (m_bufferViewPosition.rootBV != null)
            {
                m_position = m_bufferViewPosition.rootBV.calculateRelativePositionVector(m_bufferViewPosition.position);
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
            rV.X += m_fontManager.getCharWidth() * m_bufferShowWidth / 2;
            rV.Y -= m_fontManager.getLineSpacing() * m_bufferShowLength / 2;
            rV.Z = 0.0f;
            return rV;
        }

        /// <summary>
        /// Return the eye vector of the centre of this BufferView
        /// </summary>
        /// <returns></returns>
        public override Vector3 getEyePosition()
        {
            Vector3 rV = m_position;
            rV.Y = -rV.Y; // invert Y
            rV.X += m_fontManager.getCharWidth() * m_bufferShowWidth / 2;
            rV.Y -= m_fontManager.getLineSpacing() * m_bufferShowLength / 2;
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
        public override Vector3 getEyePosition(float zoomLevel)
        {
            Vector3 rV = m_position;

            rV.Y = -rV.Y; // invert Y
            rV.X += m_fontManager.getCharWidth() * m_bufferShowWidth / 2;
            rV.Y -= m_fontManager.getLineSpacing() * m_bufferShowLength / 2;
            rV.Z = zoomLevel;

#if QUADRANT_ZOOMING
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
#endif // QUADRANT_ZOOMING

            return rV;
        }


        /// <summary>
        /// Return the coordinates of the highlighted area use a list of bounding boxes
        /// </summary>
        /// <param name="highlightStart"></param>
        /// <param name="highlightEnd"></param>
        public List<BoundingBox> computeHighlight(Project project)
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

            // Maximum and minimum possible positions of our highlight blocks as we don't
            // want to spill out over the end of the line.
            //
            float minPosX = m_position.X;
            float maxPosX = m_position.X + m_bufferShowWidth * m_fontManager.getCharWidth();

            if (m_highlightStart.Y == m_highlightEnd.Y)
            {
                // Set start position
                //
                startPos.X = m_position.X + (m_highlightStart.X - m_bufferShowStartX) * m_fontManager.getCharWidth();
                startPos.Y = m_position.Y + (m_highlightStart.Y - m_bufferShowStartY) * m_fontManager.getLineSpacing();

                // Set end position
                //
                endPos.X = m_position.X + (m_highlightEnd.X - m_bufferShowStartX) * m_fontManager.getCharWidth();
                endPos.Y = m_position.Y + (m_highlightEnd.Y + 1 - m_bufferShowStartY) * m_fontManager.getLineSpacing();

                // Adjust ends
                //
                if (startPos.X < minPosX)
                {
                    startPos.X = minPosX;
                }
                
                if (endPos.X > maxPosX)
                {
                    endPos.X = maxPosX;
                }

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

                // Bear in mind we only need to calculate the visible highlight here - so only
                // the stuff on the screen hence the Math.Min:
                //
                for (int i = minStart; i < Math.Min(m_highlightEnd.Y + 1, m_bufferShowStartY + m_bufferShowLength); i++)
                {
                    if (i == m_highlightStart.Y)
                    {
                        if (fullLineAtStart)
                        {
                            startPos.X = m_position.X;
                        }
                        else
                        {
                            startPos.X = m_position.X + m_highlightStart.X * m_fontManager.getCharWidth();
                        }
                        endPos.X = m_position.X + m_fileBuffer.getLine(i).Replace("\t", project.getTab()).Length * m_fontManager.getCharWidth();
                    }
                    else if (i == m_highlightEnd.Y)
                    {
                        startPos.X = m_position.X;
                        endPos.X = m_position.X + m_highlightEnd.X * m_fontManager.getCharWidth();
                    }
                    else
                    {
                        startPos.X = m_position.X;
                        endPos.X = m_position.X + m_fileBuffer.getLine(i).Replace("\t", project.getTab()).Length * m_fontManager.getCharWidth();
                    }

                    startPos.Y = m_position.Y + (i - m_bufferShowStartY) * m_fontManager.getLineSpacing();
                    endPos.Y = m_position.Y + (i + 1 - m_bufferShowStartY) * m_fontManager.getLineSpacing();

                    // If we have nothing highlighted in the line then indicate this with a
                    // half box line
                    //
                    if (startPos.X == endPos.X && endPos.X == m_position.X)
                    {
                        startPos.X += m_fontManager.getCharWidth() / 2;
                    }

                    // Adjust ends
                    //
                    if (startPos.X < minPosX)
                    {
                        startPos.X = minPosX;
                    }

                    if (endPos.X > maxPosX)
                    {
                        endPos.X = maxPosX;
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
                            startPos.X = m_position.X + m_cursorPosition.X * m_fontManager.getCharWidth();
                        }
                        endPos.X = m_position.X + m_fileBuffer.getLine(i).Replace("\t", project.getTab()).Length * m_fontManager.getCharWidth();
                    }
                    else if (i == maxStart)
                    {
                        startPos.X = m_position.X;

                        if (fullLineAtEnd)
                        {
                            endPos.X = m_position.X + m_fileBuffer.getLine(i).Replace("\t", project.getTab()).Length * m_fontManager.getCharWidth();
                        }
                        else
                        {
                            endPos.X = m_position.X + m_highlightStart.X * m_fontManager.getCharWidth();
                        }
                    }
                    else
                    {
                        startPos.X = m_position.X;
                        endPos.X = m_position.X + m_fileBuffer.getLine(i).Replace("\t", project.getTab()).Length * m_fontManager.getCharWidth();
                    }

                    startPos.Y = m_position.Y + (i - m_bufferShowStartY) * m_fontManager.getLineSpacing();
                    endPos.Y = m_position.Y + (i + 1 - m_bufferShowStartY) * m_fontManager.getLineSpacing();

                    // If we have nothing highlighted in the line then indicate this with a negative
                    // half box line
                    //
                    if (startPos.X == endPos.X && endPos.X == m_position.X)
                    {
                        startPos.X += m_fontManager.getCharWidth() / 2;
                    }

                    // Adjust ends
                    //
                    if (startPos.X < minPosX)
                    {
                        startPos.X = minPosX;
                    }

                    if (endPos.X > maxPosX)
                    {
                        endPos.X = maxPosX;
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
        public void moveCursorUp(Project project, bool leftCursor)
        {
            // Move start position if tailing
            //
            if (m_tailing)
            {
                if (m_bufferShowStartY > 0)
                {
                    m_bufferShowStartY--;
                }

                return;
            }

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

                string line = m_fileBuffer.getLine(m_cursorPosition.Y).Replace("\t", project.getTab());
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
            keepVisible();
        }

        /// <summary>
        /// When we want to move the cursor down in the BufferView we're either doing this because the user
        /// wants to go down or wants to go off the end of the line (right)
        /// </summary>
        /// <param name="rightCursor"></param>
        public void moveCursorDown(bool rightCursor)
        {
            // Move the view position if tailing
            //
            if (m_tailing)
            {
                if (m_bufferShowStartY < m_fileBuffer.getLineCount())
                {
                    m_bufferShowStartY++;
                }
                return;
            }

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
                    m_bufferShowStartX = 0;
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
        public void deleteCurrentSelection(Project project)
        {
            m_fileBuffer.deleteSelection(project, screenToFilePosition(project, m_highlightStart), screenToFilePosition(project, m_highlightEnd), m_highlightStart, m_highlightEnd);

            if (m_highlightStart < m_highlightEnd)
            {
                m_cursorPosition = m_highlightStart;
            }
            else
            {
                m_cursorPosition = m_highlightEnd;
            }

            // Update the syntax highlighting
            //
            project.getSyntaxManager().updateHighlighting(m_fileBuffer /*, m_cursorPosition.Y */ );

            // Cancel our highlight
            //
            noHighlight();
        }

        /// <summary>
        /// Delete a single character at the cursor
        /// </summary>
        public void deleteSingle(Project project)
        {
            if (m_fileBuffer.getLineCount() == 0)
                return;

            m_fileBuffer.deleteSelection(project, screenToFilePosition(project, m_cursorPosition), screenToFilePosition(project, m_cursorPosition), m_highlightStart, m_highlightEnd);

            // Update the syntax highlighting
            //
            project. getSyntaxManager().updateHighlighting(m_fileBuffer /*, m_cursorPosition.Y*/);
        }

        /// <summary>
        /// Replace a highlighted selection with some text (can be multi-line)
        /// </summary>
        /// <param name="text"></param>
        public void replaceCurrentSelection(Project project, string text)
        {
            m_cursorPosition = m_fileBuffer.replaceText(project, screenToFilePosition(project, m_highlightStart), screenToFilePosition(project, m_highlightEnd), text, m_highlightStart, m_highlightEnd);

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
        /// Ensure that the cursor is on the screen
        /// </summary>
        protected void keepVisible()
        {
            // Ensure Y is visible
            //
            if (m_cursorPosition.Y < m_bufferShowStartY)
            {
                m_bufferShowStartY = m_cursorPosition.Y;
            }
            else if (m_cursorPosition.Y > (m_bufferShowStartY + m_bufferShowLength - 1))
            {
                m_bufferShowStartY = m_cursorPosition.Y - (m_bufferShowLength - 1);
            }

            // Ensure X is visible
            //
            if (m_cursorPosition.X < m_bufferShowStartX)
            {
                m_bufferShowStartX = m_cursorPosition.X;
            }
            else if (m_cursorPosition.X > (m_bufferShowStartX + m_bufferShowWidth - 1))
            {
                m_bufferShowStartX = m_cursorPosition.X - (m_bufferShowWidth - 1);
            }
        }

        /// <summary>
        /// Set the tailing position at the end of the file
        /// </summary>
        public void setTailPosition()
        {
            if (m_fileBuffer != null)
            {
                m_bufferShowStartY = m_fileBuffer.getLineCount() - m_bufferShowLength;
            }

        }

        /// <summary>
        /// Insert some text into the BufferView - we translate the screen position into a file position
        /// when we generate the command but it returns a screen position (as we pass in the project to 
        /// work out tab spaces).
        /// </summary>
        /// <param name="text"></param>
        public void insertText(Project project, string text)
        {
            m_cursorPosition = m_fileBuffer.insertText(project, screenToFilePosition(project), m_highlightStart, m_highlightEnd, text);

            // Update the syntax highlighting
            //
            project.getSyntaxManager().updateHighlighting(m_fileBuffer /*, m_cursorPosition.Y */);

            // Keep visible
            //
            keepVisible();
        }

        /// <summary>
        /// Get the current implied indent level
        /// </summary>
        /// <returns></returns>
        protected string getImpliedIndent(Project project, int line)
        {
            string rs = "";
            int lineBefore = getFirstNonSpace(project, line);
            int lineAfter = getFirstNonSpace(project, line + 1);

            if (lineBefore != -1 && lineAfter != -1 && lineBefore == lineAfter)
            {
                // Build indent
                //
                for (int i = 0; i < lineBefore; i++)
                {
                    rs += " ";
                }
            }

            return rs;
        }

        /// <summary>
        /// Insert a new line at the cursor
        /// </summary>
        public void insertNewLine(Project project, string autoindent)
        {
            // Define an indent
            //
            string indent = "";

            // Detect if we want to apply autoindent and calculate it as necessary
            //
            if (autoindent == "TRUE")
            {
                /*
                string testLine = m_fileBuffer.getLine(m_cursorPosition.Y);

                for (int i = 0; i < testLine.Length; i++)
                {
                    if (testLine[i] == ' ')
                    {
                        indent += ' ';
                    }
                    else
                    {
                        break;
                    }
                }*/


                try
                {
                    // Use the Syntax Manager to fetch an indent
                    //
                    indent = project.getSyntaxManager().getIndent(screenToFilePosition(project, m_cursorPosition));
                }
                catch (Exception)
                {
                    Logger.logMsg("BufferView::insertNewLine() - failed to get the indent");
                }
            }

            // Test current line for previous and next line indent levels - if they're the
            // same we should also use that level in preference to the one in SyntaxManager
            //
            string impliedIndent = getImpliedIndent(project, m_cursorPosition.Y);

            // If the implied indent is different we prefer that
            //
            if (impliedIndent != "" && impliedIndent.Length > indent.Length)
            {
                indent = impliedIndent;
            }

            FilePosition fP = screenToFilePosition(project);
            m_cursorPosition = m_fileBuffer.insertNewLine(project, fP, m_highlightStart, m_highlightEnd, indent);

            // Update the syntax highlighting
            //
            project.getSyntaxManager().updateHighlighting(m_fileBuffer /*, m_cursorPosition.Y */);

            keepVisible();
        }

        /// <summary>
        /// Return the text of the currently highlighted selection so we can put it in
        /// a cut and paste buffer for example.
        /// </summary>
        /// <returns></returns>
        public TextSnippet getSelection()
        {
            TextSnippet rS = new TextSnippet();

            ScreenPosition shiftStart = m_highlightStart;
            ScreenPosition shiftEnd = m_highlightEnd;

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
                if (m_fileBuffer.getLineCount() > 0)
                {
                    line = m_fileBuffer.getLine(shiftStart.Y).Substring(shiftStart.X, shiftEnd.X - shiftStart.X);
                    rS.setSnippetSingle(line);
                }
                else
                {
                    rS.setSnippetSingle("");
                }
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
            ScreenPosition fp = m_cursorPosition;
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
            ScreenPosition fp = m_cursorPosition;
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

        /// <summary>
        /// Highlight everything in the FileBuffer
        /// </summary>
        public void selectAll()
        {
            m_highlightStart.X = 0;
            m_highlightStart.Y = 0;

            if (m_fileBuffer != null && m_fileBuffer.getLineCount() > 0)
            {
                string line = m_fileBuffer.getLine(m_fileBuffer.getLineCount() - 1);
                m_highlightEnd.X = line.Length;
                m_highlightEnd.Y = m_fileBuffer.getLineCount() - 1;
            }
            else
            {
                m_highlightEnd.X = 0;
                m_highlightEnd.Y = 0;
            }
        }

        /// <summary>
        /// Rturn the locations at which we've found stuffe
        /// </summary>
        /// <returns></returns>
        public List<int> getFindLocations()
        {
            return m_searchLocations;
        }

        /// <summary>
        /// Update the locations of the search string so we can provide our preview
        /// </summary>
        /// <param name="text"></param>
        protected void updateFindLocations(string text)
        {
            m_searchLocations.Clear();
        }

        /// <summary>
        /// Find the m_searchText and move the cursor and highlight it
        /// </summary>
        /// <param name="text"></param>
        public bool find()
        {
            ScreenPosition searchPos = m_cursorPosition;
            bool searching = true;

            if (m_fileBuffer.getLineCount() == 0)
            {
                return false;
            }

            while (searching)
            {
                string line = m_fileBuffer.getLine(searchPos.Y);

                if (searchPos.X > 0)
                {
                    line = line.Substring(searchPos.X, line.Length - searchPos.X);
                }

                // Ensure that 
                int findPos = line.IndexOf(m_searchText);

                if (findPos != -1) // found something
                {
                    // Adjust to real position
                    //
                    findPos += searchPos.X;

                    // If we're searching from the start of the phrase we're looking for then
                    // carry on - ignore an immediate match.
                    //
                    if (!(searchPos.Y == m_cursorPosition.Y && findPos == m_cursorPosition.X))
                    {
                        // Then jump there
                        //
                        searchPos.X = findPos;
                        m_cursorPosition = searchPos;
                        keepVisible();

                        // Set the highlight
                        //
                        m_highlightStart = searchPos;
                        m_highlightEnd = searchPos;
                        m_highlightEnd.X += m_searchText.Length;

                        return true;
                    }
                }

                // Increment the line
                //
                searchPos.Y++;

                // If we search a second line (or more) we always start at the beginning
                if (searchPos.X > 0)
                {
                    searchPos.X = 0;
                }

                if (searchPos.Y > m_fileBuffer.getLineCount() - 1)
                {
                    searching = false;
                }
            }

            return false;
        }

        /// <summary>
        /// Return the search text
        /// </summary>
        /// <returns></returns>
        public string getSearchText()
        {
            return m_searchText;
        }

        /// <summary>
        /// Set the search text
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public void setSearchText(string text)
        {
            m_searchText = text;
        }
        
        /// <summary>
        /// Append a string to the search text
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public void appendToSearchText(string text)
        {
            m_searchText += text;
        }


        /// <summary>
        /// Provide a nice interface for undoing things
        /// </summary>
        /// <param name="steps"></param>
        public void undo(SyntaxManager syntaxManager, int steps = 1)
        {
            Pair<ScreenPosition, Pair<ScreenPosition, ScreenPosition>> rP = m_fileBuffer.undo(steps);

            // Set cursor position to the first
            //
            m_cursorPosition = rP.First;

            // Remove any highlight
            //
            m_highlightStart.X = 0;
            m_highlightStart.Y = 0;
            m_highlightEnd = m_highlightStart;

            // Update the syntax highlighting
            //
            syntaxManager.updateHighlighting(m_fileBuffer /*, m_cursorPosition.Y */);

            // Ensure that the cursor is visible in the BufferView
            //
            keepVisible();

            // Fix any highlighting that was included in the command
            //
            if (rP.Second != null)
            {
                // Use the highlight information return from the undo
                //
                if (rP.Second.First.X != -1 && rP.Second.First.Y != -1)
                {
                    m_highlightStart = rP.Second.First;
                }

                if (rP.Second.Second.X != -1 && rP.Second.Second.Y != -1)
                {
                    m_highlightEnd = rP.Second.Second;
                }
            }
        }

        /// <summary>
        /// Redo a certain number of commands
        /// </summary>
        /// <param name="syntaxManager"></param>
        /// <param name="steps"></param>
        public void redo(SyntaxManager syntaxManager, int steps = 1)
        {
            Pair<ScreenPosition, Pair<ScreenPosition, ScreenPosition>> rP = m_fileBuffer.redo(1);

            m_cursorPosition = rP.First;

            // Remove any highlight
            //
            m_highlightStart.X = 0;
            m_highlightStart.Y = 0;
            m_highlightEnd = m_highlightStart;

            // Update the syntax highlighting
            //
            syntaxManager.updateHighlighting(m_fileBuffer /*, m_cursorPosition.Y */);

            // Ensure that the cursor is visible in the BufferView
            //
            keepVisible();

            // Fix any highlighting that was included in the command
            //
            if (rP.Second != null)
            {
                // Use the highlight information return from the undo
                //
                if (rP.Second.First.X != -1 && rP.Second.First.Y != -1)
                {
                    m_highlightStart = rP.Second.First;
                }

                if (rP.Second.Second.X != -1 && rP.Second.Second.Y != -1)
                {
                    m_highlightEnd = rP.Second.Second;
                }
            }
        }


        /// <summary>
        /// Get the real position in the file of the X cursor compensating for any tabs - just wraps
        /// the project screenToFile method.
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        protected int screenToFileX(Project project)
        {
            // Only fetch the line if we have one to fetch
            //
            if (m_cursorPosition.Y == 0 && m_fileBuffer.getLineCount() == 0)
            {
                return m_cursorPosition.X;
            }

            // Fetch the line and expand tabs for screenLine using project helper
            //
            string line = m_fileBuffer.getLine(m_cursorPosition.Y);

            return project.screenToFile(line, m_cursorPosition.X);
        }

        /// <summary>
        /// Compensate for any tabs when converting from screen position to file position
        /// </summary>
        public FilePosition screenToFilePosition(Project project)
        {
            
            int x = screenToFileX(project);
            int y = m_cursorPosition.Y;

            return new FilePosition(x, y);
        }

        /// <summary>
        /// ScreenPosition to FilePosition using the project helper
        /// </summary>
        /// <param name="project"></param>
        /// <param name="sP"></param>
        /// <returns></returns>
        public FilePosition screenToFilePosition(Project project, ScreenPosition sP)
        {
            // Only fetch the line if we have one to fetch
            //
            if (sP.Y != 0 && sP.Y >= m_fileBuffer.getLineCount())
            {
                throw new Exception("BufferView::screenToFilePosition() - cannot fetch line " + sP.Y);
            }

            // Fetch the line and expand tabs for screenLine using project helper
            //
            string getLine = m_fileBuffer.getLine(sP.Y);

            // Create return type and populate
            //
            FilePosition fP = new FilePosition(sP);
            fP.X = project.screenToFile(getLine, sP.X);

            return fP;
        }


        /// <summary>
        /// Move the cursor right - taking into account end of lines, not fitting on screen, wrapping
        /// etc. etc.
        /// </summary>
        /// <param name="steps"></param>
        public void moveCursorRight(Project project, int steps = 1)
        {
            // Test for empty file firstly
            //
            if (m_fileBuffer == null || m_fileBuffer.getLineCount() == 0)
            {
                return;
            }

            string line = m_fileBuffer.getLine(m_cursorPosition.Y);

            int preLength = line.Length;

            // We always need total number of tabs in the line to allow for adjustments
            //
            int lineLength = line.Replace("\t", project.getTab()).Length;

            // Add numTabs to the line.Length for all calculations
            //
            if (m_cursorPosition.X + steps < lineLength)
            {
                int numTabs = line.Substring(screenToFileX(project), steps).Where(item => item == '\t').Count();

                // Adjust movement by number of tabs we've found
                //
                m_cursorPosition.X += (steps - numTabs) + (numTabs * project.getTab().Length);

                // Incremement the buffer show start position if we're going over the visible end
                // of the line.
                //
                if (m_cursorPosition.X - m_bufferShowStartX >= m_bufferShowWidth)
                {
                    m_bufferShowStartX += steps;
                }
            }
            else
            {
                moveCursorDown(true); // use the built-in method for this
            }
        }

        /// <summary>
        /// Move the cursor left a number of steps
        /// </summary>
        /// <param name="steps"></param>
        public void moveCursorLeft(Project project, int steps = 1)
        {
            // Test for empty file firstly
            //
            if (m_fileBuffer == null || m_fileBuffer.getLineCount() == 0)
            {
                return;
            }

            string line = m_fileBuffer.getLine(m_cursorPosition.Y);
            
            // We always need total number of tabs in the line to allow for adjustments
            //
            int lineLength = line.Replace("\t", project.getTab()).Length;

            if (m_cursorPosition.X - steps > 0 && m_cursorPosition.X - steps < lineLength)
            {
                // Get number of tabs within our movement string
                //
                int numTabs = line.Substring(screenToFileX(project) - steps, steps).Where(item => item == '\t').Count();

                // Add extra tab steps in here
                //
                steps += numTabs;
            }

            if (m_cursorPosition.X > 0)
            {
                m_cursorPosition.X -= steps;
            }
            else
            {
                if (m_bufferShowStartX > 0) // reduce the buffer start X
                {
                    m_bufferShowStartX -= steps;
                }
                else
                {
                    if (m_cursorPosition.Y > 0) // reduce the line we're on if we can
                    {
                        moveCursorUp(project, true);
                        //m_cursorPosition.Y--;
                        //m_cursorPosition.X = m_fileBuffer.getLine(m_cursorPosition.Y).Length;
                    }
                }
            }
        }

        /// <summary>
        /// This store the place in a WrappedEndoFBuffer call that the old log file
        /// becomes the new log file.
        /// </summary>
        protected int m_logRunTerminator = -1;

        /// <summary>
        /// Get the terminator between old and new log file for wrapped lines
        /// </summary>
        /// <returns></returns>
        public int getLogRunTerminator()
        {
            return m_logRunTerminator;
        }

        /// <summary>
        /// Map of FileBuffer line to Wrapped line position - we use this to work backwards from mouse clicks
        /// </summary>
        protected Dictionary<int, int> m_wrappedMap = null;

        /// <summary>
        /// Adjustment to our m_wrappedMap to use when calculating file position
        /// </summary>
        protected int m_wrapAdjustment = 0;

        /// <summary>
        /// When we're autowrapping a BufferView we'll need to work out in advance how many rows
        /// we need to use to return a given position.  We do this for the end of the file by working
        /// backwards from the last rows upwards.   We can also set the m_logRunTerminator as this
        /// point to highlight differences between a current run with this log file (potentially)
        /// and a new one.   Not that there is a mapping between FilePosition and ScreenPosition here.
        /// </summary>
        /// <returns></returns>
        public List<string> getWrappedEndofBuffer(int lastRunPosition = -1)
        {
            // Our return list
            //
            List<string> rS = new List<string>();

            // We need a file buffer for this
            //
            if (m_fileBuffer == null)
            {
                return rS;
            }

            // We keep on going until we've filled the buffer or we're at the first line
            //
            //int lineNumber = Math.Max(0, m_fileBuffer.getLineCount() - m_bufferShowLength);
            int lineNumber = Math.Max(0, m_bufferShowStartY);
            if (m_bufferShowStartY == m_fileBuffer.getLineCount())
            {
                lineNumber = Math.Max(0, m_fileBuffer.getLineCount() - m_bufferShowLength);
            }

            // Set logRunTerminator to not active
            //
            m_logRunTerminator = -1;

            // We may have a terminator to position if this is true
            if (lineNumber < lastRunPosition)
            {
                m_logRunTerminator = 0;
            }

            // Initialise the wrapped map the first time we check this
            //
            if (m_wrappedMap == null)
            {
                m_wrappedMap = new Dictionary<int, int>();
            }

            // Clear wrapped map
            //
            m_wrappedMap.Clear();

            while (lineNumber < Math.Min(m_bufferShowStartY + m_bufferShowLength, m_fileBuffer.getLineCount()))
            {
                string fetchLine = m_fileBuffer.getLine(lineNumber);

                if (fetchLine.Length <= m_bufferShowWidth)
                {
                    m_wrappedMap.Add(rS.Count(), lineNumber);
                    rS.Add(fetchLine);
                }
                else
                {
                    string splitLine = fetchLine;
                    string addLine;

                    while (splitLine.Length > m_bufferShowWidth)
                    {
                        addLine = splitLine.Substring(0, Math.Min(m_bufferShowWidth, splitLine.Length));
                        m_wrappedMap.Add(rS.Count(), lineNumber);
                        rS.Add(addLine);

                        // Reset split line and decrement line number
                        //
                        splitLine = splitLine.Substring(m_bufferShowWidth, splitLine.Length - m_bufferShowWidth);
                    }

                    m_wrappedMap.Add(rS.Count(), lineNumber);
                    rS.Add(splitLine);
                }

                // Increment the m_logRunTerminator until the lineNumber exceeds the last run position
                if (lineNumber < lastRunPosition)
                {
                    m_logRunTerminator = rS.Count();
                }

                // Decrement line number
                //
                lineNumber++;
            }

            // Trim off any rows at the beginning that are overflowing the length of the
            // visible buffer - this can occur of course because we don't know the length
            // of the lines before we split it (we're doing this dumbly rather than smartly).
            //
            if (rS.Count > m_bufferShowLength)
            {
                int adjustLength = rS.Count - m_bufferShowLength;

                // We need to adjust the m_logRunTerminator accordingly
                //
                m_logRunTerminator -= adjustLength;

                // Now remove the range that we won't display
                //
                rS.RemoveRange(0, adjustLength);

                // We also have to adjust the m_wrappedMap key by the number of steps 
                //
                m_wrapAdjustment = adjustLength;
            }

            return rS;
        }

        /// <summary>
        /// Get the real line for a wrapped line
        /// </summary>
        /// <param name="wrappedLine"></param>
        /// <returns></returns>
        public int convertWrappedLineToFileLine(int wrappedLine)
        {
            try
            {
                var result = m_wrappedMap.Where(item => item.Key == wrappedLine + m_wrapAdjustment).First();

                string line = m_fileBuffer.getLine(result.Value);
                return result.Value;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        /// <summary>
        /// Top left position - same as position
        /// </summary>
        /// <returns></returns>
        public Vector3 getTopLeft()
        {
            return m_position;
        }

        /// <summary>
        /// Bottom right screen position
        /// </summary>
        /// <returns></returns>
        public Vector3 getBottomRight()
        {
            Vector3 rP = m_position;
            rP.X += getVisibleWidth();
            rP.Y += getVisibleHeight();
            return rP;
        }

        /// <summary>
        /// Bounding box defined by getTopLeft() and getBottomRight() methods
        /// </summary>
        /// <returns></returns>
        public BoundingBox getBoundingBox()
        {
            return new BoundingBox(getTopLeft(), getBottomRight());
        }

        /// <summary>
        /// Background colour
        /// </summary>
        /// <returns></returns>
        public Color getBackgroundColour()
        {
            return m_backgroundColour;
        }

        /// <summary>
        /// Set the background colour
        /// </summary>
        /// <param name="colour"></param>
        public void setBackgroundColour(Color colour)
        {
            m_backgroundColour = colour;
        }

        /// <summary>
        /// BufferView width defined by font size
        /// </summary>
        /// <returns></returns>
        public override float getWidth()
        {
            return m_fontManager.getCharWidth() * m_bufferShowWidth;
        }

        /// <summary>
        /// BufferView height defined by font size
        /// </summary>
        /// <returns></returns>
        public override float getHeight()
        {
            return m_fontManager.getLineSpacing() * m_bufferShowLength;
        }

        /// <summary>
        /// We have no depth as a BufferView
        /// </summary>
        /// <returns></returns>
        public override float getDepth()
        {
            return 0.0f;
        }

        /// <summary>
        /// This is not implemented yet at this level
        /// </summary>
        public override void draw(Project project, FriendlierState state, GameTime gameTime, SpriteBatch spriteBatch, Effect effect)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Draw textures such as highlights
        /// </summary>
        /// <param name="effect"></param>
        public override void drawTextures(Effect effect)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calculate a bounding box
        /// </summary>
        /// <param name="position"></param>
        /// <param name="factor"></param>
        /// <returns></returns>
        public override BoundingBox calculateRelativePositionBoundingBox(ViewPosition position, int factor = 1)
        {
            BoundingBox bb = new BoundingBox();
            bb.Min = calculateRelativePositionVector(position, factor);
            bb.Max = bb.Min;
            bb.Max.X += getWidth();
            bb.Max.Y += getHeight();
            return bb;
        }

        /// <summary>
        /// Calculate the position of the next BufferView relative to us - these factors aren't constant
        /// and shouldn't be declared as such but for the moment they usually do.  We can also specify a 
        /// factor to spread out the bounding boxes further if they are required.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="factor"></param>
        /// <returns></returns>
        public override Vector3 calculateRelativePositionVector(ViewPosition position, int factor = 1)
        {
            Vector3 rP = m_position;

            try
            {
                if (m_fontManager.getLineSpacing() == 0 || m_fontManager.getCharWidth() == 0)
                {
                    throw new Exception("XygloView::calculateRelativePosition() - some of our basic settings are zero - cannot calculate");
                }
            }
            catch (Exception)
            {
                return rP;
            }

            switch (position)
            {
                case ViewPosition.Above:
                    rP = m_position - (new Vector3(0.0f, factor * (m_bufferShowLength + m_viewHeightSpacing) * m_fontManager.getLineSpacing(), 0.0f));
                    break;

                case ViewPosition.Below:
                    rP = m_position + (new Vector3(0.0f, factor * (m_bufferShowLength + m_viewHeightSpacing) * m_fontManager.getLineSpacing(), 0.0f));
                    break;

                case ViewPosition.Left:
                    rP = m_position - (new Vector3(factor * m_fontManager.getCharWidth() * (m_bufferShowWidth + m_viewWidthSpacing), 0.0f, 0.0f));
                    break;

                case ViewPosition.Right:
                    rP = m_position + (new Vector3(factor * m_fontManager.getCharWidth() * (m_bufferShowWidth + m_viewWidthSpacing), 0.0f, 0.0f));
                    break;

                default:
                    throw new Exception("Unknown position parameter passed");
            }

            return rP;
        }

    }
}
