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
//using System.Windows.Forms;

namespace Xyglo
{
    public enum FriendlierState
    {
        TextEditing,
        FileOpen,
        FileSaveAs,
        BrowseDirectory
    };

    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Friendlier : Game
    {
        // XNA stuff
        //
        GraphicsDeviceManager m_graphics;
        SpriteBatch m_spriteBatch;
        SpriteBatch m_overlaySpriteBatch;
        SpriteFont m_spriteFont;

        /// <summary>
        /// The state of our application - what we're doing at the moment
        /// </summary>
        FriendlierState m_state;

        // Our effects - one with textures for fonts, one without for lines
        //
        BasicEffect m_basicEffect;
        BasicEffect m_lineEffect;

        /// <summary>
        /// Projection Matrix
        /// </summary>
        Matrix m_projection;

        /// <summary>
        /// Eye/Camera location
        /// </summary>
        Vector3 m_eye = new Vector3(0f, 0f, 500f);  // 275 is good

        /// <summary>
        /// Camera target
        /// </summary>
        Vector3 m_target;

        /// <summary>
        /// Cursor coordinates in 3D
        /// </summary>
        Vector3 m_cursorCoords = Vector3.Zero;

        // textSize is going to define everything
        //
        float m_textSize = 1.0f;

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
        FilePosition m_shiftStart = new FilePosition();

        /// <summary>
        /// Where did we release shift?
        /// </summary>
        FilePosition m_shiftEnd = new FilePosition();

        /// <summary>
        /// Is our current selection ready for something to happen to it?
        /// </summary>
        bool m_selectionValid = false;

        /// <summary>
        /// List of FileBuffers that we can handle
        /// </summary>
        List<FileBuffer> m_fileBuffers = new List<FileBuffer>();

        /// <summary>
        /// List of BufferViews - views on 
        /// </summary>
        List<BufferView> m_bufferViews = new List<BufferView>();

        /// <summary>
        /// The buffer we are currently editing
        /// </summary>
        BufferView m_activeBufferView = null;

        /// <summary>
        /// A flat texture
        /// </summary>
        Texture2D m_flatTexture;

        /// <summary>
        /// Currently active BufferView
        /// </summary>
        int m_activeBufferViewId = 0;

        /// <summary>
        /// Rotations are stored in this vector
        /// </summary>
        Vector3 m_rotations = new Vector3();

        /// <summary>
        /// Our view matrix
        /// </summary>
        Matrix m_viewMatrix = new Matrix();

        /// <summary>
        /// We can use this to communicate something to the user about the last command
        /// </summary>
        string m_temporaryMessage = "";


        /// <summary>
        /// Get the actual cursor position in the current active buffer view
        /// </summary>
        /// <returns></returns>
        protected FilePosition getActiveCursorPosition()
        {
            FilePosition fp = m_activeBufferView.getCursorPosition();
            fp.Y += m_activeBufferView.m_bufferShowStart;

            return fp;
        }

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

            // Set the editing state
            //
            m_state = FriendlierState.TextEditing;

            // Don't use these directly - use the InitGraphicsMode below
            //
            //m_graphics.IsFullScreen = true;
            //PresentationParameters pp = GraphicsDevice.PresentationParameters;
            //pp.BackBufferFormat = SurfaceFormat.

            //InitGraphicsMode(1920, 1080, true);
            //InitGraphicsMode(1000, 600, false);
            InitGraphicsMode(800, 500, false);
            
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
                    //loadFonts();
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
                        //loadFonts();
                        return true;
                    }
                }
            }
            return false;
        }

        protected void loadFonts()
        {

            // Font loading
            //
            if (m_graphics.GraphicsDevice.Viewport.Width < 1024)
            {
                Logger.logMsg("Using Small Font");
                m_spriteFont = Content.Load<SpriteFont>("Courier New");
            }
            else
            {
                Logger.logMsg("Using Large Font");
                m_spriteFont = Content.Load<SpriteFont>("Courier New Large");
            }

            // Text size has to be scaled to actual font size
            //
            m_textSize = (float)((int)(1400.0f / (float)(m_spriteFont.LineSpacing))) / 100.0f;
            Logger.logMsg("Text Size = " + m_textSize);

            Logger.logMsg("SPRITE FONT line spacing = " + m_spriteFont.LineSpacing);
        }


        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            m_spriteBatch = new SpriteBatch(m_graphics.GraphicsDevice);

            // Always load the fonts
            //
            loadFonts();

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

            // Store these sizes and positions
            //
            m_charWidth = m_spriteFont.MeasureString("X").X * m_textSize;
            m_lineHeight = m_spriteFont.MeasureString("X").Y * m_textSize;

            // Create the overlay SpriteBatch
            //
            m_overlaySpriteBatch = new SpriteBatch(m_graphics.GraphicsDevice);

            // Create a flat texture for drawing rectangles etc
            //
            Color[] foregroundColors = new Color[1];
            foregroundColors[0] = Color.White;
            m_flatTexture = new Texture2D(m_graphics.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            m_flatTexture.SetData(foregroundColors);

            // Load and buffer the file
            //
            FileBuffer file1 = new FileBuffer(m_fileName);
            m_fileBuffers.Add(file1);

            // Add a view
            //
            BufferView view1 = new BufferView(file1, new Vector3(0f, 0f, 0f), 0, 20, m_charWidth, m_lineHeight);
            m_bufferViews.Add(view1);

            // Ensure that we are in the correct position to view this buffer so there's no initial movement
            //
            m_eye = view1.getEyePosition();
            m_target = view1.getLookPosition();
            m_eye.Z = m_zoomLevel;

            // Set the active buffer view
            //
            setActiveBuffer();
        }


        /// <summary>
        /// Set which BufferView is the active one with a cursor in it
        /// </summary>
        /// <param name="view"></param>
        protected void setActiveBuffer(BufferView item = null)
        {
            try
            {
                // Set active buffer
                //
                if (item == null)
                {
                    m_activeBufferView = m_bufferViews[m_activeBufferViewId];
                }
                else
                {
                    m_activeBufferViewId = m_bufferViews.IndexOf(item);
                    m_activeBufferView = item;
                }
            }
            catch (Exception e)
            {
                Logger.logMsg("Cannot locate BufferView item in list " + e.ToString());
                return;
            }

            Logger.logMsg("Active buffer view is " + m_activeBufferViewId);

            Vector3 eyePos = m_activeBufferView.getEyePosition();
            eyePos.Z = m_zoomLevel;

            flyToPosition(eyePos);
            //m_eye = m_activeBufferView.getEyePosition();
            //m_target = m_activeBufferView.getLookPosition();

            Logger.logMsg("Buffer position = " + m_activeBufferView.getPosition());
            Logger.logMsg("Look position = " + m_target);
            Logger.logMsg("Eye position = " + m_eye);
        }

        protected float m_zoomLevel = 500.0f;

        Vector3 m_up = new Vector3(0, 1, 0);
        Vector3 m_look = new Vector3(0, 0, -1);
        Vector3 m_right = new Vector3(1, 0, 0);

        // Y axis rotation - also known as Yaw
        //
        private void RotateAroundY(float angle)
        {
            m_rotations.Y += angle;

            // keep the value in the range 0-360 (0 - 2 PI radians)
            if (m_rotations.Y > Math.PI * 2)
                m_rotations.Y -= MathHelper.Pi * 2;
            else if (m_rotations.Y < 0)
                m_rotations.Y += MathHelper.Pi * 2;
        }

        private void RotateAroundX(float angle)
        {
            m_rotations.X += angle;

            // keep the value in the range 0-360 (0 - 2 PI radians)
            if (m_rotations.X > Math.PI * 2)
                m_rotations.X -= MathHelper.Pi * 2;
            else if (m_rotations.Y < 0)
                m_rotations.X += MathHelper.Pi * 2;
        }

        private void RotateAroundZ(float angle)
        {
            m_rotations.Z += angle;

            // keep the value in the range 0-360 (0 - 2 PI radians)
            if (m_rotations.Z > Math.PI * 2)
                m_rotations.Z -= MathHelper.Pi * 2;
            else if (m_rotations.Y < 0)
                m_rotations.Z += MathHelper.Pi * 2;
        }

        /// <summary>
        /// Ensure the cursor is within the boundaries set by the file and not floating in space
        /// /// </summary>
        protected void fixCursor()
        {
            int curPosX = m_activeBufferView.getCursorPosition().X;
            int curPosY = m_activeBufferView.getCursorPosition().Y;
            string line = m_fileBuffers[0].getLine(curPosY);
            int lineLength = line.Length;

            if (curPosX > lineLength)
            {
                m_activeBufferView.setCursorPosition(new FilePosition(lineLength, curPosY));
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
            //
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.Escape))
                this.Exit();

            // Control key state
            //
            if (m_ctrlDown && Keyboard.GetState(PlayerIndex.One).IsKeyUp(Keys.LeftControl) &&
                Keyboard.GetState(PlayerIndex.One).IsKeyUp(Keys.RightControl))
            {
                m_ctrlDown = false;
            }
            else
            {
                if (!m_ctrlDown && (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.LeftControl) ||
                    Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.RightControl)))
                {
                    m_ctrlDown = true;
                }
            }

            if (checkKeyState(Keys.Up, gameTime))
            {
                if (m_ctrlDown)
                {
                    // Add a new BufferView above current position
                    //
                    addBufferView(BufferView.BufferPosition.Above);
                }
                else
                {
                    if (m_activeBufferView.getCursorPosition().Y > 0)
                    {
                        FilePosition fp = m_activeBufferView.getCursorPosition();
                        fp.Y--;
                        m_activeBufferView.setCursorPosition(fp); ;
                    }
                    else
                    {
                        // Nudge up the buffer
                        //
                        if (m_activeBufferView.m_bufferShowStart > 0)
                        {
                            m_activeBufferView.m_bufferShowStart--;

                            if (m_shiftDown)
                            {
                                m_shiftStart.Y++;
                            }
                        }
                    }

                    fixCursor();
                }
            }
            else if (checkKeyState(Keys.Down, gameTime))
            {
                if (m_ctrlDown)
                {
                    // Add a new BufferView below current position
                    //
                    addBufferView(BufferView.BufferPosition.Below);
                }
                else
                {
                    if (m_activeBufferView.getCursorPosition().Y < m_activeBufferView.m_bufferShowLength)
                    {
                        FilePosition fp = m_activeBufferView.getCursorPosition();
                        fp.Y++;
                        m_activeBufferView.setCursorPosition(fp);
                    }
                    else
                    {
                        // Nudge down the buffer
                        //
                        if (m_activeBufferView.m_bufferShowStart < m_fileBuffers[0].getLineCount() - 1)
                        {
                            m_activeBufferView.m_bufferShowStart++;
                            if (m_shiftDown)
                            {
                                m_shiftStart.Y--;
                            }
                        }
                    }
                    fixCursor();
                }
            }
            else if (checkKeyState(Keys.Left, gameTime))
            {
                if (m_ctrlDown)
                {
                    // Add a new BufferView to the left of current position
                    //
                    addBufferView(BufferView.BufferPosition.Left);
                }
                else
                {
                    if (m_activeBufferView.getCursorPosition().X > 0)
                    {
                        FilePosition fp = m_activeBufferView.getCursorPosition();
                        fp.X--;
                        m_activeBufferView.setCursorPosition(fp);
                    }
                    fixCursor();
                }
            }
            else if (checkKeyState(Keys.Right, gameTime))
            {
                if (m_ctrlDown)
                {
                    // Add a new BufferView to the right of current position
                    //
                    addBufferView(BufferView.BufferPosition.Right);
                }
                else
                {

                    if (m_activeBufferView.getCursorPosition().X < m_activeBufferView.m_bufferShowWidth)
                    {

                        FilePosition fp = m_activeBufferView.getCursorPosition();
                        fp.X++;
                        m_activeBufferView.setCursorPosition(fp);
                    }
                    fixCursor();
                }
                
            }
            else if (checkKeyState(Keys.End, gameTime))
            {
                FilePosition fp = m_activeBufferView.getCursorPosition();
                fp.X = m_activeBufferView.m_fileBuffer.getLine(fp.Y + m_activeBufferView.m_bufferShowStart).Length;
                m_activeBufferView.setCursorPosition(fp);
            }
            else if (checkKeyState(Keys.Home, gameTime))
            {
                FilePosition fp = m_activeBufferView.getCursorPosition();
                fp.X = 0;
                m_activeBufferView.setCursorPosition(fp);
            }
                /*
            else if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.OemPeriod))
            {
                Vector3 newPosition = m_eye;
                newPosition += m_up;
                flyToPosition(newPosition);
            }
            else if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.OemComma))
            {
                Vector3 newPosition = m_eye;
                newPosition -= m_up;
                flyToPosition(newPosition);
            }
            else if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.F1))
            {
                Vector3 newPosition = m_eye;
                newPosition -= m_look * 10;
                flyToPosition(newPosition);
            }
            else if (checkKeyState(Keys.F2, gameTime) && m_eye.Z < 1000)
            {
                Vector3 newPosition = m_eye;
                newPosition += m_look;
                flyToPosition(newPosition);
            }
                 * */
            else if (checkKeyState(Keys.F1, gameTime))
            {
                m_zoomLevel -= 500.0f;

                if (m_zoomLevel < 500.0f)
                {
                    m_zoomLevel = 500.0f;
                }
                setActiveBuffer();
            }
            else if (checkKeyState(Keys.F2, gameTime))
            {
                m_zoomLevel += 500.0f;
                setActiveBuffer();
            }
            else if (checkKeyState(Keys.F3, gameTime))
            {
                InitGraphicsMode(1920, 1080, true);
            }
            else if (checkKeyState(Keys.F4, gameTime))
            {
                InitGraphicsMode(1000, 600, false );
            }
            else if (checkKeyState(Keys.F5, gameTime))
            {
                m_activeBufferViewId--;

                if (m_activeBufferViewId < 0)
                {
                    m_activeBufferViewId += m_bufferViews.Count;
                }
                setActiveBuffer();
            }
            else if (checkKeyState(Keys.F6, gameTime))
            {
                m_activeBufferViewId = (m_activeBufferViewId + 1) % m_bufferViews.Count;
                setActiveBuffer();

            }
            else if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.Insert)) // Reset
            {
                m_eye.X = 12f;
                m_eye.Y = 5f;
                m_eye.Z = 0f;
            }
            else if (checkKeyState(Keys.PageDown, gameTime))
            {
                pageDown();
            }
            else if (checkKeyState(Keys.PageUp, gameTime))
            {
                pageUp();
            }
            else if (checkKeyState(Keys.Scroll, gameTime))
            {
                if (m_activeBufferView.isLocked())
                {
                    m_activeBufferView.setLock(false, 0);
                }
                else
                {
                    m_activeBufferView.setLock(true, m_activeBufferView.getCursorPosition().Y);
                }
            }
            else if (checkKeyState(Keys.Delete, gameTime) || checkKeyState(Keys.Back, gameTime))
            {
                // If we have a valid selection then delete it
                //
                if (m_selectionValid)
                {
                    FilePosition shiftStart = m_shiftStart;
                    FilePosition shiftEnd = m_shiftEnd;
                    shiftStart.Y += m_activeBufferView.m_bufferShowStart;
                    shiftEnd.Y += m_activeBufferView.m_bufferShowStart;

                    m_fileBuffers[0].deleteSelection(shiftStart, shiftEnd);

                    deletionCursor(m_shiftStart, m_shiftEnd);
                    fixCursor();

                    m_selectionValid = false;
                }
                else // delete at cursor
                {
                    if (checkKeyState(Keys.Delete, gameTime))
                    {
                        FilePosition cursorPosition = m_activeBufferView.getCursorPosition();
                        cursorPosition.Y += m_activeBufferView.m_bufferShowStart;

                        m_fileBuffers[0].deleteSelection(cursorPosition, cursorPosition);
                        deletionCursor(m_activeBufferView.getCursorPosition(), m_activeBufferView.getCursorPosition());
                    }
                    else if (checkKeyState(Keys.Back, gameTime))
                    {
                        FilePosition fp = m_activeBufferView.getCursorPosition();

                        if (fp.X > 0)
                        {
                            // Decrement and set X
                            //
                            fp.X--;
                            m_activeBufferView.setCursorPosition(fp); ;

                            // Modify Y for cursor position
                            FilePosition cursorPosition = new FilePosition(m_activeBufferView.getCursorPosition());
                            cursorPosition.Y += m_activeBufferView.m_bufferShowStart;

                            m_fileBuffers[0].deleteSelection(cursorPosition, cursorPosition);
                            deletionCursor(fp, fp);
                        }
                        else if (fp.Y > 0)
                        {
                            fp.Y -= 1;
                            fp.X = m_fileBuffers[0].getLine(Convert.ToInt16(fp.Y)).Length;
                            m_activeBufferView.setCursorPosition(fp);

                            FilePosition cursorPosition = fp;
                            cursorPosition.Y += m_activeBufferView.m_bufferShowStart;

                            m_fileBuffers[0].deleteSelection(cursorPosition, cursorPosition);
                            deletionCursor(fp, fp);
                        }
                    }
                }
            }
            else
            {
                if (checkKeyState(Keys.Z, gameTime))
                {
                    // Undo
                    //
                    if (m_ctrlDown)
                    {
                        // Undo a certain number of steps
                        //
                        try
                        {
                            // We call the undo against the FileBuffer and this returns the cursor position
                            // resulting from this action.
                            //
                            m_activeBufferView.setCursorPosition(m_fileBuffers[0].undo(1));
                        }
                        catch (Exception /* e */)
                        {
                            //System.Windows.Forms.MessageBox.Show("Undo stack is empty - " + e.Message);
                            //Logger.logMsg("Got exception " + e.Message);
                            m_temporaryMessage = "[NOUNDO]";
                        }

                        fixCursor();
                    }
                }

                if (//Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.LeftShift) ||
                    //Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.RightShift) ||
                    Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.RightControl) ||
                    Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.LeftControl))
                {
                    // we have an action key so we ignore it for insert purposes
                    ;
                }
                else
                {
                    // Detect a key being hit
                    //
                    foreach (Keys keyDown in Keyboard.GetState().GetPressedKeys())
                    {
                        bool testKey = true;

                        foreach (Keys lastKeys in m_lastKeyboardState.GetPressedKeys())
                        {
                            if (keyDown == lastKeys)
                            {
                                testKey = false;
                            }
                        }

                        // Test to see if we've already processed this key - if not then we can print it out
                        //
                        if (testKey)
                        {
                            FilePosition fp = getActiveCursorPosition();

                            if (keyDown == Keys.Enter)
                            {
                                // Insert a line
                                //
                                fp = m_activeBufferView.m_fileBuffer.insertNewLine(getActiveCursorPosition());
                            }
                            else
                            {
                                string key = "";

                                switch (keyDown)
                                {
                                    case Keys.LeftShift:
                                    case Keys.RightShift:
                                    case Keys.LeftControl:
                                    case Keys.RightControl:
                                    case Keys.LeftAlt:
                                    case Keys.RightAlt:
                                        break;

                                    case Keys.D0:
                                        if (m_shiftDown)
                                        {
                                            key = ")";
                                        }
                                        else
                                        {
                                            key = "0";
                                        }
                                        break;

                                    case Keys.D1:
                                        if (m_shiftDown)
                                        {
                                            key = "!";
                                        }
                                        else
                                        {
                                            key = "1";
                                        }
                                        break;

                                    case Keys.D2:
                                        if (m_shiftDown)
                                        {
                                            key = "@";
                                        }
                                        else
                                        {
                                            key = "2";
                                        }
                                        break;

                                    case Keys.D3:
                                        if (m_shiftDown)
                                        {
                                            key = "#";
                                        }
                                        else
                                        {
                                            key = "3";
                                        }
                                        break;

                                    case Keys.D4:
                                        if (m_shiftDown)
                                        {
                                            key = "$";
                                        }
                                        else
                                        {
                                            key = "4";
                                        }
                                        break;

                                    case Keys.D5:
                                        if (m_shiftDown)
                                        {
                                            key = "%";
                                        }
                                        else
                                        {
                                            key = "5";
                                        }
                                        break;

                                    case Keys.D6:
                                        if (m_shiftDown)
                                        {
                                            key = "^";
                                        }
                                        else
                                        {
                                            key = "6";
                                        }
                                        break;

                                    case Keys.D7:
                                        if (m_shiftDown)
                                        {
                                            key = "&";
                                        }
                                        else
                                        {
                                            key = "7";
                                        }
                                        break;

                                    case Keys.D8:
                                        if (m_shiftDown)
                                        {
                                            key = "*";
                                        }
                                        else
                                        {
                                            key = "8";
                                        }
                                        break;

                                    case Keys.D9:
                                        if (m_shiftDown)
                                        {
                                            key = "(";
                                        }
                                        else
                                        {
                                            key = "9";
                                        }
                                        break;


                                    case Keys.Space:
                                        key = " ";
                                        break;

                                    case Keys.OemPlus:
                                        if (m_shiftDown)
                                        {
                                            key = "+";
                                        }
                                        else
                                        {
                                            key = "=";
                                        }
                                        break;

                                    case Keys.OemMinus:
                                        if (m_shiftDown)
                                        {
                                            key = "_";
                                        }
                                        else
                                        {
                                            key = "-";
                                        }
                                        break;

                                    case Keys.OemPeriod:
                                        if (m_shiftDown)
                                        {
                                            key = ">";
                                        }
                                        else
                                        {
                                            key = ".";
                                        }
                                        break;

                                    case Keys.OemComma:
                                        if (m_shiftDown)
                                        {
                                            key = "<";
                                        }
                                        else
                                        {
                                            key = ",";
                                        }
                                        break;

                                    default:
                                        key = keyDown.ToString();
                                        if (!m_shiftDown)
                                        {
                                            key = key.ToLower();
                                        }
                                        break;
                                }

                                if (key != "")
                                {

                                    // Do we need to do some deletion or replacing?  If shift is down and we've highlighted an area
                                    // then we need to replace something.
                                    //
                                    if (m_shiftStart != m_shiftEnd && !m_shiftDown && m_selectionValid)
                                    {
                                        // Replace selection with value of "key"
                                        //
                                        Logger.logMsg("Replacing selection with '" + key + "'");

                                        FilePosition shiftStart = m_shiftStart;
                                        FilePosition shiftEnd = m_shiftEnd;
                                        shiftStart.Y += m_activeBufferView.m_bufferShowStart;
                                        shiftEnd.Y += m_activeBufferView.m_bufferShowStart;

                                        fp = m_fileBuffers[0].replaceText(shiftStart, shiftEnd, key);

                                        // To make sure we do this only once we now invalidate this selection
                                        //
                                        m_selectionValid = false;
                                    }
                                    else
                                    {
                                        // Insert the text on the FileBuffer and capture the return position
                                        //
                                        fp = m_fileBuffers[0].insertText(getActiveCursorPosition(), key);

                                        m_shiftDown = false;
                                        m_selectionValid = false;
                                        m_shiftStart = m_activeBufferView.getCursorPosition();
                                        m_shiftEnd = m_activeBufferView.getCursorPosition();
                                    }
                                }
                            }

                            // Set the cursor position to whatever was returned by the relevant command
                            //
                            m_activeBufferView.setCursorPosition(fp);
                            fixCursor();
                        }
                    }

                }
            }

            // Check for this change as necessary
            //
            changeEyePosition(gameTime);

            // Update cursor coordinations from cursor movement
            //
            m_cursorCoords.X = m_activeBufferView.getPosition().X + (m_activeBufferView.getCursorPosition().X * m_charWidth);
            m_cursorCoords.Y = m_activeBufferView.getPosition().Y + (m_activeBufferView.getCursorPosition().Y * m_lineHeight);

            // Save the last state if it has changed and clear any temporary message
            //
            if (m_lastKeyboardState != Keyboard.GetState())
            {
                m_lastKeyboardState = Keyboard.GetState();
                m_temporaryMessage = "";
            }

            base.Update(gameTime);

        }

        /// <summary>
        /// Find a good position for a new BufferView relative to the current active position
        /// </summary>
        /// <param name="position"></param>
        protected void addBufferView(BufferView.BufferPosition position)
        {
            bool occupied = false;

            // Initial new pos is here from active BufferView
            //
            Vector3 newPos = m_activeBufferView.calculateRelativePosition(position);
            do
            {
                occupied = false;

                foreach (BufferView cur in m_bufferViews)
                {
                    if (cur.getPosition() == newPos)
                    {
                        // We get the next available slot in the same direction away from original
                        //
                        newPos = cur.calculateRelativePosition(position);
                        occupied = true;
                        break;
                    }
                }
            } while (occupied);

            BufferView newBufferView = new BufferView(m_activeBufferView, newPos);
            newBufferView.m_textColour = Color.LawnGreen;
            m_bufferViews.Add(newBufferView);
            setActiveBuffer(newBufferView);

        }
        /// <summary>
        /// Move the eye to a new position
        /// </summary>
        /// <param name="newPosition"></param>
        protected void flyToPosition(Vector3 newPosition)
        {
            m_newEyePosition = newPosition;
            m_changingPositionLastGameTime = TimeSpan.Zero;
            m_changingEyePosition = true;
        }

        /// <summary>
        /// If we've deleted a section then zap the cursor to the beginning of the deletion block
        /// </summary>
        /// <param name="shiftStart"></param>
        /// <param name="shiftEnd"></param>
        protected void deletionCursor(FilePosition shiftStart, FilePosition shiftEnd)
        {
            if (shiftStart.Y < shiftEnd.Y || (shiftStart.Y == shiftEnd.Y && m_shiftStart.X < m_shiftEnd.X))
            {
                m_activeBufferView.setCursorPosition(shiftStart);
            }
            else
            {
                m_activeBufferView.setCursorPosition(shiftEnd);
            }
        }

        /// <summary>
        /// The new destination for our Eye position
        /// </summary>
        protected Vector3 m_newEyePosition;

        /// <summary>
        /// Are we changing eye position?
        /// </summary>
        protected bool m_changingEyePosition = false;

        protected TimeSpan m_changingPositionLastGameTime;

        protected TimeSpan m_movementPause = new TimeSpan(0, 0, 0, 0, 10);

        protected Vector3 m_vFly;

        /// <summary>
        /// Transform current to intended
        /// </summary>
        /// <param name="delta"></param>
        protected void changeEyePosition(GameTime gameTime)
        {
            if (m_changingEyePosition)
            {
                // If more than twenty ms has elapsed
                //
                try
                {
                    if (m_changingPositionLastGameTime == TimeSpan.Zero)
                    {
                        m_vFly = (m_newEyePosition - m_eye) / 20;
                        //m_vFly.Normalize();
                        m_changingPositionLastGameTime = gameTime.TotalGameTime;
                    }


                    if (gameTime.TotalGameTime - m_changingPositionLastGameTime > m_movementPause)
                    {
                        m_eye += m_vFly;
                        m_target.X += m_vFly.X;
                        m_target.Y += m_vFly.Y;
                        m_changingPositionLastGameTime = gameTime.TotalGameTime;
                        //m_view = Matrix.CreateLookAt(m_eye, Vector3.Zero, Vector3.Up);

                        Logger.logMsg("Eye is now at " + m_eye.ToString());
                        Logger.logMsg("FINAL Position is " + m_newEyePosition.ToString());
                    }

                    BoundingSphere testArrived = new BoundingSphere(m_newEyePosition, 1.0f);

                    ContainmentType result;
                    testArrived.Contains(ref m_eye, out result);
                    if (result == ContainmentType.Contains)
                    {
                        m_eye = m_newEyePosition;
                        m_target.X = m_newEyePosition.X;
                        m_target.Y = m_newEyePosition.Y;
                        m_changingEyePosition = false;
                    }

                }
                catch (Exception e)
                {
                    Console.Write("Got timing exception " + e.Message);
                }
            }
        }

        // Gets a single key click - but also repeats if it's still held down after a while
        //
        bool checkKeyState(Keys check, GameTime gameTime)
        {
            // Do we have any keys pressed down?  If not return
            //
            Keys[] keys = Keyboard.GetState(PlayerIndex.One).GetPressedKeys();
            if (keys.Length == 0 || check == Keys.LeftControl || check == Keys.RightControl ||
                check == Keys.LeftAlt || check == Keys.RightAlt)
                return false;

            double repeatHold = 0.6; // number of seconds to wait before repeating a key

            // Test shift here to keep valid selections alive until next key click
            //
            if (m_shiftDown && Keyboard.GetState(PlayerIndex.One).IsKeyUp(Keys.LeftShift) &&
                Keyboard.GetState(PlayerIndex.One).IsKeyUp(Keys.RightShift))
            {
                m_shiftDown = false;
                m_shiftEnd = m_activeBufferView.getCursorPosition();
                m_selectionValid = true;
            }
            else
            {
                if (!m_shiftDown && (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.LeftShift) ||
                    Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.RightShift)))
                {
                    m_shiftStart = m_activeBufferView.getCursorPosition();
                    m_shiftDown = true;
                }
            }

            // Is the checked key down?
            //
            if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(check))
            {
                if (m_lastKeyboardState.IsKeyUp(check))
                {
                    m_heldKey = check;
                    m_heldDownStartTime = gameTime.TotalGameTime.TotalSeconds;
                    return true;
                }

                // Check to see if the key has been held down for a while
                //
                if (gameTime.TotalGameTime.TotalSeconds - m_heldDownStartTime > repeatHold) 
                {
                    return true;
                }

                // It hasn't
                //
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

            Vector3 nearPoint = m_graphics.GraphicsDevice.Viewport.Unproject(nearsource, m_projection, m_viewMatrix, world);

            Vector3 farPoint = m_graphics.GraphicsDevice.Viewport.Unproject(farsource, m_projection, m_viewMatrix, world);

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
            // Set background colour
            //
            m_graphics.GraphicsDevice.Clear(Color.Black);

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
            m_viewMatrix = Matrix.CreateLookAt(m_eye, m_target, Vector3.Up);

            m_projection = Matrix.CreateTranslation(-0.5f, -0.5f, 0) * Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 0.1f, 10000f);

            m_basicEffect.World = Matrix.CreateScale(1, -1, 1); // *Matrix.CreateTranslation(textPosition);
            m_basicEffect.View = m_viewMatrix;
            m_basicEffect.Projection = m_projection;

            m_lineEffect.View = m_viewMatrix;
            m_lineEffect.Projection = m_projection;
            m_lineEffect.World = Matrix.CreateScale(1, -1, 1);

            // Here we need to vary the parameters to the SpriteBatch - to the BasicEffect and also the font size.
            // For large fonts we need to be able to downscale them effectively so that they will still look good
            // at higher reoslutions.
            //
            //m_basicEffect.TextureEnabled = true;
            //m_basicEffect.SpecularPower = 100.0f;
            //m_basicEffect.SpecularColor = new Vector3(100, 100, 100);

            //m_spriteBatch.Begin(0, null, null, DepthStencilState.DepthRead, RasterizerState.CullNone, m_basicEffect);
            //m_spriteBatch.Begin(0, null, null, null, null, m_basicEffect);
            //m_spriteBatch.Begin(0, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.DepthRead, RasterizerState.CullNone, m_basicEffect);

            if (m_graphics.GraphicsDevice.Viewport.Width < 1024)
            {
                m_spriteBatch.Begin(SpriteSortMode.Texture, BlendState.AlphaBlend, SamplerState.AnisotropicWrap, DepthStencilState.DepthRead, RasterizerState.CullNone, m_basicEffect);
            }
            else
            {
                m_spriteBatch.Begin(SpriteSortMode.Texture, BlendState.Additive, SamplerState.AnisotropicWrap, DepthStencilState.DepthRead, RasterizerState.CullNone, m_basicEffect);
            }
            
            for (int i = 0; i < m_bufferViews.Count; i++)
            {
                drawFileBuffer(m_bufferViews[i], m_bufferViews[i].m_fileBuffer);
            }

            m_spriteBatch.End();

            // Draw the Overlay HUD
            //
            drawOverlay();

            // Cursor and cursor highlight
            //
            drawCursor(m_cursorCoords, gameTime);
            drawHighlight(gameTime);

            base.Draw(gameTime);
        }

        /// <summary>
        /// Draw the HUD Overlay for the editor
        /// </summary>
        protected void drawOverlay()
        {
            string fileName = "";
            if (m_activeBufferView != null)
            {
                // Set the filename
                fileName = "\"" + m_activeBufferView.m_fileBuffer.getShortFileName() + "\"";

                if (m_activeBufferView.m_fileBuffer.isModified())
                {
                    fileName += " [Modified]";
                }

                fileName += " " + m_activeBufferView.m_fileBuffer.getLineCount() + " lines";
            }

            // We can't trust m_shiftDown
            //
            if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.LeftShift) ||
                Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.RightShift))
            {
                fileName += " [SHFT]";
            }

            if (m_ctrlDown)
            {
                fileName += " [CTRL]";
            }

            // Add any temporary message on to the end of the message
            //
            fileName += " " + m_temporaryMessage;

            // Convert lineHeight back to normal size by dividing by m_textSize modifier
            //
            float yPos = m_graphics.GraphicsDevice.Viewport.Height - m_lineHeight/m_textSize;

            // Debug eye position
            //
            string eyePosition = "[EyePosition] X " + m_eye.X + ",Y " + m_eye.Y + ",Z " + m_eye.Z;
            float xPos = m_graphics.GraphicsDevice.Viewport.Width - eyePosition.Length * m_charWidth;

            string modeString = "none";

            switch (m_state)
            {
                case FriendlierState.TextEditing:
                    modeString = "edit";
                    break;

                case FriendlierState.FileOpen:
                    modeString = "browse";
                    break;

                case FriendlierState.FileSaveAs:
                    modeString = "save";
                    break;

                default:
                    modeString = "free";
                    break;
            }

            float modeStringXPos = m_graphics.GraphicsDevice.Viewport.Width - modeString.Length * m_charWidth * 2;


            // http://forums.create.msdn.com/forums/p/61995/381650.aspx
            //
            m_overlaySpriteBatch.Begin(SpriteSortMode.Texture, BlendState.AlphaBlend);
            //m_overlaySpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.LinearWrap, DepthStencilState.None,RasterizerState.CullCounterClockwise);

            // hardcode the font size to 1.0f so it looks nice
            //
            m_overlaySpriteBatch.DrawString(m_spriteFont, fileName, new Vector2(0.0f, yPos), Color.White, 0, Vector2.Zero, 1.0f, 0, 0);
            m_overlaySpriteBatch.DrawString(m_spriteFont, eyePosition, new Vector2(0.0f, 0.0f), Color.White, 0, Vector2.Zero, 1.0f, 0, 0);
            m_overlaySpriteBatch.DrawString(m_spriteFont, modeString, new Vector2(modeStringXPos, 0.0f), Color.White, 0, Vector2.Zero, 1.0f, 0, 0);
            m_overlaySpriteBatch.End();
        }

        /// <summary>
        /// Draw a cursor and make it blink in position
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
            v1.Y += m_activeBufferView.getLineHeight();

            Vector3 v2 = p; // Vector3.Transform(p, m_view);
            v2.X += 1;

            renderQuad(v1, v2);
            //DebugShapeRenderer.AddBoundingBox(new BoundingBox(v1, v2), m_activeBufferView.m_cursorColour);
        }

        /// <summary>
        /// Draw a BufferView
        /// </summary>
        /// <param name="view"></param>
        /// <param name="file"></param>
        protected void drawFileBuffer(BufferView view, FileBuffer file)
        {
            //Matrix invertY = new Matrix(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

            Color bufferColour = view.m_textColour;

            if (view == m_activeBufferView)
            {
                bufferColour = Color.White;
            }

            float yPosition = 0.0f;

            Vector2 lineOrigin = new Vector2();
            Vector3 viewSpaceTextPosition = view.getPosition();

            int showStart = view.m_bufferShowStart;
            int showEnd = file.getLineCount();

            if (view.m_bufferShowStart > file.getLineCount() - 1)
            {
                showStart = file.getLineCount() - 1;
            }

            if (view.m_bufferShowStart + view.m_bufferShowLength < file.getLineCount() - 1)
            {
                showEnd = view.m_bufferShowStart + view.m_bufferShowLength;
            }
            else
            {
                showEnd = file.getLineCount();
            }

            // Draw all the text lines to the height of the buffer
            //
            for (int i = showStart; i < showEnd; i++)
            {
                string line = file.getLine(i);

                if (line.Length > view.m_bufferShowWidth)
                {
                    line = line.Substring(0, view.m_bufferShowWidth);
                }

                m_spriteBatch.DrawString(m_spriteFont, line, new Vector2(viewSpaceTextPosition.X, viewSpaceTextPosition.Y + yPosition), bufferColour, 0, lineOrigin, m_textSize, 0, 0);
                yPosition += m_lineHeight; // m_spriteFont.MeasureString(line).Y * m_textSize;
            }

            // Draw overlaid ID on this window if we're far enough away to use it
            //
            if (m_zoomLevel > 800.0f)
            {
                int viewId = m_bufferViews.IndexOf(view);
                string bufferId = viewId.ToString();
                Color seeThroughColour = bufferColour;
                seeThroughColour.A = 70;
                m_spriteBatch.DrawString(m_spriteFont, bufferId, new Vector2(viewSpaceTextPosition.X, viewSpaceTextPosition.Y), seeThroughColour, 0, lineOrigin, m_textSize * 19.0f, 0, 0);
            }

            drawScrollbar(view, file);
        }

        /// <summary>
        /// Draw a scroll bar for a BufferView
        /// </summary>
        /// <param name="view"></param>
        /// <param name="file"></param>
        protected void drawScrollbar(BufferView view, FileBuffer file)
        {
            Vector3 sbPos = view.getPosition();
            float height = view.m_bufferShowLength * m_lineHeight;

            Rectangle sbBackGround = new Rectangle(Convert.ToInt16(sbPos.X - m_textSize * 30.0f),
                                                   Convert.ToInt16(sbPos.Y),
                                                   1,
                                                   Convert.ToInt16(height));

            // Draw scroll bar
            //
            m_spriteBatch.Draw(m_flatTexture, sbBackGround, Color.DarkCyan);

            // Draw viewing window
            float start = view.m_bufferShowStart;
            float length = view.m_fileBuffer.getLineCount();
            float scrollStart = start / length * height;
            float scrollLength = view.m_bufferShowLength / length * height;

            Rectangle sb = new Rectangle(Convert.ToInt16(sbPos.X - m_textSize * 30.0f),
                                         Convert.ToInt16(sbPos.Y + scrollStart),
                                         1,
                                         Convert.ToInt16(scrollLength));
            // Draw scroll bar window position
            //
            m_spriteBatch.Draw(m_flatTexture, sb, Color.LightGoldenrodYellow);

        }


        /// <summary>
        /// Page down on the active BufferView
        /// </summary>
        protected void pageDown()
        {
            m_activeBufferView.m_bufferShowStart += 20;
            if (m_activeBufferView.m_bufferShowStart > m_fileBuffers[0].getLineCount() - 1)
            {
                m_activeBufferView.m_bufferShowStart = m_fileBuffers[0].getLineCount() - 1;
            }
        }

        /// <summary>
        /// Page up on the active BufferView
        /// </summary>
        protected void pageUp()
        {
            m_activeBufferView.m_bufferShowStart -= 20;

            if (m_activeBufferView.m_bufferShowStart < 0)
            {
                m_activeBufferView.m_bufferShowStart = 0;
            }
        }

        /// <summary>
        /// This draws a highlight area on the screen when we hold shift down
        /// </summary>
        void drawHighlight(GameTime gameTime)
        {
            if (m_shiftDown)
            {
                //Logger.logMsg("Drawing highlight box");

                Vector3 highlightStart = new Vector3();
                Vector3 highlightEnd = new Vector3();

                // Highlight if we're on the same line
                //
                if (m_shiftStart.Y == m_activeBufferView.getCursorPosition().Y)
                {
                    // Set start position
                    //
                    highlightStart.X = m_activeBufferView.getPosition().X + m_shiftStart.X * m_charWidth;
                    highlightStart.Y = m_activeBufferView.getPosition().Y + m_shiftStart.Y * m_lineHeight;

                    // Set end position
                    //
                    highlightEnd.X = m_activeBufferView.getPosition().X + m_activeBufferView.getCursorPosition().X * m_charWidth;
                    highlightEnd.Y = m_activeBufferView.getPosition().Y + (m_activeBufferView.getCursorPosition().Y + 1) * m_lineHeight;

                    renderQuad(highlightStart, highlightEnd);
                }
                else if (m_shiftStart.Y < m_activeBufferView.getCursorPosition().Y) // Highlight down
                {
                    for (int i = Convert.ToInt16(m_shiftStart.Y); i < Convert.ToInt16(m_activeBufferView.getCursorPosition().Y) + 1; i++)
                    {
                        if (i == m_shiftStart.Y)
                        {
                            highlightStart.X = m_activeBufferView.getPosition().X + m_shiftStart.X * m_charWidth;
                            highlightEnd.X = m_activeBufferView.getPosition().X + m_activeBufferView.m_fileBuffer.getLine(i + m_activeBufferView.m_bufferShowStart).Length * m_charWidth;
                        }
                        else if (i == m_activeBufferView.getCursorPosition().Y)
                        {
                            highlightStart.X = m_activeBufferView.getPosition().X;
                            highlightEnd.X = m_activeBufferView.getPosition().X + m_activeBufferView.getCursorPosition().X * m_charWidth;
                        }
                        else
                        {
                            highlightStart.X = m_activeBufferView.getPosition().X;
                            highlightEnd.X = m_activeBufferView.getPosition().X + m_activeBufferView.m_fileBuffer.getLine(i + m_activeBufferView.m_bufferShowStart).Length * m_charWidth;
                        }

                        highlightStart.Y = m_activeBufferView.getPosition().Y + i * m_lineHeight;
                        highlightEnd.Y = highlightStart.Y + m_lineHeight;

                        renderQuad(highlightStart, highlightEnd);
                    }
                    //BoundingBox bb = new BoundingBox();
                }
                else  // Highlight up
                {
                    for (int i = Convert.ToInt16(m_activeBufferView.getCursorPosition().Y); i < Convert.ToInt16(m_shiftStart.Y) + 1; i++)
                    {
                        if (i == m_activeBufferView.getCursorPosition().Y)
                        {
                            highlightStart.X = m_activeBufferView.getPosition().X + m_activeBufferView.getCursorPosition().X * m_charWidth;
                            highlightEnd.X = m_activeBufferView.getPosition().X + m_activeBufferView.m_fileBuffer.getLine(i + m_activeBufferView.m_bufferShowStart).Length * m_charWidth;
                        }
                        else if (i == m_shiftStart.Y)
                        {
                            highlightStart.X = m_activeBufferView.getPosition().X;
                            highlightEnd.X = m_activeBufferView.getPosition().X + m_shiftStart.X * m_charWidth;
                        }
                        else
                        {
                            highlightStart.X = m_activeBufferView.getPosition().X;
                            highlightEnd.X = m_activeBufferView.getPosition().X + m_activeBufferView.m_fileBuffer.getLine(i + m_activeBufferView.m_bufferShowStart).Length * m_charWidth;
                        }

                        highlightStart.Y = m_activeBufferView.getPosition().Y + i * m_lineHeight;
                        highlightEnd.Y = highlightStart.Y + m_lineHeight;

                        renderQuad(highlightStart, highlightEnd);
                    }
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
            /*
            Color[] foregroundColors = new Color[1];
            foregroundColors[0] = Color.White;

            Texture2D td = new Texture2D(m_graphics.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            td.SetData(foregroundColors);
            */

            VertexPositionTexture[] vpt = new VertexPositionTexture[4];
            Vector2 tp = new Vector2(0, 1);
            vpt[0] = new VertexPositionTexture(topLeft, tp);
            vpt[1] = new VertexPositionTexture(topRight, tp);
            vpt[2] = new VertexPositionTexture(bottomRight, tp);
            vpt[3] = new VertexPositionTexture(bottomLeft, tp);

            //m_spriteBatch.Begin(0, null, null, DepthStencilState.DepthRead, RasterizerState.CullNone, m_basicEffect);
            m_spriteBatch.Begin(SpriteSortMode.Texture, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.DepthRead, RasterizerState.CullNone, m_basicEffect);

            m_spriteBatch.Draw(m_flatTexture, new Rectangle(Convert.ToInt16(topLeft.X),
                                                 Convert.ToInt16(topLeft.Y),
                                                 Convert.ToInt16(bottomRight.X) - Convert.ToInt16(topLeft.X),
                                                 Convert.ToInt16(bottomRight.Y) - Convert.ToInt16(topLeft.Y)),
                                                 m_activeBufferView.m_highlightColour);  
            m_spriteBatch.End();
        }
    }
}
