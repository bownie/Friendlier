#region File Description
//-----------------------------------------------------------------------------
// FileRenderer.cs
//
// Copyright (C) Xyglo. All rights reserved.
//
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Xyglo
{
    public static class FileRenderer
    {
        class SubjectShape
        {
            /// <summary>
            /// The array of vertices the shape can use.
            /// </summary>
            public VertexPositionColor[] Vertices;

            /// <summary>
            /// The number of lines to draw for this shape.
            /// </summary>
            public int LineCount;

            /// <summary>
            /// The length of time to keep this shape visible.
            /// </summary>
            public float Lifetime;
        }

        // We use a cache system to reuse our DebugShape instances to avoid creating garbage
        //
        private static readonly List<SubjectShape> cachedShapes = new List<SubjectShape>();
        private static readonly List<SubjectShape> activeShapes = new List<SubjectShape>();

        // Allocate an array to hold our vertices; this will grow as needed by our renderer
        //
        private static VertexPositionColor[] verts = new VertexPositionColor[64];

        // FileName to display
        //
        private static String             fileName;

        // Our graphics device and the effect we use to render the shapes
        //
        private static GraphicsDevice   graphics;
        private static BasicEffect      basicEffect;

        // Font to store
        //
        private static SpriteFont       m_font;
        private static SpriteBatch      m_spriteBatch;


        public static Vector3 horizontalUnit = new Vector3(5f, 0f, 0f);
        public static Vector3 verticalUnit = new Vector3(0f, 5f, 0f);
        public static Vector3 depthUnit = new Vector3(0f, 0f, -0.5f);

        public static void Initialize(GraphicsDevice graphicsDevice, SpriteFont subjectFont)
        {
            // If we already have a graphics device, we've already initialized once. We don't allow that.
            if (graphics != null)
                throw new InvalidOperationException("Initialize can only be called once.");

            // Save the graphics device
            graphics = graphicsDevice;

            // Create and initialize our effect
            basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.TextureEnabled = false;
            basicEffect.DiffuseColor = Vector3.One;
            basicEffect.World = Matrix.Identity;

            //basicEffect.EnableDefaultLighting();
            //basicEffect.PreferPerPixelLighting = true;

            // Set up our font
            //
            m_font = subjectFont;

            // And a SpriteBatch
            //
            m_spriteBatch = new SpriteBatch(graphicsDevice);
        }

        /// <summary>
        /// A method used for sorting our cached shapes based on the size of their vertex arrays.
        /// </summary>
        private static int CachedShapesSort(SubjectShape s1, SubjectShape s2)
        {
            return s1.Vertices.Length.CompareTo(s2.Vertices.Length);
        }

        /// <summary>
        /// Gets a DebugShape instance for a given line counta and lifespan.
        /// </summary>
        private static SubjectShape GetShapeForLines(int lineCount, float life)
        {
            SubjectShape shape = null;

            // We go through our cached list trying to find a shape that contains
            // a large enough array to hold our desired line count. If we find such
            // a shape, we move it from our cached list to our active list and break
            // out of the loop.
            int vertCount = lineCount * 2;
            for (int i = 0; i < cachedShapes.Count; i++)
            {
                if (cachedShapes[i].Vertices.Length >= vertCount)
                {
                    shape = cachedShapes[i];
                    cachedShapes.RemoveAt(i);
                    activeShapes.Add(shape);
                    break;
                }
            }

            // If we didn't find a shape in our cache, we create a new shape and add it
            // to the active list.
            if (shape == null)
            {
                shape = new SubjectShape { Vertices = new VertexPositionColor[vertCount] };
                activeShapes.Add(shape);
            }

            // Set the line count and lifetime of the shape based on our parameters.
            shape.LineCount = lineCount;
            shape.Lifetime = life;

            return shape;
        }

        public static void Draw(GameTime gameTime, Matrix view, Matrix projection)
        {
            // Update our effect with the matrices.
            basicEffect.View = view;
            basicEffect.Projection = projection;

            // Calculate the total number of vertices we're going to be renderinm_graph.
            int vertexCount = 0;
            foreach (var shape in activeShapes)
                vertexCount += shape.LineCount * 2;

            // If we have some vertices to draw
            if (vertexCount > 0)
            {
                // Make sure our array is large enough
                if (verts.Length < vertexCount)
                {
                    // If we have to resize, we make our array twice as large as necessary so
                    // we hopefully won't have to resize it for a while.
                    verts = new VertexPositionColor[vertexCount * 2];
                }

                // Now go through the shapes again to move the vertices to our array and
                // add up the number of lines to draw.
                int lineCount = 0;
                int vertIndex = 0;
                foreach (SubjectShape shape in activeShapes)
                {
                    lineCount += shape.LineCount;
                    int shapeVerts = shape.LineCount * 2;
                    for (int i = 0; i < shapeVerts; i++)
                        verts[vertIndex++] = shape.Vertices[i];
                }

                // Start our effect to begin renderinm_graph.
                basicEffect.CurrentTechnique.Passes[0].Apply();

                // We draw in a loop because the Reach profile only supports 65,535 primitives. While it's
                // not incredibly likely, if a game tries to render more than 65,535 lines we don't want to
                // crash. We handle this by doing a loop and drawing as many lines as we can at a time, capped
                // at our limit. We then move ahead in our vertex array and draw the next set of lines.
                int vertexOffset = 0;
                while (lineCount > 0)
                {
                    // Figure out how many lines we're going to draw
                    int linesToDraw = Math.Min(lineCount, 65535);

                    // Draw the lines
                    graphics.DrawUserPrimitives(PrimitiveType.LineList, verts, vertexOffset, linesToDraw);

                    // Move our vertex offset ahead based on the lines we drew
                    vertexOffset += linesToDraw * 2;

                    // Remove these lines from our total line count
                    lineCount -= linesToDraw;
                }
            }
        }

        public static void startBatch(BasicEffect basicEffect)
        {
            //m_spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.DepthRead, RasterizerState.CullNone, basicEffect);
            m_spriteBatch.Begin(0, null, null, DepthStencilState.DepthRead, RasterizerState.CullNone, basicEffect);
        }

        public static void endBatch()
        {
            m_spriteBatch.End();
        }

        /// <summary>
        /// Edit a File in our renderer
        /// </summary>
        /// <param name="fileIn"></param>
        public static void editFile(Vector3 v, Vector3 pry, Color color, float life, string fileIn)
        {
            // Set our editing filename
            //
            fileName = fileIn;

            int bufferLimit = 30;
            List<string> list = new List<string>();
 
            // Check for file existing
            //
            using (StreamReader sr = new StreamReader(fileName))
            {
                string line;
                while ((line = sr.ReadLine()) != null && list.Count < bufferLimit)
                {
                    list.Add(line);
                }
            }

            Vector3 textPosition = v + horizontalUnit / 2 - verticalUnit / 2;

            //basicEffect.World = Matrix.CreateScale(1, -1, 1) * Matrix.CreateTranslation(textPosition);


            // OLD METHOD
            
            //spriteBatch.Begin(0, null, null, DepthStencilState.DepthRead, RasterizerState.CullNone, basicEffect);
            //spriteBatch.Begin(0, BlendState.AlphaBlend, null, DepthStencilState.DepthRead, RasterizerState.CullNone, basicEffect);
            //m_spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.DepthRead, RasterizerState.CullNone, basicEffect);

            // Nice looking
            //
            //m_spriteBatch.Begin(0, null, null, DepthStencilState.DepthRead, RasterizerState.CullNone, basicEffect);

            Matrix invertY = new Matrix(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

            float yPosition = 0.0f;

            // textSize is going to define everything
            //
            const float textSize = 0.05f;

            float xAdjust = textSize * 10.0f;
            float yAdjust = textSize * 10.0f;

            Vector2 adjustOrigin = new Vector2(xAdjust, yAdjust);
            Vector2 lineOrigin = new Vector2(v.X, v.Y) + adjustOrigin;

            float maxWidth = 0.0f;

            foreach (string line in list)
            {
                if (m_font.MeasureString(line).X > maxWidth)
                {
                    maxWidth = m_font.MeasureString(line).X;
                }

                Vector3 viewSpaceTextPosition = Vector3.Transform(textPosition, basicEffect.View * invertY);

                m_spriteBatch.DrawString(m_font, line, new Vector2(viewSpaceTextPosition.X, viewSpaceTextPosition.Y + yPosition), Color.White, 0, lineOrigin, textSize, 0, 0);

                //Vector2 textOrigin = m_font.MeasureString(line) / 2;
                //m_spriteBatch.DrawString(m_font, line, new Vector2(viewSpaceTextPosition.X, viewSpaceTextPosition.Y), Color.White, 0, textOrigin, textSize, 0, viewSpaceTextPosition.Z);
                yPosition += m_font.MeasureString(line).Y * textSize;

            }

            //m_spriteBatch.End();
            
            /*
            // NEW METHOD
            //
            float yPosition = 0.0f;

            // textSize is going to define everything
            //
            const float textSize = 0.1f;

            float xAdjust = textSize * 10.0f;
            float yAdjust = textSize * 10.0f;

            Vector2 adjustOrigin = new Vector2(xAdjust, yAdjust);
            Vector2 lineOrigin = new Vector2(v.X, v.Y) + adjustOrigin;

            float maxWidth = 0.0f;

            Matrix invertY = new Matrix(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
            basicEffect.World = Matrix.CreateScale(1, -1, 1) * Matrix.CreateTranslation(textPosition);


            foreach(string line in list)
            {
                if (  m_font.MeasureString(line).X > maxWidth )
                {
                    maxWidth = m_font.MeasureString(line).X;
                }

                Vector3 viewSpaceTextPosition = Vector3.Transform(textPosition, basicEffect.View * invertY);

                m_spriteBatch.DrawString(m_font, line, new Vector2(viewSpaceTextPosition.X, viewSpaceTextPosition.Y + yPosition), Color.White, 0, lineOrigin, textSize, 0, 0);
                
                //Vector2 textOrigin = m_font.MeasureString(line) / 2;
                //m_spriteBatch.DrawString(m_font, line, new Vector2(viewSpaceTextPosition.X, viewSpaceTextPosition.Y), Color.White, 0, textOrigin, textSize, 0, viewSpaceTextPosition.Z);
                yPosition += m_font.MeasureString(line).Y * textSize;

            }
            */


            // Get a DebugShape we can use to draw the triangle
            //
            SubjectShape shape = GetShapeForLines(8, life);

            Vector3 boxWidth = new Vector3(maxWidth * textSize + xAdjust / textSize * 2.0f, 0, 0);
            Vector3 boxHeight = new Vector3(0, m_font.MeasureString(list[0]).Y * textSize * list.Count + yAdjust / textSize * 2.0f, 0);

            // Add the vertices to the shape
            //
            shape.Vertices[0] = new VertexPositionColor(v, color);
            shape.Vertices[1] = new VertexPositionColor(v + boxWidth, color);
            shape.Vertices[2] = new VertexPositionColor(v + boxWidth, color);
            shape.Vertices[3] = new VertexPositionColor(v + boxWidth - boxHeight, color);
            shape.Vertices[4] = new VertexPositionColor(v + boxWidth - boxHeight, color);
            shape.Vertices[5] = new VertexPositionColor(v - boxHeight, color);
            shape.Vertices[6] = new VertexPositionColor(v - boxHeight, color);
            shape.Vertices[7] = new VertexPositionColor(v, color);
        }

    }
}
