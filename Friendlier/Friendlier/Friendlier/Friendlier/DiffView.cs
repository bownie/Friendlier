using System;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;


namespace Xyglo
{
    /// <summary>
    /// A DiffView is a type of XygloView which provides information about the differences
    /// between two or more BufferViews.
    /// 
    /// The premise of the DiffView is;
    /// 
    /// - accepts two BufferViews
    /// - generate a new Window with Read Only view of these views linked together
    /// - allows cutting and pasting from this view to the editable BufferViews
    /// 
    /// </summary>
    [DataContract(Name = "Friendlier", Namespace = "http://www.xyglo.com")]
    public class DiffView : XygloView
    {
        // ------------------------------- MEMBER VARIABLES ----------------------------------
        //
        /// <summary>
        /// First BufferView we are comparing
        /// </summary>
        protected BufferView m_sourceBufferView1 = null;

        /// <summary>
        /// Second BufferView we are comparing
        /// </summary>
        protected BufferView m_sourceBufferView2 = null;

        /// <summary>
        /// First Target BufferView
        /// </summary>
        protected AnnotatedBufferView m_targetBufferView1 = null;

        /// <summary>
        /// Second Target BufferView
        /// </summary>
        protected AnnotatedBufferView m_targetBufferView2 = null;

        /// <summary>
        /// FileBuffer 1 for target
        /// </summary>
        protected FileBuffer m_fileBuffer1 = null;

        /// <summary>
        /// FileBuffer 2 for target
        /// </summary>
        protected FileBuffer m_fileBuffer2 = null;

        /// <summary>
        /// A flat texture we use for drawing coloured blobs like highlighting and cursors
        /// </summary>
        protected Texture2D m_flatTexture;

        /// <summary>
        /// Highlight SpriteBatch is kept locally
        /// </summary>
        SpriteBatch m_highLightSpriteBatch = null;

        // List on which to push highlights and colours
        //
        List<Pair<Pair<Vector3, Vector3>, Color>> m_highlightList = new List<Pair<Pair<Vector3, Vector3>, Color>>();

        // -------------------------------- CONSTRUCTORS --------------------------------------
        //
        public DiffView(GraphicsDeviceManager graphics, Project project, BufferView bv1, BufferView bv2)
        {
            // Set our members
            //
            m_fontManager = project.getFontManager();
            m_sourceBufferView1 = bv1;
            m_sourceBufferView2 = bv2;

            // Override colours for this type of view?
            //

            // And initialise
            //
            initialise(graphics, project);

            // Create the SpriteBatch
            m_highLightSpriteBatch = new SpriteBatch(graphics.GraphicsDevice);
        }


