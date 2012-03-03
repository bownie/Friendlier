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
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;



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
        ///////////////// MEMBER VARIABLES //////////////////

        // XNA stuff
        //

        GraphicsDeviceManager m_graphics;

        /// <summary>
        /// One SpriteBatch
        /// </summary>
        SpriteBatch m_spriteBatch;

        /// <summary>
        /// Another SpriteBatch for the overlay
        /// </summary>
        SpriteBatch m_overlaySpriteBatch;

        /// <summary>
        /// One local SpriteFont - not sure if we need this now
        /// </summary>
        SpriteFont m_spriteFont;

        /// <summary>
        /// The state of our application - what we're doing at the moment
        /// </summary>
        FriendlierState m_state;

        /// <summary>
        /// Basic effect has with textures for fonts
        /// </summary>
        BasicEffect m_basicEffect;

        /// <summary>
        /// Line effect has no textures
        /// </summary>
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
        /// Current project
        /// </summary>
        static protected Project m_project;

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
        /// Is either Alt key being held down?
        /// </summary>
        bool m_altDown = false;

        /// <summary>
        /// Use this to store number when we've got ALT down - to select a new BufferView
        /// </summary>
        protected string m_gotoBufferView = "";

        /// <summary>
        /// Confirmation state 
        /// </summary>
        enum ConfirmState
        {
            None,
            FileSave,
            FileSaveCancel
        }

        /// <summary>
        /// Confirmation state - expecting Y/N
        /// </summary>
        ConfirmState m_confirmState = ConfirmState.None;

        /// <summary>
        /// The buffer we are currently editing
        /// </summary>
        BufferView m_activeBufferView = null;

        /// <summary>
        /// A flat texture
        /// </summary>
        Texture2D m_flatTexture;

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
        /// End time for the temporary message
        /// </summary>
        double m_temporaryMessageEndTime;

        /// <summary>
        /// Texture for a Directory Node
        /// </summary>
        Texture2D m_dirNodeTexture;

        /// <summary>
        /// File system watcher
        /// </summary>
        protected List<FileSystemWatcher> m_watcherList = new List<FileSystemWatcher>(); 
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public Friendlier()
        {
            m_graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // Antialiasing
            //
            m_graphics.PreferMultiSampling = true;

            // Set the editing state
            //
            m_state = FriendlierState.TextEditing;
        }


        /// <summary>
        /// Project constructor
        /// </summary>
        /// <param name="project"></param>
        public Friendlier(Project project)
        {
            m_graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // Antialiasing
            //
            m_graphics.PreferMultiSampling = true;

            // File name
            //
            m_project = project;

            // Set the editing state
            //
            m_state = FriendlierState.TextEditing;

            // Set windowed mode as default
            //
            windowedMode();
           
#if WINDOWS_PHONE
            TargetElapsedTime = TimeSpan.FromTicks(333333);
            graphics.IsFullScreen = true;
#endif
        }

        /////////////////////////////// METHODS //////////////////////////////////////


        /// <summary>
        /// Get the FileBuffer id of the active view
        /// </summary>
        /// <returns></returns>
        protected int getActiveBufferIndex()
        {
            return m_project.getFileBuffers().IndexOf(m_activeBufferView.getFileBuffer());
        }


        /// <summary>
        /// Enable windowed mode
        /// </summary>
        protected void windowedMode()
        {
            // Some of the mods we've used
            //
            //InitGraphicsMode(640, 480, false);
            //InitGraphicsMode(720, 576, false);
            //InitGraphicsMode(800, 500, false);
            //InitGraphicsMode(960, 768, false);
            //InitGraphicsMode(1920, 1080, true);

            int maxWidth = 0;
            int maxHeight = 0;

            foreach (DisplayMode dm in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
            {
                // Set both when either is greater
                if (dm.Width > maxWidth || dm.Height > maxHeight)
                {
                    maxWidth = dm.Width;
                    maxHeight = dm.Height;
                }
            }

            int windowWidth = 640;
            int windowHeight = 480;

            if (maxWidth >= 1920)
            {
                windowWidth = 960;
                windowHeight = 768;
            } else if (maxWidth >= 1280)
            {
                windowWidth = 800;
                windowHeight = 500;
            }
            else if (maxWidth >= 1024)
            {
                windowWidth = 720;
                windowHeight = 576;
            }

            // Set the graphics modes
            initGraphicsMode(windowWidth, windowHeight, false);
        }

        /// <summary>
        /// Enable full screen mode
        /// </summary>
        protected void fullScreenMode()
        {
            int maxWidth = 0;
            int maxHeight = 0;

            foreach (DisplayMode dm in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
            {
                // Set both when either is greater
                if (dm.Width > maxWidth || dm.Height > maxHeight)
                {
                    maxWidth = dm.Width;
                    maxHeight = dm.Height;
                }
            }

            // Set the graphics modes
            initGraphicsMode(maxWidth, maxHeight, true);
        }

        /// <summary>
        /// Initialise the project - load all the FileBuffers and select the correct BufferView
        /// </summary>
        /// <param name="project"></param>
        public void initialiseProject()
        {
            // Load all the files
            //
            m_project.loadFiles();

            // Ensure that all the BufferViews are populated with the charwidth and lineheights and
            // also that the relative positioning is correct - we have to do this in two passes.
            foreach (BufferView bv in m_project.getBufferViews())
            {
                bv.setCharWidth(m_charWidth);
                bv.setLineHeight(m_lineHeight);
            }

            // Now do some jiggery pokery to make sure positioning is correct and that
            // any cursors or highlights are within bounds.
            //
            foreach (BufferView bv in m_project.getBufferViews())
            {
                // check positions
                //
                bv.calculateMyRelativePosition();

                // check boundaries for cursor and highlighting
                //
                bv.verifyBoundaries();

#if FILESYSTEMWATCHER
                string name = bv.getFileBuffer().getFilepath();
                DateTime lastModTime = File.GetLastWriteTime(bv.getFileBuffer().getFilepath());

                // If this BufferView is tailing a file then find out what file it is and watch it
                //
                if (bv.isTailing())
                {
                    string dirName = System.IO.Path.GetDirectoryName(bv.getFileBuffer().getFilepath());
                    string fileName = System.IO.Path.GetFileName(bv.getFileBuffer().getFilepath());

                    bool alreadyWatching = false;
                    foreach (FileSystemWatcher fw in m_watcherList)
                    {
                        if (fw.Path == dirName && fw.Filter == fileName)
                        {
                            alreadyWatching = true;
                            break;
                        }
                    }

                    if (!alreadyWatching)
                    {
                        FileSystemWatcher watch = new FileSystemWatcher(dirName);
                        watch.Filter = fileName;
                        watch.Changed += new FileSystemEventHandler(OnFileChanged);

                        // Push to m_watcherlist to keep it alive
                        //
                        m_watcherList.Add(watch);

                        // Begin watching
                        //
                        watch.EnableRaisingEvents = true;
                    }
                }
#endif
            }


            // Get the BufferView id we've selected and set the BufferView
            //
            m_activeBufferView = m_project.getSelectedBufferView();

            // Ensure that we are in the correct position to view this buffer so there's no initial movement
            //
            m_eye = m_activeBufferView.getEyePosition();
            m_target = m_activeBufferView.getLookPosition();
            m_eye.Z = m_zoomLevel;

            // Set the active buffer view
            //
            setActiveBuffer();

            // Set-up the single FileSystemView we have
            //
            m_fileSystemView = new FileSystemView(m_filePath, new Vector3(-800.0f, 0f, 0f), m_lineHeight, m_charWidth);
        }

        /// <summary>
        /// A event handler for FileSystemWatcher
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private static void OnFileChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            Logger.logMsg("Friendlier::OnFileChanged() - File: " + e.FullPath + " " + e.ChangeType);

            foreach(FileBuffer fb in m_project.getFileBuffers())
            {
                if (fb.getFilepath() == e.FullPath)
                {
                    fb.forceRefetchFile();
                }
            }

            /*
            foreach(FileSystemWatcher fsw in m_watcherList)
            {
                string fullPath = fsw.Path + @"\" + fsw.Filter;

                if (fullPath == e.FullPath)
                {
                    fsw.EnableRaisingEvents = true;
                }
            }
             * */
           
            //FileSystemWatcher watcher = FileSystemWatcher(source);
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
        private bool initGraphicsMode(int iWidth, int iHeight, bool bFullScreen)
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

                    Logger.logMsg("Friendlier::initGraphicsMode() - width = " + iWidth + ", height = " + iHeight + ", fullscreen = " + bFullScreen.ToString());
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

                        Logger.logMsg("Friendlier::initGraphicsMode() - width = " + iWidth + ", height = " + iHeight + ", fullscreen = " + bFullScreen.ToString());
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Set the current main display SpriteFont to something in keeping with the resolution and reset some important variables.
        /// </summary>
        protected void setSpriteFont()
        {
            // Font loading - set our text size a bit fluffily at the moment
            //
            if (m_graphics.GraphicsDevice.Viewport.Width < 960)
            {
                Logger.logMsg("Friendlier:setSpriteFont() - using Small Window font");
                m_spriteFont = FontManager.getSmallWindowFont();
                m_textSize = 8.0f / (float)(m_spriteFont.LineSpacing) * GraphicsDevice.Viewport.AspectRatio;
            }
            else if (m_graphics.GraphicsDevice.Viewport.Width < 1024)
            {
                Logger.logMsg("Friendlier:setSpriteFont() - using Window font");
                m_spriteFont = FontManager.getWindowFont();
                m_textSize = 8.0f / (float)(m_spriteFont.LineSpacing) * GraphicsDevice.Viewport.AspectRatio;
            }
            else
            {
                Logger.logMsg("Friendlier:setSpriteFont() - using Full Screen font");
                m_spriteFont = FontManager.getFullScreenFont();
                m_textSize = (float)((int)(1400.0f / (float)(m_spriteFont.LineSpacing))) / 100.0f;
            }

            // to handle tabs for the moment convert them to single spaces
            //
            m_spriteFont.DefaultCharacter = ' ';
            Logger.logMsg("Friendlier:setSpriteFont() - using default character '" + m_spriteFont.DefaultCharacter + "'");
            Logger.logMsg("Friendlier:setSpriteFont() - you must get these three variables correct for each position to avoid nasty looking fonts:");
            Logger.logMsg("Friendlier:setSpriteFont() - zoom level = " + m_zoomLevel);
            Logger.logMsg("Friendlier:setSpriteFont() - setting line spacing = " + m_spriteFont.LineSpacing);
            Logger.logMsg("Friendlier:setSpriteFont() - setting text size = " + m_textSize);

            // Store these sizes and positions
            //
            m_charWidth = m_spriteFont.MeasureString("X").X * m_textSize;
            m_lineHeight = m_spriteFont.MeasureString("X").Y * m_textSize;

            Logger.logMsg("Friendlier:setSpriteFont() - m_charWidth = " + m_charWidth);
            Logger.logMsg("Friendlier:setSpriteFont() - m_lineHeight = " + m_lineHeight);

            // Now we need to make all of our BufferViews have this setting too
            //
            foreach (BufferView bv in m_project.getBufferViews())
            {
                bv.setCharWidth(m_charWidth);
                bv.setLineHeight(m_lineHeight);
            }

            // Now recalculate positions
            //
            foreach (BufferView bv in m_project.getBufferViews())
            {
                bv.calculateMyRelativePosition();
            }

            // Reset the active BufferView
            //
            setActiveBuffer();
        }


        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Initialise and load fonts into our Content context by family.
            //
            //FontManager.initialise(Content, "Lucida Sans Typewriter");
            //FontManager.initialise(Content, "Sax Mono");
            FontManager.initialise(Content, "Bitstream Vera Sans Mono");

            // Create a new SpriteBatch, which can be used to draw textures.
            m_spriteBatch = new SpriteBatch(m_graphics.GraphicsDevice);

            // Set up the SpriteFont for the chosen resolution
            //
            setSpriteFont();

            // Create some textures
            //
            //
            m_dirNodeTexture = Shapes.CreateCircle(m_graphics.GraphicsDevice, 100);

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

            // Create the overlay SpriteBatch
            //
            m_overlaySpriteBatch = new SpriteBatch(m_graphics.GraphicsDevice);

            // Create a flat texture for drawing rectangles etc
            //
            Color[] foregroundColors = new Color[1];
            foregroundColors[0] = Color.White;
            m_flatTexture = new Texture2D(m_graphics.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            m_flatTexture.SetData(foregroundColors);

            // Initialise the project
            //
            initialiseProject();
        }

        /// <summary>
        /// Use another method to set the active BufferView
        /// </summary>
        /// <param name="item"></param>
        protected void setActiveBuffer(int item)
        {
            if (item >= 0 && item < m_project.getBufferViews().Count)
            {
                Logger.logMsg("Friendlier::setActiveBuffer() - setting active BufferView " + item);
                setActiveBuffer(m_project.getBufferViews()[item]);
            }
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
                    m_activeBufferView = m_project.getSelectedBufferView();
                }
                else
                {
                    m_project.setSelectedBufferView(item);
                    m_activeBufferView = item;
                }
            }
            catch (Exception e)
            {
                Logger.logMsg("Cannot locate BufferView item in list " + e.ToString());
                return;
            }

            Logger.logMsg("Friendlier:setActiveBuffer() - active buffer view is " + m_project.getSelectedBufferViewId());

            Vector3 eyePos = m_activeBufferView.getEyePosition();
            eyePos.Z = m_zoomLevel;

            flyToPosition(eyePos);

            // Set the state of the application to TextEditing
            //
            m_state = FriendlierState.TextEditing;

#if ACTIVE_BUFFER_DEBUG
            Logger.logMsg("Friendlier:setActiveBuffer() - buffer position = " + m_activeBufferView.getPosition());
            Logger.logMsg("Friendlier:setActiveBuffer() - look position = " + m_target);
            Logger.logMsg("Friendlier:setActiveBuffer() - eye position = " + m_eye);
#endif
        }

        protected void setFileView()
        {
            Vector3 eyePos = m_fileSystemView.getEyePosition();
            eyePos.Z = m_zoomLevel;

            flyToPosition(eyePos);
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

        // Set up the file save mode
        //
        protected void selectSaveFile()
        {
            // Enter this mode and clear and existing message
            //
            m_state = FriendlierState.FileSaveAs;
            m_temporaryMessage = "";

            // Set temporary bird's eye view
            //
            Vector3 newPosition = m_eye;
            newPosition.Z = 600.0f;

            // Fly there
            //
            flyToPosition(newPosition);
        }

        protected void selectOpenFile()
        {
            // Enter this mode and clear and existing message
            //
            m_state = FriendlierState.FileOpen;
            m_temporaryMessage = "";

            // Set temporary bird's eye view
            //
            Vector3 newPosition = m_eye;
            newPosition.Z = 600.0f;

            // Fly there
            //
            flyToPosition(newPosition);
        }

        /// <summary>
        /// Open a file at the selected location
        /// </summary>
        protected void openHighlightedFile(GameTime gameTime, bool readOnly = false, bool tailFile = false)
        {
            //string fileToOpen = m_fileSystemView.getHighlightedFile();
            if (m_fileSystemView.atDriveLevel())
            {
                // First extract the drive letter and set the path
                //
                m_fileSystemView.setHighlightedDrive();
            }

            // If we're not at the root directory
            //
            if (m_fileSystemView.getHighlightIndex() > 0)
            {
                string subDirectory = "";

                // Set the directory to the sub directory and reset the highlighter
                //
                try
                {
                    if (m_fileSystemView.getHighlightIndex() - 1 < m_fileSystemView.getDirectoryInfo().GetDirectories().Length)
                    {
                        // Set error directory in case of failure to test access
                        //
                        DirectoryInfo directoryToAccess = m_fileSystemView.getDirectoryInfo().GetDirectories()[m_fileSystemView.getHighlightIndex() - 1];
                        subDirectory = directoryToAccess.Name;

                        // Test access
                        //
                        DirectoryInfo[] testAccess = directoryToAccess.GetDirectories();


                        FileInfo[] testFiles = directoryToAccess.GetFiles();

                        m_fileSystemView.setDirectory(directoryToAccess.FullName);
                        m_fileSystemView.setHighlightIndex(0);
                    }
                    else
                    {
                        int fileIndex = m_fileSystemView.getHighlightIndex() - 1 - m_fileSystemView.getDirectoryInfo().GetDirectories().Length;
                        FileInfo fileInfo = m_fileSystemView.getDirectoryInfo().GetFiles()[fileIndex];

                        Logger.logMsg("The file you selected is " + fileInfo.Name);

                        // OPEN FILE
                        //
                        BufferView newBV = addNewFileBuffer(fileInfo.FullName, readOnly, tailFile);

                        setActiveBuffer(newBV);
                    }
                }
                catch (Exception /* e */)
                {
                    setTemporaryMessage("Cannot access \"" + subDirectory + "\"", gameTime, 2);
                }
            }

        }

        protected void completeSaveFile(GameTime gameTime)
        {

            try
            {
                m_activeBufferView.getFileBuffer().save();
                Vector3 newPosition = m_eye;
                newPosition.Z = 500.0f;

                flyToPosition(newPosition);
                m_state = FriendlierState.TextEditing;

                setTemporaryMessage("[Saved]", gameTime, 2);
            }
            catch (Exception)
            {
                setTemporaryMessage("Failed to save to " + m_activeBufferView.getFileBuffer().getFilepath(), gameTime, 2);
            }
        }


        /// <summary>
        /// Ensure that buffers are saved
        /// </summary>
        protected void checkExit(GameTime gameTime)
        {
            // Save our project
            //
            m_project.dataContractSerialise();

            // Firstly check for any unsaved buffers and warn
            //
            bool unsaved = false;
            foreach (FileBuffer fb in m_project.getFileBuffers())
            {
                if (fb.isModified())
                {
                    unsaved = true;
                    break;
                }
            }

            if (unsaved)
            {
                setTemporaryMessage("[Unsaved Buffers.  Save?  Y/N/C]", gameTime, 0);
                m_confirmState = ConfirmState.FileSaveCancel;
                m_state = FriendlierState.FileSaveAs;
            }
            else
            {
                this.Exit();
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
            if (checkKeyState(Keys.Escape, gameTime))
            {
                // Depends where we are in the process here - check state

                Vector3 newPosition = m_eye;
                newPosition.Z = 500.0f;

                switch (m_state)
                {
                    case FriendlierState.TextEditing:
                                checkExit(gameTime);
                                break;

                    case FriendlierState.FileOpen:
                    case FriendlierState.FileSaveAs:
                    default:
                                break;
                }

                m_state = FriendlierState.TextEditing;

                // Fly back to correct position
                //
                flyToPosition(newPosition);
            }

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

            // Shift key state
            //
            if (m_shiftDown && Keyboard.GetState(PlayerIndex.One).IsKeyUp(Keys.LeftShift) &&
                Keyboard.GetState(PlayerIndex.One).IsKeyUp(Keys.RightShift))
            {
                if (Keyboard.GetState(PlayerIndex.One).IsKeyUp(Keys.LeftControl) &&
                    Keyboard.GetState(PlayerIndex.One).IsKeyUp(Keys.RightControl) &&
                    Keyboard.GetState(PlayerIndex.One).IsKeyUp(Keys.LeftAlt) &&
                    Keyboard.GetState(PlayerIndex.One).IsKeyUp(Keys.RightAlt))
                {
                    m_shiftDown = false;
                }
            }
            else
            {
                if (!m_shiftDown && (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.LeftShift) ||
                    Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.RightShift)))
                {
                    m_shiftDown = true;

                    // Also set our current bufferview highlighting if we're not already highlighting
                    //
                    if (!m_activeBufferView.gotHighlight())
                    {
                        m_activeBufferView.startHighlight();
                    }
                }
            }

            // Alt key state
            //
            if (m_altDown && Keyboard.GetState(PlayerIndex.One).IsKeyUp(Keys.LeftAlt) &&
                Keyboard.GetState(PlayerIndex.One).IsKeyUp(Keys.RightAlt))
            {
                m_altDown = false;

                // Check for something in our number store
                //
                if (m_gotoBufferView != "")
                {
                    Logger.logMsg("Firendlier - got a number " + m_gotoBufferView);

                    setActiveBuffer(Convert.ToInt16(m_gotoBufferView));
                    m_gotoBufferView = "";
                }
            }
            else
            {
                if (!m_altDown && (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.LeftAlt) ||
                    Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.RightAlt)))
                {
                    m_altDown = true;
                }
            }

