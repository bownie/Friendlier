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
    }
}
