#region File Description
//-----------------------------------------------------------------------------
// DevRenderEngine.cs
//
// Copyright (C) Xyglo. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Xyglo
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class DevRenderEngine : Game
    {
        GraphicsDeviceManager   graphics;
        SpriteBatch             spriteBatch;

        SpriteFont spriteFont;// new font variable
        BasicEffect basicEffect; // new basic effect

        // The shapes that we'll be drawing
        BoundingBox             box;
        BoundingFrustum         frustum;
        BoundingSphere          sphere;

        public DevRenderEngine()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferMultiSampling = true;
            
            //graphics.IsFullScreen = true;

            //PresentationParameters pp = GraphicsDevice.PresentationParameters;
            //pp.BackBufferFormat = SurfaceFormat.

            //InitGraphicsMode(1024, 768, true);

#if WINDOWS_PHONE
            TargetElapsedTime = TimeSpan.FromTicks(333333);
            graphics.IsFullScreen = true;
#endif
        }

        /// <summary>
        /// Attempt to set the display mode to the desired resolution.  Itterates through the display
        /// capabilities of the default graphics adapter to determine if the graphics adapter supports the
        /// requested resolution.  If so, the resolution is set and the function returns true.  If not,
        /// no change is made and the function returns false.
        /// </summary>
        /// <param name="iWidth">Desired screen width.</param>
        /// <param name="iHeight">Desired screen height.</param>
        /// <param name="bFullScreen">True if you wish to go to Full Screen, false for Windowed Mode.</param>
        private bool InitGraphicsMode(int iWidth, int iHeight, bool bFullScreen)
        {
            // If we aren't using a full screen mode, the height and width of the window can
            // be set to anything equal to or smaller than the actual screen size.
            if (bFullScreen == false)
            {
                if ((iWidth <= GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width)
                    && (iHeight <= GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height))
                {
                    graphics.PreferredBackBufferWidth = iWidth;
                    graphics.PreferredBackBufferHeight = iHeight;
                    graphics.IsFullScreen = bFullScreen;
                    graphics.ApplyChanges();
                    return true;
                }
            }
            else
            {
                // If we are using full screen mode, we should check to make sure that the display
                // adapter can handle the video mode we are trying to set.  To do this, we will
                // iterate thorugh the display modes supported by the adapter and check them against
                // the mode we want to set.
                foreach (DisplayMode dm in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
                {
                    // Check the width and height of each mode against the passed values
                    if ((dm.Width == iWidth) && (dm.Height == iHeight))
                    {
                        // The mode is supported, so set the buffer formats, apply changes and return
                        graphics.PreferredBackBufferWidth = iWidth;
                        graphics.PreferredBackBufferHeight = iHeight;
                        graphics.IsFullScreen = bFullScreen;
                        graphics.ApplyChanges();
                        return true;
                    }
                }
            }
            return false;
        }

        // Set the 3D model to draw.
        Model myModel;

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            spriteFont = Content.Load<SpriteFont>("Courier New");

            //myModel = Content.Load<Model>("Models\\untitled");

            // Create a box that is centered on the origin and extends from (-3, -3, -3) to (3, 3, 3)
            box = new BoundingBox(new Vector3(-3f), new Vector3(3f));

            // Create our frustum to simulate a camera sitting at the origin, looking down the X axis, with a 16x9
            // aspect ratio, a near plane of 1, and a far plane of 5
            Matrix frustumView = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitX, Vector3.Up);
            Matrix frustumProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 16f / 9f, 1f, 5f);
            frustum = new BoundingFrustum(frustumView * frustumProjection);

            // Create a sphere that is centered on the origin and has a radius of 3
            sphere = new BoundingSphere(Vector3.Zero, 3f);

            // Initialize our renderer
            DebugShapeRenderer.Initialize(GraphicsDevice);

            // Initialize our other renderer
            //
            SubjectRenderer.Initialize(GraphicsDevice, spriteFont);

            // Make mouse visible
            //
            IsMouseVisible = true;

            /* NEW METHOD for font projection */
            basicEffect = new BasicEffect(GraphicsDevice)
            {
                TextureEnabled = true,
                VertexColorEnabled = true,
            };
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allow the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.Escape))
                this.Exit();

            if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.Up))
            {
                m_eye.Z -= 0.1f;
            }
            else if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.Down))
            {
                m_eye.Z += 0.1f;
            }
            else if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.Left))
            {
                m_eye.X += 0.1f;
            }
            else if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.Right))
            {
                m_eye.X -= 0.1f;
            }
            else if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.A))
            {
                m_eye.Y += 0.1f;
            }
            else if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.Z))
            {
                m_eye.Y -= 0.1f;
            }
            else if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.Space)) // Reset
            {
                m_eye.X = 12f;
                m_eye.Y = 5f;
                m_eye.Z = 0f;
            }

            base.Update(gameTime);
        }

        // Our POV - original postion
        //
        Vector3 m_eye = new Vector3(0f, 0f, 18f);
        Vector3 m_lastEyePosition = new Vector3(-1f, -1f, -1f);

        // Are we spinning?
        //
        bool spinning = false;

        Vector2 FontPos = new Vector2(150, 150);

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // Set last eye position
            //
            //if (m_lastEyePosition == m_eye)
            //{
                //return;
            //}

            /*
            spriteBatch.Begin();

            // Draw Hello World
            string output = "Hello World";

            // Find the center of the string
            Vector2 FontOrigin = spriteFont.MeasureString(output) / 2;

            // Draw the string
            spriteBatch.DrawString(spriteFont, output, FontPos, Color.LightGreen,
                0, FontOrigin, 1.0f, SpriteEffects.None, 0.5f);
            spriteBatch.End();
             */
            

            // If spinning then spin around current position based on time.
            //
            if (spinning)
            {
                float angle = (float)gameTime.TotalGameTime.TotalSeconds;
                m_eye.X = (float)Math.Cos(angle * .5f) * 12f;
                m_eye.Z = (float)Math.Sin(angle * .5f) * 12f;
            }

            // Construct our view and projection matrices
            //
            Matrix view = Matrix.CreateLookAt(m_eye, Vector3.Zero, Vector3.Up);
            Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, .1f, 1000f);


            //basicEffect.World = Matrix.CreateScale(1, -1, 1);// *Matrix.CreateTranslation(textPosition);
            basicEffect.View = view;
            basicEffect.Projection = projection;

            // Add our shapes to be rendered
            //
            DebugShapeRenderer.AddBoundingBox(box, Color.Yellow);
            DebugShapeRenderer.AddBoundingFrustum(frustum, Color.Green);
            DebugShapeRenderer.AddBoundingSphere(sphere, Color.Red);

            // Also add a triangle and a line
            //
            //DebugShapeRenderer.AddTriangle(new Vector3(-1f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(0f, 2f, 0f), Color.Purple);
            //DebugShapeRenderer.AddLine(new Vector3(0f, 0f, 0f), new Vector3(3f, 3f, 3f), Color.Brown);


            //DebugShapeRenderer.AddTriangle(new Vector3(-10f, 2f, 0f), new Vector3(-7f, 6f, 0f), new Vector3(0f, -8f, 0f), Color.Blue);
            //DebugShapeRenderer.AddBoundingSphere(sphere, Color.White);


            // Render our shapes now
            //
            DebugShapeRenderer.Draw(gameTime, view, projection);

            // Subject renderer work
            //
            SubjectRenderer.AddSubjectShape(new Vector3(-0f, -0f, 0f), Color.Pink, basicEffect, "My pink Shape");
            SubjectRenderer.AddSubjectShape(new Vector3(-4f, -2f, 5f), Color.Blue, basicEffect, "My BLUE shape");

            /*
            for (float i = 0; i < 100; i++)
            {
                Color myColour = new Color(100 + i, 200 - i, i, 1);

                float X = 50 -  400f / i * 10; 
                float Y = 200f / i;
                float Z = 100f / i;
                SubjectRenderer.AddSubjectShape(new Vector3(X, Y, Z), myColour, basicEffect, "New shape");
            }
*/

            SubjectRenderer.Draw(gameTime, view, projection);


            //Vector2 position = new Vector2(50, 50);


            // Store this
            //
            m_lastEyePosition = m_eye;


            /* NEW TEST */
            /*
            Vector3 textPosition = new Vector3(-2, 3, -1);

            const string message = "hello, world!";
            Vector2 textOrigin = spriteFont.MeasureString(message) / 2;
            const float textSize = 0.025f;

            spriteBatch.Begin(0, null, null, DepthStencilState.DepthRead, RasterizerState.CullNone, basicEffect);
            spriteBatch.DrawString(spriteFont, message, Vector2.Zero, Color.White, 0, textOrigin, textSize, 0, 0);
            spriteBatch.End();
            */

            base.Draw(gameTime);
        }
    }
}
