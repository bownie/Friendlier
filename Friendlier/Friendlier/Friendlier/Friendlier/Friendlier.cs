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
using System.Diagnostics;
using System.Management;
using Microsoft.VisualBasic;



namespace Xyglo
{
    public enum FriendlierState
    {
        TextEditing,        // default mode
        FileOpen,           // opening a file
        FileSaveAs,         // saving a file as
        Information,        // show an information pane
        Help,               // show a help pane
        Configuration,      // configuration mode
        PositionScreenOpen, // where to position a screen when opening a file
        PositionScreenNew,  // where to position a new screen
        PositionScreenCopy, // where to position a copied FileBuffer/BufferView
        FindText,           // Enter some text to find
        ManageProject,      // View and edit the files in our project
        SplashScreen        // What we see when we're arriving in the application
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

        /// <summary>
        /// Are we spinning?
        /// </summary>
        bool spinning = false;

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
        /// Turn on and off file save confirmation
        /// </summary>
        bool m_confirmFileSave = false;

        /// <summary>
        /// Confirmation state - expecting Y/N
        /// </summary>
        ConfirmState m_confirmState = ConfirmState.None;

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
        /// Start time for the temporary message
        /// </summary>
        double m_temporaryMessageStartTime;

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
        /// Position in which we should open or create a new screen
        /// </summary>
        protected BufferView.BufferPosition m_newPosition;

        /// <summary>
        /// CPU performance meter
        /// </summary>
        //protected PerformanceCounter m_cpuCounter;

        /// <summary>
        /// RAM counter
        /// </summary>
        //protected PerformanceCounter m_memCounter;

        /// <summary>
        /// Store the last performance counter for CPU
        /// </summary>
        protected CounterSample m_lastCPUSample;

        /// <summary>
        /// Store the last performance counter for CPU
        /// </summary>
        protected CounterSample m_lastMemSample;

        /// <summary>
        /// Number of milliseconds between system status fetches
        /// </summary>
        protected TimeSpan m_systemFetchSpan = new TimeSpan(0, 0, 0, 1, 0);

        /// <summary>
        /// When we last fetched the system status
        /// </summary>
        protected TimeSpan m_lastSystemFetch = new TimeSpan(0, 0, 0, 0, 0);

        /// <summary>
        /// Percentage of system load
        /// </summary>
        protected float m_systemLoad = 0.0f;

        /// <summary>
        /// Percentage of system load
        /// </summary>
        protected float m_memoryAvailable = 0.0f;

        /// <summary>
        /// Physical Memory 
        /// </summary>
        protected float m_physicalMemory;

        /// <summary>
        /// List of files that need writing
        /// </summary>
        protected List<FileBuffer> m_filesToWrite;

        /// <summary>
        /// File selected in Open state - to be opened
        /// </summary>
        protected string m_selectedFile;

        /// <summary>
        /// Read only status of file to be opened (m_selectedFile)
        /// </summary>
        protected bool m_fileIsReadOnly = false;

        /// <summary>
        /// Tailing status of file to be opened (m_selectedFile)
        /// </summary>
        protected bool m_fileIsTailing = false;

        /// <summary>
        /// Current Z position - we call it m_zoomLevel
        /// </summary>
        protected float m_zoomLevel = 500.0f;

        /// <summary>
        /// The new destination for our Eye position
        /// </summary>
        protected Vector3 m_newEyePosition;

        /// <summary>
        /// Are we changing eye position?
        /// </summary>
        protected bool m_changingEyePosition = false;

        /// <summary>
        /// Used when changing the eye position - movement timer
        /// </summary>
        protected TimeSpan m_changingPositionLastGameTime;

        /// <summary>
        /// Frame rate of animation when moving between eye positions
        /// </summary>
        protected TimeSpan m_movementPause = new TimeSpan(0, 0, 0, 0, 10);

        /// <summary>
        /// This is the vector we're flying in - used to increment position each frame when
        /// moving between eye positions.
        /// </summary>
        protected Vector3 m_vFly;

        /// <summary>
        /// How many steps between eye start and eye end fly position
        /// </summary>
        protected int m_flySteps = 15;

        /// <summary>
        /// Initial path for FileSystemView
        /// </summary>
        protected string m_filePath = @"C:\";

        /// <summary>
        /// An object that wraps our view of the file system
        /// </summary>
        protected FileSystemView m_fileSystemView;

        /// <summary>
        /// A variable we use to store our save filename as we edit it (we have no forms)
        /// </summary>
        protected string m_saveFileName;

        /// <summary>
        /// Greyed out colour for background text
        /// </summary>
        protected Color m_greyedColour = new Color(50, 50, 50, 50);

        /// <summary>
        /// User help string
        /// </summary>
        protected string m_userHelp;

        /// <summary>
        /// We have a local instance of our FontManager
        /// </summary>
        protected FontManager m_fontManager = new FontManager();

        /// <summary>
        /// Position in configuration list when selecting something
        /// </summary>
        protected int m_configPosition;

        /// <summary>
        /// Item colour for a list of things
        /// </summary>
        protected Color m_itemColour = Color.DarkOrange;

        /// <summary>
        /// Highlight color for an element in a list
        /// </summary>
        protected Color m_highlightColour = Color.LightGreen;

        /// <summary>
        /// If we're in the Configuration state then look at this variable
        /// </summary>
        protected bool m_editConfigurationItem = false;

        /// <summary>
        /// The new value of the configuration item
        /// </summary>
        protected string m_editConfigurationItemValue;


        /// <summary>
        /// Worker thread for the PerformanceCounters
        /// </summary>
        protected PerformanceWorker m_counterWorker;

        /// <summary>
        /// The thread that is used for the counter
        /// </summary>
        protected Thread m_counterWorkerThread;

        /// <summary>
        /// What we're searching for
        /// </summary>
        protected string m_searchText = "";

        /// <summary>
        /// When this is instantiated it makes a language specific syntax handler to
        /// provide syntax highlighting, suggestions for autocompletes and also indent
        /// levels.
        /// </summary>
        protected SyntaxManager m_syntaxManager;

        /////////////////////////////// CONSTRUCTORS ////////////////////////////

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

            // Set physical memory
            //
            Microsoft.VisualBasic.Devices.ComputerInfo ci = new Microsoft.VisualBasic.Devices.ComputerInfo();
            m_physicalMemory = (float)(ci.TotalPhysicalMemory / (1024 * 1024));

            // Populate the user help
            //
            populateUserHelp();
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

            // Set physical memory
            //
            Microsoft.VisualBasic.Devices.ComputerInfo ci = new Microsoft.VisualBasic.Devices.ComputerInfo();
            m_physicalMemory = (float)(ci.TotalPhysicalMemory / (1024 * 1024));

            // Set windowed mode as default
            //
            windowedMode();

            // Populate the user help
            //
            populateUserHelp();

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
            return m_project.getFileBuffers().IndexOf(m_project.getSelectedBufferView().getFileBuffer());
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
            // Build the syntax manager - for the moment we force it to Cpp
            //
            m_syntaxManager = new CppSyntaxManager(m_project);

            // Initialise the configuration item if it's null - this is in case we've persisted
            // a version of the project without a configuration item it will create it here.
            //
            m_project.buildInitialConfiguration();

            // Load all the files - if we have nothing in this project then create a BufferView
            // and a FileBuffer.
            //
            if (m_project.getFileBuffers().Count == 0)
            {
                addNewFileBuffer();
            }
            else
            {
                m_project.loadFiles();
            }

            // Ensure that all the BufferViews are populated with the charwidth and lineheights and
            // also that the relative positioning is correct - we have to do this in two passes.
            foreach (BufferView bv in m_project.getBufferViews())
            {
                bv.setCharWidth(m_fontManager.getCharWidth());
                bv.setLineHeight(m_fontManager.getLineHeight());
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
            //m_activeBufferView = m_project.getSelectedBufferView();

            // Ensure that we are in the correct position to view this buffer so there's no initial movement
            //
            
            m_eye = m_project.getSelectedBufferView().getEyePosition();
            m_target = m_project.getSelectedBufferView().getLookPosition();
            m_eye.Z = m_zoomLevel;

            // Set the active buffer view
            //
            setActiveBuffer();

            // Set-up the single FileSystemView we have
            //
            m_fileSystemView = new FileSystemView(m_filePath, new Vector3(-800.0f, 0f, 0f), m_fontManager.getLineHeight(), m_fontManager.getCharWidth());
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
                m_fontManager.setFontState(FontManager.FontType.Small);
                Logger.logMsg("Friendlier:setSpriteFont() - using Small Window font");
            }
            else if (m_graphics.GraphicsDevice.Viewport.Width < 1024)
            {
                m_fontManager.setFontState(FontManager.FontType.Window);
                Logger.logMsg("Friendlier:setSpriteFont() - using Window font");
            }
            else
            {
                Logger.logMsg("Friendlier:setSpriteFont() - using Full font");
                m_fontManager.setFontState(FontManager.FontType.Full);
            }

            // to handle tabs for the moment convert them to single spaces
            //
            Logger.logMsg("Friendlier:setSpriteFont() - you must get these three variables correct for each position to avoid nasty looking fonts:");
            Logger.logMsg("Friendlier:setSpriteFont() - zoom level = " + m_zoomLevel);

            // Log these sizes 
            //
            Logger.logMsg("Friendlier:setSpriteFont() - Font getCharWidth = " + m_fontManager.getCharWidth());
            Logger.logMsg("Friendlier:setSpriteFont() - Font getLineHeight = " + m_fontManager.getLineHeight());
            Logger.logMsg("Friendlier:setSpriteFont() - Font getLineSpacing = " + m_fontManager.getLineSpacing());
            Logger.logMsg("Friendlier:setSpriteFont() - Font getTextScale = " + m_fontManager.getTextScale());

            // Now we need to make all of our BufferViews have this setting too
            //
            foreach (BufferView bv in m_project.getBufferViews())
            {
                bv.setCharWidth(m_fontManager.getCharWidth());
                bv.setLineHeight(m_fontManager.getLineHeight());
            }

