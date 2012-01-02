#region File Description
//-----------------------------------------------------------------------------
// SubjectRenderer.cs
//
// Copyright (C) Xyglo. All rights reserved.
//
#endregion
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Xyglo
{
    public static class SubjectRenderer
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


        // Our graphics device and the effect we use to render the shapes
        //
        private static GraphicsDevice graphics;
        private static BasicEffect effect;

        // Font to store
        //
        private static SpriteFont font;
        private static SpriteBatch spriteBatch;

        // An array we use to get corners from frustums and bounding boxes
        //
        private static Vector3[] corners = new Vector3[8];

        // This holds the vertices for our unit sphere that we will use when drawing bounding spheres
        //
        private const int sphereResolution = 30; // number of points in sphere
        private const int sphereLineCount = (sphereResolution + 1) * 3;
        //private static Vector3[] unitSphere;

        /// <summary>
        /// Initializes the renderer.
        /// </summary>
        /// <param name="graphicsDevice">The GraphicsDevice to use for renderinm_graph.</param>
        public static void Initialize(GraphicsDevice graphicsDevice, SpriteFont subjectFont)
        {
            // If we already have a graphics device, we've already initialized once. We don't allow that.
            if (graphics != null)
                throw new InvalidOperationException("Initialize can only be called once.");

            // Save the graphics device
            graphics = graphicsDevice;

            // Create and initialize our effect
            effect = new BasicEffect(graphicsDevice);
            effect.VertexColorEnabled = true;
            effect.TextureEnabled = false;
            effect.DiffuseColor = Vector3.One;
            effect.World = Matrix.Identity;

            // Set up our font
            //
            font = subjectFont;

            // And a SpriteBatch
            //
            spriteBatch = new SpriteBatch(graphicsDevice);

            // Create our unit sphere vertices
            //InitializeSphere();
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
            effect.View = view;
            effect.Projection = projection;

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
                effect.CurrentTechnique.Passes[0].Apply();

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

        public static void AddSubjectShape(Vector3 v, Color color, BasicEffect basicEffect, String text)
        {
            AddSubjectShape(v, color, 0f, basicEffect, text);
        }

        public static Vector3 horizontalUnit = new Vector3(5f, 0f, 0f);
        public static Vector3 verticalUnit = new Vector3(0f, 5f, 0f);
        public static Vector3 depthUnit = new Vector3(0f, 0f, -0.5f);


        // Render  
        //private static Quad quad;
        //private static VertexDeclaration quadVertexDec;
        //private static RenderTarget2D rtSprite;
        //private static Matrix view, projection, world;

        /// <summary>
        /// Adds a SubjectShape to be rendered.
        /// </summary>
        public static void AddSubjectShape(Vector3 v, Color color, float life, BasicEffect basicEffect, String text)
        {

            // Get a DebugShape we can use to draw the triangle
            SubjectShape shape = GetShapeForLines(8, life);

            // Add the vertices to the shape
            //
            shape.Vertices[0] = new VertexPositionColor(v, color);
            shape.Vertices[1] = new VertexPositionColor(v + horizontalUnit, color);
            shape.Vertices[2] = new VertexPositionColor(v + horizontalUnit, color);
            shape.Vertices[3] = new VertexPositionColor(v + horizontalUnit - verticalUnit, color);
            shape.Vertices[4] = new VertexPositionColor(v + horizontalUnit - verticalUnit, color);
            shape.Vertices[5] = new VertexPositionColor(v - verticalUnit, color);
            shape.Vertices[6] = new VertexPositionColor(v - verticalUnit, color);
            shape.Vertices[7] = new VertexPositionColor(v, color);


            /*
            spriteBatch.Begin();

            // Draw Hello World
            string output = "SubjectShape";

            // Find the center of the string
            Vector2 FontOrigin = font.MeasureString(output) / 2;

            // Draw the string
            spriteBatch.DrawString(font, output, new Vector2(10, 10), Color.LightGreen,
                0, FontOrigin, 1.0f, SpriteEffects.None, 0.5f);
            spriteBatch.End();
            */

            /*
            view = Matrix.CreateLookAt(new Vector3(0, 0, 1), Vector3.Zero, Vector3.Up);
            projection = Matrix.CreatePerspectiveFieldOfView(Convert.ToSingle(Math.PI / 4), 1.33f, 1, 500);
            world = Matrix.Identity;

            rtSprite = new RenderTarget2D(graphics, 100, 20, true, SurfaceFormat.Color, DepthFormat.Depth16);

            quad = new Quad(Vector3.Forward, Vector3.Backward, Vector3.Up, 2, 2);
            effect = new BasicEffect(graphics);
            effect.EnableDefaultLighting();
            effect.TextureEnabled = true;
            effect.View = view;
            effect.Projection = projection;
            effect.World = world;

            //quadVertexDec = new VertexDeclaration(graphics, VertexPositionNormalTexture);  
            */
            Vector3 textPosition = v + horizontalUnit / 2 - verticalUnit / 2;

            //Vector3 textPosition = v;  //new Vector3(0.2f, 0.3f, 0.1f);

//            effect.World = Matrix.CreateScale(1, -1, 1) * Matrix.CreateTranslation(textPosition);
            //effect.View = view;
            //effect.Projection = projection;

            basicEffect.World = Matrix.CreateScale(1, -1, 1) * Matrix.CreateTranslation(textPosition);

            Vector2 textOrigin = font.MeasureString(text) / 2;
            const float textSize = 1.0f;

            spriteBatch.Begin(0, null, null, DepthStencilState.DepthRead, RasterizerState.CullNone, basicEffect);
            spriteBatch.DrawString(font, text, Vector2.Zero, Color.White, 0, textOrigin, textSize, 0, 0);

            spriteBatch.End();

        }

    }
}