        // ----------------------------------- METHODS ------------------------------------------
        //
        /// <summary>
        /// Initialise this object with a project
        /// </summary>
        protected void initialise(GraphicsDeviceManager graphics, Project project)
        {
            if (m_fileBuffer1 == null)
                m_fileBuffer1 = new FileBuffer();

            if (m_fileBuffer2 == null)
                m_fileBuffer2 = new FileBuffer();

            // Create two BufferViews with these new FileBuffers - these are internal to this
            // DiffView and therefore don't need any positional information - they will be 
            // drawn by the DiffView specific code.
            //
            if (m_targetBufferView1 == null)
            {
                m_targetBufferView1 = new AnnotatedBufferView(m_fontManager);
                m_targetBufferView1.setFileBuffer(m_fileBuffer1);
            }

            if (m_targetBufferView2 == null)
            {
                m_targetBufferView2 = new AnnotatedBufferView(m_fontManager);
                m_targetBufferView2.setFileBuffer(m_fileBuffer2);
            }

            // At this point we can work out our width and height and try to find a 
            // suitable position for this DiffBuffer
            //
            m_position = project.getFreePosition(m_sourceBufferView1, getWidth(), getHeight());

            // Create a flat texture for drawing rectangles etc
            //
            Color[] foregroundColors = new Color[1];
            foregroundColors[0] = Color.White;
            m_flatTexture = new Texture2D(graphics.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            m_flatTexture.SetData(foregroundColors);

        }

        /// <summary>
        /// Get first destination BufferView
        /// </summary>
        /// <returns></returns>
        public BufferView getSourceBufferView1()
        {
            return m_sourceBufferView1;
        }

        /// <summary>
        /// Get second destination BufferView
        /// </summary>
        /// <returns></returns>
        public BufferView getSourceBufferView2()
        {
            return m_sourceBufferView2;
        }

        /// <summary>
        /// Accepts two source BufferViews and processes them through to two destination
        /// BufferViews which are spaced and coloured accordingly.
        /// </summary>
        /// <param name="bv1"></param>
        /// <param name="bv2"></param>
        /// <returns></returns>
        public bool process()
        {
            DiffMatchPatch.diff_match_patch dm = new DiffMatchPatch.diff_match_patch();
            //List<DiffMatchPatch.Diff> rL = dm.diff_lineMode(m_sourceBufferView1.getFileBuffer().getTextString(), m_sourceBufferView2.getFileBuffer().getTextString());
            List<DiffMatchPatch.Diff> rL = dm.diff_main(m_sourceBufferView1.getFileBuffer().getTextString(), m_sourceBufferView2.getFileBuffer().getTextString());

            if (rL.Count == 0)
                return false;

            // Make the diffs human readable
            //
            dm.diff_cleanupSemantic(rL);

            // We have to do some state management here to make some sense
            // of our diff.
            //
            DiffMatchPatch.Diff lastDiff;

            m_fileBuffer1.clear();
            m_fileBuffer2.clear();

            // Leftline and rightline store the line we're currently on in the
            // relevant diff output file.
            //
            int leftLine = 0;
            int rightLine = 0;
            bool newLine = false;

            // Process all the diffs
            //
            foreach (DiffMatchPatch.Diff diff in rL)
            {
                // Split the string on carriage returns
                //
                string[] splitString = diff.text.Split('\n');

                // Have we got a newline at all?
                //
                newLine = diff.text.Contains('\n');

                // We need to work out if we have any padding lines to insert.
                // If padding is positive then we do and we have to decide
                // whether it's on the left or right side according to the
                // diff type.
                //
                int padding = splitString.Length - 1;


                // Now we can append lines with real strings on them
                //
                for (int i = 0; i < splitString.Count(); i++)
                {
                    if (diff.operation == DiffMatchPatch.Operation.EQUAL) // Simple case
                    {
                        // Append with no annotation
                        //
                        m_fileBuffer1.appendLineIfNotExist(leftLine, splitString[i]);
                        m_fileBuffer2.appendLineIfNotExist(rightLine, splitString[i]);

                        // Increment both target line numbers to point to next line
                        // only if we have a newLine and we're not on the last iteration
                        //
                        if (newLine && i != splitString.Count() - 1)
                        {
                            leftLine++;
                            rightLine++;
                        }

                    } else if (diff.operation == DiffMatchPatch.Operation.DELETE)
                    {
                        m_fileBuffer1.appendLineIfNotExist(leftLine, splitString[i]);
                        m_targetBufferView2.setAnnotation(rightLine, LineAnnotation.LineModified);

                        if (newLine && i != splitString.Count() - 1)
                        {
                            m_targetBufferView1.setAnnotation(leftLine, LineAnnotation.LineDeleted);
                            leftLine++;
                        }
                        else
                        {
                            m_targetBufferView1.setAnnotation(leftLine, LineAnnotation.LineModified);
                        }

                    } else if (diff.operation == DiffMatchPatch.Operation.INSERT)
                    {
                        m_fileBuffer2.appendLineIfNotExist(rightLine, splitString[i]);
                        m_targetBufferView1.setAnnotation(leftLine, LineAnnotation.LineModified);

                        if (newLine && i != splitString.Count() - 1)
                        {
                            m_targetBufferView2.setAnnotation(rightLine, LineAnnotation.LineInserted);
                            rightLine++;
                        }
                        else
                        {
                            m_targetBufferView2.setAnnotation(rightLine, LineAnnotation.LineModified);
                        }
                    }
                }

                // Now allow for any padding
                //
                if (diff.operation == DiffMatchPatch.Operation.DELETE)
                {
                    // Pad on the right hand side
                    //
                    for (int i = 0; i < padding; i++)
                    {
                        m_fileBuffer2.appendLine("");
                        m_targetBufferView2.setAnnotation(rightLine, LineAnnotation.LinePadding);
                        rightLine++;
                    }

                } else if (diff.operation == DiffMatchPatch.Operation.INSERT)
                {
                    // Pad on the left hand side
                    //
                    for (int i = 0; i < padding; i++)
                    {
                        m_fileBuffer1.appendLine("");
                        m_targetBufferView1.setAnnotation(leftLine, LineAnnotation.LinePadding);
                        leftLine++;
                    }
                }

                // A DELETE means that something is removed from the left hand file
                // but if this is followed by an INSERT then this is a change.  In 
                // this was we have to reconstruct the line meanings from the Diff
                // context.
                //
                /*
                if (diff.operation == DiffMatchPatch.Operation.DELETE)
                {
                    //if (diff.text.Contains('\n'))
                    //{
                        string[] splitString = diff.text.Split('\n');
                        for (int i = 0; i < splitString.Count(); i++)
                        {
                            if (splitString[i] == "")
                                break;

                            m_fileBuffer1.appendLine(m_sourceBufferView1.getFileBuffer().getLine(leftLine));
                            m_targetBufferView1.setAnnotation(leftLine, LineAnnotation.LineModified);

                            // Only increment n - 1 times for the left hand side only
                            //
                            if (i < Math.Max(splitString.Count() - 1, 0))
                            {
                                m_fileBuffer2.appendLine("");
                                m_targetBufferView2.setAnnotation(rightLine, LineAnnotation.LinePadding);
                                leftLine++;
                            }
                        }
                    //}
                    //else
                    //{
                        // Don't append anything
                        //m_fileBuffer1.appendToLine(leftLine, diff.text);

                        //m_targetBufferView1.setAnnotation(leftLine, LineAnnotation.LineModified);
                        //m_targetBufferView2.setAnnotation(rightLine, LineAnnotation.LineModified);
                    //}
                }
                else if (diff.operation == DiffMatchPatch.Operation.INSERT)
                {
                    //if (diff.text.Contains('\n'))
                    //{
                        string[] splitString = diff.text.Split('\n');
                        for (int i = 0; i < splitString.Count(); i++)
                        {
                            if (splitString[i] == "")
                                break;

                            m_fileBuffer1.appendLine("");
                            m_fileBuffer2.appendLine(m_sourceBufferView2.getFileBuffer().getLine(rightLine));


                            m_targetBufferView1.setAnnotation(leftLine, LineAnnotation.LinePadding);
                            m_targetBufferView2.setAnnotation(rightLine, LineAnnotation.LineInserted);

                            // Only increment n - 1 times for the right hand side only
                            //
                            if (i < Math.Max(splitString.Count() - 1, 0))
                            {
                                rightLine++;
                            }
                        }
                    //}
                    //else
                    //{
                        // Special case for empty diff
                        //
                        //m_fileBuffer1.appendLine("");
                        //m_targetBufferView1.setAnnotation(leftLine, LineAnnotation.LinePadding);
                        //leftLine++;

                        //m_targetBufferView2.setAnnotation(rightLine, LineAnnotation.LineModified);
                    //}
                    
                }
                else // EQUAL
                {
                    //if (diff.text.Contains('\n'))
                    //{
                        string[] splitString = diff.text.Split('\n');
                        for (int i = 0; i < splitString.Count(); i++)
                        {
                            if (splitString[i] == "")
                                break;

                            string leftLineString = "";

                            if (leftLine < m_sourceBufferView1.getFileBuffer().getLineCount())
                            {
                                leftLineString = m_sourceBufferView1.getFileBuffer().getLine(leftLine);
                            }

                            string rightLineString = "";

                            if (rightLine < m_sourceBufferView2.getFileBuffer().getLineCount())
                            {
                                rightLineString = m_sourceBufferView2.getFileBuffer().getLine(rightLine);
                            }

                            m_fileBuffer1.appendLine(leftLineString);
                            m_fileBuffer2.appendLine(rightLineString);

                            // Only increment n - 1 times for both lines
                            //
                            if (i < Math.Max(splitString.Count() - 1, 0))
                            {
                                leftLine++;
                                rightLine++;
                            }
                        }
                    //}
                    //else
                    //{
                        //m_fileBuffer1.setLine(leftLine, m_sourceBufferView1.getFileBuffer().getLine(leftLine));
                        //m_fileBuffer2.setLine(rightLine, m_sourceBufferView1.getFileBuffer().getLine(rightLine));
                    //}
                 * */
                //}

                lastDiff = diff;
            }

            return true;
        }

        /// <summary>
        /// BufferView width defined by font size and we add a gap between the two
        /// </summary>
        /// <returns></returns>
        public override float getWidth()
        {
            return m_fontManager.getCharWidth() * (m_targetBufferView1.getBufferShowWidth() + m_targetBufferView2.getBufferShowWidth() + m_viewWidthSpacing);
        }

        /// <summary>
        /// BufferView height defined by font size and we add a gap between the two
        /// </summary>
        /// <returns></returns>
        public override float getHeight()
        {
            return m_fontManager.getCharWidth() * (m_targetBufferView1.getBufferShowLength() + m_targetBufferView2.getBufferShowLength() + m_viewHeightSpacing);
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
        /// Return the number of differences
        /// </summary>
        /// <returns></returns>
        public int getDifferences()
        {
            return 0;
        }

        /// <summary>
        /// Get the maximum buffer show length
        /// </summary>
        /// <returns></returns>
        public int getMaxBufferShowLength()
        {
            return Math.Max(m_targetBufferView1.getBufferShowLength(), m_targetBufferView2.getBufferShowLength());
        }

        /// <summary>
        /// We can draw ourselves here
        /// </summary>
        public override void draw(Project project, FriendlierState state, GameTime gameTime, SpriteBatch spriteBatch, Effect effect)
        {
            Color bufferColour = m_textColour;

            if (state != FriendlierState.TextEditing && state != FriendlierState.FindText && state != FriendlierState.DiffPicker)
            {
                bufferColour = m_greyedColour;
            }

            // How dark should our non-highlighted BufferViews be?
            //
            //float greyDivisor = 2.0f;

            // Take down the colours and alpha of the non selected buffer views to draw a visual distinction
            //
            //if (view != m_project.getSelectedBufferView())
            //{
                //bufferColour.R = (byte)(bufferColour.R / greyDivisor);
                //bufferColour.G = (byte)(bufferColour.G / greyDivisor);
                //bufferColour.B = (byte)(bufferColour.B / greyDivisor);
                //bufferColour.A = (byte)(bufferColour.A / greyDivisor);
  //          }
//
            float yPosition = 0.0f;

            Vector3 viewSpaceTextPosition = m_position;

            // Clear the highlight list every time we draw - a bit wasteful for the moment
            //
            m_highlightList.Clear();

            // Show the first target buffer view
            //
            for (int i = 0; i < m_targetBufferView1.getBufferShowLength(); i++)
            {
                if (i + m_targetBufferView1.getBufferShowStartY() >= m_fileBuffer1.getLineCount())
                    break;

                spriteBatch.DrawString(
                    project.getFontManager().getFont(),
                    m_fileBuffer1.getLine(i + m_targetBufferView1.getBufferShowStartY()),
                    new Vector2((int)viewSpaceTextPosition.X, (int)(viewSpaceTextPosition.Y + yPosition)),
                    bufferColour,
                    0,
                    Vector2.Zero,
                    project.getFontManager().getTextScale(),
                    0,
                    0);

                // Draw an annotation
                //
                if (m_targetBufferView1.hasAnnotation(i + m_targetBufferView1.getBufferShowStartY()))
                {
                    // Determine quad position
                    //
                    Vector3 startQuad = viewSpaceTextPosition;
                    startQuad.Y += yPosition;
                    Vector3 endQuad = startQuad + new Vector3(m_targetBufferView2.getVisibleWidth(), project.getFontManager().getLineSpacing(), 0);

                    // Ensure that quad appears behind text
                    //
                    //startQuad.Z -= 20.0f;
                    //endQuad.Z -= 20.0f;

                    // Push onto highlight list
                    //
                    m_highlightList.Add(new Pair<Pair<Vector3, Vector3>, Color>(new Pair<Vector3, Vector3>(startQuad, endQuad), m_targetBufferView1.getLineHighlightColour(i + m_targetBufferView1.getBufferShowStartY())));
                }

                yPosition += project.getFontManager().getLineSpacing();
            }

            // Move the start X position and reset Y
            //
            viewSpaceTextPosition.X += (m_targetBufferView1.getBufferShowWidth() + m_viewWidthSpacing) * m_fontManager.getCharWidth();
            yPosition = 0.0f;

            // Now draw the second BufferView
            //
            for (int i = 0; i < m_targetBufferView2.getBufferShowLength(); i++)
            {
                if (i + m_targetBufferView2.getBufferShowStartY() >= m_fileBuffer2.getLineCount())
                    break;

                spriteBatch.DrawString(
                    project.getFontManager().getFont(),
                    m_fileBuffer2.getLine(i + m_targetBufferView2.getBufferShowStartY()),
                    new Vector2((int)viewSpaceTextPosition.X, (int)(viewSpaceTextPosition.Y + yPosition)),
                    bufferColour,
                    0,
                    Vector2.Zero,
                    project.getFontManager().getTextScale(),
                    0,
                    0);

                // Draw an annotation
                //
                if (m_targetBufferView2.hasAnnotation(i + m_targetBufferView1.getBufferShowStartY()))
                {
                    //Logger.logMsg("DRAWING ANNOTATION (2)");

                    // Determine quad position
                    //
                    Vector3 startQuad = viewSpaceTextPosition;
                    startQuad.Y += yPosition;
                    Vector3 endQuad = startQuad + new Vector3(m_targetBufferView2.getVisibleWidth(), project.getFontManager().getLineSpacing(), 0);

                    // Ensure that quad appears behind text
                    //
                    //startQuad.Z -= 2.0f;
                    //endQuad.Z -= 2.0f;

                    // Push to highlight list
                    //
                    m_highlightList.Add(new Pair<Pair<Vector3, Vector3>, Color>(new Pair<Vector3, Vector3>(startQuad, endQuad), m_targetBufferView2.getLineHighlightColour(i + m_targetBufferView1.getBufferShowStartY())));
                }

                yPosition += project.getFontManager().getLineSpacing();
            }
        }

        /// <summary>
        /// Render all the Highlights in our own SpriteBatch
        /// </summary>
        public override void drawTextures(Effect effect)
        {
            // Render all our quads in one pass
            //
            m_highLightSpriteBatch.Begin(SpriteSortMode.Texture, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.DepthRead, RasterizerState.CullNone, effect);

            foreach (Pair<Pair<Vector3, Vector3>, Color> highlight in m_highlightList)
            {
                renderQuad(highlight.First.First, highlight.First.Second, highlight.Second, m_highLightSpriteBatch);
            }
            m_highLightSpriteBatch.End();
        }

        /// <summary>
        /// Renders a quad at a given position - we can wrap this within another spritebatch call
        /// </summary>
        /// <param name="topLeft"></param>
        /// <param name="bottomRight"></param>
        protected void renderQuad(Vector3 topLeft, Vector3 bottomRight, Color quadColour, SpriteBatch spriteBatch)
        {
            Vector3 bottomLeft = new Vector3(topLeft.X, bottomRight.Y, topLeft.Z);
            Vector3 topRight = new Vector3(bottomRight.X, topLeft.Y, bottomRight.Z);

            // We should be caching this rather than newing it all the time
            //
            VertexPositionTexture[] vpt = new VertexPositionTexture[4];
            Vector2 tp = new Vector2(0, 1);
            vpt[0] = new VertexPositionTexture(topLeft, tp);
            vpt[1] = new VertexPositionTexture(topRight, tp);
            vpt[2] = new VertexPositionTexture(bottomRight, tp);
            vpt[3] = new VertexPositionTexture(bottomLeft, tp);

            spriteBatch.Draw(m_flatTexture, new Rectangle(Convert.ToInt16(topLeft.X),
                                                 Convert.ToInt16(topLeft.Y),
                                                 Convert.ToInt16(bottomRight.X) - Convert.ToInt16(topLeft.X),
                                                 Convert.ToInt16(bottomRight.Y) - Convert.ToInt16(topLeft.Y)),
                                                 quadColour);
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
            rV.X += m_fontManager.getCharWidth() * (m_targetBufferView1.getBufferShowWidth() + m_targetBufferView2.getBufferShowWidth() + m_viewWidthSpacing) / 2;
            rV.Y -= m_fontManager.getLineSpacing() * (m_targetBufferView1.getBufferShowLength() + m_targetBufferView2.getBufferShowLength() + m_viewHeightSpacing) / 2;
            rV.Z = zoomLevel;

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
            rV.X += m_fontManager.getCharWidth() * (m_targetBufferView1.getBufferShowWidth() + m_targetBufferView2.getBufferShowWidth() + m_viewWidthSpacing) / 2;
            rV.Y -= m_fontManager.getLineSpacing() * (m_targetBufferView1.getBufferShowLength() + m_targetBufferView2.getBufferShowLength() + m_viewHeightSpacing) / 2;
            rV.Z += 600.0f;
            return rV;
        }

    }
}