            // Now recalculate positions
            //
            foreach (BufferView bv in m_project.getBufferViews())
            {
                bv.calculateMyRelativePosition();
            }

            // Reset the active BufferView
            //
            //setActiveBuffer();
        }


        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Start up the worker thread
            //
            m_counterWorker = new PerformanceWorker();
            m_counterWorkerThread = new Thread(m_counterWorker.startWorking);
            m_counterWorkerThread.Start();

            // Loop until worker thread activates.
            //
            while (!m_counterWorkerThread.IsAlive) ;
            Thread.Sleep(1);

            // Initialise and load fonts into our Content context by family.
            //
            //FontManager.initialise(Content, "Lucida Sans Typewriter");
            //FontManager.initialise(Content, "Sax Mono");
            m_fontManager.initialise(Content, "Bitstream Vera Sans Mono", GraphicsDevice.Viewport.AspectRatio);

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
        /// At a zoom level where we want to rotate and reset the active buffer
        /// </summary>
        /// <param name="direction"></param>
        protected void setActiveBuffer(BufferView.ViewCycleDirection direction)
        {
            m_project.getSelectedBufferView().rotateQuadrant(direction);
            setActiveBuffer();
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
                // Either set the BufferView 
                if (item != null)
                {
                    m_project.setSelectedBufferView(item);
                }
                    /*
                else if (m_project.getBufferViews().Count == 0) // Or if we have none then create one
                {
                    m_project.addBufferView(new BufferView());
                }
                     */
            }
            catch (Exception e)
            {
                Logger.logMsg("Cannot locate BufferView item in list " + e.ToString());
                return;
            }

            Logger.logMsg("Friendlier:setActiveBuffer() - active buffer view is " + m_project.getSelectedBufferViewId());

            // All the maths is done in the Buffer View
            //
            Vector3 eyePos = m_project.getSelectedBufferView().getEyePosition(m_zoomLevel);

            flyToPosition(eyePos);

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

        /// <summary>
        /// Got to the FileOpen mode in the overall application state.  This will zoom out from the
        /// bufferview and grey it out and allow us to select a file
        /// </summary>
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
        /// Switch to the Configuration mode
        /// </summary>
        protected void showConfigurationScreen()
        {
            m_state = FriendlierState.Configuration;
            m_temporaryMessage = "";
        }

        /// <summary>
        /// Close the active buffer view
        /// </summary>
        protected void closeActiveBuffer(GameTime gameTime)
        {
            if (m_project.getBufferViews().Count > 1)
            {
                int index = m_project.getBufferViews().IndexOf(m_project.getSelectedBufferView());
                m_project.removeBufferView(m_project.getSelectedBufferView());

                // Ensure that the index is not greater than number of bufferviews
                //
                if (index > m_project.getBufferViews().Count - 1)
                {
                    index = m_project.getBufferViews().Count - 1;
                }

                //m_project.setSelectedBufferViewId(index);

                setActiveBuffer(index);
            }
            else
            {
                setTemporaryMessage("[LAST BUFFER]", gameTime, 2);
            }
        }

        /// <summary>
        /// Traverase a directory and allow opening/saving at that point according to state
        /// </summary>
        protected void traverseDirectory(GameTime gameTime, bool readOnly = false, bool tailFile = false)
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

                        Logger.logMsg("Friendler::traverseDirectory() - selected a file " + fileInfo.Name);

                        // Set these values and the status
                        //
                        m_fileIsReadOnly = readOnly;
                        m_fileIsTailing = tailFile;
                        m_selectedFile = fileInfo.FullName;

