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
    }
}
