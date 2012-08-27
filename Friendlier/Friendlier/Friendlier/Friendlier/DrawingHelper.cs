#region File Description
//-----------------------------------------------------------------------------
// DrawingHelper.cs
//
// Copyright (C) Xyglo Ltd. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Xyglo
{
    /// <summary>
    /// Class that we can use to stick a lot of our drawing code in.
    /// </summary>
    public class DrawingHelper
    {
        /// <summary>
        /// A Friendlier Project
        /// </summary>
        protected Project m_project;

        /// <summary>
        /// Store a local texture
        /// </summary>
        protected Texture2D m_flatTexture;

        /// <summary>
        /// BoundingBox for the BufferView preview
        /// </summary>
        protected BoundingBox m_previewBoundingBox;

        /// <summary>
        /// Greyed out colour for background text
        /// </summary>
        protected Color m_greyedColour = new Color(30, 30, 30, 50);

        /// <summary>
        /// How dark should our non-highlighted BufferViews be?
        /// </summary>
        protected float m_greyDivisor = 2.0f;

        /// <summary>
        /// List of highlights we're going to draw.  We don't want to fetch this everytime we
        /// draw the BufferView.
        /// </summary>
        protected List<Highlight> m_highlights;

        /// <summary>
        /// The colour of our banner
        /// </summary>
        protected Color m_bannerColour = new Color(180, 180, 180, 180);

        /// <summary>
        /// Start time for a banner
        /// </summary>
        protected double m_bannerStartTime = -1;

        /// <summary>
        /// Banner message
        /// </summary>
        protected string m_bannerString;

        /// <summary>
        /// Duration of a banner
        /// </summary>
        protected float m_bannerDuration;

        /// <summary>
        /// Strings within a banner if there are multiple
        /// </summary>
        protected List<string> m_bannerStringList;

        /// <summary>
        /// Set preview bounding box
        /// </summary>
        /// <param name="bb"></param>
        public void setPreviewBoundingBox(BoundingBox bb)
        {
            m_previewBoundingBox = bb;
        }

        /// <summary>
        /// Local top left vector
        /// </summary>
        protected Vector3 m_bottomLeft = new Vector3();

        /// <summary>
        /// Local top right vector
        /// </summary>
        protected Vector3 m_topRight = new Vector3();

        /// <summary>
        /// User help string
        /// </summary>
        protected string m_userHelp;

        // ---------------------------------- CONSTRUCTORS -------------------------------
        //

        /// <summary>
        /// Construct our helper
        /// </summary>
        /// <param name="project"></param>
        /// <param name="flatTexture"></param>
        public DrawingHelper(Project project, Texture2D flatTexture, float windowWidth, float windowHeight)
        {
            m_project = project;
            m_flatTexture = flatTexture;
            setPreviewBoundingBox(windowWidth, windowHeight);

            // Populate the user help
            //
            populateUserHelp();
        }

        // ---------------------------------- METHODS -----------------------------------
        //


        /// <summary>
        /// Draws a little map of our BufferViews onto a panner/scanner area
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="spriteBatch"></param>
        public void drawBufferViewMap(GameTime gameTime, SpriteBatch spriteBatch)
        {
            BoundingBox bb = m_project.getBoundingBox();

            // Modify alpha according to the type of the line
            //
            float minX = bb.Min.X;
            float minY = bb.Min.Y;
            float maxX = bb.Max.X;
            float maxY = bb.Max.Y;

            float diffX = maxX - minX;
            float diffY = maxY - minY;

            Vector2 topLeft = Vector2.Zero;
            Vector2 bottomRight = Vector2.Zero;

            float previewX = m_previewBoundingBox.Max.X - m_previewBoundingBox.Min.X;
            float previewY = m_previewBoundingBox.Max.Y - m_previewBoundingBox.Min.Y;

            foreach(BufferView bv in m_project.getBufferViews())
            {
                topLeft.X = m_previewBoundingBox.Min.X;
                topLeft.Y = m_previewBoundingBox.Min.Y;
                bottomRight.X = m_previewBoundingBox.Min.X;
                bottomRight.Y = m_previewBoundingBox.Min.Y;

                topLeft.X += ( ( bv.getBoundingBox().Min.X - minX ) / diffX ) * previewX;
                topLeft.Y += ( ( bv.getBoundingBox().Min.Y - minY ) / diffY ) * previewY;

                bottomRight.X += ( ( bv.getBoundingBox().Max.X - minX ) / diffX ) * previewX;
                bottomRight.Y += ( ( bv.getBoundingBox().Max.Y - minY ) / diffY ) * previewY;

                drawQuad(spriteBatch, topLeft, bottomRight, Color.LightYellow, (bv == m_project.getSelectedBufferView()) ? 0.5f : 0.1f);
            }
        }

        /// <summary>
        /// Draw a box for us - accepts floats and we don't force these to int so watch
        /// your inputs please.
        /// </summary>
        /// <param name="topLeft"></param>
        /// <param name="bottomRight"></param>
        /// <param name="colour"></param>
        /// <param name="width"></param>
        public void drawBox(SpriteBatch spriteBatch, Vector2 topLeft, Vector2 bottomRight, Color colour, float alpha = 1.0f, int width = 1)
        {
            Vector2 xDiff = bottomRight - topLeft;
            Vector2 yDiff = xDiff;
            xDiff.Y = 0;
            yDiff.X = 0;

            drawLine(spriteBatch, topLeft, topLeft + xDiff, colour, alpha, width);
            drawLine(spriteBatch, topLeft + xDiff, bottomRight, colour, alpha, width);
            drawLine(spriteBatch, bottomRight, topLeft + yDiff, colour, alpha, width);
            drawLine(spriteBatch, topLeft + yDiff, topLeft, colour, alpha, width);
        }


        /// <summary>
        /// Update the preview bounding box with new coordinates if the screen size changes for example
        /// </summary>
        /// <param name="windowWidth"></param>
        /// <param name="windowHeight"></param>
        public void setPreviewBoundingBox(float windowWidth, float windowHeight)
        {
            Vector3 topLeft = Vector3.Zero;
            topLeft.X = windowWidth - m_project.getFontManager().getOverlayFont().MeasureString("X").X * 10;
            topLeft.Y = windowHeight - m_project.getFontManager().getOverlayFont().LineSpacing * 6;

            Vector3 bottomRight = Vector3.Zero;
            bottomRight.X = windowWidth - m_project.getFontManager().getOverlayFont().MeasureString("X").X * 3;
            bottomRight.Y = windowHeight - m_project.getFontManager().getOverlayFont().LineSpacing * 2;

            m_previewBoundingBox.Min = topLeft;
            m_previewBoundingBox.Max = bottomRight;
        }


        /// <summary>
        /// Draw line wrapper - accepts floats and we don't force these to int so watch
        /// your inputs please.
        /// </summary>
        /// <param name="topLeft"></param>
        /// <param name="bottomRight"></param>
        /// <param name="colour"></param>
        /// <param name="width"></param>
        public void drawLine(SpriteBatch spriteBatch, Vector2 topLeft, Vector2 bottomRight, Color colour, float alpha = 1.0f, int width = 1)
        {
            float angle = (float)Math.Atan2(bottomRight.Y - topLeft.Y, bottomRight.X - topLeft.X);
            float length = Vector2.Distance(topLeft, bottomRight);
            spriteBatch.Draw(m_flatTexture, topLeft, null, colour * alpha,
                             angle, Vector2.Zero, new Vector2(length, width),
                             SpriteEffects.None, 0);
        }

        /// <summary>
        /// Just a wrapper for a draw quad
        /// </summary>
        /// <param name="topLeft"></param>
        /// <param name="bottomRight"></param>
        /// <param name="colour"></param>
        /// <param name="alpha"></param>
        /// <param name="width"></param>
        public void drawQuad(SpriteBatch spriteBatch, Vector2 topLeft, Vector2 bottomRight, Color colour, float alpha = 1.0f)
        {
            spriteBatch.Draw(m_flatTexture, new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)(bottomRight.X - topLeft.X), (int)(bottomRight.Y - topLeft.Y)), colour * alpha);
        }

        /// <summary>
        /// Render a quad to a supplied SpriteBatch
        /// </summary>
        /// <param name="topLeft"></param>
        /// <param name="bottomRight"></param>
        /// <param name="quadColour"></param>
        /// <param name="spriteBatch"></param>
        public void renderQuad(Vector3 topLeft, Vector3 bottomRight, Color quadColour, SpriteBatch spriteBatch)
        {
            m_bottomLeft.X = topLeft.X;
            m_bottomLeft.Y = bottomRight.Y;
            m_bottomLeft.Z = topLeft.Z;

            m_topRight.X = bottomRight.X;
            m_topRight.Y = topLeft.Y;
            m_topRight.Z = bottomRight.Z;

            spriteBatch.Draw(m_flatTexture, new Rectangle(Convert.ToInt16(topLeft.X),
                                                 Convert.ToInt16(topLeft.Y),
                                                 Convert.ToInt16(bottomRight.X) - Convert.ToInt16(topLeft.X),
                                                 Convert.ToInt16(bottomRight.Y) - Convert.ToInt16(topLeft.Y)),
                                                 quadColour);
        }


        /// <summary>
        /// This draws a highlight area on the screen when we hold shift down
        /// </summary>
        public void drawHighlight(GameTime gameTime, SpriteBatch spriteBatch)
        {
            List<BoundingBox> bb = m_project.getSelectedBufferView().computeHighlight(m_project);

            // Draw the bounding boxes
            //
            foreach (BoundingBox highlight in bb)
            {
                renderQuad(highlight.Min, highlight.Max, m_project.getSelectedBufferView().getHighlightColor(), spriteBatch);
            }
        }

        /// <summary>
        /// Draw an overview of the project from a file perspective and allow modification
        /// </summary>
        public void drawManageProject(SpriteBatch spriteBatch, GameTime gameTime, ModelBuilder modelBuilder, GraphicsDeviceManager graphics, int configPosition, out int linesDisplayed)
        {
            string text = "";

            // Star the spritebatch
            //
            spriteBatch.Begin();

            // The maximum width of an entry in the file list
            //
            int maxWidth = ((int)((float)m_project.getSelectedBufferView().getBufferShowWidth() * 0.9f));

            // This is very simply modelled at the moment
            //
            foreach (string fileName in modelBuilder.getReturnString().Split('\n'))
            {
                // Ignore the last split
                //
                if (fileName != "")
                {
                    if ((modelBuilder.getRootString().Length + fileName.Length) < maxWidth)
                    {
                        text += modelBuilder.getRootString() + fileName + "\n";
                    }
                    else
                    {
                        //text += m_project.buildFileString(m_modelBuilder.getRootString(), fileName, maxWidth) + "\n";
                        text += m_project.estimateFileStringTruncation(modelBuilder.getRootString(), fileName, maxWidth) + "\n";
                    }

                }
            }

            if (text == "")
            {
                text = "[Project contains no Files]";
            }

            // Draw the main text screen - using the m_configPosition as the place holder
            //
            linesDisplayed = drawTextScreen(spriteBatch, gameTime, graphics, text, 0, 0, configPosition);
            
            // Draw the project file name
            //
            drawCentredTextOverlay(spriteBatch, graphics, 3, "Project Overview", Color.AntiqueWhite);
            drawCentredTextOverlay(spriteBatch, graphics, 5, "Project file : " + m_project.getProjectFile(), Color.LightSeaGreen);

            // Help text
            //
            string commandText = "[Delete] - remove file from project       [Insert]  - edit file\n";
            commandText +=       "[Home]   - change project file location   [N]       - create new project file";
            drawCentredTextOverlay(spriteBatch, graphics, 30, commandText, Color.LightCoral);

            // End the batch
            //
            spriteBatch.End();
        }

        /// <summary>
        /// Draw the help screen
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="gameTime"></param>
        public int drawHelpScreen(SpriteBatch spriteBatch, GameTime gameTime, GraphicsDeviceManager graphics, int textScreenPositionY)
        {
            spriteBatch.Begin();
            int screenLength = drawTextScreen(spriteBatch, gameTime, graphics, m_userHelp, textScreenPositionY);
            spriteBatch.End();

            return screenLength;
        }


        /// <summary>
        /// Format a screen of information text - also return how many formatted rows of information we
        /// are displaying so we can decide about paging.
        /// </summary>
        /// <param name="text"></param>
        public int drawTextScreen(SpriteBatch spriteBatch, GameTime gameTime, GraphicsDeviceManager graphics, string text, int textScreenPositionY, int fixedWidth = 0, int highlight = -1)
        {
            Vector3 fp = m_project.getSelectedBufferView().getPosition();

            // Always start from 0 for offsets
            //
            float yPos = 0.0f;
            float xPos = 0.0f;

            // Split out the input line
            //
            string[] infoRows = text.Split('\n');

            // We need to store this value so that page up and page down work
            //
            int textScreenLength = infoRows.Length;

            //  Position the information centrally
            //
            int longestRow = 0;
            for (int i = 0; i < textScreenLength; i++)
            {
                if (infoRows[i].Length > longestRow)
                {
                    longestRow = infoRows[i].Length;
                }
            }

            // Limit the row length when centring
            //
            if (fixedWidth == 0)
            {
                if (longestRow > m_project.getSelectedBufferView().getBufferShowWidth())
                {
                    longestRow = m_project.getSelectedBufferView().getBufferShowWidth();
                }
            }
            else
            {
                longestRow = fixedWidth;
            }

            // Calculate endline
            //
            int endLine = textScreenPositionY + Math.Min(infoRows.Length - textScreenPositionY, m_project.getSelectedBufferView().getBufferShowLength());

            // Modify by height of the screen to centralise
            //
            yPos += (graphics.GraphicsDevice.Viewport.Height / 2) - (m_project.getFontManager().getLineSpacing(FontManager.FontType.Overlay) * (endLine - textScreenPositionY) / 2);

            // Adjust xPos
            //
            xPos = (graphics.GraphicsDevice.Viewport.Width / 2) - (longestRow * m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay) / 2);

            // hardcode the font size to 1.0f so it looks nice
            //
            for (int i = textScreenPositionY; i < endLine; i++)
            {
                // Always Always Always render a string on an integer - never on a float as it looks terrible
                //
                spriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), infoRows[i], new Vector2((int)xPos, (int)yPos), (highlight == i ? Color.LightBlue : Color.White), 0, Vector2.Zero, 1.0f, 0, 0);
                yPos += m_project.getFontManager().getLineSpacing(FontManager.FontType.Overlay);
            }

            // Draw a page header if we need to
            //
            yPos = m_project.getFontManager().getLineSpacing(FontManager.FontType.Overlay) * 5;

            double dPages = Math.Ceiling((float)textScreenLength / (float)m_project.getSelectedBufferView().getBufferShowLength());
            double cPage = Math.Ceiling((float)(textScreenPositionY + 1) / ((float)m_project.getSelectedBufferView().getBufferShowLength()));

            if (dPages > 1)
            {
                string pageString = "---- Page " + cPage + " of " + dPages + " ----";

                // 3 character adjustment below
                //
                xPos = (graphics.GraphicsDevice.Viewport.Width / 2) - ((pageString.Length + 3) * m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay) / 2);

                // Always Always Always render a string on an integer - never on a float as it looks terrible
                //
                spriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), pageString, new Vector2((int)xPos, (int)yPos), Color.LightSeaGreen);
            }

            return textScreenLength;
        }

        /// <summary>
        /// Format a screen of information text - slightly different to a help screen as
        /// the text can be dynamic (i.e. times)
        /// </summary>
        /// <param name="text"></param>
        public void drawInformationScreen(SpriteBatch spriteBatch, GameTime gameTime, GraphicsDeviceManager graphics, out int linesDisplayed)
        {
            // Set up the string
            //
            string text = "";

            // Start spritebatch
            //
            spriteBatch.Begin();

            drawCentredTextOverlay(spriteBatch, graphics, 3, "File Information", Color.AntiqueWhite);

            string truncFileName = m_project.estimateFileStringTruncation("", m_project.getSelectedBufferView().getFileBuffer().getFilepath(), 75);

            text += truncFileName + "\n\n";
            text += "File status        : " + (m_project.getSelectedBufferView().getFileBuffer().isWriteable() ? "Writeable " : "Read Only") + "\n";
            text += "File lines         : " + m_project.getSelectedBufferView().getFileBuffer().getLineCount() + "\n";
            text += "File created       : " + m_project.getSelectedBufferView().getFileBuffer().getCreationSystemTime().ToString() + "\n";
            text += "File last modified : " + m_project.getSelectedBufferView().getFileBuffer().getLastWriteSystemTime().ToString() + "\n";
            text += "File last accessed : " + m_project.getSelectedBufferView().getFileBuffer().getLastFetchSystemTime().ToString() + "\n";

            text += "\n"; // divider
            text += "Project name:      : " + m_project.m_projectName + "\n";
            text += "Project created    : " + m_project.getCreationTime().ToString() + "\n";
            text += "Project file       : " + m_project.getExternalProjectDefinitionFile() + "\n";
            text += "Profile base dir   : " + m_project.getExternalProjectBaseDirectory() + "\n";
            text += "Number of files    : " + m_project.getFileBuffers().Count + "\n";
            text += "File lines         : " + m_project.getFilesTotalLines() + "\n";
            text += "FileBuffers        : " + m_project.getFileBuffers().Count + "\n";
            text += "BufferViews        : " + m_project.getBufferViews().Count + "\n";
            text += "\n"; // divider

            // Some timings
            //
            TimeSpan nowDiff = (DateTime.Now - m_project.getCreationTime());
            TimeSpan activeTime = m_project.m_activeTime + (DateTime.Now - m_project.m_lastAccessTime);
            text += "Project age        : " + nowDiff.Days + " days, " + nowDiff.Hours + " hours, " + nowDiff.Minutes + " minutes\n"; //, " + nowDiff.Seconds + " seconds\n";
            text += "Total editing time : " + activeTime.Days + " days, " + activeTime.Hours + " hours, " + activeTime.Minutes + " minutes, " + activeTime.Seconds + " seconds\n";

            // Draw screen of a fixed width
            //
            linesDisplayed = drawTextScreen(spriteBatch, gameTime, graphics, text, 0, 75);

            spriteBatch.End();
        }

        /// <summary>
        /// Useful helper method to draw some text with specified colour and position on the pre-opened spriteBatch.
        /// </summary>
        /// <param name="screenPosition"></param>
        /// <param name="text"></param>
        /// <param name="textColour"></param>
        protected void drawTextOverlay(SpriteBatch spriteBatch, FilePosition position, string text, Color textColour)
        {
            int xPos = (int)((float)position.X * m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay));
            int yPos = (int)((float)position.Y * m_project.getFontManager().getLineSpacing(FontManager.FontType.Overlay));

            spriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), text, new Vector2(xPos, yPos), textColour);
        }

        /// <summary>
        /// Automatically draw and centre a string - if there are line breaks in it then compensate for that
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="graphics"></param>
        /// <param name="lines"></param>
        /// <param name="text"></param>
        /// <param name="textColour"></param>
        protected void drawCentredTextOverlay(SpriteBatch spriteBatch, GraphicsDeviceManager graphics, int lines, string text, Color textColour)
        {
            int maxWidth = 0;
            foreach (string subString in text.Split('\n'))
            {
                if (subString.Length > maxWidth)
                {
                    maxWidth = subString.Length;
                }
            }
            int xPos = (graphics.GraphicsDevice.Viewport.Width / 2) - (int)((float)maxWidth / 2 * m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay));
            int yPos = (int)((float)lines * m_project.getFontManager().getLineSpacing(FontManager.FontType.Overlay));
            spriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), text, new Vector2(xPos, yPos), textColour);
        }


        /// <summary>
        /// Populate the user help string - could do this from a resource file really
        /// </summary>
        protected void populateUserHelp()
        {
            m_userHelp += "Key Help\n\n";

            m_userHelp += "F1  - Cycle down through buffer views\n";
            m_userHelp += "F2  - Cycle up through buffer views\n";
            m_userHelp += "F3  - Search again\n";
            m_userHelp += "F6  - Perform Build\n";
            //m_userHelp += "F7  - Zoom Out\n";
            //m_userHelp += "F8  - Zoom In\n";
            m_userHelp += "F9  - Rotate anticlockwise around group of 4\n";
            m_userHelp += "F10 - Rotate clockwise around group of 4\n";
            m_userHelp += "F11 - Toggle Full Screen Mode\n";
            //m_userHelp += "F12 - Windowed Mode\n";

            m_userHelp += "Alt + N - New buffer view on new buffer\n";
            m_userHelp += "Alt + B - Copy existing buffer view on existing buffer\n";
            m_userHelp += "Alt + O - Open file\n";
            m_userHelp += "Alt + S - Save (as) file\n";
            m_userHelp += "Alt + C - Close buffer view\n";

            m_userHelp += "Alt + H - Help screen\n";
            m_userHelp += "Alt + G - Settings screen\n";

            m_userHelp += "Alt + Z - Undo\n";
            m_userHelp += "Alt + Y - Redo\n";
            m_userHelp += "Alt + A - Select All\n";
            m_userHelp += "Alt + F - Find\n";
            m_userHelp += "Alt + [number keys] - Jump to numbered buffer view\n";

            m_userHelp += "\n\n";

            m_userHelp += "Mouse Help\n\n";
            m_userHelp += "Left click & drag   - Move Window with gravity on new window centres\n";
            m_userHelp += "Left click on File  - Move Cursor there\n";
            m_userHelp += "Left click shift    - Change highlight in BufferView\n";
            m_userHelp += "Scrollwheel in/out  - Zoom in/out\n";
            m_userHelp += "Shift & Scrollwheel - Cursor up and down in current BufferView\n";

        }

        /// <summary>
        /// Draw a BufferView in any state that we wish to - this means showing the lines of the
        /// file/buffer we want to see at the current cursor position with highlighting as required.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="gameTime"></param>
        public void drawFileBuffer(SpriteBatch spriteBatch, BufferView view, GameTime gameTime, FriendlierState state, BufferView buildStdOutView, BufferView buildStdErrView, float zoomLevel, double textScale)
        {
            Color bufferColour = view.getTextColour();

            if (state != FriendlierState.TextEditing && state != FriendlierState.GotoLine && state != FriendlierState.FindText && state != FriendlierState.DiffPicker)
            {
                bufferColour = m_greyedColour;
            }

            // Take down the colours and alpha of the non selected buffer views to draw a visual distinction
            //
            if (view != m_project.getSelectedBufferView())
            {
                bufferColour.R = (byte)(bufferColour.R / m_greyDivisor);
                bufferColour.G = (byte)(bufferColour.G / m_greyDivisor);
                bufferColour.B = (byte)(bufferColour.B / m_greyDivisor);
                bufferColour.A = (byte)(bufferColour.A / m_greyDivisor);
            }

            float yPosition = 0.0f;

            //Vector2 lineOrigin = new Vector2();
            Vector3 viewSpaceTextPosition = view.getPosition();

            // Draw all the text lines to the height of the buffer
            //
            // This is default empty line character
            string line, fetchLine;
            int bufPos = view.getBufferShowStartY();
            Color highlightColour;

            // If we are tailing a file then let's look at the last X lines of it only
            //
            if (view.isTailing())
            {
                // We don't do this all the time so let the FileBuffer work out when we've updated
                // the file and need to change the viewing position to tail it.
                //
                view.getFileBuffer().refetchFile(gameTime, m_project.getSyntaxManager());
                //bufPos = view.getFileBuffer().getLineCount() - view.getBufferShowLength();


                // Ensure that we're always at least at zero
                //
                if (bufPos < 0)
                {
                    bufPos = 0;
                }
            }

            // We do tailing and read only files here
            //
            if (view.isTailing() && view.isReadOnly())
            {
                // Ensure that we're tailing correctly by adjusting bufferview position
                //


                // We let the view do the hard work with the wrapped lines
                //
                List<string> lines;

                if (view == buildStdOutView)
                {
                    lines = view.getWrappedEndofBuffer(m_project.getStdOutLastLine());
                }
                else if (view == buildStdErrView)
                {
                    lines = view.getWrappedEndofBuffer(m_project.getStdErrLastLine());
                }
                else
                {
                    // Default
                    //
                    lines = view.getWrappedEndofBuffer();
                }

                Color bufferColourLastRun = new Color(50, 50, 50, 50);

                for (int i = 0; i < lines.Count; i++)
                {
                    spriteBatch.DrawString(m_project.getFontManager().getViewFont(view.getViewSize()), lines[i], new Vector2((int)viewSpaceTextPosition.X, (int)viewSpaceTextPosition.Y + yPosition),
                        (i < view.getLogRunTerminator() ? bufferColourLastRun : bufferColour), 0, Vector2.Zero, m_project.getFontManager().getTextScale(), 0, 0);
                    yPosition += m_project.getFontManager().getLineSpacing(view.getViewSize());
                }
            }
            else
            {
                for (int i = 0; i < view.getBufferShowLength(); i++)
                {
                    line = "~";

                    if (i + bufPos < view.getFileBuffer().getLineCount() && view.getFileBuffer().getLineCount() != 0)
                    {
                        // Fetch the line and convert any tabs to relevant spaces
                        //
                        fetchLine = view.getFileBuffer().getLine(i + bufPos).Replace("\t", m_project.getTab());

                        if (fetchLine.Length - view.getBufferShowStartX() > view.getBufferShowWidth())
                        {
                            // Get a valid section of it
                            //
                            line = fetchLine.Substring(view.getBufferShowStartX(), Math.Min(fetchLine.Length - view.getBufferShowStartX(), view.getBufferShowWidth()));

                            if (view.getBufferShowStartX() + view.getBufferShowWidth() < fetchLine.Length)
                            {
                                line += " [>]";
                            }
                        }
                        else
                        {
                            if (view.getBufferShowStartX() >= 0 && view.getBufferShowStartX() < fetchLine.Length)
                            {
                                line = fetchLine.Substring(view.getBufferShowStartX(), fetchLine.Length - view.getBufferShowStartX());
                            }
                            else
                            {
                                line = "";
                            }
                        }
                    }

                    // Get the highlighting for the line
                    //
                    m_highlights = view.getFileBuffer().getHighlighting(i + bufPos, view.getBufferShowStartX(), view.getBufferShowWidth());

                    // Only do syntax highlighting when we're not greyed out
                    //
                    // !!! Could be performance problem here with highlights
                    //
                    if (m_highlights.Count > 0 && bufferColour != m_greyedColour)
                    {
                        // Need to print the line by section with some unhighlighted
                        //
                        int nextHighlight = 0;

                        // Start from wherever we're showing from
                        //
                        int xPos = 0; // view.getBufferShowStartX();

                        // Step through with xPos all the highlights in our collection
                        //
                        while (nextHighlight < m_highlights.Count && xPos < line.Length)
                        {
                            // Sort out the colour
                            //
                            highlightColour = m_highlights[nextHighlight].getColour();

                            // If not active view then temper colour
                            //
                            if (view != m_project.getSelectedBufferView())
                            {
                                highlightColour.R = (byte)(highlightColour.R / m_greyDivisor);
                                highlightColour.G = (byte)(highlightColour.G / m_greyDivisor);
                                highlightColour.B = (byte)(highlightColour.B / m_greyDivisor);
                                highlightColour.A = (byte)(highlightColour.A / m_greyDivisor);
                            }

                            // If the highlight starts beyond the string end then skip it -
                            // and we quit out of the highlighting process as highlights are
                            // sorted (hopefully correctly).
                            //
                            if (m_highlights[nextHighlight].m_startHighlight.X >= line.Length)
                            {
                                xPos = line.Length;
                                continue;
                            }

                            if (xPos < m_highlights[nextHighlight].m_startHighlight.X || nextHighlight >= m_highlights.Count)
                            {
                                string subLineToHighlight = line.Substring(xPos, m_highlights[nextHighlight].m_startHighlight.X - xPos);

                                // Not sure we need this for the moment
                                //
                                int screenXpos = m_project.fileToScreen(line.Substring(0, xPos)).Length;

                                //if (screenXpos != xPos)
                                //{
                                //Logger.logMsg("GOT TAB");
                                //}

                                spriteBatch.DrawString(
                                    m_project.getFontManager().getViewFont(view.getViewSize()),
                                    subLineToHighlight,
                                    new Vector2((int)viewSpaceTextPosition.X + m_project.getFontManager().getCharWidth(view.getViewSize()) * xPos, (int)(viewSpaceTextPosition.Y + yPosition)),
                                    bufferColour,
                                    0,
                                    Vector2.Zero,
                                    m_project.getFontManager().getTextScale() * (float)textScale,
                                    0,
                                    0);

                                xPos = m_highlights[nextHighlight].m_startHighlight.X;
                            }

                            if (xPos == m_highlights[nextHighlight].m_startHighlight.X)
                            {
                                // Capture substring, increment xPos and draw the highlighted area - watch for
                                // highlights that span lines longer than our presented line (line).
                                //
                                string subLineInHighlight = line.Substring(m_highlights[nextHighlight].m_startHighlight.X,
                                                                           Math.Min(m_highlights[nextHighlight].m_endHighlight.X - m_highlights[nextHighlight].m_startHighlight.X, line.Length - m_highlights[nextHighlight].m_startHighlight.X));

                                spriteBatch.DrawString(
                                    m_project.getFontManager().getViewFont(view.getViewSize()),
                                    subLineInHighlight,
                                    new Vector2((int)viewSpaceTextPosition.X + m_project.getFontManager().getCharWidth(view.getViewSize()) * xPos, (int)(viewSpaceTextPosition.Y + yPosition)),
                                    highlightColour,
                                    0,
                                    Vector2.Zero,
                                    m_project.getFontManager().getTextScale() * (float)textScale,
                                    0,
                                    0);

                                // Step past this highlight
                                //
                                xPos = m_highlights[nextHighlight].m_endHighlight.X;
                                nextHighlight++;
                            }
                        }

                        // Draw the remainder of the line
                        //
                        if (xPos < line.Length)
                        {
                            string remainder = line.Substring(xPos, line.Length - xPos);

                            spriteBatch.DrawString(
                                m_project.getFontManager().getViewFont(view.getViewSize()),
                                remainder,
                                new Vector2((int)viewSpaceTextPosition.X + m_project.getFontManager().getCharWidth(view.getViewSize()) * xPos, (int)(viewSpaceTextPosition.Y + yPosition)),
                                bufferColour,
                                0,
                                Vector2.Zero,
                                m_project.getFontManager().getTextScale() * (float)textScale,
                                0,
                                0);
                        }
                    }
                    else  // draw the line without highlighting
                    {
                        spriteBatch.DrawString(
                            m_project.getFontManager().getViewFont(view.getViewSize()),
                            line,
                            new Vector2((int)viewSpaceTextPosition.X, (int)(viewSpaceTextPosition.Y + yPosition)),
                            bufferColour,
                            0,
                            Vector2.Zero,
                            m_project.getFontManager().getTextScale() * (float)textScale,
                            0,
                            0);
                    }

                    yPosition += m_project.getFontManager().getLineSpacing(view.getViewSize());
                }
            }

            // Draw overlaid ID on this window if we're far enough away to use it
            //
            if (zoomLevel > 950.0f)
            {
                int viewId = m_project.getBufferViews().IndexOf(view);
                string bufferId = viewId.ToString();

                if (view.isTailing())
                {
                    bufferId += "(T)";
                }
                else if (view.isReadOnly())
                {
                    bufferId += "(RO)";
                }

                Color seeThroughColour = bufferColour;
                seeThroughColour.A = 70;
                spriteBatch.DrawString(m_project.getFontManager().getViewFont(view.getViewSize()), bufferId, new Vector2((int)viewSpaceTextPosition.X, (int)viewSpaceTextPosition.Y), seeThroughColour, 0, Vector2.Zero, m_project.getFontManager().getTextScale() * 16.0f, 0, 0);

                // Show a filename
                //
                string fileName = view.getFileBuffer().getShortFileName();
                spriteBatch.DrawString(m_project.getFontManager().getViewFont(view.getViewSize()), fileName, new Vector2((int)viewSpaceTextPosition.X, (int)viewSpaceTextPosition.Y), seeThroughColour, 0, Vector2.Zero, m_project.getFontManager().getTextScale() * 4.0f, 0, 0);
            }
        }


        /// <summary>
        /// Draw the HUD Overlay for the editor with information about the current file we're viewing
        /// and position in that file.
        /// </summary>
        public void drawOverlay(SpriteBatch spriteBatch, GameTime gameTime, GraphicsDeviceManager graphics, FriendlierState state, string gotoLine, bool shiftDown, bool ctrlDown, bool altDown, Vector3 eye,
                                   string temporaryMessage, double temporaryMessageStartTime, double temporaryMessageEndTime)
        {
#if SCROLLING_TEXT
            // Flag that we set during this method
            //
            bool drawScrollingText = false;
#endif

            // Set our colour according to the state of Friendlier
            //
            Color overlayColour = Color.White;
            if (state != FriendlierState.TextEditing && state != FriendlierState.GotoLine && state != FriendlierState.FindText && state != FriendlierState.DiffPicker)
            {
                overlayColour = m_greyedColour;
            }

            // Set up some of these variables here
            //
            string positionString = m_project.getSelectedBufferView().getCursorPosition().Y + "," + m_project.getSelectedBufferView().getCursorPosition().X;
            float positionStringXPos = graphics.GraphicsDevice.Viewport.Width - positionString.Length * m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay) - (m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay) * 14);
            float filePercent = 0.0f;

            // Filename is where we put the filename plus other assorted gubbins or we put a
            // search string in there depending on the mode.
            //
            string fileName = "";

            if (state == FriendlierState.FindText)
            {
                // Draw the search string down there
                fileName = "Search: " + m_project.getSelectedBufferView().getSearchText();
            }
            else if (state == FriendlierState.GotoLine)
            {
                fileName = "Goto line: " + gotoLine;
            }
            else
            {
                if (m_project.getSelectedBufferView() != null && m_project.getSelectedBufferView().getFileBuffer() != null)
                {
                    // Set the filename
                    if (m_project.getSelectedBufferView().getFileBuffer().getShortFileName() != "")
                    {
                        fileName = "\"" + m_project.getSelectedBufferView().getFileBuffer().getShortFileName() + "\"";
                    }
                    else
                    {
                        fileName = "<New Buffer>";
                    }

                    if (m_project.getSelectedBufferView().getFileBuffer().isModified())
                    {
                        fileName += " [Modified]";
                    }

                    fileName += " " + m_project.getSelectedBufferView().getFileBuffer().getLineCount() + " lines";
                }
                else
                {
                    fileName = "<New Buffer>";
                }

                // Add some other useful states to our status line
                //
                if (m_project.getSelectedBufferView().isReadOnly())
                {
                    fileName += " [RDONLY]";
                }

                if (m_project.getSelectedBufferView().isTailing())
                {
                    fileName += " [TAIL]";
                }

                if (shiftDown)
                {
                    fileName += " [SHFT]";
                }

                if (ctrlDown)
                {
                    fileName += " [CTRL]";
                }

                if (altDown)
                {
                    fileName += " [ALT]";
                }

            }

            BufferView bv = m_project.getSelectedBufferView();

            // Convert lineHeight back to normal size by dividing by m_textSize modifier
            //
            float yPos = graphics.GraphicsDevice.Viewport.Height - m_project.getFontManager().getLineSpacing(FontManager.FontType.Overlay);

            string modeString = "none";

            switch (state)
            {
                case FriendlierState.TextEditing:
                    modeString = "edit";
                    break;

                case FriendlierState.FileOpen:
                    modeString = "browsing";
                    break;

                case FriendlierState.FileSaveAs:
                    modeString = "saving file";
                    break;

                case FriendlierState.DiffPicker:
                    modeString = "performing diff";
                    break;

                default:
                    modeString = "free";
                    break;
            }

            float modeStringXPos = graphics.GraphicsDevice.Viewport.Width - modeString.Length * m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay) - (m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay) * 8);

            if (m_project.getSelectedBufferView().getFileBuffer() != null && m_project.getSelectedBufferView().getFileBuffer().getLineCount() > 0)
            {
                filePercent = (float)(m_project.getSelectedBufferView().getCursorPosition().Y) /
                              (float)(Math.Max(1, m_project.getSelectedBufferView().getFileBuffer().getLineCount() - 1));
            }

            string filePercentString = ((int)(filePercent * 100.0f)) + "%";
            float filePercentStringXPos = graphics.GraphicsDevice.Viewport.Width - filePercentString.Length * m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay) - (m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay) * 3);

            // Debug eye position
            //
            if (m_project.getViewMode() != Project.ViewMode.Formal)
            {
                string eyePosition = "[EyePosition] X " + eye.X + ",Y " + eye.Y + ",Z " + eye.Z;
                float xPos = graphics.GraphicsDevice.Viewport.Width - eyePosition.Length * m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay);
                spriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), eyePosition, new Vector2(0.0f, 0.0f), overlayColour, 0, Vector2.Zero, 1.0f, 0, 0);
            }

            // hardcode the font size to 1.0f so that it looks nice
            //
            spriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), fileName, new Vector2(0.0f, (int)yPos), overlayColour, 0, Vector2.Zero, 1.0f, 0, 0);
            spriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), modeString, new Vector2((int)modeStringXPos, 0.0f), overlayColour, 0, Vector2.Zero, 1.0f, 0, 0);
            spriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), positionString, new Vector2((int)positionStringXPos, (int)yPos), overlayColour, 0, Vector2.Zero, 1.0f, 0, 0);
            spriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), filePercentString, new Vector2((int)filePercentStringXPos, (int)yPos), overlayColour, 0, Vector2.Zero, 1.0f, 0, 0);

            // Draw any temporary message
            //
            drawTemporaryMessage(spriteBatch, graphics, gameTime, Color.HotPink, temporaryMessage, temporaryMessageStartTime, temporaryMessageEndTime);