                        if (m_state == FriendlierState.FileOpen)
                        {
                            // Now we need to choose a position for the new file we're opening
                            //
                            m_state = FriendlierState.PositionScreenOpen;
                        }
                        else if (m_state == FriendlierState.FileSaveAs)
                        {
                            // Set the FileBuffer path
                            //
                            m_project.getSelectedBufferView().getFileBuffer().setFilepath(m_selectedFile);
                            m_project.getSelectedBufferView().getFileBuffer().save();

                            // Check if we need to remove this FileBuffer from the todo list - it's not important if we can't
                            // remove it here but we should try to anyway.
                            //
                            if (m_filesToWrite.Remove(m_project.getSelectedBufferView().getFileBuffer()))
                            {
                                Logger.logMsg("Friendlier::Update() - could not find FileBuffer to remove from m_filesToWrite");
                            }
                            else
                            {
                                Logger.logMsg("Friendlier::Update() - total files left to write is now " + m_filesToWrite.Count);
                            }

                            // If we have finished saving all of our files then we can exit (although we check once again)
                            //
                            if (m_filesToWrite.Count == 0)
                            {
                                checkExit(gameTime);
                            }
                        }
                    }
                }
                catch (Exception /* e */)
                {
                    setTemporaryMessage("Cannot access \"" + subDirectory + "\"", gameTime, 2);
                }
            }
        }

        /// <summary>
        /// Completing a File->Save operation
        /// </summary>
        /// <param name="gameTime"></param>
        protected void completeSaveFile(GameTime gameTime)
        {
            try
            {
                m_project.getSelectedBufferView().getFileBuffer().save();

                if (m_filesToWrite != null && m_filesToWrite.Count > 0)
                {
                    if (m_filesToWrite.Remove(m_project.getSelectedBufferView().getFileBuffer()))
                    {
                        Logger.logMsg("Friendlier::completeSaveFile() - files remaining to be written " + m_filesToWrite.Count);
                    }
                }

                Vector3 newPosition = m_eye;
                newPosition.Z = 500.0f;

                flyToPosition(newPosition);
                m_state = FriendlierState.TextEditing;

                setTemporaryMessage("[Saved]", gameTime, 2);
            }
            catch (Exception)
            {
                setTemporaryMessage("Failed to save to " + m_project.getSelectedBufferView().getFileBuffer().getFilepath(), gameTime, 2);
            }
        }


        /// <summary>
        /// Exit but ensuring that buffers are saved
        /// </summary>
        protected void checkExit(GameTime gameTime, bool force = false)
        {
            // Save our project
            //
            m_project.dataContractSerialise();

            // Firstly check for any unsaved buffers and warn
            //
            bool unsaved = false;

            // Only check BufferViews status if we're not forcing an exit
            //
            if (!force)
            {
                foreach (FileBuffer fb in m_project.getFileBuffers())
                {
                    if (fb.isModified())
                    {
                        unsaved = true;
                        break;
                    }
                }
            }

            // Likewise only save if we want to
            //
            if (unsaved && !force)
            {
                if (m_confirmState == ConfirmState.FileSaveCancel)
                {
                    setTemporaryMessage("", gameTime, 1);
                    m_confirmState = ConfirmState.None;
                    return;
                }
                else
                {
                    setTemporaryMessage("[Unsaved Buffers.  Save?  Y/N/C]", gameTime, 0);
                    m_confirmState = ConfirmState.FileSaveCancel;
                    //m_state = FriendlierState.FileSaveAs;
                }
            }
            else
            {
                // Clear the worker thread and exit
                //
                m_counterWorker.requestStop();
                m_counterWorkerThread.Join();
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

                    
                    case FriendlierState.FileSaveAs:
                        setTemporaryMessage("[Cancelled Quit]", gameTime, 0.5);
                        m_confirmState = ConfirmState.None;
                        m_state = FriendlierState.TextEditing;
                        m_filesToWrite = null;
                        break;

                    case FriendlierState.FileOpen:
                    case FriendlierState.Information:
                    case FriendlierState.Configuration:
                    case FriendlierState.PositionScreenOpen:
                    case FriendlierState.PositionScreenNew:
                    case FriendlierState.PositionScreenCopy:
                    case FriendlierState.ManageProject:
                    case FriendlierState.SplashScreen:
                    default:
                        m_state = FriendlierState.TextEditing;
                                break;
                }

                // Fly back to correct position
                //
                flyToPosition(newPosition);
            }

            // If we're viewing some information then only escape can get us out
            // of this mode.  Note that we also have to mind any animations so we
            // also want to ensure that m_changingEyePosition is not true.
            //
            if ((m_state == FriendlierState.Information || m_state == FriendlierState.Help) && m_changingEyePosition == false)
            {
                return;
            }

            // This helps us count through our lists of file to save if we're trying to exit
            //
            if (m_filesToWrite != null && m_filesToWrite.Count > 0)
            {
                m_project.setSelectedBufferView(m_filesToWrite[0]);
                m_eye = m_project.getSelectedBufferView().getEyePosition();
                selectSaveFile();
            }

            // For PositionScreen state we want not handle events here other than direction keys - this section
            // decides where to place a new, opened or copied BufferView.
            //
            if (m_state == FriendlierState.PositionScreenOpen || m_state == FriendlierState.PositionScreenNew || m_state == FriendlierState.PositionScreenCopy)
            {
                bool gotSelection = false;

                if (checkKeyState(Keys.Left, gameTime))
                {
                    Logger.logMsg("Friendler::Update() - position screen left");
                    m_newPosition = BufferView.BufferPosition.Left;
                    gotSelection = true;
                }
                else if (checkKeyState(Keys.Right, gameTime))
                {
                    m_newPosition = BufferView.BufferPosition.Right;
                    gotSelection = true;
                    Logger.logMsg("Friendler::Update() - position screen right");
                }
                else if (checkKeyState(Keys.Up, gameTime))
                {
                    m_newPosition = BufferView.BufferPosition.Above;
                    gotSelection = true;
                    Logger.logMsg("Friendler::Update() - position screen up");
                }
                else if (checkKeyState(Keys.Down, gameTime))
                {
                    m_newPosition = BufferView.BufferPosition.Below;
                    gotSelection = true;
                    Logger.logMsg("Friendler::Update() - position screen down");
                }

                // If we have discovered a position for our pending new window
                //
                if (gotSelection)
                {
                    if (m_state == FriendlierState.PositionScreenOpen)
                    {
                        // Open the file 
                        //
                        BufferView newBV = addNewFileBuffer(m_selectedFile, m_fileIsReadOnly, m_fileIsTailing);
                        setActiveBuffer(newBV);
                        m_state = FriendlierState.TextEditing;
                    }
                    else if (m_state == FriendlierState.PositionScreenNew)
                    {
                        // Use the convenience function
                        //
                        BufferView newBV = addNewFileBuffer();
                        setActiveBuffer(newBV);
                        m_state = FriendlierState.TextEditing;
                    }
                    else if (m_state == FriendlierState.PositionScreenCopy)
                    {
                        // Use the copy constructor
                        //
                        BufferView newBV = new BufferView(m_project.getSelectedBufferView(), m_newPosition);
                        m_project.addBufferView(newBV);
                        setActiveBuffer(newBV);
                        m_state = FriendlierState.TextEditing;
                    }
                }

                
                return;
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
                    if (!m_project.getSelectedBufferView().gotHighlight())
                    {
                        m_project.getSelectedBufferView().startHighlight();
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
                    Logger.logMsg("Friendlier - got a number " + m_gotoBufferView);

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
                else if (m_state == FriendlierState.Configuration && m_editConfigurationItem == false) // Configuration changes
                {
                    if (m_configPosition > 0)
                    {
                        m_configPosition--;
                    }
                }
                else
                {
                    if (m_altDown)
                    {
                        // Attempt to move right if there's a BufferView there
                        //
                        detectMove(BufferView.BufferPosition.Above, gameTime);
                    }
                    else
                    {
                        m_project.getSelectedBufferView().moveCursorUp(false);

                        if (m_shiftDown)
                        {
                            m_project.getSelectedBufferView().extendHighlight();  // Extend 
                        }
                        else
                        {
                            m_project.getSelectedBufferView().noHighlight(); // Disable
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
                else if (m_state == FriendlierState.Configuration && m_editConfigurationItem == false) // Configuration changes
                {
                    if (m_configPosition < m_project.getConfigurationListLength() - 1)
                    {
                        m_configPosition++;
                    }
                }
                else
                {
                    if (m_altDown)
                    {
                        // Attempt to move right if there's a BufferView there
                        //
                        detectMove(BufferView.BufferPosition.Below, gameTime);
                    }
                    else
                    {
                        m_project.getSelectedBufferView().moveCursorDown(false);

                        if (m_shiftDown)
                        {
                            m_project.getSelectedBufferView().extendHighlight();
                        }
                        else
                        {
                            m_project.getSelectedBufferView().noHighlight(); // Disable
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
                    if (m_ctrlDown)
                    {
                        m_project.getSelectedBufferView().wordJumpCursorLeft();
                    }
                    else if (m_altDown)
                    {
                        // Attempt to move right if there's a BufferView there
                        //
                        detectMove(BufferView.BufferPosition.Left, gameTime);
                    }
                    else
                    {
                        m_project.getSelectedBufferView().moveCursorLeft();
                        /*
                        if (m_project.getSelectedBufferView().getCursorPosition().X > 0)
                        {
                            FilePosition fp = m_project.getSelectedBufferView().getCursorPosition();
                            fp.X--;
                            m_project.getSelectedBufferView().setCursorPosition(fp);
                        }
                        else
                        {
                            m_project.getSelectedBufferView().moveCursorUp(true);
                        }
                        */
                    }

                    if (m_shiftDown)
                    {
                        m_project.getSelectedBufferView().extendHighlight();  // Extend
                    }
                    else
                    {
                        m_project.getSelectedBufferView().noHighlight(); // Disable
                    }
                }
            }
            else if (checkKeyState(Keys.Right, gameTime))
            {
                if (m_state == FriendlierState.FileSaveAs || m_state == FriendlierState.FileOpen)
                {
                    traverseDirectory(gameTime);
                }
                else
                {
                    if (m_ctrlDown)
                    {
                        m_project.getSelectedBufferView().wordJumpCursorRight();
                    }
                    else if (m_altDown)
                    {
                        // Attempt to move right if there's a BufferView there
                        //
                        detectMove(BufferView.BufferPosition.Right, gameTime);
                    }
                    else
                    {
                        m_project.getSelectedBufferView().moveCursorRight();
                        /*
                        if (m_project.getSelectedBufferView().getCursorPosition().X < m_project.getSelectedBufferView().getBufferShowWidth())
                        {
                            FilePosition fp = m_project.getSelectedBufferView().getCursorPosition();

                            if (m_project.getSelectedBufferView().getFileBuffer().getLineCount() > 0)
                            {
                                if (fp.X < m_project.getSelectedBufferView().getFileBuffer().getLine(fp.Y).Length)
                                {
                                    fp.X++;
                                    m_project.getSelectedBufferView().setCursorPosition(fp);
                                }
                            }
                        }
                        else
                        {
                            m_project.getSelectedBufferView().moveCursorDown(true);
                        }*/
                    }

                    if (m_shiftDown)
                    {
                        m_project.getSelectedBufferView().extendHighlight(); // Extend
                    }
                    else
                    {
                        m_project.getSelectedBufferView().noHighlight(); // Disable
                    }
                }
            }
            else if (checkKeyState(Keys.End, gameTime))
            {
                FilePosition fp = m_project.getSelectedBufferView().getCursorPosition();
                fp.X = m_project.getSelectedBufferView().getFileBuffer().getLine(fp.Y).Length;
                m_project.getSelectedBufferView().setCursorPosition(fp);

                // Change the X offset if the row is longer than the visible width
                //
                if (fp.X > m_project.getSelectedBufferView().getBufferShowWidth())
                {
                    int bufferX = fp.X - m_project.getSelectedBufferView().getBufferShowWidth();
                    m_project.getSelectedBufferView().setBufferShowStartX(bufferX);
                }

                if (m_shiftDown)
                {
                    m_project.getSelectedBufferView().extendHighlight(); // Extend
                }
                else
                {
                    m_project.getSelectedBufferView().noHighlight(); // Disable
                }

            }
            else if (checkKeyState(Keys.Home, gameTime))
            {
                // Reset the cursor to zero
                //
                FilePosition fp = m_project.getSelectedBufferView().getCursorPosition();
                fp.X = 0;
                m_project.getSelectedBufferView().setCursorPosition(fp);

                // Reset any X offset to zero
                //
                m_project.getSelectedBufferView().setBufferShowStartX(0);

                if (m_shiftDown)
                {
                    m_project.getSelectedBufferView().extendHighlight(); // Extend
                }
                else
                {
                    m_project.getSelectedBufferView().noHighlight(); // Disable
                }
            }
            else if (checkKeyState(Keys.F9, gameTime)) // Spin anticlockwise though BVs
            {
                m_zoomLevel = 1000.0f;
                setActiveBuffer(BufferView.ViewCycleDirection.Anticlockwise);
            }
            else if (checkKeyState(Keys.F10, gameTime)) // Spin clockwise through BVs
            {
                m_zoomLevel = 1000.0f;
                setActiveBuffer(BufferView.ViewCycleDirection.Clockwise);
            }
            else if (checkKeyState(Keys.F3, gameTime))
            {
                doSearch(gameTime);
            }
            else if (checkKeyState(Keys.F7, gameTime))
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
            else if (checkKeyState(Keys.F8, gameTime))
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
            else if (checkKeyState(Keys.F6, gameTime))
            {
                doBuildCommand(gameTime);
            }
            else if (checkKeyState(Keys.F11, gameTime))
            {
                windowedMode();
                setSpriteFont();
            }
            else if (checkKeyState(Keys.F12, gameTime))
            {
                fullScreenMode();
                setSpriteFont();
            }
            else if (checkKeyState(Keys.F1, gameTime))  // Cycle down through BufferViews
            {
                int newValue = m_project.getSelectedBufferViewId() - 1;
                if (newValue < 0)
                {
                    newValue += m_project.getBufferViews().Count;
                }

                m_project.setSelectedBufferViewId(newValue);
                setActiveBuffer();
            }
            else if (checkKeyState(Keys.F2, gameTime)) // Cycle up through BufferViews
            {
                int newValue = (m_project.getSelectedBufferViewId() + 1) % m_project.getBufferViews().Count;
                m_project.setSelectedBufferViewId(newValue);
                setActiveBuffer();
            }
            /* else if (checkKeyState(Keys.F7, gameTime))
            {
                setFileView();
            } */
            else if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.Insert)) // Reset
            {
                m_eye.X = 12f;
                m_eye.Y = 5f;
                m_eye.Z = 0f;
            }
            else if (checkKeyState(Keys.PageDown, gameTime))
            {
                m_project.getSelectedBufferView().pageDown();

                if (m_shiftDown)
                {
                    m_project.getSelectedBufferView().extendHighlight(); // Extend
                }
                else
                {
                    m_project.getSelectedBufferView().noHighlight(); // Disable
                }
            }
            else if (checkKeyState(Keys.PageUp, gameTime))
            {
                m_project.getSelectedBufferView().pageUp();

                if (m_shiftDown)
                {
                    m_project.getSelectedBufferView().extendHighlight(); // Extend
                }
                else
                {
                    m_project.getSelectedBufferView().noHighlight(); // Disable
                }
            }
            else if (checkKeyState(Keys.Scroll, gameTime))
            {
                if (m_project.getSelectedBufferView().isLocked())
                {
                    m_project.getSelectedBufferView().setLock(false, 0);
                }
                else
                {
                    m_project.getSelectedBufferView().setLock(true, m_project.getSelectedBufferView().getCursorPosition().Y);
                }
            }
            else if (checkKeyState(Keys.Tab, gameTime)) // Insert a tab space
            {
                m_project.getSelectedBufferView().insertText("  ");
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
                else if (m_state == FriendlierState.FindText && checkKeyState(Keys.Back, gameTime))
                {
                    // Delete charcters from the file name if we have one
                    //
                    if (m_searchText.Length > 0)
                    {
                        m_searchText = m_searchText.Substring(0, m_searchText.Length - 1);
                    }
                }
                else if (m_state == FriendlierState.Configuration && m_editConfigurationItem && checkKeyState(Keys.Back, gameTime))
                {
                    if (m_editConfigurationItemValue.Length > 0)
                    {
                        m_editConfigurationItemValue = m_editConfigurationItemValue.Substring(0, m_editConfigurationItemValue.Length - 1);
                    }
                }
                else if (m_project.getSelectedBufferView().gotHighlight()) // If we have a valid highlighted selection then delete it (normal editing)
                {
                    // All the clever stuff with the cursor is done at the BufferView level and it also
                    // calls the command in the FileBuffer.
                    //
                    m_project.getSelectedBufferView().deleteCurrentSelection();
                }
                else // delete at cursor
                {
                    if (checkKeyState(Keys.Delete, gameTime))
                    {
                        m_project.getSelectedBufferView().deleteSingle();
                    }
                    else if (checkKeyState(Keys.Back, gameTime))
                    {
                        FilePosition fp = m_project.getSelectedBufferView().getCursorPosition();

                        if (fp.X > 0)
                        {
                            // Decrement and set X
                            //
                            fp.X--;
                            m_project.getSelectedBufferView().setCursorPosition(fp);
                            m_project.getSelectedBufferView().deleteSingle();
                        }
                        else if (fp.Y > 0)
                        {
                            fp.Y -= 1;
                            fp.X = m_project.getSelectedBufferView().getFileBuffer().getLine(Convert.ToInt16(fp.Y)).Length;
                            m_project.getSelectedBufferView().setCursorPosition(fp);

                            m_project.getSelectedBufferView().deleteSingle();
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
                                if (m_project.getSelectedBufferView().getFileBuffer().getFilepath() == "")
                                {
                                    selectSaveFile();
                                }
                                else
                                {
                                    // Attempt save
                                    //
                                    m_project.getSelectedBufferView().getFileBuffer().save();

                                    // Save has completed without error
                                    //
                                    setTemporaryMessage("[Saved]", gameTime, 2);
                                    m_state = FriendlierState.TextEditing;
                                }
                            }
                            else if (m_confirmState == ConfirmState.FileSaveCancel)
                            {
                                // First of all save all open buffers we can write and save
                                // a list of all those we can't
                                //
                                m_filesToWrite = new List<FileBuffer>();

                                foreach (FileBuffer fb in m_project.getFileBuffers())
                                {
                                    if (fb.isWriteable())
                                    {
                                        fb.save();
                                    }
                                    else
                                    {
                                        m_filesToWrite.Add(fb);
                                    }
                                }

                                // All files saved then exit
                                //
                                if (m_filesToWrite.Count == 0)
                                {
                                    checkExit(gameTime);
                                }
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
                            // Exit nicely
                            //
                            checkExit(gameTime, true);
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
                        string text = m_project.getSelectedBufferView().getSelection().getClipboardString();

                        // We can only set this is the text is not empty
                        if (text != "")
                        {
                            System.Windows.Forms.Clipboard.SetText(text);
                        }
                    }
                    else if (checkKeyState(Keys.X, gameTime)) // Cut
                    {
                        Logger.logMsg("Friendler::update() - cut");

                        System.Windows.Forms.Clipboard.SetText(m_project.getSelectedBufferView().getSelection().getClipboardString());
                        m_project.getSelectedBufferView().deleteCurrentSelection();
                    }
                    else if (checkKeyState(Keys.V, gameTime)) // Paste
                    {
                        if (System.Windows.Forms.Clipboard.ContainsText())
                        {
                            Logger.logMsg("Friendler::update() - pasting text");
                            // If we have a selection then replace it - else insert
                            //
                            if (m_project.getSelectedBufferView().gotHighlight())
                            {
                                m_project.getSelectedBufferView().replaceCurrentSelection(System.Windows.Forms.Clipboard.GetText());
                            }
                            else
                            {
                                m_project.getSelectedBufferView().insertText(System.Windows.Forms.Clipboard.GetText());
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
                            if (m_project.getSelectedBufferView().getFileBuffer().getUndoPosition() > 0)
                            {
                                //m_project.getSelectedBufferView().setCursorPosition(m_project.getSelectedBufferView().getFileBuffer().undo(1));
                                m_project.getSelectedBufferView().undo(1);
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
                            if (m_project.getSelectedBufferView().getFileBuffer().getUndoPosition() <
                                m_project.getSelectedBufferView().getFileBuffer().getCommandStackLength())
                            {
                                m_project.getSelectedBufferView().setCursorPosition(m_project.getSelectedBufferView().getFileBuffer().redo(1));
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
                    else if (checkKeyState(Keys.A, gameTime))  // Select all
                    {
                        m_project.getSelectedBufferView().selectAll();
                    }
                }
                else if (m_altDown) // ALT down action
                {
                    if (checkKeyState(Keys.S, gameTime) && m_project.getSelectedBufferView().getFileBuffer().isModified())
                    {
                        // If we want to confirm save then ask
                        //
                        if (m_confirmFileSave)
                        {
                            setTemporaryMessage("[Confirm Save? Y/N]", gameTime, 0);
                            m_confirmState = ConfirmState.FileSave;
                        }
                        else  // just save
                        {
                            // Select a file path if we need one
                            //
                            if (m_project.getSelectedBufferView().getFileBuffer().getFilepath() == "")
                            {
                                selectSaveFile();
                            }
                            else
                            {
                                // Attempt save
                                //
                                m_project.getSelectedBufferView().getFileBuffer().save();

                                // Save has completed without error
                                //
                                setTemporaryMessage("[Saved]", gameTime, 2);
                                m_state = FriendlierState.TextEditing;
                            }
                        }
                    }
                    else if (checkKeyState(Keys.N, gameTime)) // New BufferView on new FileBuffer
                    {
                        m_state = FriendlierState.PositionScreenNew;
                    }
                    else if (checkKeyState(Keys.B, gameTime)) // New BufferView on same FileBuffer (copy the existing BufferView)
                    {
                        m_state = FriendlierState.PositionScreenCopy;
                    }
                    else if (checkKeyState(Keys.O, gameTime)) // Open a file
                    {
                        selectOpenFile();
                    }
                    else if (checkKeyState(Keys.H, gameTime)) // Show the help screen
                    {
                        m_state = FriendlierState.Help;
                    }
                    else if (checkKeyState(Keys.I, gameTime)) // Show the information screen
                    {
                        m_state = FriendlierState.Information;
                    }
                    else if (checkKeyState(Keys.G, gameTime)) // Show the config screen
                    {
                        showConfigurationScreen();
                    }
                    else if (checkKeyState(Keys.C, gameTime)) // Close current BufferView
                    {
                        closeActiveBuffer(gameTime);
                    }
                    else if (checkKeyState(Keys.M, gameTime))
                    {
                        m_state = FriendlierState.ManageProject; // Manage the files in the project
                        Vector3 newPos = m_eye;
                        newPos.Z = 800.0f;
                        flyToPosition(newPos);
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
                    else if (checkKeyState(Keys.F, gameTime)) // Find text
                    {
                        Logger.logMsg("Friendler::update() - find");
                        m_state = FriendlierState.FindText;
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
                                FilePosition fp = m_project.getSelectedBufferView().getCursorPosition();

                                if (keyDown == Keys.Enter)
                                {
                                    if (m_state == FriendlierState.FileSaveAs)
                                    {
                                        // Check that the filename is valid
                                        //
                                        if (m_saveFileName != "" && m_saveFileName != null)
                                        {
                                            m_project.getSelectedBufferView().getFileBuffer().setFilepath(m_fileSystemView.getPath() + m_saveFileName);

                                            Logger.logMsg("Friendlier::Update() - file name = " + m_project.getSelectedBufferView().getFileBuffer().getFilepath());

                                            completeSaveFile(gameTime);

                                            // Now if we have remaining files to write then we need to carry on saving files
                                            //
                                            if (m_filesToWrite != null)
                                            {
                                                //m_filesToWrite.Remove(m_project.getSelectedBufferView().getFileBuffer());

                                                // If we have remaining files to edit then set the active BufferView to one that
                                                // looks over this file - then fly to it and choose and file location.
                                                //
                                                if (m_filesToWrite.Count > 0)
                                                {
                                                    m_project.setSelectedBufferView(m_filesToWrite[0]);
                                                    m_eye = m_project.getSelectedBufferView().getEyePosition();
                                                    selectSaveFile();
                                                }
                                                else // We're done 
                                                {
                                                    m_filesToWrite = null;
                                                    Logger.logMsg("Friendlier::Update() - saved some files.  Quitting.");

                                                    // Exit nicely and ensure we serialise
                                                    //
                                                    checkExit(gameTime);
                                                }
                                            }
                                            else
                                            {
                                                // Exit nicely and ensure we serialise
                                                //
                                                checkExit(gameTime);
                                            }
                                        }
                                    }
                                    else if (m_state == FriendlierState.FileOpen)
                                    {
                                        traverseDirectory(gameTime);
                                    }
                                    else if (m_state == FriendlierState.Configuration)
                                    {
                                        // Set this status so that we edit the item
                                        //
                                        if (m_editConfigurationItem == false)
                                        {
                                            // Go into item edit mode and copy across the current value
                                            m_editConfigurationItem = true;
                                            m_editConfigurationItemValue = m_project.getConfigurationItem(m_configPosition).Value;
                                        }
                                        else
                                        {
                                            // Completed editing the item - now set it
                                            //
                                            m_editConfigurationItem = false;
                                            m_project.updateConfigurationItem(m_project.getConfigurationItem(m_configPosition).Name, m_editConfigurationItemValue);
                                        }
                                    }
                                    else if (m_state == FriendlierState.FindText)
                                    {
                                        doSearch(gameTime);
                                    }
                                    else
                                    {
                                        // Insert a line into the editor
                                        //
                                        string indent = "";

                                        try
                                        {
                                            indent = m_project.getConfigurationValue("AUTOINDENT");
                                        }
                                        catch (Exception e)
                                        {
                                            Logger.logMsg("Friendlier::Update() - couldn't get AUTOINDENT from config - " + e.Message);
                                        }

                                        m_project.getSelectedBufferView().insertNewLine(indent);

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
                                                traverseDirectory(gameTime, true, true);
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
                                            Logger.logMsg("Friendlier::update() - got key = " + keyDown.ToString());
                                            break;
                                    }

                                    if (key != "")
                                    {
                                        if (m_state == FriendlierState.FileSaveAs) // File name
                                        {
                                            //Logger.logMsg("Writing letter " + key);
                                            m_saveFileName += key;
                                        }
                                        else if (m_state == FriendlierState.Configuration && m_editConfigurationItem) // Configuration item
                                        {
                                            m_editConfigurationItemValue += key;
                                        }
                                        else if (m_state == FriendlierState.FindText)
                                        {
                                            m_searchText += key;
                                        }
                                        else if (m_state == FriendlierState.TextEditing)
                                        {
                                            // Do we need to do some deletion or replacing?
                                            //
                                            if (m_project.getSelectedBufferView().gotHighlight())
                                            {
                                                m_project.getSelectedBufferView().replaceCurrentSelection(key);
                                            }
                                            else
                                            {
                                                m_project.getSelectedBufferView().insertText(key);
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


        /// <summary>
        /// Run a search on the current BufferView
        /// </summary>
        /// <returns></returns>
        protected void doSearch(GameTime gameTime)
        {
            if (!m_project.getSelectedBufferView().find(m_searchText))
            {
                setTemporaryMessage("[ \"" + m_searchText + "\" NOT FOUND]", gameTime, 1);
            }

            m_state = FriendlierState.TextEditing;
        }


        /// <summary>
        /// Are we pressing on a number key?
        /// </summary>
        /// <returns></returns>
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

            // Store the start and end time for this message - start time is used for
            // scrolling.
            //
            m_temporaryMessageStartTime = gameTime.TotalGameTime.TotalSeconds;
            m_temporaryMessageEndTime = m_temporaryMessageStartTime + seconds;
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
            if (m_project.getSelectedBufferView() != null)
            {
                newPos = getFreeBufferViewPosition(m_newPosition); // use the m_newPosition for the direction
            }

            BufferView newBV = new BufferView(newFB, newPos, 0, 20, m_fontManager.getCharWidth(), m_fontManager.getLineHeight(), fileIndex, readOnly);
            newBV.setTailing(tailFile);
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
            Vector3 newPos = m_project.getSelectedBufferView().calculateRelativePosition(position);
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

            BufferView newBufferView = new BufferView(m_project.getSelectedBufferView(), newPos);
            //newBufferView.m_textColour = Color.LawnGreen;
            m_project.addBufferView(newBufferView);
            setActiveBuffer(newBufferView);
        }


        /// <summary>
        /// Locate a BufferView located in a specified direction - if we find one then
        /// we set that as the active buffer view.
        /// </summary>
        /// <param name="position"></param>
        protected void detectMove(BufferView.BufferPosition position, GameTime gameTime)
        {
            // First get the position of a potential BufferView
            //
            Vector3 searchPosition = m_project.getSelectedBufferView().calculateRelativePosition(position);

            // Store the id of the current view
            //
            int fromView = m_project.getSelectedBufferViewId();

            // Search by index
            //
            for (int i = 0; i < m_project.getBufferViews().Count; i++)
            {
                if (m_project.getBufferViews()[i].getPosition() == searchPosition)
                {
                    m_project.setSelectedBufferViewId(i);
                    break;
                }
            }

            // Now set the active buffer if we need to - if not give a warning
            //
            if (fromView != m_project.getSelectedBufferViewId())
            {
                setActiveBuffer();
            }
            else
            {
                setTemporaryMessage("[NO BUFFERVIEW]", gameTime, 0.5);
            }
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
        /// Transform current eye position to an intended eye position over time
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
                        m_vFly = (m_newEyePosition - m_eye) / m_flySteps;
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
#if DEBUG_FLYING
                        Logger.logMsg("Friendlier::changeEyePosition() - eye is now at " + m_eye.ToString());
                        Logger.logMsg("Friendlier::changeEyePosition() - final position is " + m_newEyePosition.ToString());
#endif
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

            // In the manage project mode we zoom off into the distance
            //
            if (m_state == FriendlierState.ManageProject)
            {
                drawManageProject(gameTime);
                m_spriteBatch.End();
            }
            else
            {
                // Draw all the BufferViews for most modes
                //
                for (int i = 0; i < m_project.getBufferViews().Count; i++)
                {
                    drawFileBuffer(m_project.getBufferViews()[i], gameTime);
                }

                // If we're choosing a file then
                //
                if (m_state == FriendlierState.FileSaveAs || m_state == FriendlierState.FileOpen || m_state == FriendlierState.PositionScreenOpen || m_state == FriendlierState.PositionScreenNew || m_state == FriendlierState.PositionScreenCopy)
                {
                    drawDirectoryChooser(gameTime);
                    m_spriteBatch.End();
                }
                else if (m_state == FriendlierState.Help)
                {
                    m_spriteBatch.End();

                    drawTextScreen(gameTime, m_userHelp);
                }
                else if (m_state == FriendlierState.Information)
                {
                    m_spriteBatch.End();

                    drawInformationScreen(gameTime);
                }
                else if (m_state == FriendlierState.Configuration)
                {
                    m_spriteBatch.End();

                    drawConfigurationScreen(gameTime);
                }
                else
                {

                    // We only draw the scrollbar on the active view
                    //
                    drawScrollbar(m_project.getSelectedBufferView());

                    m_spriteBatch.End();

                    // Draw the Overlay HUD
                    //
                    drawOverlay(gameTime);

                    // Cursor and cursor highlight - none for tailed bufferviews
                    //
                    if (!m_project.getSelectedBufferView().isTailing())
                    {
                        drawCursor(gameTime);
                        drawHighlight(gameTime);
                    }
                }
            }

            base.Draw(gameTime);
        }

        /// <summary>
        /// Manage project
        /// </summary>
        protected void drawManageProject(GameTime gameTime)
        {

        }

        /// <summary>
        /// Draw the HUD Overlay for the editor with information about the current file we're viewing
        /// and position in that file.
        /// </summary>
        protected void drawOverlay(GameTime gameTime)
        {
            // Set our colour according to the state of Friendlier
            //
            Color overlayColour = Color.White;
            if (m_state != FriendlierState.TextEditing && m_state != FriendlierState.FindText)
            {
                overlayColour = m_greyedColour; 
            }

            // Filename is where we put the filename plus other assorted gubbins or we put a
            // search string in there depending on the mode.
            //
            string fileName = "";

            if (m_state == FriendlierState.FindText)
            {
                // Draw the search string down there
                fileName = "Search: " + m_searchText;
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

                    // If the temporary message is going to be too long for the space we have then
                    // we need to scroll it in that space.
                    //

                    //if (m_temporaryMessage.Length > 10) // hard code length for the moment
                    //{
                        fileName += " " + m_temporaryMessage;
                    //}
                    
                }
            }

            // Convert lineHeight back to normal size by dividing by m_textSize modifier
            //
            float yPos = m_graphics.GraphicsDevice.Viewport.Height - ( m_fontManager.getLineHeight() / m_fontManager.getTextScale() );

            // Debug eye position
            //
            string eyePosition = "[EyePosition] X " + m_eye.X + ",Y " + m_eye.Y + ",Z " + m_eye.Z;
            float xPos = m_graphics.GraphicsDevice.Viewport.Width - eyePosition.Length * m_fontManager.getCharWidth();

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

            float modeStringXPos = m_graphics.GraphicsDevice.Viewport.Width - modeString.Length * m_fontManager.getCharWidth() - (m_fontManager.getCharWidth() * 10);

            string positionString = m_project.getSelectedBufferView().getCursorPosition().Y + m_project.getSelectedBufferView().getBufferShowStartY() + "," + m_project.getSelectedBufferView().getCursorPosition().X;
            float positionStringXPos = m_graphics.GraphicsDevice.Viewport.Width - positionString.Length * m_fontManager.getCharWidth() - (m_fontManager.getCharWidth() * 18);

            float filePercent = 0.0f;

            if (m_project.getSelectedBufferView().getFileBuffer() != null && m_project.getSelectedBufferView().getFileBuffer().getLineCount() > 0)
            {
                filePercent = (float)(m_project.getSelectedBufferView().getCursorPosition().Y) /
                              (float)(Math.Max(1, m_project.getSelectedBufferView().getFileBuffer().getLineCount() - 1));
            }


            string filePercentString = ((int)(filePercent * 100.0f)) + "%";
            float filePercentStringXPos = m_graphics.GraphicsDevice.Viewport.Width - filePercentString.Length * m_fontManager.getCharWidth() - (m_fontManager.getCharWidth() * 5);


            // http://forums.create.msdn.com/forums/p/61995/381650.aspx
            //
            //m_overlaySpriteBatch.Begin(SpriteSortMode.Texture, BlendState.AlphaBlend);
            //m_overlaySpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.LinearWrap, DepthStencilState.None,RasterizerState.CullCounterClockwise);
            m_overlaySpriteBatch.Begin();

            // hardcode the font size to 1.0f so it looks nice
            //
            m_overlaySpriteBatch.DrawString(m_fontManager.getOverlayFont(), fileName, new Vector2(0.0f, yPos), overlayColour, 0, Vector2.Zero, 1.0f, 0, 0);
            m_overlaySpriteBatch.DrawString(m_fontManager.getOverlayFont(), eyePosition, new Vector2(0.0f, 0.0f), overlayColour, 0, Vector2.Zero, 1.0f, 0, 0);
            m_overlaySpriteBatch.DrawString(m_fontManager.getOverlayFont(), modeString, new Vector2(modeStringXPos, 0.0f), overlayColour, 0, Vector2.Zero, 1.0f, 0, 0);
            m_overlaySpriteBatch.DrawString(m_fontManager.getOverlayFont(), positionString, new Vector2(positionStringXPos, yPos), overlayColour, 0, Vector2.Zero, 1.0f, 0, 0);
            m_overlaySpriteBatch.DrawString(m_fontManager.getOverlayFont(), filePercentString, new Vector2(filePercentStringXPos, yPos), overlayColour, 0, Vector2.Zero, 1.0f, 0, 0);
            m_overlaySpriteBatch.End();

            drawSystemLoad(gameTime);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameTime"></param>
        protected void drawSystemLoad(GameTime gameTime)
        {
            Vector3 startPosition = m_project.getSelectedBufferView().getPosition();
            int linesHigh = 6;

            startPosition.X = startPosition.X + m_project.getSelectedBufferView().getVisibleWidth() + m_fontManager.getCharWidth() * 8;
            startPosition.Y = startPosition.Y + (m_project.getSelectedBufferView().getVisibleHeight() / 2) - (m_fontManager.getLineHeight() * linesHigh / 2);

            float height = m_fontManager.getLineHeight() * linesHigh;
            float width = m_fontManager.getCharWidth() / 2;

            // Only fetch some new samples when this timespan has elapsed
            //
            TimeSpan mySpan = gameTime.TotalGameTime;

            //Logger.logMsg("MYSPAN = " + mySpan.ToString());
            //Logger.logMsg("LAST FETCH = " + m_lastSystemFetch.ToString());
            //Logger.logMsg("DIFFERENCE = " + (mySpan - m_lastSystemFetch).ToString());

            if (mySpan - m_lastSystemFetch > m_systemFetchSpan)
            {
                if (m_counterWorker.m_cpuCounter != null && m_counterWorker.m_memCounter != null)
                {
                    CounterSample newCS = m_counterWorker.getCpuSample();
                    CounterSample newMem = m_counterWorker.getMemorySample();

                    // Calculate the percentages
                    //
                    m_systemLoad = CounterSample.Calculate(m_lastCPUSample, newCS);
                    m_memoryAvailable = CounterSample.Calculate(m_lastMemSample, newMem);

                    // Store the last samples
                    //
                    m_lastCPUSample = newCS;
                    m_lastMemSample = newMem;
                }

                m_lastSystemFetch = mySpan;

#if SYTEM_DEBUG
                Logger.logMsg("Friendlier::drawSystemLoad() - load is now " + m_systemLoad);
                Logger.logMsg("Friendlier::drawSystemLoad() - memory is now " + m_memoryAvailable);
                Logger.logMsg("Friendlier::drawSystemLoad() - physical memory available is " + m_physicalMemory);
#endif
            }


            // Draw background for CPU counter
            //
            Vector3 p1 = startPosition;
            Vector3 p2 = startPosition;

            p1.Y += height;
            p1.X += 1;

            renderQuad(p1, p2, Color.DarkGray);

            // Draw CPU load over the top
            //
            p1 = startPosition;
            p2 = startPosition;

            p1.Y += height;
            p2.Y += height - (m_systemLoad * height / 100.0f);
            p1.X += 1;

            renderQuad(p1, p2, Color.DarkGreen);

            // Draw background for Memory counter
            //
            startPosition.X += m_fontManager.getCharWidth();
            p1 = startPosition;
            p2 = startPosition;

            p1.Y += height;
            p1.X += 1;

            renderQuad(p1, p2, Color.DarkGray);

            // Draw Memory over the top
            //
            p1 = startPosition;
            p2 = startPosition;

            p1.Y += height;
            p2.Y += height - (height * m_memoryAvailable / m_physicalMemory);
            p1.X += 1;

            renderQuad(p1, p2, Color.DarkOrange);
        }


        /// <summary>
        /// Draw a cursor and make it blink in position
        /// </summary>
        /// <param name="v"></param>
        protected void drawCursor(GameTime gameTime)
        {
            // Don't draw the cursor if we're not the active window or if we're confirming 
            // something on the screen.
            //
            if (!this.IsActive || m_confirmState != ConfirmState.None || m_state == FriendlierState.FindText)
            {
                return;
            }

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
            Vector3 v1 = m_project.getSelectedBufferView().getCursorCoordinates();
            v1.Y += m_project.getSelectedBufferView().getLineHeight();

            Vector3 v2 = m_project.getSelectedBufferView().getCursorCoordinates();
            v2.X += 1;

            renderQuad(v1, v2, m_project.getSelectedBufferView().getHighlightColor());
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

            Vector3 startPosition = m_project.getSelectedBufferView().getPosition();

            if (m_state == FriendlierState.FileOpen)
            {
                line = "Open file...";
            }
            else if (m_state == FriendlierState.FileSaveAs)
            {
                line = "Save as...";
            }
            else if (m_state == FriendlierState.PositionScreenNew || m_state == FriendlierState.PositionScreenOpen || m_state == FriendlierState.PositionScreenCopy)
            {
                line = "Choose a position...";
            } else
            {
                line = "Unknown FriendlierState...";
            }

            // Draw header line
            //
            m_spriteBatch.DrawString(m_fontManager.getFont(), line, new Vector2(startPosition.X, startPosition.Y - m_project.getSelectedBufferView().getLineHeight() * 3), Color.White, 0, lineOrigin, m_fontManager.getTextScale() * 2.0f, 0, 0);

            // If we're using this method to position a new window only then don't show the directory chooser part..
            //
            if (m_state == FriendlierState.PositionScreenNew || m_state == FriendlierState.PositionScreenCopy)
            {
                return;
            }

            Color dirColour = Color.White;
            
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
                            yPosition += m_fontManager.getLineHeight() * 1.5f;
                            line = "...";
                        }

                        m_spriteBatch.DrawString(m_fontManager.getFont(),
                             line,
                             new Vector2(startPosition.X, startPosition.Y + yPosition),
                             (lineNumber == m_fileSystemView.getHighlightIndex() ? m_highlightColour : (lineNumber == endShowing ? Color.White : dirColour)),
                             0,
                             lineOrigin,
                             m_fontManager.getTextScale() * 1.5f,
                             0, 0);

                        yPosition += m_fontManager.getLineHeight() * 1.5f;
                    }

                    lineNumber++;
                }
            }
            else // This is where we draw Directories and Files
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
                m_spriteBatch.DrawString(m_fontManager.getFont(), line, new Vector2(startPosition.X, startPosition.Y), (m_fileSystemView.getHighlightIndex() == 0 ? m_highlightColour : dirColour), 0, lineOrigin, m_fontManager.getTextScale() * 2.0f, 0, 0);

                yPosition += m_fontManager.getLineHeight() * 3.0f;

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
                            yPosition += m_fontManager.getLineHeight() * 1.5f;
                            line = "...";
                        }

                        m_spriteBatch.DrawString(m_fontManager.getFont(),
                             line,
                             new Vector2(startPosition.X, startPosition.Y + yPosition),
                             (lineNumber == m_fileSystemView.getHighlightIndex() ? m_highlightColour : (lineNumber == endShowing ? Color.White : dirColour)),
                             0,
                             lineOrigin,
                             m_fontManager.getTextScale() * 1.5f,
                             0, 0);

                        yPosition += m_fontManager.getLineHeight() * 1.5f;
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
                            yPosition += m_fontManager.getLineHeight() * 1.5f;
                            line = "...";
                        }

                        m_spriteBatch.DrawString(m_fontManager.getFont(),
                                                 line,
                                                 new Vector2(startPosition.X, startPosition.Y + yPosition),
                                                 (lineNumber == m_fileSystemView.getHighlightIndex() ? m_highlightColour : (lineNumber == endShowing ? Color.White : m_itemColour)),
                                                 0,
                                                 lineOrigin,
                                                 m_fontManager.getTextScale() * 1.5f,
                                                 0, 0);

                        yPosition += m_fontManager.getLineHeight() * 1.5f;
                    }
                    lineNumber++;
                }
            }

            if (m_temporaryMessageEndTime > gameTime.TotalGameTime.TotalSeconds && m_temporaryMessage != "")
            {
                // Add any temporary message on to the end of the message
                //
                m_spriteBatch.DrawString(m_fontManager.getFont(),
                                         m_temporaryMessage,
                                         new Vector2(startPosition.X, startPosition.Y - 30.0f),
                                         Color.LightGoldenrodYellow,
                                         0,
                                         lineOrigin,
                                         m_fontManager.getTextScale() * 1.5f,
                                         0,
                                         0);
            }
        }

        /// <summary>
        /// Draw a BufferView in any state that we wish to - this means showing the lines of the
        /// file/buffer we want to see at the current cursor position with highlighting as required.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="file"></param>
        protected void drawFileBuffer(BufferView view, GameTime gameTime)
        {
            Color bufferColour = view.getTextColour();

            if (m_state != FriendlierState.TextEditing && m_state != FriendlierState.FindText)
            {
                bufferColour = m_greyedColour;
            }

            float yPosition = 0.0f;

            Vector2 lineOrigin = new Vector2();
            Vector3 viewSpaceTextPosition = view.getPosition();

            // Draw all the text lines to the height of the buffer
            //
            // This is default empty line character
            string line, fetchLine;
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

            // Pre-render any wrapped code so we know how many lines we need to show.
            //

#if WRAP_ATTEMPT
            // i is a line counter for reading from the file - because we can wrap rows we
            // need to keep it separate from yPosition
            // 
            int i = 0;

            // Draw the lines in the visible buffer
            //
            //for (int i = 0; i < view.getBufferShowLength(); i++)
            while (yPosition < m_fontManager.getLineHeight() * view.getBufferShowLength())
            {
                line = "~";

                if (view.getFileBuffer() != null)
                {
                    if (i + bufPos < view.getFileBuffer().getLineCount() && view.getFileBuffer().getLineCount() != 0)
                    {
                        // Fetch the line
                        //
                        fetchLine = view.getFileBuffer().getLine(i + bufPos);

                        //Logger.logMsg("FETCHLINE = " + fetchLine);

                        // Now ensure that we're only seeing the segment of the line that the cursor is in
                        // as it could be beyond the length of the window.
                        //
                        if (fetchLine.Length > view.getBufferShowStartX())
                        {
                            if (view.isReadOnly()) // wrap long lines
                            {
                                if (fetchLine.Length < view.getBufferShowWidth())
                                {
                                    m_spriteBatch.DrawString(m_fontManager.getFont(), fetchLine, new Vector2(viewSpaceTextPosition.X, viewSpaceTextPosition.Y + yPosition), bufferColour, 0, lineOrigin, m_fontManager.getTextScale() * 1.0f, 0, 0);
                                }
                                else
                                {
                                    string splitLine = fetchLine;

                                    while (splitLine.Length >= view.getBufferShowWidth())
                                    {
                                        // Split on window width and draw
                                        //
                                        line = splitLine.Substring(0, Math.Min(view.getBufferShowWidth(), splitLine.Length));
                                        m_spriteBatch.DrawString(m_fontManager.getFont(), line, new Vector2(viewSpaceTextPosition.X, viewSpaceTextPosition.Y + yPosition), bufferColour, 0, lineOrigin, m_fontManager.getTextScale() * 1.0f, 0, 0);

                                        // If the line is still wrapping then cut off the piece we've just printed and go around again
                                        //
                                        if (splitLine.Length >= view.getBufferShowWidth())
                                        {
                                            // Remove the part of the line written and increment
                                            //
                                            splitLine = splitLine.Substring(view.getBufferShowWidth(), splitLine.Length - view.getBufferShowWidth());
                                            yPosition += m_fontManager.getLineHeight();

                                            // Reset line here as we'll quit out at the next loop and be written below
                                            line = splitLine;
                                        }
                                    }
                                }
                            }
                            else // only display a subset of the line if it's over the visible width
                            {
                                line = fetchLine.Substring(view.getBufferShowStartX(), Math.Min(fetchLine.Length - view.getBufferShowStartX(), view.getBufferShowWidth()));

                                // Wrap if read only
                                //
                                if (fetchLine.Length - view.getBufferShowStartX() > view.getBufferShowWidth())
                                {
                                    line += "  [>]";
                                }
                            }
                        }
                        else
                        {
                            line = "";
                        }

                        m_spriteBatch.DrawString(m_fontManager.getFont(), line, new Vector2(viewSpaceTextPosition.X, viewSpaceTextPosition.Y + yPosition), bufferColour, 0, lineOrigin, m_fontManager.getTextScale() * 1.0f, 0, 0);
                    }
                    
                }

                yPosition += m_fontManager.getLineHeight();
                i++;
            }
#endif

            if (view.isTailing() && view.isReadOnly())
            {
                List<string> lines = view.getWrappedEndofBuffer();

                for (int i = 0; i < lines.Count; i++)
                {
                    m_spriteBatch.DrawString(m_fontManager.getFont(), lines[i], new Vector2(viewSpaceTextPosition.X, viewSpaceTextPosition.Y + yPosition), bufferColour, 0, lineOrigin, m_fontManager.getTextScale(), 0, 0);
                    yPosition += m_fontManager.getLineHeight();
                }
            }
            else
            {
                for (int i = 0; i < view.getBufferShowLength(); i++)
                {
                    line = "~";

                    if (i + bufPos < view.getFileBuffer().getLineCount() && view.getFileBuffer().getLineCount() != 0)
                    {
                        // Fetch the line
                        //
                        fetchLine = view.getFileBuffer().getLine(i + bufPos);

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
                            if (view.getBufferShowStartX() < fetchLine.Length)
                            {
                                line = fetchLine.Substring(view.getBufferShowStartX(), fetchLine.Length - view.getBufferShowStartX());
                            }
                            else
                            {
                                line = "";
                            }
                        }
                    }
                    m_spriteBatch.DrawString(m_fontManager.getFont(), line, new Vector2(viewSpaceTextPosition.X, viewSpaceTextPosition.Y + yPosition), bufferColour, 0, lineOrigin, m_fontManager.getTextScale(), 0, 0);
                    yPosition += m_fontManager.getLineHeight();
                }
            }

            // Draw overlaid ID on this window if we're far enough away to use it
            //
            if (m_zoomLevel > 800.0f)
            {
                int viewId = m_project.getBufferViews().IndexOf(view);
                string bufferId = viewId.ToString();
                Color seeThroughColour = bufferColour;
                seeThroughColour.A = 70;
                m_spriteBatch.DrawString(m_fontManager.getFont(), bufferId, new Vector2(viewSpaceTextPosition.X, viewSpaceTextPosition.Y), seeThroughColour, 0, lineOrigin, m_fontManager.getTextScale() * 19.0f, 0, 0);
            }
        }

        /// <summary>
        /// Draw a scroll bar for a BufferView
        /// </summary>
        /// <param name="view"></param>
        /// <param name="file"></param>
        protected void drawScrollbar(BufferView view)
        {
            Vector3 sbPos = view.getPosition();
            float height = view.getBufferShowLength() * m_fontManager.getLineHeight();

            Rectangle sbBackGround = new Rectangle(Convert.ToInt16(sbPos.X - m_fontManager.getTextScale() * 30.0f),
                                                   Convert.ToInt16(sbPos.Y),
                                                   1,
                                                   Convert.ToInt16(height));

            // Draw scroll bar
            //
            m_spriteBatch.Draw(m_flatTexture, sbBackGround, Color.DarkCyan);

            // Draw viewing window
            float start = view.getBufferShowStartY();
            float length = 0;

            // Get the line count
            //
            if (view.getFileBuffer() != null)
            {
                length = view.getFileBuffer().getLineCount();
            }

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

                Rectangle sb = new Rectangle(Convert.ToInt16(sbPos.X - m_fontManager.getTextScale() * 30.0f),
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
            List<BoundingBox> bb = m_project.getSelectedBufferView().computeHighlight();

            // Draw the bounding boxes
            //
            foreach (BoundingBox highlight in bb)
            {
                renderQuad(highlight.Min, highlight.Max, m_project.getSelectedBufferView().getHighlightColor());
            }
        }

        /// <summary>
        /// Renders a quad at a given position
        /// </summary>
        /// <param name="topLeft"></param>
        /// <param name="bottomRight"></param>
        protected void renderQuad(Vector3 topLeft, Vector3 bottomRight, Color quadColour)
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

            //m_spriteBatch.Begin(0, null, null, DepthStencilState.DepthRead, RasterizerState.CullNone, m_basicEffect);
            m_spriteBatch.Begin(SpriteSortMode.Texture, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.DepthRead, RasterizerState.CullNone, m_basicEffect);

            m_spriteBatch.Draw(m_flatTexture, new Rectangle(Convert.ToInt16(topLeft.X),
                                                 Convert.ToInt16(topLeft.Y),
                                                 Convert.ToInt16(bottomRight.X) - Convert.ToInt16(topLeft.X),
                                                 Convert.ToInt16(bottomRight.Y) - Convert.ToInt16(topLeft.Y)),
                                                 quadColour);  
            m_spriteBatch.End();
        }

        /// <summary>
        /// Populate the user help string - could do this from a resource file really
        /// </summary>
        protected void populateUserHelp()
        {
            m_userHelp += "User Help\n\n";

            m_userHelp += "F1  - Cycle down through buffer views\n";
            m_userHelp += "F2  - Cycle up through buffer views\n";
            m_userHelp += "F3  - Search again\n";
            m_userHelp += "F6  - Perform Build\n";
            m_userHelp += "F7  - Zoom Out\n";
            m_userHelp += "F8  - Zoom In\n";
            m_userHelp += "F9  - Rotate anticlockwise around group of 4\n";
            m_userHelp += "F10 - Rotate clockwise around group of 4\n";
            m_userHelp += "F11 - Full Screen Mode\n";
            m_userHelp += "F12 - Windowed Mode\n";

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
        }

        
        /// <summary>
        /// Format a screen of information text - slightly different to a help screen as
        /// the text can be dynamic (i.e. times)
        /// </summary>
        /// <param name="text"></param>
        protected void drawInformationScreen(GameTime gameTime)
        {
            // Set up the string
            //
            string text = "";

            text += "Project name:      : " + m_project.m_projectName + "\n";
            text += "Project created    : " + m_project.getCreationTime().ToString() + "\n";
            text += "Total files        : " + m_project.getFileBuffers().Count + "\n";
            text += "Total lines        : " + m_project.getFilesTotalLines() + "\n";

            // Some timings
            //
            TimeSpan nowDiff = (DateTime.Now - m_project.getCreationTime());
            TimeSpan activeTime = m_project.m_activeTime + (DateTime.Now - m_project.m_lastAccessTime);
            text += "Project age        : " + nowDiff.Days + " days, " + nowDiff.Hours + " hours, " + nowDiff.Minutes + " minutes\n"; //, " + nowDiff.Seconds + " seconds\n";
            text += "Total editing time : " + activeTime.Days + " days, " + activeTime.Hours + " hours, " + activeTime.Minutes + " minutes, " + activeTime.Seconds + " seconds\n";
            
            // Draw screen of a fixed width
            //
            drawTextScreen(gameTime, text, 60);

            /*
            Vector3 fp = m_project.getSelectedBufferView().getPosition();

            // Always start from 0 for offsets
            float yPos = 0.0f;
            float xPos = 0.0f;

            // Split out the input line
            //
            string[] infoRows = text.Split('\n');

            //  Position the information centrally
            //
            int lines = infoRows.Length;
            int longestRow = 0;
            for (int i = 0; i < lines; i++)
            {
                if (infoRows[i].Length > longestRow)
                {
                    longestRow = infoRows[i].Length;
                }
            }

            // Limit the row length
            //
            if (longestRow > 80)
            {
                longestRow = 80;
            }

            // Modify by height of the screen to centralise
            //
            yPos += (m_graphics.GraphicsDevice.Viewport.Height / 2) - (m_fontManager.getLineHeight(FontManager.FontType.Overlay) * lines / 2);

            // Adjust xPos
            //
            xPos = (m_graphics.GraphicsDevice.Viewport.Width / 2) - (longestRow * m_fontManager.getCharWidth(FontManager.FontType.Overlay) / 2);

            m_overlaySpriteBatch.Begin();

            // hardcode the font size to 1.0f so it looks nice
            //
            foreach (string line in text.Split('\n'))
            {
                m_overlaySpriteBatch.DrawString(m_fontManager.getOverlayFont(), line, new Vector2(xPos, yPos), Color.White, 0, Vector2.Zero, 1.0f, 0, 0);
                yPos += m_fontManager.getLineHeight(FontManager.FontType.Overlay);
            }

            m_overlaySpriteBatch.End();
            */
        }

        /// <summary>
        /// Format a screen of information text
        /// </summary>
        /// <param name="text"></param>
        protected void drawTextScreen(GameTime gameTime, string text, int fixedWidth = 0)
        {
            Vector3 fp = m_project.getSelectedBufferView().getPosition();

            // Always start from 0 for offsets
            float yPos = 0.0f;
            float xPos = 0.0f;

            // Split out the input line
            //
            string [] infoRows = text.Split('\n');

            //  Position the information centrally
            //
            int lines = infoRows.Length;
            int longestRow = 0;
            for (int i = 0; i < lines; i++)
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

            // Modify by height of the screen to centralise
            //
            yPos += (m_graphics.GraphicsDevice.Viewport.Height / 2) - (m_fontManager.getLineHeight(FontManager.FontType.Overlay) * lines / 2);

            // Adjust xPos
            //
            xPos = (m_graphics.GraphicsDevice.Viewport.Width / 2) - (longestRow * m_fontManager.getCharWidth(FontManager.FontType.Overlay) / 2);

            m_overlaySpriteBatch.Begin();

            // hardcode the font size to 1.0f so it looks nice
            //
            foreach (string line in text.Split('\n'))
            {
                m_overlaySpriteBatch.DrawString(m_fontManager.getOverlayFont(), line, new Vector2(xPos, yPos), Color.White, 0, Vector2.Zero, 1.0f, 0, 0);
                yPos += m_fontManager.getLineHeight(FontManager.FontType.Overlay);
            }

            m_overlaySpriteBatch.End();

        }

        /// <summary>
        /// Draw a screen which allows us to configure some settings
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="text"></param>
        protected void drawConfigurationScreen(GameTime gameTime)
        {
            Vector3 fp = m_project.getSelectedBufferView().getPosition();

            // Starting positions
            //
            float yPos = 5 * m_fontManager.getLineHeight(FontManager.FontType.Overlay);
            float xPos = 10 * m_fontManager.getCharWidth(FontManager.FontType.Overlay);

            // Start the spritebatch
            //
            m_overlaySpriteBatch.Begin();

            if (m_editConfigurationItem) // Edit a single configuration item
            {
                string text = "Edit configuration item";

                m_overlaySpriteBatch.DrawString(m_fontManager.getOverlayFont(), text, new Vector2(xPos, yPos), Color.White, 0, Vector2.Zero, 1.0f, 0, 0);
                yPos += m_fontManager.getLineHeight(FontManager.FontType.Overlay) * 2;

                m_overlaySpriteBatch.DrawString(m_fontManager.getOverlayFont(), m_project.getConfigurationItem(m_configPosition).Name, new Vector2(xPos, yPos), Color.White, 0, Vector2.Zero, 1.0f, 0, 0);
                yPos += m_fontManager.getLineHeight(FontManager.FontType.Overlay);


                m_overlaySpriteBatch.DrawString(m_fontManager.getOverlayFont(), m_editConfigurationItemValue, new Vector2(xPos, yPos), Color.White, 0, Vector2.Zero, 1.0f, 0, 0);
            }
            else
            {
                string text = "External Build Information";

                m_overlaySpriteBatch.DrawString(m_fontManager.getOverlayFont(), text, new Vector2(xPos, yPos), Color.White, 0, Vector2.Zero, 1.0f, 0, 0);
                yPos += m_fontManager.getLineHeight(FontManager.FontType.Overlay) * 2;

                // Write all the configuration items out - if we're highlight one of them then change
                // the colour.
                //
                for (int i = 0; i < m_project.getConfigurationListLength(); i++)
                {
                    string item = m_project.getConfigurationItem(i).Name + "  =  " + m_project.getConfigurationItem(i).Value;
                    m_overlaySpriteBatch.DrawString(m_fontManager.getOverlayFont(), item, new Vector2(xPos, yPos), (i == m_configPosition ? m_highlightColour : m_itemColour), 0, Vector2.Zero, 1.0f, 0, 0);
                    yPos += m_fontManager.getLineHeight(FontManager.FontType.Overlay);
                }
            }

            m_overlaySpriteBatch.End();
        }


        /// <summary>
        /// Perform an external build
        /// </summary>
        protected void doBuildCommand(GameTime gameTime)
        {
            Logger.logMsg("Friendlier::doBuildCommand() - attempting to run build command");

            // Check that we can find the build command
            //
            try
            {
                //string[] commandList = m_project.getBuildCommand().Split(' ');
                string[] commandList = m_project.getConfigurationValue("BUILDCOMMAND").Split(' ');

                if (commandList.Length == 0)
                {
                    setTemporaryMessage("Build command not defined", gameTime, 2);
                }
                else
                {
                    // We ensure that full path is given to build command at this time
                    //
                    if (!File.Exists(commandList[0]))
                    {
                        setTemporaryMessage("Build command not found : \"" + commandList[0] + "\"", gameTime, 2);
                    }
                    else
                    {
                        string buildDir = m_project.getConfigurationValue("BUILDDIRECTORY");
                        string buildLog = m_project.getConfigurationValue("BUILDLOG");

                        if (!Directory.Exists(buildDir))
                        {
                            setTemporaryMessage("Build directory doesn't exist : \"" + buildDir + "\"", gameTime, 2);
                            return;
                        }

                        // Now ensure that the build log is visible on the screen somewhere
                        //
                        BufferView bv = m_project.findBufferView(buildLog);

                        if (bv == null)
                        {
                            bv = addNewFileBuffer(buildLog, true, true);
                        }

                        // Move to that BufferView
                        //
                        setActiveBuffer(bv);

                        // Build the argument list
                        //
                        ProcessStartInfo info = new ProcessStartInfo();
                        info.WorkingDirectory = buildDir;
                        info.UseShellExecute = false;
                        info.FileName = m_project.getCommand();
                        info.WindowStyle = ProcessWindowStyle.Hidden;
                        info.CreateNoWindow = true;
                        info.Arguments = m_project.getArguments(); // +" >> " + m_project.getBuildLog();
                        info.RedirectStandardOutput = true;
                        info.RedirectStandardError = true;

                        Process process = new Process();
                        process.StartInfo = info;
                        process.OutputDataReceived += new DataReceivedEventHandler(logBuildStdOut);
                        process.ErrorDataReceived += new DataReceivedEventHandler(logBuildStdErr);

                        process.EnableRaisingEvents = true;

                        Logger.logMsg("Friendlier::doBuildCommand() - working directory = " + info.WorkingDirectory);
                        Logger.logMsg("Friendlier::doBuildCommand() - filename = " + info.FileName);
                        Logger.logMsg("Friendlier::doBuildCommand() - arguments = " + info.Arguments);

                        // Start the external build command and check the logs
                        //
                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        //Process.Start(m_project.getBuildCommand());
                        setTemporaryMessage("Starting build..", gameTime, 4);

                        //string stdout = proc.StandardOutput.ReadToEnd();
                        //string stderr = proc.StandardError.ReadToEnd();
                        process.WaitForExit();//

                        if (process.ExitCode != 0)
                        {
                            Logger.logMsg("BUILD DIDN'T WORK");
                        }

                        // Need to get some error data
                        //
                        //process.er
                    }
                }
            }
            catch (Exception e)
            {
                Logger.logMsg("Can't run command " + e.Message);
            }

            Logger.logMsg("Friendlier::doBuildCommand() - completed build command (?)");

        }

        /// <summary>
        /// Lock the log file for writing
        /// </summary>
        protected Mutex m_logFileMutex = new Mutex();

        /// <summary>
        /// Write the stdout from the build process to a log file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void logBuildStdOut(object sender, DataReceivedEventArgs e)
        {
            string logBody = (string)e.Data;
            string time = string.Format("{0:yyyyMMdd HH:mm:ss}", DateTime.Now);

            // Lock log file for writing
            //
            m_logFileMutex.WaitOne();

            System.IO.TextWriter logFile = new StreamWriter(m_project.getConfigurationValue("BUILDLOG"), true);
            logFile.WriteLine("INF:" + time + ":" + logBody);
            logFile.Flush();
            logFile.Close();
            logFile = null;

            // Unlock
            //
            m_logFileMutex.ReleaseMutex();
        }

        /// <summary>
        /// Write stderr
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void logBuildStdErr(object sender, DataReceivedEventArgs e)
        {
            string logBody = (string)e.Data;
            string time = string.Format("{0:yyyyMMdd HH:mm:ss}", DateTime.Now);

            // Lock log file for writing
            //
            m_logFileMutex.WaitOne();

            System.IO.TextWriter logFile = new StreamWriter(m_project.getConfigurationValue("BUILDLOG"), true);
            logFile.WriteLine("ERR:" + time + ":" + logBody);
            logFile.Flush();
            logFile.Close();
            logFile = null;

            // Unlock
            //
            m_logFileMutex.ReleaseMutex();
        }

    }
}
