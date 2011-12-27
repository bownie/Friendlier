#region File Description
//-----------------------------------------------------------------------------
// DevRenderEngine.cs
//
// Copyright (C) Xyglo. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


namespace Xyglo
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Friendlier : Game
    {
        // XNA stuff
        //
        GraphicsDeviceManager m_graphics;
        SpriteBatch m_spriteBatch;
        SpriteFont m_spriteFont;

        // Our effects - one with textures for fonts, one without for lines
        //
        BasicEffect m_basicEffect;
        BasicEffect m_lineEffect;

        Matrix m_view;
        Matrix m_projection;

        /// <summary>
        /// Eye location
        /// </summary>
        Vector3 m_eye = new Vector3(0f, 0f, 275f);  // 275 is good

        //Vector3 m_lastEyePosition = new Vector3(-1f, -1f, -1f);

        /// <summary>
        /// The unit position in a text file (character and line) 
        /// </summary>
        Vector2 m_cursorPosition /*= new FilePosition(); */ = new Vector2(0, 0);

        /// <summary>
        /// Cursor coordinates in 3D
        /// </summary>
        Vector3 m_cursorCoords = Vector3.Zero;

        /// <summary>
        /// 3d coordinates of the top left hand of the file
        /// </summary>
        Vector3 m_fileCoords = new Vector3(-180f, -100f, 0f);

        // textSize is going to define everything
        //
        const float m_textSize = 0.5f;

        /// <summary>
        /// Are we spinning?
        /// </summary>
        bool spinning = false;

        /// <summary>
        /// The coordinate height of a line
        /// </summary>
        float m_lineHeight = 0.0f;

        /// <summary>
        /// The coordinate width of a character
        /// </summary>
        float m_charWidth = 0.0f;

        /// <summary>
        /// File we are editing
        /// </summary>
        string m_fileName;

        /// <summary>
        /// Last keyboard state so that we can compare with current
        /// </summary>
        KeyboardState m_lastKeyboardState;

        /// <summary>
        /// Which key has been held down most recently?
        /// </summary>
        Keys m_heldKey;

        /// <summary>
        /// When did we start holding down a key?
        /// </summary>
        double m_heldDownStartTime = 0;

        /// <summary>
        /// Is either Shift key being held?
        /// </summary>
        bool m_shiftDown = false;

        /// <summary>
        /// Is either Control key being held down?
        /// </summary>
        bool m_ctrlDown = false;

        /// <summary>
        /// Where did shift get initially held down?
        /// </summary>
        Vector2 m_shiftStart = new Vector2(); // = new FilePosition();

        /// <summary>
        /// Where did we release shift?
        /// </summary>
        Vector2 m_shiftEnd = new Vector2(); // = new FilePosition();

        /// <summary>
        /// Is our current selection ready for something to happen to it?
        /// </summary>
        bool m_selectionValid = false;

        /// <summary>
        /// List of FileBuffers that we can handle
        /// </summary>
        List<FileBuffer> m_fileBuffers = new List<FileBuffer>();

        //Vector3 m_filePosition = new Vector3(-200f, -20f, 0f);
        //public Vector3 horizontalUnit = new Vector3(5f, 0f, 0f);
        //public Vector3 verticalUnit = new Vector3(0f, 5f, 0f);
        //public Vector3 depthUnit = new Vector3(0f, 0f, -0.5f);

        // Let's start playing with some Shader effects
        //
        //Effect effect;

        // The shapes that we'll be drawing
        //BoundingBox             box;
        //BoundingFrustum         frustum;
        //BoundingSphere          sphere;

        // Set the 3D model to draw.
        //Model myModel;

        public Friendlier(string file)
        {
            m_graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // Antialiasing
            //
            m_graphics.PreferMultiSampling = true;

            // File name
            //
            m_fileName = file;

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
                    m_graphics.PreferredBackBufferWidth = iWidth;
                    m_graphics.PreferredBackBufferHeight = iHeight;
                    m_graphics.IsFullScreen = bFullScreen;
                    m_graphics.ApplyChanges();
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
                        m_graphics.PreferredBackBufferWidth = iWidth;
                        m_graphics.PreferredBackBufferHeight = iHeight;
                        m_graphics.IsFullScreen = bFullScreen;
                        m_graphics.ApplyChanges();
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            m_spriteBatch = new SpriteBatch(m_graphics.GraphicsDevice);

            // Font loading
            //
            m_spriteFont = Content.Load<SpriteFont>("Courier New");
            //spriteFont = Content.Load<SpriteFont>("Miramonte");
            //spriteFont = Content.Load<SpriteFont>("cn");
            //spriteFont.Spacing = 10;

            //myModel = Content.Load<Model>("Models\\untitled");

            // Create a box that is centered on the origin and extends from (-3, -3, -3) to (3, 3, 3)
            //
            //box = new BoundingBox(new Vector3(-3f), new Vector3(3f));

            // Create our frustum to simulate a camera sitting at the origin, looking down
            // the X axis, with a 16x9 aspect ratio, a near plane of 1, and a far plane of 5
            //
            //Matrix frustumView = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitX, Vector3.Up);
            //Matrix frustumProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 16f / 9f, 1f, 5f);
            //frustum = new BoundingFrustum(frustumView * frustumProjection);

            // Create a sphere that is centered on the origin and has a radius of 3
            //
            //sphere = new BoundingSphere(Vector3.Zero, 3f);

            // Initialize our renderer
            //
            DebugShapeRenderer.Initialize(m_graphics.GraphicsDevice);

            // Initialize our other renderer
            //
            SubjectRenderer.Initialize(m_graphics.GraphicsDevice, m_spriteFont);

            // Initialise the file renderer
            //
            //FileRenderer.Initialize(GraphicsDevice, m_spriteFont);

            // Make mouse visible
            //
            IsMouseVisible = true;

            /* NEW METHOD for font projection */
            m_basicEffect = new BasicEffect(m_graphics.GraphicsDevice)
            {
                TextureEnabled = true,
                VertexColorEnabled = true,
                //World = Matrix.Identity,
                //DiffuseColor = Vector3.One

            };

            // Create and initialize our effect
            m_lineEffect = new BasicEffect(m_graphics.GraphicsDevice);
            m_lineEffect.VertexColorEnabled = true;
            m_lineEffect.TextureEnabled = false;
            m_lineEffect.DiffuseColor = Vector3.One;
            m_lineEffect.World = Matrix.Identity;


            //effect = Content.Load<Effect>("Effects/Ambient");

            // Store these sizes and positions
            //
            m_charWidth = m_spriteFont.MeasureString("X").X * m_textSize;
            m_lineHeight = m_spriteFont.MeasureString("X").Y * m_textSize;
            m_cursorCoords = m_fileCoords;

            // Load and buffer the file
            //
            m_fileBuffers.Add(new FileBuffer(m_fileName));
        }


        /// <summary>
        /// Ensure the cursor is within the boundaries set by the file and not floating in space
        /// /// </summary>
        protected void fixCursor()
        {
            int curPosX = Convert.ToInt16(m_cursorPosition.X);
            int curPosY = Convert.ToInt16(m_cursorPosition.Y);
            string line = m_fileBuffers[0].getLine(curPosY);
            int lineLength = line.Length;

            if (curPosX > lineLength)
            {
                m_cursorPosition.X = lineLength;
            }

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

            if (checkKeyState(Keys.Up, gameTime))
            {
                if (m_cursorPosition.Y > 0)
                {
                    m_cursorPosition.Y--;
                }

                fixCursor();
            }
            else if (checkKeyState(Keys.Down, gameTime))
            {
                if (m_cursorPosition.Y < 80)
                {
                    m_cursorPosition.Y++;
                }
                fixCursor();
            }
            else if (checkKeyState(Keys.Left, gameTime))
            {
                if (m_cursorPosition.X > 0)
                {
                    m_cursorPosition.X--;
                }
            }
            else if (checkKeyState(Keys.Right, gameTime))
            {
                if (m_cursorPosition.X < 80)
                {
                    m_cursorPosition.X++;
                }

                fixCursor();
            }
            else if (checkKeyState(Keys.End, gameTime))
            {
                m_cursorPosition.X = m_fileBuffers[0].getLine(Convert.ToInt16(m_cursorPosition.Y)).Length;
            }
            else if (checkKeyState(Keys.Home, gameTime))
            {
                m_cursorPosition.X = 0;
            }
            else if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.OemPeriod))
            {
                m_eye.X += 3f;
            }
            else if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.OemComma))
            {
                m_eye.X -= 3f;
            }
            else if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.A))
            {
                //m_eye.Y += 3f;
                m_eye.Z -= 10.0f;
            }
            else if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.Z))
            {
                //m_eye.Y -= 3f;
                m_eye.Z += 10.0f;
            }
            else if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.Space)) // Reset
            {
                m_eye.X = 12f;
                m_eye.Y = 5f;
                m_eye.Z = 0f;
            }
            else if (checkKeyState(Keys.Delete, gameTime) || checkKeyState(Keys.Back, gameTime))
            {
                // If we have a valid selection then delete it
                //
                if (m_selectionValid)
                {
                    FilePosition shiftStart = new FilePosition(m_shiftStart);
                    FilePosition shiftEnd = new FilePosition(m_shiftEnd);
                    m_fileBuffers[0].deleteSelection(shiftStart, shiftEnd);

                    deletionCursor(m_shiftStart, m_shiftEnd);
                    fixCursor();

                    m_selectionValid = false;
                }
                else // delete at cursor
                {
                    if (checkKeyState(Keys.Delete, gameTime))
                    {
                        FilePosition cursorPosition = new FilePosition(m_cursorPosition);
                        m_fileBuffers[0].deleteSelection(cursorPosition, cursorPosition);
                        deletionCursor(m_cursorPosition, m_cursorPosition);
                    }
                    else if (checkKeyState(Keys.Back, gameTime))
                    {
                        if (m_cursorPosition.X > 0)
                        {
                            m_cursorPosition.X -= 1;
                            FilePosition cursorPosition = new FilePosition(m_cursorPosition);
                            m_fileBuffers[0].deleteSelection(cursorPosition, cursorPosition);
                            deletionCursor(m_cursorPosition, m_cursorPosition);                            
                        }
                        else if (m_cursorPosition.Y > 0)
                        {
                            m_cursorPosition.Y -= 1;
                            m_cursorPosition.X = m_fileBuffers[0].getLine(Convert.ToInt16(m_cursorPosition.Y)).Length;

                            FilePosition cursorPosition = new FilePosition(m_cursorPosition);

                            m_fileBuffers[0].deleteSelection(cursorPosition, cursorPosition);
                            deletionCursor(m_cursorPosition, m_cursorPosition);
                        }
                    }

                }
            }
            else
            {
                // Do we need to do some deletion or replacing?  If shift is down and we've highlighted an area
                // then we need to replace something.
                //
                if (m_shiftStart != m_shiftEnd && !m_shiftDown && m_selectionValid)
                {
                    foreach (Keys keyDown in Keyboard.GetState().GetPressedKeys())
                    {
                        Console.WriteLine("Replace SELECTION with " + keyDown.ToString());
                    }

                    // To make sure we do this only once we now invalidate this selection
                    //
                    m_selectionValid = false;
                }

            }

            // Update cursor coordinations from cursor movement
            //
            m_cursorCoords.X = m_fileCoords.X + (m_cursorPosition.X * m_charWidth);
            m_cursorCoords.Y = m_fileCoords.Y + (m_cursorPosition.Y * m_lineHeight);

            // Save the last state
            //
            m_lastKeyboardState = Keyboard.GetState();

            base.Update(gameTime);
        }

        /// <summary>
        /// If we've deleted a section then zap the cursor to the beginning of the deletion block
        /// </summary>
        /// <param name="shiftStart"></param>
        /// <param name="shiftEnd"></param>
        protected void deletionCursor(Vector2 shiftStart, Vector2 shiftEnd)
        {
            if (shiftStart.Y < shiftEnd.Y || (shiftStart.Y == shiftEnd.Y && m_shiftStart.X < m_shiftEnd.X))
            {
                m_cursorPosition = shiftStart;
            }
            else
            {
                m_cursorPosition = shiftEnd;
            }
        }

        // Gets a single key click - but also repeats if it's still held down after a while
        //
        bool checkKeyState(Keys check, GameTime gameTime)
        {
            // Do we have any keys pressed down?  If not return
            //
            Keys[] keys = Keyboard.GetState(PlayerIndex.One).GetPressedKeys();
            if (keys.Length == 0)
                return false;

            double repeatHold = 0.6; // number of seconds to wait before repeating a key

            if (Keyboard.GetState(PlayerIndex.One).IsKeyUp(Keys.LeftControl) &&
                Keyboard.GetState(PlayerIndex.One).IsKeyUp(Keys.RightControl))
            {
                if (m_ctrlDown)
                {
                    Console.WriteLine("CTRL up");
                    m_ctrlDown = false;
                }
                else
                {
                    if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.LeftControl) ||
                        Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.RightControl))
                    {
                        Console.WriteLine("CTRL down");
                        m_ctrlDown = true;
                    }
                }
            }


            // Test for shift only if we're not using other modifiers
            //
            if (Keyboard.GetState(PlayerIndex.One).IsKeyUp(Keys.LeftControl) &&
                Keyboard.GetState(PlayerIndex.One).IsKeyUp(Keys.RightControl) &&
                Keyboard.GetState(PlayerIndex.One).IsKeyUp(Keys.LeftAlt) &&
                Keyboard.GetState(PlayerIndex.One).IsKeyUp(Keys.RightAlt))
            {
                if (m_shiftDown)
                {
                    if (Keyboard.GetState(PlayerIndex.One).IsKeyUp(Keys.LeftShift) &&
                        Keyboard.GetState(PlayerIndex.One).IsKeyUp(Keys.RightShift))
                    {
                        m_shiftDown = false;
                        m_shiftEnd = m_cursorPosition;
                        m_selectionValid = true;
                    }
                }
                else
                {
                    if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.LeftShift) ||
                        Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.RightShift))
                    {
                        Console.WriteLine("SHIFT DOWN");
                        m_shiftStart = m_cursorPosition;
                        m_shiftDown = true;
                    }
                }
            }

            // Is the checked key down?
            //
            if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(check))
            {
                if (m_lastKeyboardState.IsKeyUp(check))
                {
                    //Console.WriteLine("RESETTING HELDDOWNSTARTTIME");
                    m_heldKey = check;
                    m_heldDownStartTime = gameTime.TotalGameTime.TotalSeconds;
                    return true;
                }

                if (gameTime.TotalGameTime.TotalSeconds - m_heldDownStartTime > repeatHold) 
                {
                    //Console.WriteLine("Held down = " + (gameTime.TotalGameTime.TotalSeconds - m_heldDownStartTime).ToString());
                    return true;
                }

                //Console.WriteLine("TOTAL TIME = " + (gameTime.TotalGameTime.TotalSeconds - m_heldDownStartTime));

                return false;
            }

            if (m_heldKey == check)
            {
                m_heldDownStartTime = gameTime.TotalGameTime.TotalSeconds;
            }

            return false;
        }


        public Ray GetPickRay()
        {
            MouseState mouseState = Mouse.GetState();

            int mouseX = mouseState.X;
            int mouseY = mouseState.Y;

            Vector3 nearsource = new Vector3((float)mouseX, (float)mouseY, 0f);
            Vector3 farsource = new Vector3((float)mouseX, (float)mouseY, 1f);

            Matrix world = Matrix.CreateTranslation(0, 0, 0);

            Vector3 nearPoint = m_graphics.GraphicsDevice.Viewport.Unproject(nearsource, m_projection, m_view, world);

            Vector3 farPoint = m_graphics.GraphicsDevice.Viewport.Unproject(farsource, m_projection, m_view, world);

            // Create a ray from the near clip plane to the far clip plane.
            Vector3 direction = farPoint - nearPoint;
            direction.Normalize();
            Ray pickRay = new Ray(nearPoint, direction);

            return pickRay;
        }


        /*
        public void checkMouseClick()
        {
            MouseState mouseState = Mouse.GetState();
            if (mouseState.LeftButton == ButtonState.Pressed)
            {

                Ray pickRay = GetPickRay();


                //Nullable<float> result = pickRay.Intersects(triangleBB);
                int selectedIndex = -1;
                float selectedDistance = float.MaxValue;
                for (int i = 0; i < worldObjects.Length; i++)
                {
                    worldObjects[i].texture2D = sphereTexture;
                    BoundingSphere sphere = worldObjects[i].model.Meshes[0].BoundingSphere;
                    sphere.Center = worldObjects[i].position;
                    Nullable<float> result = pickRay.Intersects(sphere);
                    if (result.HasValue == true)
                    {
                        if (result.Value < selectedDistance)
                        {
                            selectedIndex = i;
                            selectedDistance = result.Value;
                        }
                    }
                }
                if (selectedIndex > -1)
                {
                    worldObjects[selectedIndex].texture2D = selectedTexture;
                }

            }
        }
        */

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

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
            // See here for alternatives:
            // 
            // http://www.toymaker.info/Games/XNA/html/xna_camera.html
            // 
            m_view = Matrix.CreateLookAt(m_eye, Vector3.Zero, Vector3.Up);
            m_projection = /*Matrix.CreateTranslation(-0.5f, -0.5f, 0) * */ Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000f);

            m_basicEffect.World = Matrix.CreateScale(1, -1, 1); // *Matrix.CreateTranslation(textPosition);
            m_basicEffect.View = m_view;
            m_basicEffect.Projection = m_projection;

            m_lineEffect.View = m_view;
            m_lineEffect.Projection = m_projection;
            m_lineEffect.World = Matrix.CreateScale(1, -1, 1);


            // Pitch, Roll, Yaw - to rotate our rendered image in 3D
            //
            //Vector3 pry = new Vector3(0, 30, 0);

            //Vector3 filePos2 = new Vector3(-10f, 10f, -3f);
            //Vector3 filePos3 = new Vector3(-60f, -50f, -30f);

            /*
            FileRenderer.startBatch(m_basicEffect);
            FileRenderer.editFile(filePos1, pry, Color.Blue, 0f, fileName);
            FileRenderer.editFile(filePos2, pry, Color.Red, 0f, fileName);
            FileRenderer.editFile(filePos3, pry, Color.DarkRed, 0f, fileName);
            FileRenderer.endBatch();
            */
            
            m_spriteBatch.Begin(0, null, null, DepthStencilState.DepthRead, RasterizerState.CullNone, m_basicEffect);
            drawFileBuffer(m_fileCoords, m_fileName);


            m_spriteBatch.End();

            drawCursor(m_cursorCoords, gameTime);
            drawHighlight(gameTime);

            //FileRenderer.Draw(gameTime, view, projection);
            //DebugShapeRenderer.AddLine(filePos1, filePos2, Color.Yellow);
            //DebugShapeRenderer.AddLine(filePos2, filePos3, Color.Yellow);

            DebugShapeRenderer.Draw(gameTime, m_view, m_projection);

            base.Draw(gameTime);
        }


        /// <summary>
        /// Draw a cursor and make it blink in a position???
        /// </summary>
        /// <param name="v"></param>
        protected void drawCursor(Vector3 p, GameTime gameTime)
        {
            double dTS = gameTime.TotalGameTime.TotalSeconds;
            int blinkRate = 3;

            // Test for when we're showing this
            //
            if (Convert.ToInt16(dTS * blinkRate) % 2 != 0)
            {
                return;
            }

            //Vector3 viewSpaceTextPosition = Vector3.Transform(p, m_basicEffect.View);

            // Blinks rate
            //
            Vector3 v1 = p; // Vector3.Transform(p, m_view);
            v1.Y += 7.0f;

            Vector3 v2 = p; // Vector3.Transform(p, m_view);
            //v2.Y += 0.0f;

            //Console.WriteLine("Writing cursor at time " + tS.TotalSeconds);
            //Vector3.Transform(verts[vertIndex].Position, view);

            DebugShapeRenderer.AddBoundingBox(new BoundingBox(v1, v2), Color.Yellow);
            /*
            VertexPositionColor[] verts = new VertexPositionColor[2];

            verts[0] = new VertexPositionColor(v1, Color.Yellow);
            verts[1] = new VertexPositionColor(v2, Color.Yellow);

            m_lineEffect.CurrentTechnique.Passes[0].Apply();

            m_graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, verts, 0, 1,
                            VertexPositionColorTexture.VertexDeclaration);
            */
            //DebugShapeRenderer.AddLine(v1, v2, Color.Yellow);
        }

        protected void drawFileBuffer(Vector3 v, string m_fileName)
        {
            Matrix invertY = new Matrix(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

            float yPosition = 0.0f;

            //float xAdjust = textSize * 10.0f;
            //float yAdjust = textSize * 10.0f;

            //Vector2 adjustOrigin = new Vector2();
            Vector2 lineOrigin = new Vector2(); //v.X, v.Y) + adjustOrigin;
            Vector3 viewSpaceTextPosition = v; // Vector3.Transform(v, m_view /* * invertY*/ );

//            float maxWidth = 0.0f;
            //Console.WriteLine("First line at " + viewSpaceTextPosition.Y);

            for (int i = 0; i < m_fileBuffers[0].getLineCount(); i++)
            {
                string line = m_fileBuffers[0].getLine(i);

                if (line.Length > 80)
                {
                    line = line.Substring(0, 80);
                }

                //if (m_spriteFont.MeasureString(line).X > maxWidth)
                //{
                //maxWidth = m_spriteFont.MeasureString(line).X;
                //}

                m_spriteBatch.DrawString(m_spriteFont, line, new Vector2(viewSpaceTextPosition.X, viewSpaceTextPosition.Y + yPosition), Color.White, 0, lineOrigin, m_textSize, 0, 0);
                yPosition += m_lineHeight; // m_spriteFont.MeasureString(line).Y * m_textSize;
            }
        }

        /// <summary>
        /// This draws a highlight area on the screen when we hold shift down
        /// </summary>
        void drawHighlight(GameTime gameTime)
        {
            if (m_shiftDown)
            {
                //Console.WriteLine("Drawing highlight box");

                Vector3 highlightStart = new Vector3();
                Vector3 highlightEnd = new Vector3();

                // Highlight if we're on the same line
                //
                if (m_shiftStart.Y == m_cursorPosition.Y)
                {
                    // Set start position
                    //
                    highlightStart.X = m_fileCoords.X + m_shiftStart.X * m_charWidth;
                    highlightStart.Y = m_fileCoords.Y + m_shiftStart.Y * m_lineHeight;

                    // Set end position
                    //
                    highlightEnd.X = m_fileCoords.X + m_cursorPosition.X * m_charWidth;
                    highlightEnd.Y = m_fileCoords.Y + ( m_cursorPosition.Y + 1 ) * m_lineHeight;

                    renderQuad(highlightStart, highlightEnd);
                    //DebugShapeRenderer.AddBoundingBox(new BoundingBox(highlightStart, highlightEnd), Color.Yellow);
                }
                else if (m_shiftStart.Y < m_cursorPosition.Y) // Highlight down
                {
                    for (int i = Convert.ToInt16(m_shiftStart.Y); i < Convert.ToInt16(m_cursorPosition.Y) + 1; i++)
                    {
                        if (i == m_shiftStart.Y)
                        {
                            highlightStart.X = m_fileCoords.X + m_shiftStart.X * m_charWidth;
                            highlightEnd.X = m_fileCoords.X + m_fileBuffers[0].getLine(i).Length * m_charWidth;
                        }
                        else if (i == m_cursorPosition.Y)
                        {
                            highlightStart.X = m_fileCoords.X;
                            highlightEnd.X = m_fileCoords.X + m_cursorPosition.X * m_charWidth;
                        }
                        else
                        {
                            highlightStart.X = m_fileCoords.X;
                            highlightEnd.X = m_fileCoords.X + m_fileBuffers[0].getLine(i).Length * m_charWidth;
                        }

                        highlightStart.Y = m_fileCoords.Y + i * m_lineHeight;
                        highlightEnd.Y = highlightStart.Y + m_lineHeight;

                        renderQuad(highlightStart, highlightEnd);
                        //DebugShapeRenderer.AddBoundingBox(new BoundingBox(highlightStart, highlightEnd), Color.Yellow);
                    }
                    //BoundingBox bb = new BoundingBox();
                }
                else  // Highlight up
                {
                    for (int i = Convert.ToInt16(m_cursorPosition.Y); i < Convert.ToInt16(m_shiftStart.Y) + 1; i++)
                    {
                        if (i == m_cursorPosition.Y)
                        {
                            highlightStart.X = m_fileCoords.X + m_cursorPosition.X * m_charWidth;
                            highlightEnd.X = m_fileCoords.X + m_fileBuffers[0].getLine(i).Length * m_charWidth;
                        }
                        else if (i == m_shiftStart.Y)
                        {
                            highlightStart.X = m_fileCoords.X;
                            highlightEnd.X = m_fileCoords.X + m_shiftStart.X * m_charWidth;
                        }
                        else
                        {
                            highlightStart.X = m_fileCoords.X;
                            highlightEnd.X = m_fileCoords.X + m_fileBuffers[0].getLine(i).Length * m_charWidth;
                        }

                        highlightStart.Y = m_fileCoords.Y + i * m_lineHeight;
                        highlightEnd.Y = highlightStart.Y + m_lineHeight;

                        renderQuad(highlightStart, highlightEnd);
                        //DebugShapeRenderer.AddBoundingBox(new BoundingBox(highlightStart, highlightEnd), Color.Yellow);
                    }

                    /*
                    // We traverse right to left the whole shape of the highlight cursor
                    //
                    int startX = Convert.ToInt16(Math.Min(m_shiftStart.X, m_cursorCoords.X));
                    int startY = Convert.ToInt16(Math.Min(m_shiftStart.Y, m_cursorCoords.Y));
                    int endX = Convert.ToInt16(Math.Max(m_shiftStart.X, m_cursorCoords.X));
                    int endY = Convert.ToInt16(Math.Max(m_shiftStart.Y, m_cursorCoords.Y));
                    
                    if (m_shiftStart.X < m_cursorCoords.X)
                    {
                        startX = m_shiftStart.X;
                        endX = m_cursorCoords.X;

                        for (int i = startY; i < endY; i++)
                        {
                            highlightStart.X = m_fileCoords.X + 
                            DebugShapeRenderer.AddLine(
                        }
                    }

                    }
                    else
                    {
                    }

                    
                    for (int i = startY; i < endY; i++)
                    {
                        highlightStart.X = m_fileCoords.X + 
                        DebugShapeRenderer.AddLine(

                    }

                    DebugShapeRenderer.AddBoundingBox(new BoundingBox(highlightStart, highlightEnd), Color.White);
                    */
                    //DebugShapeRenderer.addCursorHighlight(highlightStart, m_lineHeight, m_charWidth, List<int> lineLengths, Color colour)
                }
            }
        }


        protected void renderQuad(Vector3 topLeft, Vector3 bottomRight)
        {
            Vector3 bottomLeft = new Vector3(topLeft.X, bottomRight.Y, topLeft.Z);
            Vector3 topRight = new Vector3(bottomRight.X, topLeft.Y, bottomRight.Z);
            /*
            Quad quad = new Quad(Vector3.Forward, Vector3.Backward, Vector3.Up, 2, 2);

            VertexElement [] vE = new VertexElement[20];

            VertexDeclaration quadVertexDec = new VertexDeclaration()
            */
            Color[] foregroundColors = new Color[1];
            foregroundColors[0] = Color.White;

            Texture2D td = new Texture2D(m_graphics.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            td.SetData(foregroundColors);

            VertexPositionTexture[] vpt = new VertexPositionTexture[4];
            Vector2 tp = new Vector2(0, 1);
            vpt[0] = new VertexPositionTexture(topLeft, tp);
            vpt[1] = new VertexPositionTexture(topRight, tp);
            vpt[2] = new VertexPositionTexture(bottomRight, tp);
            vpt[3] = new VertexPositionTexture(bottomLeft, tp);

            //m_spriteBatch.Begin(0, null, null, DepthStencilState.DepthRead, RasterizerState.CullNone, m_basicEffect);
            m_spriteBatch.Begin(SpriteSortMode.Texture, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.DepthRead, RasterizerState.CullNone, m_basicEffect);

            m_spriteBatch.Draw(td, new Rectangle(Convert.ToInt16(topLeft.X),
                                                 Convert.ToInt16(topLeft.Y),
                                                 Convert.ToInt16(bottomRight.X) - Convert.ToInt16(topLeft.X),
                                                 Convert.ToInt16(bottomRight.Y) - Convert.ToInt16(topLeft.Y)),
                                                 Color.PaleVioletRed);
            m_spriteBatch.End();
        }

        

        /*
        protected void drawLine()
        {
            verts = new VertexPositionColor[vertexCount * 2];


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
                foreach (DebugShape shape in activeShapes)
                {
                    lineCount += shape.LineCount;
                    int shapeVerts = shape.LineCount * 2;
                    for (int i = 0; i < shapeVerts; i++)
                    {
                        //verts[vertIndex] = shape.Vertices[i];
                        //verts[vertIndex].Position = Vector3.Transform(verts[vertIndex].Position, view);
                        //vertIndex++;
                        verts[vertIndex++] = shape.Vertices[i];
                    }
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
        */
    }
}