#if SCROLLING_TEXT
            // Draw the scrolling text
            //
            if (m_textScrollTexture != null && drawScrollingText)
            {
                m_spriteBatch.Begin();
                m_spriteBatch.Draw(m_textScrollTexture, new Rectangle((int)((fileName.Length + 1) * m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay)), (int)yPos, m_textScrollTexture.Width, m_textScrollTexture.Height), Color.White);
                m_spriteBatch.End();
            }
#endif

        }

        /// <summary>
        /// Draw temporary message by fade in/fade out
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="overlayColour"></param>
        protected void drawTemporaryMessage(SpriteBatch spriteBatch, GraphicsDeviceManager graphics, GameTime gameTime, Color overlayColour, string temporaryMessage, double temporaryMessageStartTime, double temporaryMessageEndTime)
        {
            if (temporaryMessage == "" || gameTime.TotalGameTime.TotalSeconds > temporaryMessageEndTime)
            {
                return;
            }

            // Now calculate the colour according to the time - fade in/fade out is currently linear
            //
            Color fadeColour = overlayColour;

            float blankTime = 0.1f; // seconds
            float fadeTime = 0.4f; // seconds
            if (gameTime.TotalGameTime.TotalSeconds - temporaryMessageStartTime < blankTime)
            {
                fadeColour = Color.Black;
                fadeColour.A = 0;
            }
            else if (gameTime.TotalGameTime.TotalSeconds - temporaryMessageStartTime < fadeTime)
            {
                double percent = (gameTime.TotalGameTime.TotalSeconds - temporaryMessageStartTime - blankTime) / (fadeTime - blankTime);
                fadeColour.R = (byte)((double)overlayColour.R * percent);
                fadeColour.G = (byte)((double)overlayColour.G * percent);
                fadeColour.B = (byte)((double)overlayColour.B * percent);
                fadeColour.A = (byte)((double)overlayColour.A * percent);
            }
            else if (gameTime.TotalGameTime.TotalSeconds > temporaryMessageEndTime - fadeTime)
            {
                double percent = (temporaryMessageEndTime - gameTime.TotalGameTime.TotalSeconds) / fadeTime;
                fadeColour.R = (byte)((double)overlayColour.R * percent);
                fadeColour.G = (byte)((double)overlayColour.G * percent);
                fadeColour.B = (byte)((double)overlayColour.B * percent);
                fadeColour.A = (byte)((double)overlayColour.A * percent);
            }

            // How many lines are we going to show for this temporary message?
            //
            List<string> splitString = splitStringNicely(temporaryMessage, m_project.getSelectedBufferView().getBufferShowWidth());

            // Set x and Y accordingly
            //
            float yPos = graphics.GraphicsDevice.Viewport.Height - ((splitString.Count + 3) * m_project.getFontManager().getOverlayFont().LineSpacing);

            //m_overlaySpriteBatch.Begin();
            for (int i = 0; i < splitString.Count; i++)
            {
                float xPos = graphics.GraphicsDevice.Viewport.Width / 2 - m_project.getFontManager().getOverlayFont().MeasureString("X").X * splitString[i].Length / 2;
                spriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), splitString[i], new Vector2(xPos, yPos), fadeColour, 0, Vector2.Zero, 1.0f, 0, 0);
                yPos += m_project.getFontManager().getOverlayFont().LineSpacing;
            }
            //m_overlaySpriteBatch.End();
        }



        /// <summary>
        /// Split at string along a given length along word boundaries.   We try to split on space first,
        /// then forwardslash, then backslash.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        protected List<string> splitStringNicely(string line, int width)
        {
            List<string> rS = new List<string>();

            if (line.Length < width)
            {
                rS.Add(line);
                return rS;
            }

            int splitPos = 0;
            while (splitPos < line.Length)
            {
                // Split to max width
                //
                string splitString = line.Substring(splitPos, Math.Min(width, line.Length - splitPos));

                int findPos = splitString.LastIndexOf(" ");

                if (findPos == -1)
                {
                    findPos = splitString.LastIndexOf("/");

                    if (findPos == -1)
                    {
                        findPos = splitString.LastIndexOf("\\");

                        if (findPos == -1)
                        {
                            findPos = splitString.LastIndexOf("/");
                        }
                    }
                }

                if (findPos != -1)
                {
                    // Step past this match character for next match
                    //
                    if (findPos + splitPos < line.Length)
                    {
                        findPos++;
                    }
                    splitString = line.Substring(splitPos, findPos);
                }

                /*
                // If there's no space in our substring then we cheat
                //
                if (splitString == "")
                {
                    if ((line.Length - splitPos) < width)
                    {
                        rS.Add(line.Substring(splitPos, line.Length - splitPos));
                        splitPos = line.Length; // and exit
                    }
                    else // greater than width
                    {
                        rS.Add(line.Substring(splitPos, width));
                        splitPos += width; // and continue splitting
                    }
                }
                else
                {*/
                splitPos += splitString.Length;

                //                    if (splitPos < line.Length)
                //{
                //  splitPos++;
                //}

                rS.Add(splitString);
                //}
            }

            return rS;
        }

        /// <summary>
        /// Get the banner start time
        /// </summary>
        /// <returns></returns>
        public double getBannerStartTime()
        {
            return m_bannerStartTime;
        }

        /// <summary>
        /// Start drawing a zooming banner
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="bannerString"></param>
        /// <param name="seconds"></param>
        public void startBanner(GameTime gameTime, string bannerString, float seconds)
        {
            m_bannerStartTime = gameTime.TotalGameTime.TotalSeconds;
            m_bannerString = bannerString;
            m_bannerDuration = seconds;
            m_bannerColour.R = 255;
            m_bannerColour.B = 255;
            m_bannerColour.G = 255;
            m_bannerColour.A = 180;

            m_bannerStringList = new List<string>();

            foreach (string str in m_bannerString.Split('\n'))
            {
                m_bannerStringList.Add(str);
            }
        }

        /// <summary>
        /// Draw a zooming banner
        /// </summary>
        /// <param name="gameTime"></param>
        public void drawBanner(SpriteBatch spriteBatch, GameTime gameTime, Effect basicEffect, Texture2D splashScreen)
        {
            // Don't do anything if we don't have anything to draw
            //
            if (m_bannerStringList == null || m_bannerStringList.Count == 0)
            {
                return;
            }

            float scale = (float)(Math.Pow(gameTime.TotalGameTime.TotalSeconds - m_bannerStartTime + 0.4, 6));

            // Stop display this at some point by resetting the m_bannerStartTime - we can also stop displaying if the scale is too big
            //
            if ((m_bannerStartTime + m_bannerDuration) < gameTime.TotalGameTime.TotalSeconds)
            {
                m_bannerStartTime = -1;
                return;
            }

            if (scale > 100.0f)
            {
                m_bannerColour.A--; ;
                m_bannerColour.R--;
                m_bannerColour.B--;
                m_bannerColour.G--;
            }

            //if (m_bannerAlpha < 0)
            //{
            //m_bannerStartTime = -1;
            //return;
            //}

            Vector3 position = m_project.getSelectedBufferView().getPosition();

            // Start with centering adjustments
            //
            int maxLength = 0;
            foreach (string banner in m_bannerStringList)
            {
                if (banner.Length > maxLength)
                {
                    maxLength = banner.Length;
                }
            }
            int xPosition = 0;
            int yPosition = 0;

            bool isText = false;

            spriteBatch.Begin(SpriteSortMode.Texture, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.DepthRead, RasterizerState.CullNone, basicEffect);

            if (isText)
            {
                xPosition = -(int)((scale * maxLength * m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay) / 2));
                yPosition = -(int)((m_bannerStringList.Count * scale * m_project.getFontManager().getLineSpacing(FontManager.FontType.Overlay) / 2));

                // Add half the editing window width and height
                //
                xPosition += (int)(m_project.getSelectedBufferView().getVisibleWidth() / 2);
                yPosition += (int)(m_project.getSelectedBufferView().getVisibleHeight() / 2);

                // Add window position - so we're doing this a bit backwards but you get the idea
                //
                xPosition += (int)(position.X);
                yPosition += (int)(position.Y);

                if (m_bannerColour.R == m_bannerColour.G && m_bannerColour.G == m_bannerColour.B && m_bannerColour.B == 0)
                {
                    m_bannerStartTime = -1;
                    return;
                }


                //float scale = diff.Ticks;
                // Draw to the render target
                //
                Vector3 curPos = m_project.getSelectedBufferView().getPosition();

                spriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), m_bannerString, new Vector2((int)xPosition, (int)yPosition), m_bannerColour, 0, new Vector2(0, 0), scale, 0, 0);
            }
            else
            {
                xPosition = (int)m_project.getEyePosition().X;
                yPosition = (int)m_project.getEyePosition().Y;

                xPosition += (int)(scale * (float)splashScreen.Width / 2.0f);
                yPosition += (int)(scale * (float)splashScreen.Height / 2.0f);

                spriteBatch.Draw(splashScreen, new Vector2((int)xPosition, (int)yPosition), null, Color.White, 0f, Vector2.Zero, scale, 0, 0);
            }

            spriteBatch.End();
        }
    }
}