#if DEBUG_SHIFT_SELECTION
            FilePosition shiftStart2 = m_shiftStart;
            FilePosition shiftEnd2 = m_shiftEnd;
            shiftStart2.Y += m_activeBufferView.getBufferShowStartY();
            shiftEnd2.Y += m_activeBufferView.getBufferShowStartY();
            Logger.logMsg("SHIFT START = " + shiftStart2.Y);
            Logger.logMsg("SHIFT END   = " + shiftEnd2.Y);
#endif

            if (checkKeyState(Keys.Up, gameTime))
            {
                if (m_state == FriendlierState.FileSaveAs || m_state == FriendlierState.FileOpen)
                {
                    if (m_fileSystemView.getHighlightIndex() > 0)
                    {
                        m_fileSystemView.incrementHighlightIndex(-1);
                    }
                }
                else
                {
                    if (m_altDown)
                    {
                        // Add a new BufferView above current position
                        //
                        addBufferView(BufferView.BufferPosition.Above);
                    }
                    else
                    {
                        m_activeBufferView.moveCursorUp(false);

                        if (m_shiftDown)
                        {
                            m_activeBufferView.extendHighlight();  // Extend 
                        }
                        else
                        {
                            m_activeBufferView.noHighlight(); // Disable
                        }
                    }
                }
            }
            else if (checkKeyState(Keys.Down, gameTime))
            {
                if (m_state == FriendlierState.FileSaveAs || m_state == FriendlierState.FileOpen)
                {
                    if (m_fileSystemView.atDriveLevel())
                    {
                        // Drives are highlighted slightly differently to directories as the zero index is 
                        // counted for drives (1 for directories) hence the adjustment in the RH term
                        //
                        if (m_fileSystemView.getHighlightIndex() < m_fileSystemView.countActiveDrives() - 1)
                        {
                            m_fileSystemView.incrementHighlightIndex(1);
                        }
                    }
                    else if (m_fileSystemView.getHighlightIndex() < m_fileSystemView.getDirectoryLength())
                    {
                        m_fileSystemView.incrementHighlightIndex(1);
                    }
                }
                else
                {
                    if (m_altDown)
                    {
                        // Add a new BufferView below current position
                        //
                        addBufferView(BufferView.BufferPosition.Below);
                    }
                    else
                    {
                        m_activeBufferView.moveCursorDown(false);

                        if (m_shiftDown)
                        {
                            m_activeBufferView.extendHighlight();
                        }
                        else
                        {
                            m_activeBufferView.noHighlight(); // Disable
                        }
                    }
                }
            }
            else if (checkKeyState(Keys.Left, gameTime))
            {
                if (m_state == FriendlierState.FileSaveAs || m_state == FriendlierState.FileOpen)
                {
                    string parDirectory = "";

                    // Set the directory to the sub directory and reset the highlighter
                    //
                    try
                    {
                        DirectoryInfo parentTest = m_fileSystemView.getParent();

                        if (parentTest == null)
                        {
                            Logger.logMsg("Check devices");
                            m_fileSystemView.setDirectory(null);
                        }
                        else
                        {
                            parDirectory = m_fileSystemView.getParent().Name;
                            DirectoryInfo[] testAccess = m_fileSystemView.getParent().GetDirectories();
                            FileInfo[] testFiles = m_fileSystemView.getParent().GetFiles();

                            m_fileSystemView.setDirectory(m_fileSystemView.getParent().FullName);
                            m_fileSystemView.setHighlightIndex(0);
                        }
                    }
                    catch (Exception /*e*/)
                    {
                        setTemporaryMessage("Cannot access " + parDirectory.ToString() , gameTime, 2);
                    }
                }
                else
                {
                    if (m_altDown)
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
                            // If control is down then word jump
                            //
                            if (m_ctrlDown)
                            {
                                string line = m_activeBufferView.getFileBuffer().getLine(fp.Y);
                                /*
                                try
                                {

                                    for(int i = fp.X; i > 0; i--)
                                    {
                                        if (line[i] = ' ')
                                            retu
                                    }

                                    int jumpPosition = getPreviousSpace(line);

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
                                catch (Exception)
                                {
                                    Logger.logMsg("Friendlier:: couldn't jump");
                                    fp.X--;
                                }*/
                            }
                            else
                            {
                                fp.X--;
                            }
                            m_activeBufferView.setCursorPosition(fp);

                        }
                        else
                        {
                            m_activeBufferView.moveCursorUp(true);
                        }

                        if (m_shiftDown)
                        {
                            m_activeBufferView.extendHighlight();  // Extend
                        }
                        else
                        {
                            m_activeBufferView.noHighlight(); // Disable
                        }
                    }
                }
            }
            else if (checkKeyState(Keys.Right, gameTime))
            {
                if (m_state == FriendlierState.FileSaveAs || m_state == FriendlierState.FileOpen)
                {
                    openHighlightedFile(gameTime);
                }
                else
                {
                    if (m_altDown)
                    {
                        // Add a new BufferView to the right of current position
                        //
                        addBufferView(BufferView.BufferPosition.Right);
                    }
                    else
                    {
                        FilePosition fp = m_activeBufferView.getCursorPosition();
                        string line = m_activeBufferView.getFileBuffer().getLine(fp.Y);

                        if (m_activeBufferView.getCursorPosition().X < line.Length)
                        {
                            if (fp.X < line.Length)
                            {
                                // If control is down then word jump
                                //
                                if (m_ctrlDown)
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
                                m_activeBufferView.setCursorPosition(fp);
                            }
                        }
                        else
                        {
                            m_activeBufferView.moveCursorDown(true);
                        }
                        
                        if (m_shiftDown)
                        {
                            m_activeBufferView.extendHighlight(); // Extend
                        }
                        else
                        {
                            m_activeBufferView.noHighlight(); // Disable
                        }
                    }
                }
            }
            else if (checkKeyState(Keys.End, gameTime))
            {
                FilePosition fp = m_activeBufferView.getCursorPosition();
                fp.X = m_activeBufferView.getFileBuffer().getLine(fp.Y).Length;
                m_activeBufferView.setCursorPosition(fp);

                if (m_shiftDown)
                {
                    m_activeBufferView.extendHighlight(); // Extend
                }
                else
                {
                    m_activeBufferView.noHighlight(); // Disable
                }

            }
            else if (checkKeyState(Keys.Home, gameTime))
            {
                FilePosition fp = m_activeBufferView.getCursorPosition();
                fp.X = 0;
                m_activeBufferView.setCursorPosition(fp);

                if (m_shiftDown)
                {
                    m_activeBufferView.extendHighlight(); // Extend
                }
                else
                {
                    m_activeBufferView.noHighlight(); // Disable
                }
            }
            else if (checkKeyState(Keys.F1, gameTime))
            {
                if (m_shiftDown)
                {
                    m_zoomLevel -= 1;
                }
                else if (m_ctrlDown)
                {
                    m_zoomLevel -= 5;
                }
                else
                {
                    m_zoomLevel -= 500.0f;
                }

                if (m_zoomLevel < 500.0f)
                {
                    m_zoomLevel = 500.0f;
                }
                setActiveBuffer();
            }
            else if (checkKeyState(Keys.F2, gameTime))
            {
                if (m_shiftDown)
                {
                    m_zoomLevel += 1;
                }
                else if (m_ctrlDown)
                {
                    m_zoomLevel += 5;
                }
                else
                {
                    m_zoomLevel += 500.0f;
                }

                setActiveBuffer();
            }
            else if (checkKeyState(Keys.F3, gameTime))
            {
                fullScreenMode();
                setSpriteFont();
            }
            else if (checkKeyState(Keys.F4, gameTime))
            {
                windowedMode();
                setSpriteFont();
            }
            else if (checkKeyState(Keys.F5, gameTime))
            {
                int newValue = m_project.getSelectedBufferViewId() - 1;
                if (newValue < 0)
                {
                    newValue += m_project.getBufferViews().Count;
                }

                m_project.setSelectedBufferViewId(newValue);
                setActiveBuffer();
            }
            else if (checkKeyState(Keys.F6, gameTime))
            {
                int newValue = (m_project.getSelectedBufferViewId() + 1) % m_project.getBufferViews().Count;
                m_project.setSelectedBufferViewId(newValue);
                setActiveBuffer();
            }
            else if (checkKeyState(Keys.F7, gameTime))
            {
                setFileView();
            }
            else if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.Insert)) // Reset
            {
                m_eye.X = 12f;
                m_eye.Y = 5f;
                m_eye.Z = 0f;
            }
            else if (checkKeyState(Keys.PageDown, gameTime))
            {
                m_activeBufferView.pageDown();

                if (m_shiftDown)
                {
                    m_activeBufferView.extendHighlight(); // Extend
                }
                else
                {
                    m_activeBufferView.noHighlight(); // Disable
                }
            }
            else if (checkKeyState(Keys.PageUp, gameTime))
            {
                m_activeBufferView.pageUp();

                if (m_shiftDown)
                {
                    m_activeBufferView.extendHighlight(); // Extend
                }
                else
                {
                    m_activeBufferView.noHighlight(); // Disable
                }
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

                if (m_state == FriendlierState.FileSaveAs && checkKeyState(Keys.Back, gameTime))
                {
                    // Delete charcters from the file name if we have one
                    //
                    if (m_saveFileName.Length > 0)
                    {
                        m_saveFileName = m_saveFileName.Substring(0, m_saveFileName.Length - 1);
                    }
                }
                else if (m_activeBufferView.gotHighlight()) // If we have a valid highlighted selection then delete it (normal editing)
                {
                    // All the clever stuff with the cursor is done at the BufferView level and it also
                    // calls the command in the FileBuffer.
                    //
                    m_activeBufferView.deleteCurrentSelection();
                }
                else // delete at cursor
                {
                    if (checkKeyState(Keys.Delete, gameTime))
                    {
                        m_activeBufferView.deleteSingle();
                    }
                    else if (checkKeyState(Keys.Back, gameTime))
                    {
                        FilePosition fp = m_activeBufferView.getCursorPosition();

                        if (fp.X > 0)
                        {
                            // Decrement and set X
                            //
                            fp.X--;
                            m_activeBufferView.setCursorPosition(fp);
                            m_activeBufferView.deleteSingle();
                        }
                        else if (fp.Y > 0)
                        {
                            fp.Y -= 1;
                            fp.X = m_activeBufferView.getFileBuffer().getLine(Convert.ToInt16(fp.Y)).Length;
                            m_activeBufferView.setCursorPosition(fp);

                            m_activeBufferView.deleteSingle();
                        }
                    }
                }
            }
            else
            {
                // Actions bound to key combinations
                //
                //
                if (m_confirmState != ConfirmState.None)
                {
                    if (checkKeyState(Keys.Y, gameTime))
                    {

                        Logger.logMsg("Confirm save");
                        try
                        {
                            if (m_confirmState == ConfirmState.FileSave)
                            {
                                // Select a file path if we need one
                                //
                                if (m_activeBufferView.getFileBuffer().getFilepath() == "")
                                {
                                    selectSaveFile();
                                }
                                else
                                {
                                    // Attempt save
                                    //
                                    m_activeBufferView.getFileBuffer().save();

                                    // Save has completed without error
                                    //
                                    setTemporaryMessage("[Saved]", gameTime, 2);
                                    m_state = FriendlierState.TextEditing;
                                }
                            }
                            else if (m_confirmState == ConfirmState.FileSaveCancel)
                            {
                                foreach (FileBuffer fb in m_project.getFileBuffers())
                                {
                                    // Select a file path if we need one
                                    //
                                    if (fb.getFilepath() == "")
                                    {
                                        selectSaveFile();
                                    }
                                    else
                                    {
                                        fb.save();
                                    }
                                }

                                setTemporaryMessage("[Saved.  Exiting.]", gameTime, 5);
                                this.Exit();
                            }
                        }
                        catch (Exception e)
                        {
                            setTemporaryMessage("[Save failed with \"" + e.Message + "\" ]", gameTime, 5);
                        }

                        m_confirmState = ConfirmState.None;
                    }
                    else if (checkKeyState(Keys.N, gameTime))
                    {
                        // If no for single file save then continue - if no for FileSaveCancel then quit
                        //
                        if (m_confirmState == ConfirmState.FileSave)
                        {
                            m_temporaryMessage = "";
                            m_confirmState = ConfirmState.None;
                        }
                        else if (m_confirmState == ConfirmState.FileSaveCancel)
                        {
                            this.Exit();
                        }
                    }
                    else if (checkKeyState(Keys.C, gameTime) && m_confirmState == ConfirmState.FileSaveCancel)
                    {
                        setTemporaryMessage("[Cancelled Quit]", gameTime, 0.5);
                        m_confirmState = ConfirmState.None;
                    }
                }
                else if (m_ctrlDown)  // CTRL down action
                {
                    if (checkKeyState(Keys.C, gameTime)) // Copy
                    {
                        Logger.logMsg("Friendler::update() - copying to clipboard");
                        System.Windows.Forms.Clipboard.SetText(m_activeBufferView.getSelection().getClipboardString());
                    }
                    else if (checkKeyState(Keys.X, gameTime)) // Cut
                    {
                        Logger.logMsg("Friendler::update() - cut");

                        System.Windows.Forms.Clipboard.SetText(m_activeBufferView.getSelection().getClipboardString());
                        m_activeBufferView.deleteCurrentSelection();
                    }
                    else if (checkKeyState(Keys.V, gameTime)) // Paste
                    {
                        if (System.Windows.Forms.Clipboard.ContainsText())
                        {
                            Logger.logMsg("Friendler::update() - pasting text");
                            // If we have a selection then replace it - else insert
                            //
                            if (m_activeBufferView.gotHighlight())
                            {
                                m_activeBufferView.replaceCurrentSelection(System.Windows.Forms.Clipboard.GetText());
                            }
                            else
                            {
                                m_activeBufferView.insertText(System.Windows.Forms.Clipboard.GetText());
                            }
                        }
                    }
                    else if (checkKeyState(Keys.Z, gameTime))  // Undo
                    {
                        // Undo a certain number of steps
                        //
                        try
                        {
                            // We call the undo against the FileBuffer and this returns the cursor position
                            // resulting from this action.
                            //
                            if (m_activeBufferView.getFileBuffer().getUndoPosition() > 0)
                            {
                                m_activeBufferView.setCursorPosition(m_activeBufferView.getFileBuffer().undo(1));
                            }
                            else
                            {
                                //Logger.logMsg("Friendlier::Update() - nothing to undo");
                                setTemporaryMessage("[NOUNDO]", gameTime, 0.3);
                            }
                        }
                        catch (Exception e)
                        {
                            //System.Windows.Forms.MessageBox.Show("Undo stack is empty - " + e.Message);
                            Logger.logMsg("Friendlier::Update() - got exception " + e.Message);
                            setTemporaryMessage("[NOUNDO]", gameTime, 2);
                        }
                    }
                    else if (checkKeyState(Keys.Y, gameTime))  // Redo
                    {
                        // Undo a certain number of steps
                        //
                        try
                        {
                            // We call the undo against the FileBuffer and this returns the cursor position
                            // resulting from this action.
                            //
                            if (m_activeBufferView.getFileBuffer().getUndoPosition() <
                                m_activeBufferView.getFileBuffer().getCommandStackLength())
                            {
                                m_activeBufferView.setCursorPosition(m_activeBufferView.getFileBuffer().redo(1));
                            }
                            else
                            {
                                setTemporaryMessage("[NOREDO]", gameTime, 0.3);
                            }
                        }
                        catch (Exception e)
                        {
                            //System.Windows.Forms.MessageBox.Show("Undo stack is empty - " + e.Message);
                            Logger.logMsg("Friendlier::Update() - got exception " + e.Message);
                            setTemporaryMessage("[NOREDO]", gameTime, 2);
                        }
                    }
                }
                else if (m_altDown) // ALT down action
                {
                    if (checkKeyState(Keys.S, gameTime) && m_activeBufferView.getFileBuffer().isModified())
                    {
                        setTemporaryMessage("[Confirm Save? Y/N]", gameTime, 0);
                        m_confirmState = ConfirmState.FileSave;
                    }
                    else if (checkKeyState(Keys.N, gameTime))
                    {
                        BufferView newBV = addNewFileBuffer();
                        setActiveBuffer(newBV);
                    }
                    else if (checkKeyState(Keys.O, gameTime))
                    {
                        selectOpenFile();
                    }
                    else if (checkKeyState(Keys.D0, gameTime) ||
                             checkKeyState(Keys.D1, gameTime) ||
                             checkKeyState(Keys.D2, gameTime) ||
                             checkKeyState(Keys.D3, gameTime) ||
                             checkKeyState(Keys.D4, gameTime) ||
                             checkKeyState(Keys.D5, gameTime) ||
                             checkKeyState(Keys.D6, gameTime) ||
                             checkKeyState(Keys.D7, gameTime) ||
                             checkKeyState(Keys.D8, gameTime) ||
                             checkKeyState(Keys.D9, gameTime))
                    {
                        m_gotoBufferView += getNumberKey();
                    }
                }
                else
                    if (//Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.LeftShift) ||
                        //Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.RightShift) ||
                        Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.RightControl) ||
                        Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.LeftControl) ||
                        m_confirmState != ConfirmState.None)
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
                                FilePosition fp = m_activeBufferView.getCursorPosition();

                                if (keyDown == Keys.Enter)
                                {
                                    if (m_state == FriendlierState.FileSaveAs)
                                    {
                                        // Check that the filename is valid
                                        //
                                        if (m_saveFileName != "" && m_saveFileName != null)
                                        {
                                            m_activeBufferView.getFileBuffer().setFilepath(m_fileSystemView.getPath() + m_saveFileName);

                                            Logger.logMsg("FILE NAME = " + m_activeBufferView.getFileBuffer().getFilepath());

                                            completeSaveFile(gameTime);
                                            //Logger.logMsg("Got return");
                                        }
                                    }
                                    else if (m_state == FriendlierState.FileOpen)
                                    {
                                        openHighlightedFile(gameTime);
                                    }
                                    else
                                    {
                                        // Insert a line into the editor
                                        //
                                        m_activeBufferView.insertNewLine();

                                        //fp = m_activeBufferView.getFileBuffer().insertNewLine(m_activeBufferView.getCursorPosition());

                                        // When we come back from the insertNewLine call we have to check to see if
                                        // the new position given to us is outside the viewable area and adjust if
                                        // so.
                                        //
                                        /*
                                        if (fp.Y >= m_activeBufferView.getBufferShowLength())
                                        {
                                            // Move down by the number of rows we're overlapping
                                            //
                                            for (int i = m_activeBufferView.getBufferShowLength(); i <= fp.Y; i++)
                                            {
                                                m_activeBufferView.moveCursorDown(false);
                                            }
                                        }
                                         * */
                                    }
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

                                        case Keys.OemPipe:
                                            if (m_shiftDown)
                                            {
                                                key = "|";
                                            }
                                            else
                                            {
                                                key = "\\";
                                            }
                                            break;

                                        case Keys.OemQuestion:
                                            if (m_shiftDown)
                                            {
                                                key = "?";
                                            }
                                            else
                                            {
                                                key = "/";
                                            }
                                            break;

                                        case Keys.OemSemicolon:
                                            if (m_shiftDown)
                                            {
                                                key = ":";
                                            }
                                            else
                                            {
                                                key = ";";
                                            }
                                            break;

                                        case Keys.OemQuotes:
                                            if (m_shiftDown)
                                            {
                                                key = "#";
                                            }
                                            else
                                            {
                                                key = "~";
                                            }
                                            break;

                                        case Keys.OemTilde:
                                            if (m_shiftDown)
                                            {
                                                key = "@";
                                            }
                                            else
                                            {
                                                key = "'";
                                            }
                                            break;

                                        case Keys.OemOpenBrackets:
                                            if (m_shiftDown)
                                            {
                                                key = "{";
                                            }
                                            else
                                            {
                                                key = "[";
                                            }
                                            break;

                                        case Keys.OemCloseBrackets:
                                            if (m_shiftDown)
                                            {
                                                key = "}";
                                            }
                                            else
                                            {
                                                key = "]";
                                            }
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

                                        case Keys.A:
                                        case Keys.B:
                                        case Keys.C:
                                        case Keys.D:
                                        case Keys.E:
                                        case Keys.F:
                                        case Keys.G:
                                        case Keys.H:
                                        case Keys.I:
                                        case Keys.J:
                                        case Keys.K:
                                        case Keys.L:
                                        case Keys.M:
                                        case Keys.N:
                                        case Keys.O:
                                        case Keys.P:
                                        case Keys.Q:
                                        case Keys.R:
                                        case Keys.S:
                                        case Keys.U:
                                        case Keys.V:
                                        case Keys.W:
                                        case Keys.X:
                                        case Keys.Y:
                                        case Keys.Z:
                                            if (m_shiftDown)
                                            {
                                                key = keyDown.ToString().ToUpper();
                                            }
                                            else
                                            {
                                                key = keyDown.ToString().ToLower();
                                            }
                                            break;

                                        case Keys.T:
                                            if (m_state == FriendlierState.FileOpen)
                                            {
                                                // Open a file as read only and tail it
                                                //
                                                openHighlightedFile(gameTime, true, true);
                                            }
                                            else
                                            {
                                                if (m_shiftDown)
                                                {
                                                    key = keyDown.ToString().ToUpper();
                                                }
                                                else
                                                {
                                                    key = keyDown.ToString().ToLower();
                                                }
                                            }
                                            break;


                                        // Do nothing as default
                                        //
                                        default:
                                            key = "";
                                            Logger.logMsg("Got key = " + keyDown.ToString());
                                            break;
                                    }

                                    if (key != "")
                                    {
                                        if (m_state == FriendlierState.FileSaveAs)
                                        {
                                            //Logger.logMsg("Writing letter " + key);
                                            m_saveFileName += key;
                                        }
                                        else
                                        {
                                            // Do we need to do some deletion or replacing?
                                            //
                                            if (m_activeBufferView.gotHighlight())
                                            {
                                                m_activeBufferView.replaceCurrentSelection(key);
                                            }
                                            else
                                            {
                                                m_activeBufferView.insertText(key);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
            }

            // Check for this change as necessary
            //
            changeEyePosition(gameTime);

            // Save the last state if it has changed and clear any temporary message
            //
            if (m_lastKeyboardState != Keyboard.GetState())
            {
                m_lastKeyboardState = Keyboard.GetState();
                //m_temporaryMessage = "";
            }

            base.Update(gameTime);

        }

        protected string getNumberKey()
        {
            string key = "";

            foreach (Keys keyDown in Keyboard.GetState().GetPressedKeys())
            {
                switch(keyDown)
                {
                    case Keys.D0:
                        key = "0";
                        break;

                    case Keys.D1:
                        key = "1";
                        break;

                    case Keys.D2:
                        key = "2";
                        break;

                    case Keys.D3:
                        key = "3";
                        break;

                    case Keys.D4:
                        key = "4";
                        break;

                    case Keys.D5:
                        key = "5";
                        break;

                    case Keys.D6:
                        key = "6";
                        break;

                    case Keys.D7:
                        key = "7";
                        break;

                    case Keys.D8:
                        key = "8";
                        break;

                    case Keys.D9:
                        key = "9";
                        break;

                    default:
                        break;
                }
            }

            return key;

        }
        /// <summary>
        /// Set a temporary message until a given end time (seconds into the future)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="gameTime"></param>
        protected void setTemporaryMessage(string message, GameTime gameTime, double seconds)
        {
            m_temporaryMessage = message;

            if (seconds == 0)
            {
                seconds = 604800; // a week should be long enough to signal infinity
            }

            // Add on the current gameTime position
            //
            seconds += gameTime.TotalGameTime.TotalSeconds;

            m_temporaryMessageEndTime = seconds;
        }


        /// <summary>
        /// Add a new FileBuffer and a new BufferView and set this as active
        /// </summary>
        protected BufferView addNewFileBuffer(string filename = null, bool readOnly = false, bool tailFile = false)
        {
            // Create an empty buffer and add it to the list of buffers
            //
            FileBuffer newFB;

            if (filename == null)
            {
                newFB = new FileBuffer();
            }
            else
            {
                newFB = new FileBuffer(filename, readOnly);

                // Load the file
                //
                newFB.loadFile();
            }

            // Add the FileBuffer and keep the index for our BufferView
            //
            int fileIndex = m_project.addFileBuffer(newFB);

            // Always assign a new bufferview to the right if we have one - else default position
            //
            Vector3 newPos = Vector3.Zero;
            if (m_activeBufferView != null)
            {
                newPos = getFreeBufferViewPosition(BufferView.BufferPosition.Right);
            }

            BufferView newBV = new BufferView(newFB, newPos, 0, 20, m_charWidth, m_lineHeight, fileIndex, readOnly);
            newBV.setTailing(tailFile);
            newBV.m_textColour = Color.LightBlue;
            m_project.addBufferView(newBV);

            return newBV;
        }

        /// <summary>
        /// Find a free position around the active view
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        protected Vector3 getFreeBufferViewPosition(BufferView.BufferPosition position)
        {
            bool occupied = false;

            // Initial new pos is here from active BufferView
            //
            Vector3 newPos = m_activeBufferView.calculateRelativePosition(position);
            do
            {
                occupied = false;

                foreach (BufferView cur in m_project.getBufferViews())
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

            return newPos;
        }

        /// <summary>
        /// Find a good position for a new BufferView relative to the current active position
        /// </summary>
        /// <param name="position"></param>
        protected void addBufferView(BufferView.BufferPosition position)
        {
            Vector3 newPos = getFreeBufferViewPosition(position);

            BufferView newBufferView = new BufferView(m_activeBufferView, newPos);
            //newBufferView.m_textColour = Color.LawnGreen;
            m_project.addBufferView(newBufferView);
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
                        m_vFly = (m_newEyePosition - m_eye) / 10;
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

                // Check to see if the key has been held down for a while - for file selection menus
                // (see the m_state clause) we make the repeat interval smaller.
                //
                if (gameTime.TotalGameTime.TotalSeconds - m_heldDownStartTime > repeatHold ||
                    (m_state != FriendlierState.TextEditing &&
                    (gameTime.TotalGameTime.TotalSeconds - m_heldDownStartTime  > repeatHold / 4)))
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

            for (int i = 0; i < m_project.getBufferViews().Count; i++)
            {
                drawFileBuffer(m_project.getBufferViews()[i], gameTime);
            }

            // If we're choosing a file then
            //
            if (m_state == FriendlierState.FileSaveAs || m_state == FriendlierState.FileOpen)
            {
                drawDirectoryChooser(gameTime);
                m_spriteBatch.End();
            }
            else
            {
                m_spriteBatch.End();

                // Draw the Overlay HUD
                //
                drawOverlay(gameTime);

                // Cursor and cursor highlight - none for tailed bufferviews
                //
                if (!m_activeBufferView.isTailing())
                {
                    drawCursor(gameTime);
                    drawHighlight(gameTime);
                }
            }

            base.Draw(gameTime);
        }

        /// <summary>
        /// Initial path for FileSystemView
        /// </summary>
        protected string m_filePath = @"C:\";

        protected FileSystemView m_fileSystemView;

        /// <summary>
        /// A variable we use to store our save file name
        /// </summary>
        protected string m_saveFileName;

        /// <summary>
        /// Draw the HUD Overlay for the editor
        /// </summary>
        protected void drawOverlay(GameTime gameTime)
        {
            string fileName = "";
            if (m_activeBufferView != null)
            {
                // Set the filename
                if (m_activeBufferView.getFileBuffer().getShortFileName() != "")
                {
                    fileName = "\"" + m_activeBufferView.getFileBuffer().getShortFileName() + "\"";
                }
                else
                {
                    fileName = "<New Buffer>";
                }

                if (m_activeBufferView.getFileBuffer().isModified())
                {
                    fileName += " [Modified]";
                }

                fileName += " " + m_activeBufferView.getFileBuffer().getLineCount() + " lines";
            }

            // Add some other useful states to our status line
            //
            if (m_activeBufferView.isReadOnly())
            {
                fileName += " [RDONLY]";
            }

            if (m_activeBufferView.isTailing())
            {
                fileName += " [TAIL]";
            }

            if (m_shiftDown)
            {
                fileName += " [SHFT]";
            }

            if (m_ctrlDown)
            {
                fileName += " [CTRL]";
            }

            if (m_altDown)
            {
                fileName += " [ALT]";
            }

            double dTS = gameTime.TotalGameTime.TotalSeconds;
            if (dTS < m_temporaryMessageEndTime && m_temporaryMessage != "")
            {
                // Add any temporary message on to the end of the message
                //
                fileName += " " + m_temporaryMessage;
            }

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
                    modeString = "browsing";
                    break;

                case FriendlierState.FileSaveAs:
                    modeString = "saving file";
                    break;

                default:
                    modeString = "free";
                    break;
            }

            float modeStringXPos = m_graphics.GraphicsDevice.Viewport.Width - modeString.Length * m_charWidth - (m_charWidth * 10);
            
            string positionString = m_activeBufferView.getCursorPosition().Y + m_activeBufferView.getBufferShowStartY() + "," + m_activeBufferView.getCursorPosition().X;
            float positionStringXPos = m_graphics.GraphicsDevice.Viewport.Width - positionString.Length * m_charWidth - (m_charWidth * 18);

            float filePercent = 0.0f;

            if (m_activeBufferView.getFileBuffer().getLineCount() > 0)
            {
                filePercent = (float)(m_activeBufferView.getCursorPosition().Y + m_activeBufferView.getBufferShowStartY()) /
                              (float)(m_activeBufferView.getFileBuffer().getLineCount());
            }


            string filePercentString = ((int)(filePercent * 100.0f)) + "%";
            float filePercentStringXPos = m_graphics.GraphicsDevice.Viewport.Width - filePercentString.Length * m_charWidth - (m_charWidth * 5);


            // http://forums.create.msdn.com/forums/p/61995/381650.aspx
            //
            //m_overlaySpriteBatch.Begin(SpriteSortMode.Texture, BlendState.AlphaBlend);
            //m_overlaySpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.LinearWrap, DepthStencilState.None,RasterizerState.CullCounterClockwise);
            m_overlaySpriteBatch.Begin();

            // hardcode the font size to 1.0f so it looks nice
            //
            m_overlaySpriteBatch.DrawString(FontManager.getOverlayFont(), fileName, new Vector2(0.0f, yPos), Color.White, 0, Vector2.Zero, 1.0f, 0, 0);
            m_overlaySpriteBatch.DrawString(FontManager.getOverlayFont(), eyePosition, new Vector2(0.0f, 0.0f), Color.White, 0, Vector2.Zero, 1.0f, 0, 0);
            m_overlaySpriteBatch.DrawString(FontManager.getOverlayFont(), modeString, new Vector2(modeStringXPos, 0.0f), Color.White, 0, Vector2.Zero, 1.0f, 0, 0);
            m_overlaySpriteBatch.DrawString(FontManager.getOverlayFont(), positionString, new Vector2(positionStringXPos, yPos), Color.White, 0, Vector2.Zero, 1.0f, 0, 0);
            m_overlaySpriteBatch.DrawString(FontManager.getOverlayFont(), filePercentString, new Vector2(filePercentStringXPos, yPos), Color.White, 0, Vector2.Zero, 1.0f, 0, 0);
            m_overlaySpriteBatch.End();
        }

        /// <summary>
        /// Draw a cursor and make it blink in position
        /// </summary>
        /// <param name="v"></param>
        protected void drawCursor(GameTime gameTime)
        {
            double dTS = gameTime.TotalGameTime.TotalSeconds;
            int blinkRate = 3;

            // Test for when we're showing this
            //
            if (Convert.ToInt16(dTS * blinkRate) % 2 != 0)
            {
                return;
            }

            // Blinks rate
            //
            Vector3 v1 = m_activeBufferView.getCursorCoordinates();
            v1.Y += m_activeBufferView.getLineHeight();

            Vector3 v2 = m_activeBufferView.getCursorCoordinates();
            v2.X += 1;

            renderQuad(v1, v2);
        }

        /// <summary>
        /// This is a list of directories and files based on the current position of the FileSystemView
        /// </summary>
        /// <param name="gameTime"></param>
        protected void drawDirectoryChooser(GameTime gameTime)
        {
            // We only draw this if we've finished moving
            //
            if (m_eye != m_newEyePosition)
                return;

            // Draw header
            //
            string line;
            Vector2 lineOrigin = new Vector2();
            float yPosition = 0.0f;

            Vector3 startPosition = m_activeBufferView.getPosition();

            if (m_state == FriendlierState.FileOpen)
            {
                line = "Open file...";
            }
            else if (m_state == FriendlierState.FileSaveAs)
            {
                line = "Save as...";
            }
            else
            {
                line = "Unknown FriendlierState...";
            }

            // Draw header line
            //
            m_spriteBatch.DrawString(m_spriteFont, line, new Vector2(startPosition.X, startPosition.Y - 100.0f), Color.White, 0, lineOrigin, m_textSize * 2.0f, 0, 0);

            Color dirColour = Color.White;
            Color fileColour = Color.DarkOrange;
            Color highlightColour = Color.LightGreen;
            
            startPosition.X += 50.0f;

            int lineNumber = 0;
            int dropStep = 6;

            // Page handling in the GUI
            //
            float showPage = 6.0f; // rows before stepping down
            int showOffset = (int)(((float)m_fileSystemView.getHighlightIndex()) / showPage);

            // This works out where the list that we're showing should end
            //
            int endShowing = (m_fileSystemView.getHighlightIndex() < dropStep ? dropStep : m_fileSystemView.getHighlightIndex()) + (int)showPage;


            // Draw the drives
            //
            if (m_fileSystemView.atDriveLevel())
            {
                DriveInfo[] driveInfo = m_fileSystemView.getDrives();
                //lineNumber = 0;

                foreach (DriveInfo d in driveInfo)
                {
                    if (!d.IsReady)
                    {
                        continue;
                    }

                    if (lineNumber > m_fileSystemView.getHighlightIndex() - dropStep
                        && lineNumber <= endShowing)
                    {
                        if (lineNumber < endShowing)
                        {
                            line = "[" + d.Name + "] " + d.VolumeLabel;
                        }
                        else
                        {
                            yPosition += m_lineHeight * 1.5f;
                            line = "...";
                        }

                        m_spriteBatch.DrawString(m_spriteFont,
                             line,
                             new Vector2(startPosition.X, startPosition.Y + yPosition),
                             (lineNumber == m_fileSystemView.getHighlightIndex() ? highlightColour : (lineNumber == endShowing ? Color.White : dirColour)),
                             0,
                             lineOrigin,
                             m_textSize * 1.5f,
                             0, 0);

                        yPosition += m_lineHeight * 1.5f;
                    }

                    lineNumber++;
                }
            }
            else
            {
                // For drives and directories we highlight item 1  - not zero
                //
                lineNumber = 1;
                FileInfo[] fileInfo = m_fileSystemView.getDirectoryInfo().GetFiles();
                DirectoryInfo[] dirInfo = m_fileSystemView.getDirectoryInfo().GetDirectories();

#if DIRECTORY_CHOOSER_DEBUG
                Logger.logMsg("showPage = " + showPage);
                Logger.logMsg("showOffset = " + showOffset);
                Logger.logMsg("m_directoryHighlight = " + m_directoryHighlight);
#endif

                line = m_fileSystemView.getPath() + m_saveFileName;
                m_spriteBatch.DrawString(m_spriteFont, line, new Vector2(startPosition.X, startPosition.Y), (m_fileSystemView.getHighlightIndex() == 0 ? highlightColour : dirColour), 0, lineOrigin, m_textSize * 2.0f, 0, 0);

                yPosition += m_lineHeight * 3.0f;

                foreach (DirectoryInfo d in dirInfo)
                {
                    if (lineNumber > m_fileSystemView.getHighlightIndex() - dropStep
                        && lineNumber <= endShowing)
                    {
                        if (lineNumber < endShowing)
                        {
                            line = "[" + d.Name + "]";
                        }
                        else
                        {
                            yPosition += m_lineHeight * 1.5f;
                            line = "...";
                        }

                        m_spriteBatch.DrawString(m_spriteFont,
                             line,
                             new Vector2(startPosition.X, startPosition.Y + yPosition),
                             (lineNumber == m_fileSystemView.getHighlightIndex() ? highlightColour : (lineNumber == endShowing ? Color.White : dirColour)),
                             0,
                             lineOrigin,
                             m_textSize * 1.5f,
                             0, 0);

                        yPosition += m_lineHeight * 1.5f;
                    }

                    lineNumber++;
                }

                foreach (FileInfo f in fileInfo)
                {
                    if (lineNumber > m_fileSystemView.getHighlightIndex() - dropStep
                        && lineNumber <= endShowing)
                    {
                        if (lineNumber < endShowing)
                        {
                            line = f.Name;
                        }
                        else
                        {
                            yPosition += m_lineHeight * 1.5f;
                            line = "...";
                        }

                        m_spriteBatch.DrawString(m_spriteFont,
                                                 line,
                                                 new Vector2(startPosition.X, startPosition.Y + yPosition),
                                                 (lineNumber == m_fileSystemView.getHighlightIndex() ? highlightColour : (lineNumber == endShowing ? Color.White : fileColour)),
                                                 0,
                                                 lineOrigin,
                                                 m_textSize * 1.5f,
                                                 0, 0);

                        yPosition += m_lineHeight * 1.5f;
                    }
                    lineNumber++;
                }
            }

            if (m_temporaryMessageEndTime > gameTime.TotalGameTime.TotalSeconds && m_temporaryMessage != "")
            {
                // Add any temporary message on to the end of the message
                //
                m_spriteBatch.DrawString(m_spriteFont,
                                         m_temporaryMessage,
                                         new Vector2(startPosition.X, startPosition.Y - 30.0f),
                                         Color.LightGoldenrodYellow,
                                         0,
                                         lineOrigin,
                                         m_textSize * 1.5f,
                                         0,
                                         0);
            }
        }

        /// <summary>
        /// Draw a BufferView
        /// </summary>
        /// <param name="view"></param>
        /// <param name="file"></param>
        protected void drawFileBuffer(BufferView view, GameTime gameTime)
        {
            Color bufferColour = view.m_textColour;

            if (m_state == FriendlierState.FileSaveAs || m_state == FriendlierState.FileOpen)
            {
                Color fadeColour = new Color(100, 100 ,100, 100);
                bufferColour = fadeColour;
            }
            else if (view == m_activeBufferView)
            {
                bufferColour = Color.White;
            }

            float yPosition = 0.0f;

            Vector2 lineOrigin = new Vector2();
            Vector3 viewSpaceTextPosition = view.getPosition();

            // Draw all the text lines to the height of the buffer
            //
            // This is default empty line character
            string line;
            int bufPos = view.getBufferShowStartY();

            // If we are tailing a file then let's look at the last X lines of it only
            //
            if (view.isTailing())
            {
                // We don't do this all the time so let the FileBuffer work out when we've updated
                // the file and need to change the viewing position to tail it.
                //
                view.getFileBuffer().refetchFile(gameTime);
                bufPos = view.getFileBuffer().getLineCount() - view.getBufferShowLength();


                // Ensure that we're always at least at zero
                //
                if (bufPos < 0)
                {
                    bufPos = 0;
                }
            }

            for (int i = 0; i < view.getBufferShowLength(); i++)
            {
                line = "~";

                if (i + bufPos < view.getFileBuffer().getLineCount() &&
                    view.getFileBuffer().getLineCount() != 0)
                {
                    line = view.getFileBuffer().getLine(i + bufPos);

                    if (line.Length > view.getBufferShowWidth())
                    {
                        line = line.Substring(0, view.getBufferShowWidth()) + "  [>]";
                    }
                }

                m_spriteBatch.DrawString(m_spriteFont, line, new Vector2(viewSpaceTextPosition.X, viewSpaceTextPosition.Y + yPosition), bufferColour, 0, lineOrigin, m_textSize, 0, 0);
                yPosition += m_lineHeight; // m_spriteFont.MeasureString(line).Y * m_textSize;
            }

            // Draw overlaid ID on this window if we're far enough away to use it
            //
            if (m_zoomLevel > 800.0f)
            {
                int viewId = m_project.getBufferViews().IndexOf(view);
                string bufferId = viewId.ToString();
                Color seeThroughColour = bufferColour;
                seeThroughColour.A = 70;
                m_spriteBatch.DrawString(m_spriteFont, bufferId, new Vector2(viewSpaceTextPosition.X, viewSpaceTextPosition.Y), seeThroughColour, 0, lineOrigin, m_textSize * 19.0f, 0, 0);
            }

            drawScrollbar(view);
        }

        /// <summary>
        /// Draw a scroll bar for a BufferView
        /// </summary>
        /// <param name="view"></param>
        /// <param name="file"></param>
        protected void drawScrollbar(BufferView view)
        {
            Vector3 sbPos = view.getPosition();
            float height = view.getBufferShowLength() * m_lineHeight;

            Rectangle sbBackGround = new Rectangle(Convert.ToInt16(sbPos.X - m_textSize * 30.0f),
                                                   Convert.ToInt16(sbPos.Y),
                                                   1,
                                                   Convert.ToInt16(height));

            // Draw scroll bar
            //
            m_spriteBatch.Draw(m_flatTexture, sbBackGround, Color.DarkCyan);

            // Draw viewing window
            float start = view.getBufferShowStartY();
            float length = view.getFileBuffer().getLineCount();

            // Check for length of FileBuffer in case it's empty
            //
            if (length > 0)
            {
                float scrollStart = start / length * height;
                float scrollLength = height; // full height unless we have anything to scroll

                if (length > view.getBufferShowLength())
                {
                    scrollLength = view.getBufferShowLength() / length * height;

                    // Ensure that scroll bar highlight is no longer than scroll bar
                    //
                    //if (scrollStart + scrollLength > height)
                    //{
                        //scrollLength = height - scrollStart;
                    //}
                }

                // Minimum scrollLength
                //
                if (scrollLength < 2)
                {
                    scrollLength = 2;
                }

                // Ensure that the highlight doens't jump over the end of the scrollbar
                //
                if (scrollStart + scrollLength > height)
                {
                    scrollStart = height - scrollLength;
                }

                Rectangle sb = new Rectangle(Convert.ToInt16(sbPos.X - m_textSize * 30.0f),
                                             Convert.ToInt16(sbPos.Y + scrollStart),
                                             1,
                                             Convert.ToInt16(scrollLength));

                // Draw scroll bar window position
                //
                m_spriteBatch.Draw(m_flatTexture, sb, Color.LightGoldenrodYellow);
            }
        }


        /// <summary>
        /// This draws a highlight area on the screen when we hold shift down
        /// </summary>
        void drawHighlight(GameTime gameTime)
        {
            List<BoundingBox> bb = m_activeBufferView.computeHighlight();

            // Draw the bounding boxes
            //
            foreach (BoundingBox highlight in bb)
            {
                renderQuad(highlight.Min, highlight.Max);
            }
        }

        /// <summary>
        /// Renders a quad at a given position
        /// </summary>
        /// <param name="topLeft"></param>
        /// <param name="bottomRight"></param>
        protected void renderQuad(Vector3 topLeft, Vector3 bottomRight)
        {
            Vector3 bottomLeft = new Vector3(topLeft.X, bottomRight.Y, topLeft.Z);
            Vector3 topRight = new Vector3(bottomRight.X, topLeft.Y, bottomRight.Z);

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
