#region File Description
//-----------------------------------------------------------------------------
// Friendlier.cs
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
using BloomPostprocess;
using System.Security.Permissions;

namespace Xyglo
{
    /// <summary>
    /// FriendlierState stores the state of our Friendlier application.  We also use some other
    /// sub-switches to keep a finer grain of state but these are the main modes.
    /// </summary>
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
        SplashScreen,       // What we see when we're arriving in the application
        DiffPicker,         // Mode for picking two files for differences checking
        WindowsRearranging, // When windows are flying between positions themselves
        GotoLine,           // Go to a line
        DemoExpired         // Demo period has expired
    };


    /// <summary>
    /// Main program is defined here based on an XNA Game class.   Friendlier works around a 
    /// Project concept and expects the files and facilities around files to be handled through
    /// that mechanism.
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
        /// A third SpriteBatch for panners/differs etc utilising alpha
        /// </summary>
        SpriteBatch m_pannerSpriteBatch;

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
        /// BasicEffect for panner
        /// </summary>
        //BasicEffect m_pannerEffect;

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
        bool m_spinning = false;

        /// <summary>
        /// The bloom component
        /// </summary>
        BloomComponent m_bloom;

        // Current bloom settings index
        //
        int m_bloomSettingsIndex = 0;

        /// <summary>
        /// A local Differ object
        /// </summary>
        protected Differ m_differ = null;

        /// <summary>
        /// Position we are in the diff
        /// </summary>
        protected int m_diffPosition = 0;

        /// <summary>
        /// Current project
        /// </summary>
        static protected Project m_project;

        /// <summary>
        /// Last keyboard state so that we can compare with current
        /// </summary>
        protected KeyboardState m_lastKeyboardState;

        /// <summary>
        /// Which key has been held down most recently?
        /// </summary>
        protected Keys m_heldKey;

        /// <summary>
        /// When did we start holding down a key?
        /// </summary>
        protected double m_heldDownStartTime = 0;

        /// <summary>
        /// Last time the thing repeated
        /// </summary>
        protected double m_heldDownLastRepeatTime = 0;

        /// <summary>
        /// Is the held key actually held or just last state?   This is an awkward way of doing this.
        /// </summary>
        protected bool m_heldDownKeyValid = false;

        /// <summary>
        /// Is either Shift key being held?
        /// </summary>
        protected bool m_shiftDown = false;

        /// <summary>
        /// Is either Control key being held down?
        /// </summary>
        protected bool m_ctrlDown = false;

        /// <summary>
        /// Is either Alt key being held down?
        /// </summary>
        protected bool m_altDown = false;

        /// <summary>
        /// Is either Windows key held down?
        /// </summary>
        protected bool m_windowsDown = false;

        /// <summary>
        /// Use this to store number when we've got ALT down - to select a new BufferView
        /// </summary>
        protected string m_gotoBufferView = "";

        /// <summary>
        /// The position where the project model will be viewable
        /// </summary>
        protected Vector3 m_projectPosition = Vector3.Zero;

        /// <summary>
        /// Goto line string holder
        /// </summary>
        protected string m_gotoLine = "";

        /// <summary>
        /// Confirmation state 
        /// </summary>
        public enum ConfirmState
        {
            None,
            FileSave,
            FileSaveCancel,
            CancelBuild,
            ConfirmQuit
        }

        /// <summary>
        /// Flag used to confirm quit
        /// </summary>
        protected bool m_confirmQuit = false;

        /// <summary>
        /// The index of the last directory we went into so we can save it
        /// </summary>
        protected int m_lastHighlightIndex = 0;

        /// <summary>
        /// Turn on and off file save confirmation
        /// </summary>
        protected bool m_confirmFileSave = false;

        /// <summary>
        /// Confirmation state - expecting Y/N
        /// </summary>
        public ConfirmState m_confirmState = ConfirmState.None;

        /// <summary>
        /// A flat texture we use for drawing coloured blobs like highlighting and cursors
        /// </summary>
        protected Texture2D m_flatTexture;

        /// <summary>
        /// A rendertarget for the text scroller
        /// </summary>
        protected RenderTarget2D m_textScroller;

        /// <summary>
        /// A texture we can render a text string to and scroll
        /// </summary>
        protected Texture2D m_textScrollTexture;

        /// <summary>
        /// Rotations are stored in this vector
        /// </summary>
        Vector3 m_rotations = new Vector3();

        /// <summary>
        /// Our view matrix
        /// </summary>
        Matrix m_viewMatrix = new Matrix();

        /// <summary>
        /// A bounding frustrum to allow us to cull objects not visible
        /// </summary>
        protected BoundingFrustum m_frustrum;

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
        //Texture2D m_dirNodeTexture;

        /// <summary>
        /// File system watcher
        /// </summary>
        protected List<FileSystemWatcher> m_watcherList = new List<FileSystemWatcher>();

        /// <summary>
        /// Position in which we should open or create a new screen
        /// </summary>
        protected BufferView.ViewPosition m_newPosition;

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
        /// Are we allowed to process keyboard events?
        /// </summary>
        protected TimeSpan m_processKeyboardAllowed = TimeSpan.Zero;

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
        /// Step for zooming
        /// </summary>
        protected float m_zoomStep = 50.0f;

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
        /// If our target position is not centred below our eye then we also have a vector here we need to
        /// modify.
        /// </summary>
        protected Vector3 m_vFlyTarget;

        /// <summary>
        /// How many steps between eye start and eye end fly position
        /// </summary>
        protected int m_flySteps = 15;

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
        protected Color m_greyedColour = new Color(30, 30, 30, 50);

        /// <summary>
        /// User help string
        /// </summary>
        protected string m_userHelp;

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
        /// SmartHelp worker thread
        /// </summary>
        protected SmartHelpWorker m_smartHelpWorker;

        /// <summary>
        /// The thread that is used for the counter
        /// </summary>
        protected Thread m_smartHelpWorkerThread;

        /// <summary>
        /// Worker thread for the Kinect management
        /// </summary>
        protected KinectWorker m_kinectWorker;

        /// <summary>
        /// The thread that is used for Kinect
        /// </summary>
        protected Thread m_kinectWorkerThread;

        /// <summary>
        /// Store GameTime somewhere central
        /// </summary>
        protected GameTime m_gameTime;

        /// <summary>
        /// View for the Standard Output of a build command
        /// </summary>
        protected BufferView m_buildStdOutView;

        /// <summary>
        /// View for the Standard Error of a build command
        /// </summary>
        protected BufferView m_buildStdErrView;

        /// <summary>
        /// Process for running builds
        /// </summary>
        Process m_buildProcess = null;

        /// <summary>
        /// Exit after save as
        /// </summary>
        protected bool m_saveAsExit = false;

        /// <summary>
        /// Generate a tree from a Friendlier structure
        /// </summary>
        protected TreeBuilder m_treeBuilder = new TreeBuilder();

        /// <summary>
        /// Model builder realises a model from a tree
        /// </summary>
        protected ModelBuilder m_modelBuilder;

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
        /// The colour of our banner
        /// </summary>
        protected Color m_bannerColour = new Color(180, 180, 180, 180);

        /// <summary>
        /// Text information screen y offset for page up and page down purposes
        /// </summary>
        protected int m_textScreenPositionY = 0;

        /// <summary>
        /// Length of information screen - so we know if we can page up or down
        /// </summary>
        protected int m_textScreenLength = 0;

        /// <summary>
        /// Config screen x direction
        /// </summary>
        protected int m_configXOffset = 0;

        /// <summary>
        /// Last mouse state
        /// </summary>
        protected MouseState m_lastMouseState = new MouseState();

        /// <summary>
        /// Time of last click
        /// </summary>
        protected TimeSpan m_lastClickTime = TimeSpan.Zero;

        /// <summary>
        /// Last position of the click
        /// </summary>
        protected Vector3 m_lastClickEyePosition = Vector3.Zero;

        /// <summary>
        /// Use this for highlighting a selected BufferView temporarily
        /// </summary>
        protected Pair<BufferView, Highlight> m_clickHighlight = new Pair<BufferView, Highlight>();

        /// <summary>
        /// Time for key auto-repeat to start
        /// </summary>
        double m_repeatHoldTime = 0.6; // seconds

        // Time between autorepeats
        //
        double m_repeatInterval = 0.05; // second

        /// <summary>
        /// Testing whether arrived in bounding sphere
        /// </summary>
        protected BoundingSphere m_testArrived = new BoundingSphere();

        /// <summary>
        /// Test result
        /// </summary>
        protected ContainmentType m_testResult;

        /// <summary>
        /// Is this Window resizable - for the moment it isn't
        /// </summary>
        protected bool m_isResizable = false;

        /// <summary>
        /// Are we resizing the main window?
        /// </summary>
        protected bool m_isResizing = false;

        /// <summary>
        /// Store the last window size in case we're resizing
        /// </summary>
        protected Vector2 m_lastWindowSize = Vector2.Zero;

        /// <summary>
        /// Spalsh screen texture
        /// </summary>
        protected Texture2D m_splashScreen;

        /// <summary>
        /// Mouse wheel value
        /// </summary>
        protected int m_lastMouseWheelValue = 0;

        /// <summary>
        /// Position of last mouse click
        /// </summary>
        protected Vector3 m_lastClickPosition = Vector3.Zero;

        /// <summary>
        /// Vector resulting from last mouse click
        /// </summary>
        protected Vector3 m_lastClickVector = Vector3.Zero;

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
        /// A helper class for drawing things
        /// </summary>
        protected DrawingHelper m_drawingHelper;

        /////////////////////////////// CONSTRUCTORS ////////////////////////////

        /// <summary>
        /// Default constructor
        /// </summary>
        public Friendlier()
        {
            initialise();
        }

        /// <summary>
        /// Project constructor
        /// </summary>
        /// <param name="project"></param>
        public Friendlier(Project project)
        {
            // File name
            //
            m_project = project;

            initialise();
        }

        /////////////////////////////// METHODS //////////////////////////////////////


        /// <summary>
        /// Initialise some stuff in the constructor
        /// </summary>
        protected void initialise()
        {
            Logger.logMsg("Friendlier::initialise() - loading components");

            m_graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // Initialise the bloom component
            //
            m_bloom = new BloomComponent(this);
            Components.Add(m_bloom);

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

            // Set windowed mode as default
            //
            windowedMode();

            // Populate the user help
            //
            populateUserHelp();

            // Check the demo status and set as necessary
            //
            if (!m_project.getLicenced())
            {
                m_state = FriendlierState.DemoExpired;
            }
        }

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

            // Defaults
            //
            m_project.getFontManager().setSmallScreen(true);
            int windowWidth = 640;
            int windowHeight = 480;

            if (maxWidth >= 1920)
            {
                windowWidth = 960;
                windowHeight = 768;
                m_project.getFontManager().setSmallScreen(false);
            } else if (maxWidth >= 1280)
            {
                windowWidth = 800;
                windowHeight = 500;
                //m_project.getFontManager().
            }
            else if (maxWidth >= 1024)
            {
                windowWidth = 720;
                windowHeight = 576;
            }

            // Set this for storage
            //
            m_project.setWindowSize(windowWidth, windowHeight);
            m_project.setFullScreen(false);

            // Set the graphics modes
            initGraphicsMode(windowWidth, windowHeight, false);

            // Update this to ensure scanner appears in the right place
            //
            if (m_drawingHelper != null)
            {
                m_drawingHelper.setPreviewBoundingBox(m_graphics.GraphicsDevice.Viewport.Width, m_graphics.GraphicsDevice.Viewport.Height);
            }
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

            m_project.setFullScreen(true);

            // Set the graphics modes
            initGraphicsMode(maxWidth, maxHeight, true);

            // Update this to ensure scanner appears in the right place
            //
            if (m_drawingHelper != null)
            {
                m_drawingHelper.setPreviewBoundingBox(m_graphics.GraphicsDevice.Viewport.Width, m_graphics.GraphicsDevice.Viewport.Height);
            }
        }

        /// <summary>
        /// Initialise the project - load all the FileBuffers and select the correct BufferView
        /// </summary>
        /// <param name="project"></param>
        public void initialiseProject()
        {
            Logger.logMsg("Friendlier::initialiseProject() - initialising fonts");

            // Initialise and load fonts into our Content context by family.
            //
            //FontManager.initialise(Content, "Lucida Sans Typewriter");
            //FontManager.initialise(Content, "Sax Mono");
            //m_project.initialiseFonts(Content, "Proggy Clean", GraphicsDevice.Viewport.AspectRatio, "Nuclex");
            //m_project.getFontManager().initialise(Content, "Bitstream Vera Sans Mono", GraphicsDevice.Viewport.AspectRatio);

            m_project.initialiseFonts(Content, "Bitstream Vera Sans Mono", GraphicsDevice.Viewport.AspectRatio, "Nuclex");
            

            // We need to do this to connect up all the BufferViews, FileBuffers and the other components
            // such as FontManager etc.
            //
            m_project.connectFloatingWorld();

            // Reconnect these views if they exist
            //
            m_buildStdOutView = m_project.getStdOutView();
            m_buildStdErrView = m_project.getStdErrView();

            // Set the tab space
            //
            m_project.setTab("  ");

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

            // Now do some jiggery pokery to make sure positioning is correct and that
            // any cursors or highlights are within bounds.
            //
            foreach (BufferView bv in m_project.getBufferViews())
            {
                // check boundaries for cursor and highlighting
                //
                bv.verifyBoundaries();

                // Set any defaults that we haven't persisted - this is a version upgrade
                // catch all method.
                //
                bv.setDefaults();
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
            m_eye = m_project.getEyePosition();
            m_target = m_project.getTargetPosition();
            m_zoomLevel = m_eye.Z;

            // Set the active buffer view
            //
            setActiveBuffer();

            // Set-up the single FileSystemView we have
            //
            if (m_project.getOpenDirectory() == "")
            {
                m_project.setOpenDirectory(@"C:\");  // set Default
            }
            m_fileSystemView = new FileSystemView(m_project.getOpenDirectory(), new Vector3(-800.0f, 0f, 0f), m_project);

            // Tree builder and model builder
            //
            generateTreeModel();
        }

        /// <summary>
        /// Generate a model from the Project
        /// </summary>
        private void generateTreeModel()
        {
            Logger.logMsg("Friendlier::generateTreeModel() - starting");
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            // Firstly get a root directory for the FileBuffer tree
            //
            string fileRoot = m_project.getFileBufferRoot();

            TreeBuilderGraph rG = m_treeBuilder.buildTreeFromFiles(fileRoot, m_project.getNonNullFileBuffers());

            // Build a model and store it if we don't have one
            //
            if (m_modelBuilder == null)
            {
                m_modelBuilder = new ModelBuilder();
            }

            // Rebuild it in a given position
            //
            m_modelBuilder.build(rG, m_projectPosition);

            sw.Stop();
            Logger.logMsg("Friendlier::generateTreeModel() - completed in " + sw.ElapsedMilliseconds + " ms");
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
                    fb.forceRefetchFile(m_project.getSyntaxManager());
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

                    // Reload the bloom component
                    //
                    if (m_bloom != null)
                    {
                        Components.Remove(m_bloom);
                        //m_bloom.Dispose();
                        m_bloom = new BloomComponent(this);
                        Components.Add(m_bloom);
                    }

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

                        // Reload the bloom component
                        //
                        if (m_bloom != null)
                        {
                            Components.Remove(m_bloom);
                            //m_bloom.Dispose();
                            m_bloom = new BloomComponent(this);
                            Components.Add(m_bloom);
                        }

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
                m_project.getFontManager().setFontState(FontManager.FontType.Small);
                Logger.logMsg("Friendlier:setSpriteFont() - using Small Window font");
            }
            else if (m_graphics.GraphicsDevice.Viewport.Width < 1024)
            {
                m_project.getFontManager().setFontState(FontManager.FontType.Window);
                Logger.logMsg("Friendlier:setSpriteFont() - using Window font");
            }
            else
            {
                Logger.logMsg("Friendlier:setSpriteFont() - using Full font");
                m_project.getFontManager().setFontState(FontManager.FontType.Full);
            }

            // to handle tabs for the moment convert them to single spaces
            //
            Logger.logMsg("Friendlier:setSpriteFont() - you must get these three variables correct for each position to avoid nasty looking fonts:");
            Logger.logMsg("Friendlier:setSpriteFont() - zoom level = " + m_zoomLevel);

            // Log these sizes 
            //
            Logger.logMsg("Friendlier:setSpriteFont() - Font getCharWidth = " + m_project.getFontManager().getCharWidth());
            Logger.logMsg("Friendlier:setSpriteFont() - Font getCharHeight = " + m_project.getFontManager().getCharHeight());
            Logger.logMsg("Friendlier:setSpriteFont() - Font getLineSpacing = " + m_project.getFontManager().getLineSpacing());
            Logger.logMsg("Friendlier:setSpriteFont() - Font getTextScale = " + m_project.getFontManager().getTextScale());

            /*
            // Now we need to make all of our BufferViews have this setting too
            //
            foreach (BufferView bv in m_project.getBufferViews())
            {
                bv.setCharWidth(m_project.getFontManager().getCharWidth());
                bv.setLineHeight(m_project.getFontManager().getLineSpacing());
            }
             * */

            Logger.logMsg("Friendlier:setSpriteFont() - recalculating relative positions");

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
            Logger.logMsg("Friendlier::LoadContent() - loading resources");

            m_splashScreen = Content.Load<Texture2D>("splash");

            //bloomSettingsIndex = (bloomSettingsIndex + 1) %
              //                       BloomSettings.PresetSettings.Length;

            m_bloom.Settings = BloomSettings.PresetSettings[5];

            // Start up the worker thread for the performance counters
            //
            m_counterWorker = new PerformanceWorker();
            m_counterWorkerThread = new Thread(m_counterWorker.startWorking);
            m_counterWorkerThread.Start();

            m_smartHelpWorker = new SmartHelpWorker();
            m_smartHelpWorkerThread = new Thread(m_smartHelpWorker.startWorking);
            m_smartHelpWorkerThread.Start();

            // Loop until worker thread activates.
            //
            while (!m_counterWorkerThread.IsAlive && !m_smartHelpWorkerThread.IsAlive);
            Thread.Sleep(1);

            // Start up the worker thread for Kinect integration
            //
            m_kinectWorker = new KinectWorker();
            m_kinectWorkerThread = new Thread(m_kinectWorker.startWorking);
            m_kinectWorkerThread.Start();

            // Loop until worker thread activates.
            //
            while (!m_kinectWorkerThread.IsAlive);
            Thread.Sleep(1);

            // Initialise the project - do this only once and after the font maan
            //
            initialiseProject();

            // Create a new SpriteBatch, which can be used to draw textures.
            m_spriteBatch = new SpriteBatch(m_graphics.GraphicsDevice);

            // Panner spritebatch
            //
            m_pannerSpriteBatch = new SpriteBatch(m_graphics.GraphicsDevice);

            // Set up the SpriteFont for the chosen resolution
            //
            setSpriteFont();

            // Create some textures
            //
            //m_dirNodeTexture = Shapes.CreateCircle(m_graphics.GraphicsDevice, 100);

            // Make mouse invisible
            //
            IsMouseVisible = true;

            // Ensure that the maximise box is shown and hook up the callback
            //
            System.Windows.Forms.Form f = (System.Windows.Forms.Form)System.Windows.Forms.Form.FromHandle(this.Window.Handle);
            f.MaximizeBox = true;
            f.Resize += Window_ResizeEvent;

            // Allow user resizing if we want this
            //
            if (m_isResizable)
            {
                this.Window.AllowUserResizing = true;
                this.Window.ClientSizeChanged += new EventHandler<EventArgs>(Window_ClientSizeChanged);
            }

            this.Window.Title = "Friendlier v" + VersionInformation.getProductVersion();

            // We have to initialise this as follows to work around CA2000 warning
            //
            m_basicEffect = new BasicEffect(m_graphics.GraphicsDevice);
            m_basicEffect.TextureEnabled = true;
            m_basicEffect.VertexColorEnabled = true;

            /*
            {
                TextureEnabled = true,
                VertexColorEnabled = true,
                //Alpha = 0.5f,
                //LightingEnabled = true
                //World = Matrix.Identity,
                //DiffuseColor = Vector3.One
            };
             * */

            // Create and initialize our effect
            //
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

            // Set up the text scroller width
            //
            setTextScrollerWidth(Convert.ToInt16(m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay) * 32));

            // Hook up the drag and drop
            //
            System.Windows.Forms.Form gameForm = (System.Windows.Forms.Form)System.Windows.Forms.Form.FromHandle(Window.Handle);
            gameForm.AllowDrop = true;
            gameForm.DragEnter += new System.Windows.Forms.DragEventHandler(friendlierDragEnter);
            gameForm.DragDrop += new System.Windows.Forms.DragEventHandler(friendlierDragDrop);

            // Store the last window size
            //
            m_lastWindowSize.X = Window.ClientBounds.Width;
            m_lastWindowSize.Y = Window.ClientBounds.Height;

            // Initialise the DrawingHelper with this bounding box and some other stuff
            //
            m_drawingHelper = new DrawingHelper(m_project, m_flatTexture, m_graphics.GraphicsDevice.Viewport.Width, m_graphics.GraphicsDevice.Viewport.Height);
        }

        /// <summary>
        /// Windows resize event is captured here so that we can go to full screen mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Window_ResizeEvent(object sender, System.EventArgs e)
        {
            System.Windows.Forms.Form f = (System.Windows.Forms.Form)System.Windows.Forms.Form.FromHandle(this.Window.Handle);
            
            if (f.WindowState == System.Windows.Forms.FormWindowState.Maximized)
            {
                fullScreenMode();
            }
        }

        public void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            // Make changes to handle the new window size.            
            Logger.logMsg("Friendlier::Window_ClientSizeChanged() - got client resized event");

            // Disable the callback to this method for the moment
            this.Window.ClientSizeChanged -= new EventHandler<EventArgs>(Window_ClientSizeChanged);

            /*
            float changeWidth = Window.ClientBounds.Width - m_lastWindowSize.X;
            float changeHeight = Window.ClientBounds.Height - m_lastWindowSize.Y;
            
            if (changeWidth > changeHeight) // enforce aspect ratio on height
            {
                changeHeight = changeWidth / m_project.getFontManager().getAspectRatio();
                m_graphics.PreferredBackBufferHeight = (int)changeHeight;
            }
            else
            {
                changeWidth = changeHeight * m_project.getFontManager().getAspectRatio();
                m_graphics.PreferredBackBufferWidth = (int)changeWidth;
            }
            m_graphics.ApplyChanges();
*/

            // Calculate new window size and resize all BufferViews accordingly
            //
            if (Window.ClientBounds.Width != m_lastWindowSize.X)
            {
                m_graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
                m_graphics.PreferredBackBufferHeight = (int)(Window.ClientBounds.Width / m_project.getFontManager().getAspectRatio());
            }
            else if (Window.ClientBounds.Height != m_lastWindowSize.Y)
            {
                m_graphics.PreferredBackBufferWidth = (int)(Window.ClientBounds.Height * m_project.getFontManager().getAspectRatio());
                m_graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
            }

            m_graphics.ApplyChanges();

            // Set up the Sprite font according to new size
            //
            setSpriteFont();

            // Save these values
            //
            m_lastWindowSize.X = Window.ClientBounds.Width;
            m_lastWindowSize.Y = Window.ClientBounds.Height;

            // Store it in the project too
            //
            m_project.setWindowSize(m_lastWindowSize.X, m_lastWindowSize.Y);

            // Reenable the callback
            //
            this.Window.ClientSizeChanged += new EventHandler<EventArgs>(Window_ClientSizeChanged);
        }


        /// <summary>
        /// Creates a new text scroller of given width
        /// </summary>
        protected void setTextScrollerWidth(int width)
        {
            // Dispose
            //
            //if (m_textScroller != null)
            //{
                //m_textScroller.Dispose();
            //}

            // Set up the text scrolling texture
            //
            m_textScroller = new RenderTarget2D(m_graphics.GraphicsDevice, width, Convert.ToInt16(m_project.getFontManager().getLineSpacing(FontManager.FontType.Overlay)));
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
        /// This is the generic method for setting an active view - for the moment this
        /// sets an additional parameter.
        /// </summary>
        /// <param name="view"></param>
        protected void setActiveBuffer(XygloView view)
        {
            //m_project.setSelectedView(view);

            // Now recalculate positions
            //
            //foreach (BufferView bv in m_project.getBufferViews())
            //{
                //bv.calculateMyRelativePosition();
            //}

            // All the maths is done in the Buffer View
            //
            Vector3 eyePos = view.getEyePosition(m_zoomLevel);
            flyToPosition(eyePos);


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
                else if (m_project.getBufferViews().Count == 0) // Or if we have none then create one
                {
                    BufferView bv = new BufferView(m_project.getFontManager());
                    using (FileBuffer fb = new FileBuffer())
                    {
                        m_project.addBufferView(bv);
                        m_project.addFileBuffer(fb);
                        bv.setFileBuffer(fb);
                    }
                }

                // Unset the view selection
                //
                m_project.setSelectedView(null);
            }
            catch (Exception e)
            {
                Logger.logMsg("Cannot locate BufferView item in list " + e.ToString());
                return;
            }

            Logger.logMsg("Friendlier:setActiveBuffer() - active buffer view is " + m_project.getSelectedBufferViewId());

            // Set the font manager up with a zoom level
            //
            m_project.getFontManager().setScreenState(m_zoomLevel, m_project.isFullScreen());

            // Now recalculate positions
            //
            foreach (BufferView bv in m_project.getBufferViews())
            {
                bv.calculateMyRelativePosition();
            }

            // All the maths is done in the Buffer View
            //
            Vector3 eyePos = m_project.getSelectedBufferView().getEyePosition(m_zoomLevel);

            flyToPosition(eyePos);

            // Set title to include current filename
            // (this is not thread safe - we need to synchronise)
            //this.Window.Title = "Friendlier v" + VersionInformation.getProductVersion() + " - " + m_project.getSelectedBufferView().getFileBuffer().getShortFileName();

#if ACTIVE_BUFFER_DEBUG
            Logger.logMsg("Friendlier:setActiveBuffer() - buffer position = " + m_activeBufferView.getPosition());
            Logger.logMsg("Friendlier:setActiveBuffer() - look position = " + m_target);
            Logger.logMsg("Friendlier:setActiveBuffer() - eye position = " + m_eye);
#endif
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

            // Clear the filename
            //
            m_saveFileName = "";

            // Set temporary bird's eye view
            //
            //Vector3 newPosition = m_eye;
            //newPosition.Z = 600.0f;

            // Fly there
            //
//            flyToPosition(newPosition);
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

            // Set temporary bird's eye view if we're in close
            //
            //Vector3 newPosition = m_eye;
            //newPosition.Z = 600.0f;

            // Fly there
            //
            //flyToPosition(newPosition);
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

                setTemporaryMessage("Removed BufferView.", 2, gameTime);
            }
            else
            {
                setTemporaryMessage("Can't remove the last BufferView.", 2, gameTime);
            }
        }

        /// <summary>
        /// Traverse a directory and allow opening/saving at that point according to state
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
                        m_lastHighlightIndex = m_fileSystemView.getHighlightIndex();
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

                            if (checkFileSave())
                            {
                                if (m_filesToWrite != null)
                                {
                                    // Check if we need to remove this FileBuffer from the todo list - it's not important if we can't
                                    // remove it here but we should try to anyway.
                                    //
                                    m_filesToWrite.RemoveAt(0);
                                    Logger.logMsg("Friendlier::traverseDirectory() - total files left to write is now " + m_filesToWrite.Count);

                                    // If we have finished saving all of our files then we can exit (although we check once again)
                                    //
                                    if (m_filesToWrite.Count == 0)
                                    {
                                        if (m_saveAsExit == true)
                                        {
                                            checkExit(gameTime);
                                        }
                                        else
                                        {
                                            setActiveBuffer();
                                        }
                                    }
                                }
                                else
                                {
                                    m_state = FriendlierState.TextEditing;
                                    setActiveBuffer();
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    setTemporaryMessage("Friendlier::traverseDirectory() - Cannot access \"" + subDirectory + "\"", 2, gameTime);
                }
            }
        }

        /// <summary>
        /// Checks to see if we are licenced before saving
        /// </summary>
        /// <returns></returns>
        protected bool checkFileSave()
        {
            if (m_project.getLicenced())
            {
                m_project.getSelectedBufferView().getFileBuffer().save();
                return true;
            }

            setTemporaryMessage("Can't save due to licence issue.", 10, m_gameTime);

            return false;
        }


        /// <summary>
        /// Completing a File->Save operation
        /// </summary>
        /// <param name="gameTime"></param>
        protected void completeSaveFile(GameTime gameTime)
        {
            try
            {
                checkFileSave();

                if (m_filesToWrite != null && m_filesToWrite.Count > 0)
                {
                    m_filesToWrite.RemoveAt(0);
                    Logger.logMsg("Friendlier::completeSaveFile() - files remaining to be written " + m_filesToWrite.Count);
                }

                Vector3 newPosition = m_eye;
                newPosition.Z = 500.0f;

                flyToPosition(newPosition);
                m_state = FriendlierState.TextEditing;

                setTemporaryMessage("Saved.", 2, gameTime);
            }
            catch (Exception)
            {
                setTemporaryMessage("Failed to save to " + m_project.getSelectedBufferView().getFileBuffer().getFilepath(), 2, gameTime);
            }
        }


        /// <summary>
        /// Exit but ensuring that buffers are saved
        /// </summary>
        protected void checkExit(GameTime gameTime, bool force = false)
        {
            Logger.logMsg("Friendlier::checkExit() - checking exit with force = " + force.ToString());

            // Firstly check for any unsaved buffers and warn
            //
            bool unsaved = false;

            // Use this somewhere probably
            //
            // m_project.getLicenced() 

            // Only check BufferViews status if we're not forcing an exit
            //
            if (!force && m_saveAsExit == false && m_project.getConfigurationValue("CONFIRMQUIT").ToUpper() == "TRUE")
            {
                if (m_confirmState != ConfirmState.ConfirmQuit)
                {
                    setTemporaryMessage("Confirm quit? Y/N", 0, gameTime);
                    m_confirmState = ConfirmState.ConfirmQuit;
                }

                if (m_confirmQuit == false)
                {
                    return;
                }
            }

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
                    setTemporaryMessage("", 1, gameTime);
                    m_confirmState = ConfirmState.None;
                    return;
                }
                else
                {
                    setTemporaryMessage("Unsaved Buffers.  Save?  Y/N/C", 0, gameTime);
                    m_confirmState = ConfirmState.FileSaveCancel;
                    m_saveAsExit = true;
                    //m_state = FriendlierState.FileSaveAs;
                }
            }
            else
            {
                // If these are not null then we're completed
                if (m_kinectWorker != null)
                {
                    // Close the kinect thread
                    //
                    m_kinectWorker.requestStop();
                    m_kinectWorkerThread.Join();
                    m_kinectWorker = null;
                }

                if (m_counterWorker != null)
                {
                    // Clear the worker thread and exit
                    //
                    m_counterWorker.requestStop();
                    m_counterWorkerThread.Join();
                    m_counterWorker = null;
                }

                // Join the smart help worker 
                //
                if (m_smartHelpWorker != null)
                {
                    m_smartHelpWorker.requestStop();
                    m_smartHelpWorkerThread.Join();
                    m_smartHelpWorker = null;
                }
                    
                // Modify Z if we're in the file selector height of 600.0f
                //
                if (m_eye.Z == 600.0f)
                {
                    m_eye.Z = 500.0f;
                }

                // Store the eye and target positions to the project before serialising it.
                //
                m_project.setEyePosition(m_eye);
                m_project.setTargetPosition(m_target);
                m_project.setOpenDirectory(m_fileSystemView.getPath());

                // Do some file management to ensure we have some backup copies
                //
                m_project.manageSerialisations();

                // Save our project including any updated file statuses
                //
                m_project.dataContractSerialise();

                this.Exit();
            }
        }

        /// <summary>
        /// Set a current zoom level
        /// </summary>
        /// <param name="zoomLevel"></param>
        protected void setZoomLevel(float zoomLevel)
        {
            m_zoomLevel = zoomLevel;

            if (m_zoomLevel < 500.0f)
            {
                m_zoomLevel = 500.0f;
            }

            Vector3 eyePos = m_eye;
            eyePos.Z = m_zoomLevel;
            flyToPosition(eyePos);

            // Don't always centre on a BufferView as we don't always want this
            //setActiveBuffer();
        }

        /// <summary>
        /// Process some meta commands as part of our update statement
        /// </summary>
        /// <param name="gameTime"></param>
        /// <returns></returns>
        protected bool processMetaCommands(GameTime gameTime)
        {
            // Allow the game to exit
            //
            if (checkKeyState(Keys.Escape, gameTime))
            {
                // Check to see if we are building something
                //
                if (m_buildProcess != null)
                {
                    setTemporaryMessage("Cancel build? (Y/N)", 0, m_gameTime);
                    m_confirmState = ConfirmState.CancelBuild;
                    return true;
                }

                if (m_confirmState == ConfirmState.ConfirmQuit)
                {
                    m_confirmState = ConfirmState.None;
                    setTemporaryMessage("Cancelled quit.", 1.0, gameTime);
                    m_state = FriendlierState.TextEditing;
                    return true;
                }

                // Depends where we are in the process here - check state
                //
                Vector3 newPosition = m_eye;

                //newPosition.Z = 500.0f;

                switch (m_state)
                {
                    case FriendlierState.TextEditing:
                        checkExit(gameTime);
                        break;

                    case FriendlierState.FileSaveAs:
                        setTemporaryMessage("Cancelled quit.", 0.5, gameTime);
                        m_confirmState = ConfirmState.None;
                        m_state = FriendlierState.TextEditing;
                        m_saveAsExit = false;
                        m_filesToWrite = null;
                        break;

                    case FriendlierState.ManageProject:
                        newPosition = m_project.getSelectedBufferView().getLookPosition();
                        newPosition.Z = 500.0f;
                        m_state = FriendlierState.TextEditing;
                        m_editConfigurationItem = false;
                        break;

                    case FriendlierState.DiffPicker:
                        m_state = FriendlierState.TextEditing;

                        // Before we clear the differ we want to translate the current viewed differ
                        // position back to the Bufferviews we originally generated them from.
                        //
                        BufferView bv1 = m_differ.getSourceBufferViewLhs();
                        BufferView bv2 = m_differ.getSourceBufferViewRhs();


                        // Ensure that these are valid
                        //
                        if (bv1 != null && bv2 != null)
                        {

                            bv1.setBufferShowStartY(m_differ.diffPositionLhsToOriginalPosition(m_diffPosition));
                            bv2.setBufferShowStartY(m_differ.diffPositionRhsToOriginalPosition(m_diffPosition));

                            ScreenPosition sp1 = new ScreenPosition();
                            ScreenPosition sp2 = new ScreenPosition();

                            sp1.X = 0;
                            sp1.Y = m_differ.diffPositionLhsToOriginalPosition(m_diffPosition);

                            sp2.X = 0;
                            sp2.Y = m_differ.diffPositionRhsToOriginalPosition(m_diffPosition);
                            bv1.setCursorPosition(sp1);
                            bv2.setCursorPosition(sp2);
                        }

                        // Clear the differ object if it exists
                        //
                        if (m_differ != null)
                        {
                            m_differ.clear();
                        }
                        break;
                    
                        // Two stage exit from the Configuration edit
                        //
                    case FriendlierState.Configuration:
                        if (m_editConfigurationItem == true)
                        {
                            m_editConfigurationItem = false;
                        }
                        else
                        {
                            m_state = FriendlierState.TextEditing;
                            m_editConfigurationItem = false;
                        }
                        break;

                    case FriendlierState.FileOpen:
                    case FriendlierState.Information:
                    case FriendlierState.PositionScreenOpen:
                    case FriendlierState.PositionScreenNew:
                    case FriendlierState.PositionScreenCopy:
                    case FriendlierState.SplashScreen:
                    default:
                        m_state = FriendlierState.TextEditing;
                        m_editConfigurationItem = false;
                        break;
                }

                // Cancel any temporary message
                //
                //m_temporaryMessageEndTime = gameTime.TotalGameTime.TotalSeconds;

                // Fly back to correct position
                //
                flyToPosition(newPosition);
            }

            // If we're viewing some information then only escape can get us out
            // of this mode.  Note that we also have to mind any animations so we
            // also want to ensure that m_changingEyePosition is not true.
            //
            if ((m_state == FriendlierState.Information || m_state == FriendlierState.Help /* || m_state == FriendlierState.ManageProject */ ) && m_changingEyePosition == false)
            {
                if (checkKeyState(Keys.PageDown, gameTime))
                {
                    if (m_textScreenPositionY + m_project.getSelectedBufferView().getBufferShowLength() < m_textScreenLength)
                    {
                        m_textScreenPositionY += m_project.getSelectedBufferView().getBufferShowLength();
                    }
                }
                else if (checkKeyState(Keys.PageUp, gameTime))
                {
                    if (m_textScreenPositionY > 0)
                    {
                        m_textScreenPositionY = m_textScreenPositionY - Math.Min(m_project.getSelectedBufferView().getBufferShowLength(), m_textScreenPositionY);
                    }
                }
                return true;
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
                    m_newPosition = BufferView.ViewPosition.Left;
                    gotSelection = true;
                }
                else if (checkKeyState(Keys.Right, gameTime))
                {
                    m_newPosition = BufferView.ViewPosition.Right;
                    gotSelection = true;
                    Logger.logMsg("Friendler::Update() - position screen right");
                }
                else if (checkKeyState(Keys.Up, gameTime))
                {
                    m_newPosition = BufferView.ViewPosition.Above;
                    gotSelection = true;
                    Logger.logMsg("Friendler::Update() - position screen up");
                }
                else if (checkKeyState(Keys.Down, gameTime))
                {
                    m_newPosition = BufferView.ViewPosition.Below;
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
                        BufferView newBV = new BufferView(m_project.getFontManager(), m_project.getSelectedBufferView(), m_newPosition);
                        m_project.addBufferView(newBV);
                        setActiveBuffer(newBV);
                        m_state = FriendlierState.TextEditing;
                    }
                }


                return true;
            }

            return false;
        }

        /// <summary>
        /// Process meta keys as part of our Update() 
        /// </summary>
        protected void processMetaKeys()
        {
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

            // Windows key state
            //
            if (m_windowsDown && Keyboard.GetState(PlayerIndex.One).IsKeyUp(Keys.LeftWindows) &&
                Keyboard.GetState(PlayerIndex.One).IsKeyUp(Keys.RightWindows))
            {
                m_windowsDown = false;
            }
            else
            {
                if (!m_windowsDown && (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.LeftWindows) ||
                    Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.RightWindows)))
                {
                    m_windowsDown = true;
                }
            }

        }

        /// <summary>
        /// Process action keys in the Update() statement
        /// </summary>
        /// <param name="gameTime"></param>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected void processActionKeys(GameTime gameTime)
        {
            // Main key handling statement
            //
            if (checkKeyState(Keys.Up, gameTime))
            {
                if (m_state == FriendlierState.FileSaveAs || m_state == FriendlierState.FileOpen)
                {
                    if (m_fileSystemView.getHighlightIndex() > 0)
                    {
                        m_fileSystemView.incrementHighlightIndex(-1);
                    }
                }
                else if (m_state == FriendlierState.ManageProject || (m_state == FriendlierState.Configuration && m_editConfigurationItem == false)) // Configuration changes
                {
                    if (m_configPosition > 0)
                    {
                        m_configPosition--;
                    }
                }
                else if (m_state == FriendlierState.DiffPicker)
                {
                    if (m_diffPosition > 0)
                    {
                        m_diffPosition--;
                    }
                }
                else
                {
                    if (m_altDown && m_shiftDown) // Do zoom
                    {
                        setZoomLevel(m_zoomLevel - m_zoomStep);
                    }
                    else if (m_altDown)
                    {
                        // Attempt to move right if there's a BufferView there
                        //
                        detectMove(BufferView.ViewPosition.Above, gameTime);
                    }
                    else
                    {
                        m_project.getSelectedBufferView().moveCursorUp(m_project, false);

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
                else if (m_state == FriendlierState.ManageProject)
                {
                    if (m_configPosition < m_modelBuilder.getLeafNodesPlaces() - 1)
                    {
                        m_configPosition++;
                    }
                }
                else if (m_state == FriendlierState.DiffPicker)
                {
                    if (m_differ != null && m_diffPosition < m_differ.getMaxDiffLength())
                    {
                        m_diffPosition++;
                    }
                }
                else
                {
                    if (m_altDown && m_shiftDown) // Do zoom
                    {
                        m_zoomLevel += m_zoomStep;
                        setActiveBuffer();
                    }
                    else if (m_altDown)
                    {
                        // Attempt to move right if there's a BufferView there
                        //
                        detectMove(BufferView.ViewPosition.Below, gameTime);
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
                            m_fileSystemView.setHighlightIndex(m_lastHighlightIndex);
                        }
                    }
                    catch (Exception /*e*/)
                    {
                        setTemporaryMessage("Cannot access " + parDirectory.ToString(), 2, gameTime);
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
                        detectMove(BufferView.ViewPosition.Left, gameTime);
                    }
                    else
                    {
                        m_project.getSelectedBufferView().moveCursorLeft(m_project);
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
                        detectMove(BufferView.ViewPosition.Right, gameTime);
                    }
                    else
                    {
                        m_project.getSelectedBufferView().moveCursorRight(m_project);
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
                ScreenPosition fp = m_project.getSelectedBufferView().getCursorPosition();

                // Set X and allow for tabs
                //
                if (fp.Y < m_project.getSelectedBufferView().getFileBuffer().getLineCount())
                {
                    fp.X = m_project.getSelectedBufferView().getFileBuffer().getLine(fp.Y).Replace("\t", m_project.getTab()).Length;
                }
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
                ScreenPosition fp = m_project.getSelectedBufferView().getFirstNonSpace(m_project);

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
            else if (checkKeyState(Keys.F4, gameTime))
            {
                m_project.setViewMode(Project.ViewMode.Fun);
                startBanner(gameTime, "Friendlier\nv1.0", 5);
            }
            else if (checkKeyState(Keys.F6, gameTime))
            {
                doBuildCommand(gameTime);
            }
            else if (checkKeyState(Keys.F7, gameTime))
            {
                string command = m_project.getConfigurationValue("ALTERNATEBUILDCOMMAND");
                doBuildCommand(gameTime, command);
            }
            else if (checkKeyState(Keys.F11, gameTime)) // Toggle full screen
            {
                if (m_project.isFullScreen())
                {
                    windowedMode();
                }
                else
                {
                    fullScreenMode();
                }
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
            else if (checkKeyState(Keys.PageDown, gameTime))
            {
                if (m_state == FriendlierState.TextEditing)
                {
                    m_project.getSelectedBufferView().pageDown(m_project);

                    if (m_shiftDown)
                    {
                        m_project.getSelectedBufferView().extendHighlight(); // Extend
                    }
                    else
                    {
                        m_project.getSelectedBufferView().noHighlight(); // Disable
                    }
                }
                else if (m_state == FriendlierState.DiffPicker)
                {
                    if (m_differ != null && m_diffPosition < m_differ.getMaxDiffLength())
                    {
                        m_diffPosition += m_project.getSelectedBufferView().getBufferShowLength();

                        if (m_diffPosition >= m_differ.getMaxDiffLength())
                        {
                            m_diffPosition = m_differ.getMaxDiffLength() - 1;
                        }
                    }
                }
            }
            else if (checkKeyState(Keys.PageUp, gameTime))
            {
                if (m_state == FriendlierState.TextEditing)
                {
                    m_project.getSelectedBufferView().pageUp(m_project);

                    if (m_shiftDown)
                    {
                        m_project.getSelectedBufferView().extendHighlight(); // Extend
                    }
                    else
                    {
                        m_project.getSelectedBufferView().noHighlight(); // Disable
                    }
                }
                else if (m_state == FriendlierState.DiffPicker)
                {
                    if (m_diffPosition > 0)
                    {
                        m_diffPosition -= m_project.getSelectedBufferView().getBufferShowLength();

                        if (m_diffPosition < 0)
                        {
                            m_diffPosition = 0;
                        }
                    }
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
                m_project.getSelectedBufferView().insertText(m_project, "\t");
            }
            else if (checkKeyState(Keys.Insert, gameTime))
            {
                if (m_state == FriendlierState.ManageProject)
                {
                    if (m_configPosition >= 0 && m_configPosition < m_modelBuilder.getLeafNodesPlaces())
                    {
                        string fileToEdit  = m_modelBuilder.getSelectedModelString(m_configPosition);

                        BufferView bv = m_project.getBufferView(fileToEdit);

                        if (bv != null)
                        {
                            setActiveBuffer(bv);
                        }
                        else // create and activate
                        {
                            try
                            {
                                FileBuffer fb = m_project.getFileBuffer(fileToEdit);
                                bv = new BufferView(m_project.getFontManager(), m_project.getBufferViews()[0], BufferView.ViewPosition.Left);
                                bv.setFileBuffer(fb);
                                int bvIndex = m_project.addBufferView(bv);
                                setActiveBuffer(bvIndex);

                                Vector3 rootPosition = m_project.getBufferViews()[0].getPosition();
                                Vector3 newPosition2 = bv.getPosition();

                                Logger.logMsg(rootPosition.ToString() + newPosition2.ToString());
                                //bv.setFileBufferIndex(
                                fb.loadFile(m_project.getSyntaxManager());

                                if (m_project.getConfigurationValue("SYNTAXHIGHLIGHT").ToUpper() == "TRUE")
                                {
                                    m_project.getSyntaxManager().generateAllHighlighting(fb);
                                }

                                // Break out of Manage mode and back to editing
                                //
                                Vector3 newPosition = m_project.getSelectedBufferView().getLookPosition();
                                newPosition.Z = 500.0f;
                                m_state = FriendlierState.TextEditing;
                                m_editConfigurationItem = false;
                            }
                            catch (Exception e)
                            {
                                setTemporaryMessage("Failed to load file " + e.Message, 2, gameTime);
                            }
                        }
                    }
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
                else if (m_state == FriendlierState.FindText && checkKeyState(Keys.Back, gameTime))
                {
                    string searchText = m_project.getSelectedBufferView().getSearchText();
                    // Delete charcters from the file name if we have one
                    //
                    if (searchText.Length > 0)
                    {
                        m_project.getSelectedBufferView().setSearchText(searchText.Substring(0, searchText.Length - 1));
                    }
                }
                else if (m_state == FriendlierState.GotoLine && checkKeyState(Keys.Back, gameTime))
                {
                    if (m_gotoLine.Length > 0)
                    {
                        m_gotoLine = m_gotoLine.Substring(0, m_gotoLine.Length - 1);
                    }
                }
                else if (m_state == FriendlierState.Configuration && m_editConfigurationItem && checkKeyState(Keys.Back, gameTime))
                {
                    if (m_editConfigurationItemValue.Length > 0)
                    {
                        m_editConfigurationItemValue = m_editConfigurationItemValue.Substring(0, m_editConfigurationItemValue.Length - 1);
                    }
                }
                else if (m_state == FriendlierState.ManageProject)
                {
                    if (m_configPosition >= 0 && m_configPosition < m_modelBuilder.getLeafNodesPlaces())
                    {
                        string fileToRemove = m_modelBuilder.getSelectedModelString(m_configPosition);
                        if (m_project.removeFileBuffer(fileToRemove))
                        {
                            Logger.logMsg("Friendlier::Update() - removed FileBuffer for " + fileToRemove);

                            // Update Active Buffer as necessary
                            //
                            setActiveBuffer();

                            // Rebuild the file model
                            //
                            generateTreeModel();

                            setTemporaryMessage("Removed " + fileToRemove + " from project", 5, m_gameTime);
                        }
                        else
                        {
                            Logger.logMsg("Friendlier::Update() - failed to remove FileBuffer for " + fileToRemove);
                        }
                    }
                }
                else if (m_project.getSelectedBufferView().gotHighlight()) // If we have a valid highlighted selection then delete it (normal editing)
                {
                    // All the clever stuff with the cursor is done at the BufferView level and it also
                    // calls the command in the FileBuffer.
                    //
                    m_project.getSelectedBufferView().deleteCurrentSelection(m_project);
                }
                else // delete at cursor
                {
                    if (checkKeyState(Keys.Delete, gameTime))
                    {
                        m_project.getSelectedBufferView().deleteSingle(m_project);
                    }
                    else if (checkKeyState(Keys.Back, gameTime))
                    {
                        // Start with a file position from the screen position
                        //
                        FilePosition fp = m_project.getSelectedBufferView().screenToFilePosition(m_project);

                        // Get the character before the current one and backspace accordingly 

                        if (fp.X > 0)
                        {
                            string fetchLine = m_project.getSelectedBufferView().getCurrentLine();

                            // Decrement and set X
                            //
                            fp.X--;

                            // Now convert back to a screen position
                            fp.X = fetchLine.Substring(0, fp.X).Replace("\t", m_project.getTab()).Length;

                        }
                        else if (fp.Y > 0)
                        {
                            fp.Y -= 1;

                            // Don't forget to do tab conversions here too
                            //
                            fp.X = m_project.getSelectedBufferView().getFileBuffer().getLine(Convert.ToInt16(fp.Y)).Replace("\t", m_project.getTab()).Length;
                        }

                        m_project.getSelectedBufferView().setCursorPosition(new ScreenPosition(fp));

                        m_project.getSelectedBufferView().deleteSingle(m_project);
                    }
                }
            }
            else if (checkKeyState(Keys.Enter, gameTime))
            {
                //ScreenPosition fp = m_project.getSelectedBufferView().getCursorPosition();

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
                            if (m_saveAsExit)
                            {
                                checkExit(gameTime);
                            }
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
                else if (m_state == FriendlierState.GotoLine)
                {
                    try
                    {
                        int gotoLine = Convert.ToInt16(m_gotoLine);

                        if (gotoLine > 0)
                        {
                            if (gotoLine < m_project.getSelectedBufferView().getFileBuffer().getLineCount() - 1)
                            {
                                ScreenPosition sp = new ScreenPosition(0, gotoLine);
                                m_project.getSelectedBufferView().setCursorPosition(sp);
                            }
                            else
                            {
                                setTemporaryMessage("Attempted to go beyond end of file.", 2, gameTime);
                            }
                        }
                    }
                    catch (Exception /* e */)
                    {
                        Logger.logMsg("Probably got junk in the goto line dialog");
                        setTemporaryMessage("Lines are identified by numbers.", 2, gameTime);
                    }

                    m_gotoLine = "";
                    m_state = FriendlierState.TextEditing;
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

                    if (m_project.getSelectedBufferView().gotHighlight())
                    {
                        m_project.getSelectedBufferView().replaceCurrentSelection(m_project, "\n");
                    }
                    else
                    {
                        m_project.getSelectedBufferView().insertNewLine(m_project, indent);
                    }
                }
            }
        }

        /// <summary>
        /// Processs key combinations and commands from the keyboard - return true if we've
        /// captured a command so we don't print that character
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected bool processCombinationsCommands(GameTime gameTime)
        {
            bool rC = false;

            // Check confirm state
            //
            if (m_confirmState != ConfirmState.None)
            {
                if (checkKeyState(Keys.Y, gameTime))
                {
                    Logger.logMsg("Friendlier::processCombinationsCommands() - confirm y/n");
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
                                if (checkFileSave())
                                {
                                    // Save has completed without error
                                    //
                                    setTemporaryMessage("Saved.", 2, gameTime);
                                }

                                m_state = FriendlierState.TextEditing;
                                rC = true;
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
                                if (fb.isModified())
                                {
                                    if (fb.isWriteable())
                                    {
                                        fb.save();
                                    }
                                    else
                                    {
                                        // Only add a filebuffer if it's not the same physical file
                                        //
                                        bool addFileBuffer = true;
                                        foreach (FileBuffer fb2 in m_filesToWrite)
                                        {
                                            if (fb2.getFilepath() == fb.getFilepath())
                                            {
                                                addFileBuffer = false;
                                                break;
                                            }
                                        }

                                        if (addFileBuffer)
                                        {
                                            m_filesToWrite.Add(fb);
                                        }
                                    }
                                }
                            }

                            // All files saved then exit
                            //
                            if (m_filesToWrite.Count == 0)
                            {
                                checkExit(gameTime);
                            }
                        }
                        else if (m_confirmState == ConfirmState.CancelBuild)
                        {
                            Logger.logMsg("Friendlier::processCombinationsCommands() - cancel build");
                            m_buildProcess.Close();
                            m_buildProcess = null;
                        }
                        else if (m_confirmState == ConfirmState.ConfirmQuit)
                        {
                            m_confirmQuit = true;
                            checkExit(gameTime, true);
                        }
                        rC = true; // consume this letter
                    }
                    catch (Exception e)
                    {
                        setTemporaryMessage("Save failed with \"" + e.Message + "\".", 5, gameTime);
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
                    else if (m_confirmState == ConfirmState.CancelBuild)
                    {
                        setTemporaryMessage("Continuing build..", 2, gameTime);
                        m_confirmState = ConfirmState.None;
                    }
                    else if (m_confirmState == ConfirmState.ConfirmQuit)
                    {
                        setTemporaryMessage("Cancelled quit", 2, gameTime);
                        m_confirmState = ConfirmState.None;
                    }
                    rC = true; // consume this letter
                }
                else if (checkKeyState(Keys.C, gameTime) && m_confirmState == ConfirmState.FileSaveCancel)
                {
                    setTemporaryMessage("Cancelled Quit.", 0.5, gameTime);
                    m_confirmState = ConfirmState.None;
                    rC = true;
                }
            }
            else if (m_ctrlDown && !m_altDown)  // CTRL down and no ALT
            {
                if (checkKeyState(Keys.C, gameTime)) // Copy
                {
                    if (m_state == FriendlierState.Configuration && m_editConfigurationItem)
                    {
                        Logger.logMsg("Friendlier::processCombinationsCommands() - copying from configuration");
                        System.Windows.Forms.Clipboard.SetText(m_editConfigurationItemValue);
                    }
                    else
                    {
                        Logger.logMsg("Friendlier::processCombinationsCommands() - copying to clipboard");
                        string text = m_project.getSelectedBufferView().getSelection(m_project).getClipboardString();

                        // We can only set this is the text is not empty
                        if (text != "")
                        {
                            System.Windows.Forms.Clipboard.SetText(text);
                        }
                        rC = true;
                    }
                }
                else if (checkKeyState(Keys.X, gameTime)) // Cut
                {
                    if (m_state == FriendlierState.Configuration && m_editConfigurationItem)
                    {
                        Logger.logMsg("Friendlier::processCombinationsCommands() - cutting from configuration");
                        System.Windows.Forms.Clipboard.SetText(m_editConfigurationItemValue);
                        m_editConfigurationItemValue = "";
                    }
                    else
                    {
                        Logger.logMsg("Friendlier::processCombinationsCommands() - cut");

                        System.Windows.Forms.Clipboard.SetText(m_project.getSelectedBufferView().getSelection(m_project).getClipboardString());
                        m_project.getSelectedBufferView().deleteCurrentSelection(m_project);
                        rC = true;
                    }
                }
                else if (checkKeyState(Keys.V, gameTime)) // Paste
                {
                    if (System.Windows.Forms.Clipboard.ContainsText())
                    {
                        if (m_state == FriendlierState.Configuration && m_editConfigurationItem)
                        {
                            Logger.logMsg("Friendlier::processCombinationsCommands() - pasting into configuration");

                            // Ensure that we only get one line out of the clipboard and make sure
                            // it's the last meaningful one.
                            //
                            string lastPasteText = "";
                            foreach (string text in System.Windows.Forms.Clipboard.GetText().Split('\n'))
                            {
                                if (text != "")
                                {
                                    lastPasteText = text;
                                }
                            }

                            m_editConfigurationItemValue = lastPasteText;
                        }
                        else
                        {
                            Logger.logMsg("Friendlier::processCombinationsCommands() - pasting text");
                            // If we have a selection then replace it - else insert
                            //
                            if (m_project.getSelectedBufferView().gotHighlight())
                            {
                                m_project.getSelectedBufferView().replaceCurrentSelection(m_project, System.Windows.Forms.Clipboard.GetText());
                            }
                            else
                            {
                                m_project.getSelectedBufferView().insertText(m_project, System.Windows.Forms.Clipboard.GetText());
                            }
                            rC = true;
                     
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
                            m_project.getSelectedBufferView().undo(m_project, 1);
                        }
                        else
                        {
                            //Logger.logMsg("Friendlier::Update() - nothing to undo");
                            setTemporaryMessage("Nothing to undo.", 0.3, gameTime);
                        }
                        rC = true;
                    }
                    catch (Exception e)
                    {
                        //System.Windows.Forms.MessageBox.Show("Undo stack is empty - " + e.Message);
                        Logger.logMsg("Friendlier::processCombinationsCommands() - got exception " + e.Message);
                        setTemporaryMessage("Nothing to undo with exception.", 2, gameTime);
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
                            m_project.getSelectedBufferView().redo(m_project, 1);
                        }
                        else
                        {
                            setTemporaryMessage("Nothing to redo.", 0.3, gameTime);
                        }
                        rC = true;
                    }
                    catch (Exception e)
                    {
                        //System.Windows.Forms.MessageBox.Show("Undo stack is empty - " + e.Message);
                        Logger.logMsg("Friendlier::processCombinationsCommands() - got exception " + e.Message);
                        setTemporaryMessage("Nothing to redo.", 2, gameTime);
                    }
                }
                else if (checkKeyState(Keys.A, gameTime))  // Select all
                {
                    m_project.getSelectedBufferView().selectAll();
                    rC = true;
                }
                else if (checkKeyState(Keys.OemPlus, gameTime)) // increment bloom state
                {
                    m_bloomSettingsIndex = (m_bloomSettingsIndex + 1) % BloomSettings.PresetSettings.Length;
                    m_bloom.Settings = BloomSettings.PresetSettings[m_bloomSettingsIndex];
                    m_bloom.Visible = true;

                    setTemporaryMessage("Bloom set to " + BloomSettings.PresetSettings[m_bloomSettingsIndex].Name, 3, gameTime);
                }
                else if (checkKeyState(Keys.OemMinus, gameTime)) // decrement bloom state
                {
                    m_bloomSettingsIndex = (m_bloomSettingsIndex - 1);

                    if (m_bloomSettingsIndex < 0)
                    {
                        m_bloomSettingsIndex += BloomSettings.PresetSettings.Length;
                    }

                    m_bloom.Settings = BloomSettings.PresetSettings[m_bloomSettingsIndex];
                    m_bloom.Visible = true;
                    setTemporaryMessage("Bloom set to " + BloomSettings.PresetSettings[m_bloomSettingsIndex].Name, 3, gameTime);
                }
                else if (checkKeyState(Keys.B, gameTime)) // Toggle bloom
                {
                    m_bloom.Visible = !m_bloom.Visible;
                    setTemporaryMessage("Bloom " + (m_bloom.Visible ? "on" : "off"), 3, gameTime);
                }

            }
            else if (m_altDown && !m_ctrlDown) // ALT down action and no CTRL down
            {
                if (checkKeyState(Keys.S, gameTime) && m_project.getSelectedBufferView().getFileBuffer().isModified())
                {
                    // If we want to confirm save then ask
                    //
                    if (m_confirmFileSave)
                    {
                        setTemporaryMessage("Confirm Save? Y/N", 0, gameTime);
                        m_confirmState = ConfirmState.FileSave;
                    }
                    else  // just save
                    {
                        // Select a file path if we need one
                        //
                        if (m_project.getSelectedBufferView().getFileBuffer().getFilepath() == "")
                        {
                            m_saveAsExit = false;
                            selectSaveFile();
                        }
                        else
                        {
                            // Attempt save
                            //
                            if (checkFileSave())
                            {
                                // Save has completed without error
                                //
                                setTemporaryMessage("Saved.", 2, gameTime);
                            }

                            m_state = FriendlierState.TextEditing;
                        }
                        rC = true;
                    }
                }
                else if (checkKeyState(Keys.A, gameTime)) // Explicit save as
                {
                    m_saveAsExit = false;
                    selectSaveFile();
                    rC = true;
                }
                else if (checkKeyState(Keys.N, gameTime)) // New BufferView on new FileBuffer
                {
                    m_state = FriendlierState.PositionScreenNew;
                    rC = true;
                }
                else if (checkKeyState(Keys.B, gameTime)) // New BufferView on same FileBuffer (copy the existing BufferView)
                {
                    m_state = FriendlierState.PositionScreenCopy;
                    rC = true;
                }
                else if (checkKeyState(Keys.O, gameTime)) // Open a file
                {
                    selectOpenFile();
                    rC = true;
                }
                else if (checkKeyState(Keys.H, gameTime)) // Show the help screen
                {
                    // Reset page position and set information mode
                    //
                    m_textScreenPositionY = 0;
                    m_state = FriendlierState.Help;
                    rC = true;
                }
                else if (checkKeyState(Keys.I, gameTime)) // Show the information screen
                {
                    // Reset page position and set information mode
                    //
                    m_textScreenPositionY = 0;
                    m_state = FriendlierState.Information;
                    rC = true;
                }
                else if (checkKeyState(Keys.G, gameTime)) // Show the config screen
                {
                    // Reset page position and set information mode
                    //
                    m_textScreenPositionY = 0;
                    showConfigurationScreen();
                    rC = true;
                }
                else if (checkKeyState(Keys.C, gameTime)) // Close current BufferView
                {
                    closeActiveBuffer(gameTime);
                    rC = true;
                }
                else if (checkKeyState(Keys.D, gameTime))
                {
                    m_state = FriendlierState.DiffPicker;
                    setTemporaryMessage("Pick a BufferView to diff against", 5, gameTime);

                    // Set up the differ
                    //
                    if (m_differ == null)
                    {
                        m_differ = new Differ();
                    }
                    else
                    {
                        m_differ.clear();
                    }

                    rC = true;
                }
                else if (checkKeyState(Keys.M, gameTime))
                {
                    // Set the config position - we (re)use this to hold menu position in the manage
                    // project screen for removing file items.
                    //
                    m_configPosition = 0;

                    m_state = FriendlierState.ManageProject; // Manage the files in the project

                    // Copy current position to m_projectPosition - then rebuild model
                    //
                    m_projectPosition = m_project.getSelectedBufferView().getPosition();
                    m_projectPosition.X = -1000.0f;
                    m_projectPosition.Y = -1000.0f;

                    generateTreeModel();

                    // Fly to a new position in this mode to view the model
                    //
                    Vector3 newPos = m_projectPosition;
                    newPos.Z = 800.0f;
                    flyToPosition(newPos);
                    rC = true;
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
                    rC = true;
                }
                else if (checkKeyState(Keys.F, gameTime)) // Find text
                {
                    Logger.logMsg("Friendlier::processCombinationsCommands() - find");
                    m_state = FriendlierState.FindText;
                }
                else if (checkKeyState(Keys.L, gameTime)) // go to line
                {
                    Logger.logMsg("Friendlier::processCombinationsCommands() - goto line");
                    m_state = FriendlierState.GotoLine;
                }
            }
            else if (m_windowsDown) // Windows keys
            {
                // Initialially tried CTRL and ALT combinations but ran up against this:
                //
                // http://forums.create.msdn.com/forums/t/41522.aspx
                // 
                // and then this solution which I ignored:
                //
                // http://bnoerj.codeplex.com/wikipage?title=Bnoerj.Winshoked&referringTitle=Home
                //
                //
                if (checkKeyState(Keys.A, gameTime))
                {
                    Logger.logMsg("RELAYOUT AND FLY");  //???
                }
            }
            return rC;
        }



        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected override void Update(GameTime gameTime)
        {
            // Update the frustrum matrix
            //
            if (m_frustrum != null)
            {
                m_frustrum.Matrix = m_viewMatrix * m_projection;
            }

            if (m_processKeyboardAllowed != TimeSpan.Zero && gameTime.TotalGameTime < m_processKeyboardAllowed)
            {
                return;
            }

            // Return after these commands have been processed for the demo version
            //
            if (!m_project.getLicenced())
            {
                // Allow the game to exit
                //
                if (checkKeyState(Keys.Escape, gameTime))
                {
                    checkExit(gameTime, true);
                }

                return;
            }


            // Set the cursor to something useful
            //
            //System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.IBeam;

            // Set the startup banner on the first pass through
            //
            if (m_gameTime == null)
            {
                startBanner(gameTime, VersionInformation.getProductName() + "\n" + VersionInformation.getProductVersion(), 5);
            }

            // Store gameTime for use in helper functions
            //
            m_gameTime = gameTime;

            // Check for any mouse actions here
            //
            checkMouse(gameTime);

            // Process Escape keys and MetaCommands in this helper function
            if (processMetaCommands(gameTime))
            {
                // If we've got a key here then spin without processing keyboard input
                // for 50 milliseconds
                //
                m_processKeyboardAllowed = gameTime.TotalGameTime + new TimeSpan(0, 0, 0, 0, 50);
                return;
            }

            // Process meta keys (Shift, Alt, Ctrl) and set accordingly
            processMetaKeys();

            // Process action keys
            //
            processActionKeys(gameTime);


            // Don't process anything that's a runover character
            //
            //if (m_heldDownKeyValid == false && m_lastKeyboardState == Keyboard.GetState())
            //{
                //return;
            //}

            // Actions bound to key combinations - if we don't consume a key here then we print it
            // inside the processKeys() method.
            //
            if (!processCombinationsCommands(gameTime))
            {
                // Process keys that are left over if the above is false
                //
                processKeys(gameTime);
            }
            else
            {
                // If we've got a key here then spin without processing keyboard input
                // for 50 milliseconds
                //
                m_processKeyboardAllowed = gameTime.TotalGameTime + new TimeSpan(0, 0, 0, 0, 100);
            }

            // Check for this change as necessary
            //
            changeEyePosition(gameTime);

            // Save the last state if it has changed and clear any temporary message
            //
            if (m_lastKeyboardState != Keyboard.GetState())
            {
                m_lastKeyboardState = Keyboard.GetState();
            }

            // Save this to ensure we can keep processing
            //
            if (m_processKeyboardAllowed < gameTime.TotalGameTime)
            {
                m_processKeyboardAllowed = gameTime.TotalGameTime;
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Part of our botched keyboard managament routines
        /// </summary>
        protected Keys m_currentKeyDown;

        /// <summary>
        /// Process any keys that need to be printed
        /// </summary>
        /// <param name="gameTime"></param>
        protected void processKeys(GameTime gameTime)
        {
            // Do nothing if no keys are pressed except check for auto repeat and clear if necessary.
            // We have to adjust for any held down modifier keys here and also clear the variable as 
            // necessary.
            //
            if (Keyboard.GetState().GetPressedKeys().Length == 0 || 
                (Keyboard.GetState().GetPressedKeys().Length == 1 && (m_altDown || m_shiftDown || m_ctrlDown)))
            {
                //m_heldDownStartTime = gameTime.TotalGameTime.TotalSeconds;
                //m_heldDownLastRepeatTime = gameTime.TotalGameTime.TotalSeconds;
                m_heldDownKeyValid = false;
                return;
            }

            // We need to do a variety of tests here - test to see if a new key has been
            // pressed but also check to see if a held key is still being pressed.  If the
            // former is true then we set 'foundKey' and in the latter is true we have 
            // 'foundLastKey'.  Both have implications in terms of timing of key repeats.
            //
            //Keys keyDown = new Keys();
            bool foundKey = false;

            // Detect a key being hit that isn't one of the meta keys
            //
            foreach (Keys testKey in Keyboard.GetState().GetPressedKeys())
            {
                // Discard any processing if a meta key is involved
                //
                if (testKey == Keys.RightControl || testKey == Keys.LeftControl ||
                    testKey == Keys.LeftAlt || testKey == Keys.RightAlt)
                {
                    return;
                }
                else
                {
                    // Ignore shifts at this time
                    //
                    if (testKey != Keys.LeftShift && testKey != Keys.RightShift)
                    {
                        m_currentKeyDown = testKey;
                        foundKey = true;
                    }
                }
            }

            // Just return if we've not got anything
            //
            if (!foundKey)
            {
                return;
            }

            // Test for auto-repeating
            //
            if (m_heldKey != m_currentKeyDown || !m_heldDownKeyValid)
            {
                m_heldDownStartTime = gameTime.TotalGameTime.TotalSeconds;
                m_heldDownLastRepeatTime = gameTime.TotalGameTime.TotalSeconds;
                m_heldKey = m_currentKeyDown;
            }
            else
            {
                // We have a held down key - only repeat after the m_repeatHoldTime interval
                //
                if (gameTime.TotalGameTime.TotalSeconds - m_heldDownStartTime < m_repeatHoldTime)
                {
                    return;
                }

                if (gameTime.TotalGameTime.TotalSeconds - m_heldDownLastRepeatTime < m_repeatInterval)
                {
                    return;
                }

                // Set last repeat time
                //
                m_heldDownLastRepeatTime = gameTime.TotalGameTime.TotalSeconds;
                // If we're repeating then don't repeat too fast
                //
                //Logger.logMsg("Friendlier::processKeys() - got key repeat");
            }

            // At this point any held down key is valid for the next iteration
            //
            m_heldDownKeyValid = true;

            // Ok, let's see if we can translate a key
            //
            string key = "";

            switch (m_currentKeyDown)
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
                        key = "\"";
                    }
                    else
                    {
                        key = "'";
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
                        key = m_currentKeyDown.ToString().ToUpper();
                    }
                    else
                    {
                        key = m_currentKeyDown.ToString().ToLower();
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
                            key = m_currentKeyDown.ToString().ToUpper();
                        }
                        else
                        {
                            key = m_currentKeyDown.ToString().ToLower();
                        }
                    }
                    break;


                // Do nothing as default
                //
                default:
                    key = "";
                    //Logger.logMsg("Friendlier::update() - got key = " + keyDown.ToString());
                    break;
            }


            if (key != "")
            {
                //Logger.logMsg("Friendlier::processKeys() - processing key " + key);

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
                    m_project.getSelectedBufferView().appendToSearchText(key);
                }
                else if (m_state == FriendlierState.GotoLine)
                {
                    m_gotoLine += key;
                }
                else if (m_state == FriendlierState.TextEditing)
                {
                    // Do we need to do some deletion or replacing?
                    //
                    if (m_project.getSelectedBufferView().gotHighlight())
                    {
                        m_project.getSelectedBufferView().replaceCurrentSelection(m_project, key);
                    }
                    else
                    {
                        m_project.getSelectedBufferView().insertText(m_project, key);
                    }
                }
            }
        }

        /// <summary>
        /// Run a search on the current BufferView
        /// </summary>
        /// <returns></returns>
        protected void doSearch(GameTime gameTime)
        {
            m_state = FriendlierState.TextEditing;

            // Don't search for nothing
            //
            if (m_project.getSelectedBufferView().getSearchText() == "")
            {
                return;
            }

            // If we find something from cursor we're finished here
            //
            if (m_project.getSelectedBufferView().findFromCursor(false))
            {
                return;
            }

            // Now try to find from the top of the file
            //
            if (m_project.getSelectedBufferView().getCursorPosition().Y > 0)
            {
                // Try find from the top - if it finds something then let user know we've
                // wrapped around.
                //
                if (m_project.getSelectedBufferView().find(new ScreenPosition(0, 0), false))
                {
                    setTemporaryMessage("Search wrapped around end of file", 1.5f, gameTime);
                    return;
                }
            }

            setTemporaryMessage("\"" + m_project.getSelectedBufferView().getSearchText() + "\" not found", 3, gameTime);
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
        protected void setTemporaryMessage(string message, double seconds, GameTime gameTime)
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
            BufferView newBV = null;
            FileBuffer newFB = (filename == null ? new FileBuffer() : new FileBuffer(filename, readOnly));

            if (filename != null)
            {
                newFB.loadFile(m_project.getSyntaxManager());
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

            newBV = new BufferView(m_project.getFontManager(), newFB, newPos, 0, 20, fileIndex, readOnly);
            newBV.setTailing(tailFile);
            m_project.addBufferView(newBV);

            // Set the background colour
            //
            newBV.setBackgroundColour(m_project.getNewFileBufferColour());

            // Only do the following if tailing
            //
            if (!tailFile)
            {
                // We've add a new file so regenerate the model
                //
                generateTreeModel();

                // Now generate highlighting
                //
                if (m_project.getConfigurationValue("SYNTAXHIGHLIGHT").ToUpper() == "TRUE")
                {
                    m_project.getSyntaxManager().generateAllHighlighting(newFB);
                }
            }

            return newBV;
        }

        /// <summary>
        /// Find a free position around the active view
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        protected Vector3 getFreeBufferViewPosition(BufferView.ViewPosition position)
        {
            bool occupied = false;

            // Initial new pos is here from active BufferView
            //
            Vector3 newPos = m_project.getSelectedBufferView().calculateRelativePositionVector(position);
            do
            {
                occupied = false;

                foreach (BufferView cur in m_project.getBufferViews())
                {
                    if (cur.getPosition() == newPos)
                    {
                        // We get the next available slot in the same direction away from original
                        //
                        newPos = cur.calculateRelativePositionVector(position);
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
        protected void addBufferView(BufferView.ViewPosition position)
        {
            Vector3 newPos = getFreeBufferViewPosition(position);

            BufferView newBufferView = new BufferView(m_project.getFontManager(), m_project.getSelectedBufferView(), newPos);
            //newBufferView.m_textColour = Color.LawnGreen;
            m_project.addBufferView(newBufferView);
            setActiveBuffer(newBufferView);
        }


        /// <summary>
        /// Locate a BufferView located in a specified direction - if we find one then
        /// we set that as the active buffer view.
        /// </summary>
        /// <param name="position"></param>
        protected void detectMove(BufferView.ViewPosition position, GameTime gameTime)
        {
            // First get the position of a potential BufferView
            //
            BoundingBox searchBox = m_project.getSelectedBufferView().calculateRelativePositionBoundingBox(position);

            // Store the id of the current view
            //
            int fromView = m_project.getSelectedBufferViewId();

            // Search by index
            //
            for (int i = 0; i < m_project.getBufferViews().Count; i++)
            {
                if (m_project.getBufferViews()[i].getBoundingBox().Intersects(searchBox))
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
                setTemporaryMessage("No BufferView.", 2.0, gameTime);
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
                    // Result of any of our bounding checks
                    //

                    // Set up the flying vector for the first iteration
                    //
                    if (m_changingPositionLastGameTime == TimeSpan.Zero)
                    {
                        m_vFly = (m_newEyePosition - m_eye) / m_flySteps;

                        // Also set up the target modification vector thus
                        //
                        Vector3 tempTarget = m_newEyePosition;
                        tempTarget.Z = 0.0f;
                        m_vFlyTarget = (tempTarget - m_target) / m_flySteps;

                        //m_vFly.Normalize();
                        m_changingPositionLastGameTime = gameTime.TotalGameTime;
                    }

                    // As we close in on target ensure that we smoothly stop
                    //
                    /*
                    m_testArrived.Center = m_newEyePosition;
                    m_testArrived.Radius = 80.0f;

                    m_testArrived.Contains(ref m_eye, out m_testResult);
                    if (m_testResult == ContainmentType.Contains)
                    {
                        m_vFly *= 0.9f;
                    }
                    */

                    // Perform movement of the eye by the movement vector
                    //
                    if (gameTime.TotalGameTime - m_changingPositionLastGameTime > m_movementPause)
                    {
                        m_eye += m_vFly;

                        // modify target by the other vector
                        //
                        m_target.X += m_vFlyTarget.X;
                        m_target.Y += m_vFlyTarget.Y;
                        m_changingPositionLastGameTime = gameTime.TotalGameTime;
                        //m_view = Matrix.CreateLookAt(m_eye, Vector3.Zero, Vector3.Up);
#if DEBUG_FLYING
                        Logger.logMsg("Friendlier::changeEyePosition() - eye is now at " + m_eye.ToString());
                        Logger.logMsg("Friendlier::changeEyePosition() - final position is " + m_newEyePosition.ToString());
#endif
                    }

                    // Test arrival of the eye at destination position
                    //
                    m_testArrived.Center = m_newEyePosition;
                    m_testArrived.Radius = 1.0f;
                    m_testArrived.Contains(ref m_eye, out m_testResult);

                    if (m_testResult == ContainmentType.Contains)
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

        /// <summary>
        /// Gets a single key click - but also repeats if it's still held down after a while
        /// </summary>
        /// <param name="check"></param>
        /// <param name="gameTime"></param>
        /// <returns></returns>
        bool checkKeyState(Keys check, GameTime gameTime)
        {
            // Do we have any keys pressed down?  If not return
            //
            Keys[] keys = Keyboard.GetState(PlayerIndex.One).GetPressedKeys();
            if (keys.Length == 0 || check == Keys.LeftControl || check == Keys.RightControl ||
                check == Keys.LeftAlt || check == Keys.RightAlt)
                return false;

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
                if (gameTime.TotalGameTime.TotalSeconds - m_heldDownStartTime > m_repeatHoldTime ||
                    (m_state != FriendlierState.TextEditing &&
                    (gameTime.TotalGameTime.TotalSeconds - m_heldDownStartTime > m_repeatHoldTime / 2)))
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

        /// <summary>
        /// Get some rays to help us work out where the user is clicking
        /// </summary>
        /// <returns></returns>
        public Ray getPickRay()
        {
            MouseState mouseState = Mouse.GetState();

            int mouseX = mouseState.X;
            int mouseY = mouseState.Y;

            Vector3 nearsource = new Vector3((float)mouseX, (float)mouseY, m_zoomLevel);
            Vector3 farsource = new Vector3((float)mouseX, (float)mouseY, 0);

            Matrix world = Matrix.CreateScale(1, -1, 1); //Matrix.CreateTranslation(0, 0, 0);
            
            Vector3 nearPoint = m_graphics.GraphicsDevice.Viewport.Unproject(nearsource, m_projection, m_viewMatrix, world);
            Vector3 farPoint = m_graphics.GraphicsDevice.Viewport.Unproject(farsource, m_projection, m_viewMatrix, world);

            //farPoint.X = nearPoint.X;
            //farPoint.Y = nearPoint.Y;

            // Create a ray from the near clip plane to the far clip plane.
            //
            Vector3 direction = farPoint - nearPoint;
            direction.Normalize();
            Ray pickRay = new Ray(nearPoint, direction);

            return pickRay;
        }

        /// <summary>
        /// Double click handler
        /// </summary>
        protected void handleDoubleClick()
        {
            Logger.logMsg("Friendlier::checkMouse() - got double click");
            Pair<BufferView, Pair<ScreenPosition, ScreenPosition>> testFind = m_project.testRayIntersection(getPickRay());

            BufferView bv = (BufferView)testFind.First;
            ScreenPosition fp = (ScreenPosition)testFind.Second.First;
            ScreenPosition screenRelativePosition = (ScreenPosition)testFind.Second.Second;

            // Check for validity of bv and position here
            //
            if (bv == null || fp.Y < 0 || fp.Y > bv.getFileBuffer().getLineCount() || fp.X < 0 || fp.X >= bv.getFileBuffer().getLine(fp.Y).Length) // do nothing
                return;

            if (bv.isTailing())
            {
                handleTailingDoubleClick(bv, fp, screenRelativePosition);
            }
            else
            {
                handleStandardDoubleClick(bv, fp, screenRelativePosition);
            }
        }

        /// <summary>
        /// At this point we should have two BufferView - one selected and one we've clicked on.
        /// Now we can run a diff on the two and present some results.  Somehow.
        /// </summary>
        /// <param name="gameTime"></param>
        protected void handleDiffPick(GameTime gameTime)
        {
            Pair<BufferView, Pair<ScreenPosition, ScreenPosition>> testFind = m_project.testRayIntersection(getPickRay());

            // We're only really interesed in the BufferView
            //
            if (testFind.First != null)
            {
                BufferView bv1 = m_project.getSelectedBufferView();
                BufferView bv2 = testFind.First;

                if (bv1 != bv2)
                {
                    // Just set the views we're comparing and generate a diff.
                    // This is then drawn later on in this file.
                    //

                    // If the x positions are the wrong way around then convert them
                    //
                    if (bv1.getEyePosition().X > bv2.getEyePosition().X)
                    {
                        BufferView swap = bv1;
                        bv1 = bv2;
                        bv2 = swap;
                    }

                    // Set the two sides of the diff
                    //
                    m_project.setLHSDiff(bv1);
                    m_project.setRHSDiff(bv2);
                        
#if USING_DIFFVIEW
                    DiffView diffView = new DiffView(bv1, bv2);
                    diffView.initialise(m_graphics, m_project);
                    if (diffView.process())
                    {
                        m_project.addGenericView(diffView);

                        // Show the differences and break out a new diff view
                        //
                        setActiveBuffer(diffView);
                    }
                    else
                    {
                        setTemporaryMessage("No differences found.", 3, gameTime);
                    }
#endif
                    // Set the BufferViews
                    //
                    m_differ.setBufferViews(bv1, bv2);

                    if (!m_differ.process())
                    {
                        setTemporaryMessage("No differences found.", 3, gameTime);

                        // Set state back to default
                        //
                        m_state = FriendlierState.TextEditing;
                    }
                    else
                    {
                        setTemporaryMessage("Diff selected", 1.5f, gameTime);

                        // Now set up the position of the diff previews and make sure we've
                        // pregenerated the lines
                        //
                        Vector2 leftBox = Vector2.Zero;
                        leftBox.X = (int)((m_graphics.GraphicsDevice.Viewport.Width / 2) - m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay) * 20);
                        leftBox.Y = (int)(m_project.getFontManager().getLineSpacing(FontManager.FontType.Overlay) * 3);

                        Vector2 leftBoxEnd = leftBox;
                        leftBoxEnd.X += (int)(m_project.getFontManager().getCharWidth() * 16);
                        leftBoxEnd.Y += (int)(m_project.getFontManager().getLineSpacing() * 10);

                        Vector2 rightBox = Vector2.Zero;
                        rightBox.X = (int)((m_graphics.GraphicsDevice.Viewport.Width / 2) + m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay) * 4);
                        rightBox.Y = leftBox.Y;

                        Vector2 rightBoxEnd = rightBox;
                        rightBoxEnd.X += (int)(m_project.getFontManager().getCharWidth() * 16);
                        rightBoxEnd.Y += (int)(m_project.getFontManager().getLineSpacing() * 10);

                        // Now generate some previews and store these positions
                        //
                        m_differ.generateDiffPreviews(leftBox, leftBoxEnd, rightBox, rightBoxEnd);

                        // Store the diff position as the left hand current cursor position - still need
                        // to check that this is valid and correct when it is expanded into the 'real' diff
                        // position.
                        //
                        m_diffPosition = m_differ.originalLhsFileToDiffPosition(m_project.getSelectedBufferView().getBufferShowStartY());

                        // Now we want to fly to the mid position between the two views - we have to assume they are
                        // next to each other for this to make sense.  Only do this if it's configured as such.
                        //
                        if (m_project.getConfigurationValue("DIFFCENTRE").ToUpper() == "TRUE")
                        {
                            Vector3 look1 = (bv1.getEyePosition() + bv2.getEyePosition()) / 2;
                            look1.Z = bv1.getEyePosition().Z * 1.8f;
                            flyToPosition(look1);
                        }
                    }
                }
                else
                {
                    setTemporaryMessage("Can't diff a BufferView with itself.", 3, gameTime);

                    // Set state back to default
                    //
                    m_state = FriendlierState.TextEditing;
                }
            }
        }

        /// <summary>
        /// Handle a single left button mouse click
        /// </summary>
        /// <param name="gameTime"></param>
        protected void handleSingleClick(GameTime gameTime)
        {
            Pair<BufferView, Pair<ScreenPosition, ScreenPosition>> testFind = m_project.testRayIntersection(getPickRay());

            // Have we got a valid intersection?
            //
            if (testFind.First != null && testFind.Second.First != null)
            {
                BufferView bv = (BufferView)testFind.First;
                ScreenPosition fp = (ScreenPosition)testFind.Second.First;

                if (m_state == FriendlierState.DiffPicker)
                {
                    ScreenPosition newSP = bv.testCursorPosition(new FilePosition(fp));

                    // Do something with it like a highlight or something?
                    //
                }
                else
                {
                    ScreenPosition sp = bv.testCursorPosition(new FilePosition(fp));

                    if (sp.X != -1 && sp.Y != -1 )
                    {
                        setActiveBuffer(bv);
                        bv.mouseCursorTo(m_shiftDown, sp);
                    }
                }
            }
        }
        
        /// <summary>
        /// How we handle a double click on a normal canvas when we're editing - accept the BufferView
        /// we've clicked on, the ScreenPosition (FilePosition with expanded tabs) and also the screen
        /// relative position.
        /// </summary>
        /// <param name="bv"></param>
        /// <param name="fp"></param>
        /// <param name="screenRelativePosition"></param>
        protected void handleStandardDoubleClick(BufferView bv, ScreenPosition fp, ScreenPosition screenRelativePosition)
        {
            Logger.logMsg("Friendlier::handleStandardDoubleClick()");

            // We need to identify a valid line
            //
            if (fp.Y >= bv.getFileBuffer().getLineCount())
                return;

            // All we need to do here is find the line, see if we're clicking in a word and
            // highlight it if so.
            //
            string line = bv.getFileBuffer().getLine(fp.Y);

            // Are we on a space?  If not highlight the word
            //
            if (fp.X < line.Length && line[fp.X] != ' ')
            {
                // Find first and last occurences of spaces after splitting the string effectively
                int startWord = line.Substring(0, fp.X).LastIndexOf(' ') + 1;
                int endWord = line.Substring(fp.X, line.Length - fp.X).IndexOf(' ');

                // Adjust for no space to end of line
                //
                if (endWord == -1)
                {
                    endWord = line.Length;
                }
                else
                {
                    endWord += fp.X;
                }

                ScreenPosition sp1 = new ScreenPosition(startWord, fp.Y);
                ScreenPosition sp2 = new ScreenPosition(endWord, fp.Y);
                bv.setHighlight(sp1, sp2);
            }
        }

        /// <summary>
        /// Help to activate, set cursor and center a highlighted row on the active BufferView
        /// </summary>
        /// <param name="bv"></param>
        /// <param name="sp"></param>
        protected void setHighlightAndCenter(BufferView bv, ScreenPosition sp)
        {
            string line = bv.getFileBuffer().getLine(sp.Y);

            setActiveBuffer(bv);
            bv.setCursorPosition(sp);
            bv.setBufferShowStartY(sp.Y - bv.getBufferShowLength() / 2);

            ScreenPosition sp1 = new ScreenPosition(0, sp.Y);
            ScreenPosition sp2 = new ScreenPosition(line.Length, sp.Y);
            bv.setHighlight(sp1, sp2);
        }

        /// <summary>
        /// How we handle a double click on a tailing view
        /// </summary>
        /// <param name="bv"></param>
        /// <param name="fp"></param>
        /// <param name="screenRelativePosition"></param>
        protected void handleTailingDoubleClick(BufferView bv, ScreenPosition fp, ScreenPosition screenRelativePosition)
        {
            Logger.logMsg("Friendlier::handleTailingDoubleClick()");
            ScreenPosition testSp = bv.testCursorPosition(new FilePosition(fp));

            if (testSp.X == -1 && testSp.Y == -1)
            {
                Logger.logMsg("Friendlier::handleTailingDoubleClick() - failed in testCursorPosition");
            }
            else
            {
                // Fetch the line indicated from the file into the line variable
                //
                string line = "";
                try
                {
                    line = bv.getFileBuffer().getLine(fp.Y);
                    Logger.logMsg("Friendlier::handleTailingDoubleClick() - got a line = " + line);
                }
                catch (Exception)
                {
                    Logger.logMsg("Friendlier::handleTailingDoubleClick() - couldn't fetch line " + fp.Y);
                }

                
                // Look for a FileBuffer indicated in this line - we look up the filename from
                // the text and seek this filename in the FileBuffers.  If we find one then we
                // zap to it
                //
                Pair<FileBuffer, ScreenPosition> found = m_project.findFileBufferFromText(line, m_modelBuilder);

                if (found.First != null)  // Found one.
                {
                    FileBuffer fb = (FileBuffer)found.First;
                    ScreenPosition sp = (ScreenPosition)found.Second;

                    // Try to find a BufferView for this FileBuffer
                    //
                    bv = m_project.getBufferView(fb.getFilepath());

                    // Adjust line by 1 - hardcode this for QtCreator for the moment
                    //
                    if (sp.Y > 0)
                    {
                        sp.Y--;
                    }

                    // If we have one then zap to it
                    //
                    if (bv != null)
                    {
                        try
                        {
                            Logger.logMsg("Friendlier::handleTailingDoubleClick() - trying to active BufferView and zap to line");
                            setHighlightAndCenter(bv, sp);
                        }
                        catch (Exception)
                        {
                            Logger.logMsg("Friendlier::handleTailingDoubleClick() - couldn't activate and zap to line in file");
                        }
                    }
                    else 
                    {
                        // Create a new FileBuffer at calculated position and add it to the project
                        //
                        Vector3 newPos = m_project.getBestBufferViewPosition(m_project.getSelectedBufferView());
                        BufferView newBV = new BufferView(m_project.getFontManager(), fb, newPos, 0, 20, m_project.getFileIndex(fb), false);
                        int index = m_project.addBufferView(newBV);

                        int fileIndex = m_project.getFileIndex(fb);
                        newBV.setFileBufferIndex(fileIndex);

                        // Load the file
                        //
                        fb.loadFile(m_project.getSyntaxManager());

                        // Activate and centre
                        //
                        setHighlightAndCenter(newBV, sp);

                        return;
                    }
                }
                else // not found anything - look on the filesystem
                {
                    Logger.logMsg("Friendlier::handleTailingDoubleClick() - inspecting filesystem");

                    // By default get the build directory - we'll probably want to change ths
                    //
                    string baseDir = m_project.getConfigurationValue("BUILDDIRECTORY");

                    // If we have retrieved a line to test
                    //
                    if (line != "") 
                    {
                        // The getFileNamesFromText tries to find a filename and a FilePosition as well as
                        // an adjusted relative Scr

                        List<Pair<string, Pair<int, int>>> filePositionList = m_project.getFileNamesFromText(line);

                        foreach (Pair<string, Pair<int, int>> fpEntry in filePositionList)
                        {
                            // Clear the storage out first
                            //
                            m_fileSystemView.clearSearchDirectories();

                            // Do a search
                            //
                            m_fileSystemView.directorySearch(baseDir, fpEntry.First);

                            // Get results
                            //
                            List<string> rL = m_fileSystemView.getSearchDirectories();

                            if (rL.Count > 0)
                            {
                                Logger.logMsg("Friendlier::handleTailingDoubleClick() - got " + rL.Count + " matches for file " + fpEntry.First);

                                // Set a highlight on the current BufferView
                                //
                                int xPosition = line.IndexOf(fpEntry.First);

                                // Set up the m_clickHighlight
                                //
                                m_clickHighlight.First = m_project.getSelectedBufferView();
                                m_clickHighlight.Second = new Highlight(screenRelativePosition.Y, xPosition, xPosition + fpEntry.First.Length, fpEntry.First, HighlightType.UserHighlight);

                                // Open file and zap to it
                                //
                                BufferView newBv = addNewFileBuffer(rL[0]);
                                ScreenPosition sp = new ScreenPosition(fpEntry.Second.Second, fpEntry.Second.First);
                                setHighlightAndCenter(newBv, sp);
                                break;
                            }
                        }
                    }
                }

                // Do something with the result
                //
                return;
            }
        }

        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        /// <summary>
        /// Handle mouse click and double clicks and farm out the responsibility to other
        /// helper methods.
        /// </summary>
        /// <param name="gameTime"></param>
        public void checkMouse(GameTime gameTime)
        {
            MouseState mouseState = Mouse.GetState();

            // If our main XNA window is inactive then ignore mouse clicks
            //
            if (IsActive == false) return;

            // If we are flying somewhere then ignore mouse clicks
            //
            if (m_changingEyePosition) return;

            // Left button
            //
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
#if SAM_MOUSE_TEST
                if (sw.IsRunning)
                {
                    sw.Stop();
                    //setTemporaryMessage("Time since last mouse click was " + sw.ElapsedMilliseconds + "ms", 2, gameTime);
                    Logger.logMsg("Time since last mouse click was " + sw.ElapsedMilliseconds + "ms");
                }
                else
                {
                    sw.Reset();
                    sw.Start();
                }
#endif
                

                // Get the pick ray
                //
                Ray pickRay = getPickRay();
                int mouseX = mouseState.X;
                int mouseY = mouseState.Y;

                if (m_lastMouseState.LeftButton == ButtonState.Released)
                {
                    // Handle double clicks
                    //
                    m_lastClickPosition.X = mouseX;
                    m_lastClickPosition.Y = mouseY;
                    m_lastClickPosition.Z = 0;

                    // Double click fired
                    //
                    if ((gameTime.TotalGameTime - m_lastClickTime).TotalSeconds < 0.25f)
                    {
                        handleDoubleClick();

                        // If we return early then make sure we set m_lastMousState
                        //
                        m_lastMouseState = mouseState;

                        return;
                    }

                    m_lastClickVector = pickRay.Direction;
                    m_lastClickTime = gameTime.TotalGameTime;

                    m_lastClickEyePosition = m_eye;// m_project.getSelectedBufferView().getEyePosition(m_zoomLevel);

                    Logger.logMsg("Friender::checkMouse() - mouse clicked");
                }
                else  // We have held down the left button - so pan
                {
                    // Do panning - first set cursor to a hand
                    //
                    System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Hand;

                    // We are dragging - work out the rate
                    //
                    double deltaX = pickRay.Position.X - m_lastClickPosition.X;
                    double deltaY = pickRay.Position.Y - m_lastClickPosition.Y;

                    // Vector and angle
                    //
                    double dragVector = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                    double dragAngle = Math.Atan2(deltaY, deltaX);

                    Vector3 nowPosition = Vector3.Zero;
                    nowPosition.X = mouseState.X;
                    nowPosition.Y = mouseState.Y;
                    nowPosition.Z = 0;

                    Vector3 diffPosition = (nowPosition - m_lastClickPosition);
                    diffPosition.Z = 0;

                    //Logger.logMsg("Friendlier::checkMouse() - mouse dragged: X = " + diffPosition.X + ", Y = " + diffPosition.Y);

                    float multiplier = m_zoomLevel / m_zoomLevel;
                    m_eye.X = m_lastClickEyePosition.X - diffPosition.X * multiplier;
                    m_eye.Y = m_lastClickEyePosition.Y + diffPosition.Y * multiplier;

                    // If shift isn't down then we pan with the eye movement
                    //
                    if (!m_shiftDown)
                    {
                        m_target.X = m_eye.X;
                        m_target.Y = m_eye.Y;
                    }
                }

            }
            else if (mouseState.LeftButton == ButtonState.Released)
            {
                if (m_lastMouseState.LeftButton == ButtonState.Pressed) // Have just released on a single click
                {
                    if ((gameTime.TotalGameTime - m_lastClickTime).TotalSeconds < 0.15f)
                    {

                        if (m_state == FriendlierState.DiffPicker)
                        {
                            if (m_differ != null && m_differ.hasDiffs())
                            {
                                // We have a diff pick - let's do something to the view
                                //
                                handleSingleClick(gameTime);
                            }
                            else
                            {
                                // Generate a diff pick
                                //
                                handleDiffPick(gameTime);
                            }
                        }
                        else
                        {
                            handleSingleClick(gameTime);
                        }
                    }
                    else // we've done a long click and release we're dragging - on the release handle the drag result
                    {
                        // At this point test to see which bufferview centre we're nearest and if we're near a
                        // different one then switch to that.
                        //
                        BufferView newView = m_project.testNearBufferView(m_eye);

                        if (newView != null)
                        {
                            //Logger.logMsg("Friendlier::checkMouse() - switching to new buffer view");
                            setActiveBuffer(newView);
                        }
                        else
                        {
                            // Flyback to our existing view
                            //
                            //m_eye = m_lastClickEyePosition;
                            //m_target.X = m_lastClickEyePosition.X;
                            //m_target.Y = m_lastClickEyePosition.Y;
                            flyToPosition(m_lastClickEyePosition);
                        }

                        // Set default cursor back
                        //
                        System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
                    }
                }
            }

            // Mouse scrollwheel
            //
            if (m_lastMouseWheelValue != Mouse.GetState().ScrollWheelValue)
            {
                //Logger.logMsg("Friendlier::checkMouse() - mouse wheel value now = " + Mouse.GetState().ScrollWheelValue);

                // If shift down then scroll current view - otherwise zoom in/out
                //
                if (m_shiftDown)
                {
                    int linesDown = -(int)((m_lastMouseWheelValue - Mouse.GetState().ScrollWheelValue) / 120.0f);

                    if (linesDown < 0)
                    {
                        for (int i = 0; i < -linesDown; i++)
                        {
                            m_project.getSelectedBufferView().moveCursorDown(false);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < linesDown; i++)
                        {
                            m_project.getSelectedBufferView().moveCursorUp(m_project, false);
                        }
                    }
                }
                else
                {
                    float newZoomLevel = m_zoomLevel + (m_zoomStep * ((m_lastMouseWheelValue - Mouse.GetState().ScrollWheelValue) / 120.0f));
                    setZoomLevel(newZoomLevel);
                }

                m_lastMouseWheelValue = Mouse.GetState().ScrollWheelValue;
            }

            // Check for the release of a sizing move
            //
            if (Mouse.GetState().LeftButton == ButtonState.Released && m_isResizing)
            {
                m_isResizing = false;
            }

            // Store the last mouse state
            //
            m_lastMouseState = mouseState;

        }

        private bool m_flipFlop = true;
        private double m_nextLicenceMessage = 0.0f;

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // Draw onto the Bloom component - this is the only modification we need to make
            //
            m_bloom.BeginDraw();

            // If we're not licenced then render this
            //
            if (!m_project.getLicenced())
            {
                if (gameTime.TotalGameTime.TotalSeconds> m_nextLicenceMessage)
                {
                    if (m_flipFlop)
                    {
                        setTemporaryMessage("Friendlier demo period has expired.", 3, gameTime);
                    }
                    else
                    {
                        setTemporaryMessage("Please see www.xyglo.com for licencing details.", 3, gameTime);
                    }

                    m_flipFlop = !m_flipFlop;
                    m_nextLicenceMessage = gameTime.TotalGameTime.TotalSeconds + 5;
                }
                //renderTextScroller();
            }
            else
            {
                // Set the welcome message once
                //
                if (m_flipFlop)
                {
                    setTemporaryMessage(VersionInformation.getProductName() + " " + VersionInformation.getProductVersion(), 3, gameTime);
                    m_flipFlop = false;
                }
            }

            // Set background colour
            //
            m_graphics.GraphicsDevice.Clear(Color.Black);

            // If we are resizing then do nothing
            //
            if (m_isResizing)
            {
                base.Draw(gameTime);
                return;
            }

            // If spinning then spin around current position based on time.
            //
            if (m_spinning)
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

            // Generate frustrum
            //
            if (m_frustrum == null)
            {
                m_frustrum = new BoundingFrustum(m_viewMatrix * m_projection);
            }
            else
            {
                // You can also update frustrum matrix like this
                //
                m_frustrum.Matrix = m_viewMatrix * m_projection;
            }

            m_basicEffect.World = Matrix.CreateScale(1, -1, 1);
            m_basicEffect.View = m_viewMatrix;
            m_basicEffect.Projection = m_projection;

            m_lineEffect.View = m_viewMatrix;
            m_lineEffect.Projection = m_projection;
            m_lineEffect.World = Matrix.CreateScale(1, -1, 1);

            // In the manage project mode we zoom off into the distance
            //
            if (m_state == FriendlierState.ManageProject)
            {
                drawManageProject(gameTime);
                base.Draw(gameTime);
                return;
            }


            // Here we need to vary the parameters to the SpriteBatch - to the BasicEffect and also the font size.
            // For large fonts we need to be able to downscale them effectively so that they will still look good
            // at higher reoslutions.
            //
            m_spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.DepthRead, RasterizerState.CullNone, m_basicEffect);

            // Draw all the BufferViews for all remaining modes
            //
            for (int i = 0; i < m_project.getBufferViews().Count; i++)
            {
                if (m_differ != null && m_differ.hasDiffs() &&
                    (m_differ.getSourceBufferViewLhs() == m_project.getBufferViews()[i] ||
                        m_differ.getSourceBufferViewRhs() == m_project.getBufferViews()[i]))
                {
                    drawDiffBuffer(m_project.getBufferViews()[i], gameTime);
                }
                else
                {
                    // We have to invert the BoundingBox along the Y axis to ensure that
                    // it matches with the frustrum we're culling against.
                    //
                    BoundingBox bb = m_project.getBufferViews()[i].getBoundingBox();
                    bb.Min.Y = -bb.Min.Y;
                    bb.Max.Y = -bb.Max.Y;

                    // We only do frustrum culling for BufferViews for the moment
                    // - intersects might be too grabby but Disjoint didn't appear 
                    // to be grabby enough.
                    //
                    //if (m_frustrum.Contains(bb) != ContainmentType.Disjoint)
                    if (m_frustrum.Intersects(bb))
                    {
                        drawFileBuffer(m_project.getBufferViews()[i], gameTime);
                    }

                    // Draw a background square for all buffer views if they are coloured
                    //
                    if (m_project.getViewMode() == Project.ViewMode.Coloured)
                    {
                        m_drawingHelper.renderQuad(m_project.getBufferViews()[i].getTopLeft(), m_project.getBufferViews()[i].getBottomRight(), m_project.getBufferViews()[i].getBackgroundColour(), m_spriteBatch);
                    }
                }
            }

            // We only draw the scrollbar on the active view in the right mode
            //
            if (m_state == FriendlierState.TextEditing)
            {
                drawScrollbar(m_project.getSelectedBufferView());
            }

            // Cursor and cursor highlight
            //
            if (m_state == FriendlierState.TextEditing)
            {
                // Stop and use a different spritebatch for the highlighting and cursor
                //
                m_spriteBatch.End();
                m_spriteBatch.Begin(SpriteSortMode.Texture, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.DepthRead, RasterizerState.CullNone, m_basicEffect);

                drawCursor(gameTime, m_spriteBatch);
                m_drawingHelper.drawHighlight(gameTime, m_spriteBatch);
            }

            m_spriteBatch.End();

            // Draw our generic views
            //
            foreach (XygloView view in m_project.getGenericViews())
            {
                view.draw(m_project, m_state, gameTime, m_spriteBatch, m_basicEffect);
            }

            // If we're choosing a file then
            //
            if (m_state == FriendlierState.FileSaveAs || m_state == FriendlierState.FileOpen || m_state == FriendlierState.PositionScreenOpen || m_state == FriendlierState.PositionScreenNew || m_state == FriendlierState.PositionScreenCopy)
            {
                drawDirectoryChooser(gameTime);

            }
            else if (m_state == FriendlierState.Help)
            {
                drawTextScreen(gameTime, m_userHelp);
            }
            else if (m_state == FriendlierState.Information)
            {
                drawInformationScreen(gameTime);
            }
            else if (m_state == FriendlierState.Configuration)
            {
                drawConfigurationScreen(gameTime);
            }
            else
            {
                // http://forums.create.msdn.com/forums/p/61995/381650.aspx
                //
                m_overlaySpriteBatch.Begin();

                // Draw the Overlay HUD
                //
                drawOverlay(gameTime, m_overlaySpriteBatch);

                // Draw map of BufferViews
                //
                m_drawingHelper.drawBufferViewMap(gameTime, m_overlaySpriteBatch);

                m_overlaySpriteBatch.End();

                // Draw any differ overlay
                //
                m_pannerSpriteBatch.Begin(SpriteSortMode.Texture, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.DepthRead, RasterizerState.CullNone /*, m_pannerEffect */ );

                // Draw the differ
                //
                drawDiffer(gameTime, m_pannerSpriteBatch);

                // Draw system load
                //
                drawSystemLoad(gameTime, m_pannerSpriteBatch);

                m_pannerSpriteBatch.End();
            }

            // Draw the textures for generic views
            //
            //foreach (XygloView view in m_project.getGenericViews())
            //{
                //view.drawTextures(m_basicEffect);
            //}

            // Draw a welcome banner
            //
            if (m_bannerStartTime != -1 && m_project.getViewMode() != Project.ViewMode.Formal)
            {
                drawBanner(gameTime);
            }

            // Any Kinect information to share
            //
            drawKinectInformation();

            base.Draw(gameTime);
        }

        protected void drawKinectInformation()
        {
#if GOT_KINECT
            if (m_kinectManager.gotUser())
            {
                string skeletonPosition = m_kinectManager.getSkeletonDetails();

                m_overlaySpriteBatch.Begin();

                // hardcode the font size to 1.0f so it looks nice
                //
                m_overlaySpriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), skeletonPosition, new Vector2(50.0f, 50.0f), Color.White, 0, Vector2.Zero, 1.0f, 0, 0);
                //m_overlaySpriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), eyePosition, new Vector2(0.0f, 0.0f), overlayColour, 0, Vector2.Zero, 1.0f, 0, 0);
                //m_overlaySpriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), modeString, new Vector2((int)modeStringXPos, 0.0f), overlayColour, 0, Vector2.Zero, 1.0f, 0, 0);
                //m_overlaySpriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), positionString, new Vector2((int)positionStringXPos, (int)yPos), overlayColour, 0, Vector2.Zero, 1.0f, 0, 0);
                //m_overlaySpriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), filePercentString, new Vector2((int)filePercentStringXPos, (int)yPos), overlayColour, 0, Vector2.Zero, 1.0f, 0, 0);
                m_overlaySpriteBatch.End();
            }
#endif // GOT_KINECT
        }


        /// <summary>
        /// Draw an overview of the project from a file perspective and allow modification
        /// </summary>
        protected void drawManageProject(GameTime gameTime)
        {
            string text = "";

            // The maximum width of an entry in the file list
            //
            int maxWidth = ((int)((float)m_project.getSelectedBufferView().getBufferShowWidth() * 0.9f));

            // This is very simply modelled at the moment
            //
            foreach(string fileName in m_modelBuilder.getReturnString().Split('\n'))
            {
                // Ignore the last split
                //
                if (fileName != "")
                {
                    if ((m_modelBuilder.getRootString().Length + fileName.Length) < maxWidth)
                    {
                        text += m_modelBuilder.getRootString() + fileName + "\n";
                    }
                    else
                    {
                        //text += m_project.buildFileString(m_modelBuilder.getRootString(), fileName, maxWidth) + "\n";
                        text += m_project.estimateFileStringTruncation(m_modelBuilder.getRootString(), fileName, maxWidth) + "\n";
                    }

                }
            }

            if (text == "")
            {
                text = "[Project contains no Files]";
            }

            // Draw the main text screen - using the m_configPosition as the place holder
            //
            drawTextScreen(gameTime, text, 0, m_configPosition);

            // Some key help
            //
            string commandText = "[Delete] - remove file from project\n";
            commandText += "[Insert]  - edit file";

            FilePosition fp =
                new FilePosition(m_project.getSelectedBufferView().getBufferShowWidth()/2 - commandText.Split('\n')[0].Length/2,
                                 m_project.getSelectedBufferView().getBufferShowLength() + 9);
            drawTextOverlay(fp, commandText, Color.LightCoral);
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
        /// Draw temporary message by fade in/fade out
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="overlayColour"></param>
        protected void drawTemporaryMessage(GameTime gameTime, Color overlayColour)
        {
            if (m_temporaryMessage == "" || gameTime.TotalGameTime.TotalSeconds > m_temporaryMessageEndTime)
            {
                return;
            }

            // Now calculate the colour according to the time - fade in/fade out is currently linear
            //
            Color fadeColour = overlayColour;
            
            float blankTime = 0.1f; // seconds
            float fadeTime = 0.4f; // seconds
            if (gameTime.TotalGameTime.TotalSeconds - m_temporaryMessageStartTime < blankTime)
            {
                fadeColour = Color.Black;
                fadeColour.A = 0;
            }
            else if (gameTime.TotalGameTime.TotalSeconds - m_temporaryMessageStartTime < fadeTime)
            {
                double percent = (gameTime.TotalGameTime.TotalSeconds - m_temporaryMessageStartTime - blankTime) / (fadeTime - blankTime);
                fadeColour.R = (byte)((double)overlayColour.R * percent);
                fadeColour.G = (byte)((double)overlayColour.G * percent);
                fadeColour.B = (byte)((double)overlayColour.B * percent);
                fadeColour.A = (byte)((double)overlayColour.A * percent);
            }
            else if (gameTime.TotalGameTime.TotalSeconds > m_temporaryMessageEndTime - fadeTime)
            {
                double percent = (m_temporaryMessageEndTime - gameTime.TotalGameTime.TotalSeconds) / fadeTime;
                fadeColour.R = (byte)((double)overlayColour.R * percent);
                fadeColour.G = (byte)((double)overlayColour.G * percent);
                fadeColour.B = (byte)((double)overlayColour.B * percent);
                fadeColour.A = (byte)((double)overlayColour.A * percent);
            }

            // How many lines are we going to show for this temporary message?
            //
            List<string> splitString = splitStringNicely(m_temporaryMessage, m_project.getSelectedBufferView().getBufferShowWidth());

            // Set x and Y accordingly
            //
            float yPos = m_graphics.GraphicsDevice.Viewport.Height - ((splitString.Count + 3) * m_project.getFontManager().getOverlayFont().LineSpacing);

            //m_overlaySpriteBatch.Begin();
            for (int i = 0; i < splitString.Count; i++)
            {
                float xPos = m_graphics.GraphicsDevice.Viewport.Width / 2 - m_project.getFontManager().getOverlayFont().MeasureString("X").X * splitString[i].Length / 2;
                m_overlaySpriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), splitString[i], new Vector2(xPos, yPos), fadeColour, 0, Vector2.Zero, 1.0f, 0, 0);
                yPos += m_project.getFontManager().getOverlayFont().LineSpacing;
            }
            //m_overlaySpriteBatch.End();
        }


        /// <summary>
        /// Draw the HUD Overlay for the editor with information about the current file we're viewing
        /// and position in that file.
        /// </summary>
        protected void drawOverlay(GameTime gameTime, SpriteBatch spriteBatch)
        {
#if SCROLLING_TEXT
            // Flag that we set during this method
            //
            bool drawScrollingText = false;
#endif

            // Set our colour according to the state of Friendlier
            //
            Color overlayColour = Color.White;
            if (m_state != FriendlierState.TextEditing && m_state != FriendlierState.GotoLine && m_state != FriendlierState.FindText && m_state != FriendlierState.DiffPicker)
            {
                overlayColour = m_greyedColour; 
            }

            // Set up some of these variables here
            //
            string positionString = m_project.getSelectedBufferView().getCursorPosition().Y + "," + m_project.getSelectedBufferView().getCursorPosition().X;
            float positionStringXPos = m_graphics.GraphicsDevice.Viewport.Width - positionString.Length * m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay) - (m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay) * 14);
            float filePercent = 0.0f;

            // Filename is where we put the filename plus other assorted gubbins or we put a
            // search string in there depending on the mode.
            //
            string fileName = "";

            if (m_state == FriendlierState.FindText)
            {
                // Draw the search string down there
                fileName = "Search: " + m_project.getSelectedBufferView().getSearchText();
            }
            else if (m_state == FriendlierState.GotoLine)
            {
                fileName = "Goto line: " + m_gotoLine;
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

            }

            // Convert lineHeight back to normal size by dividing by m_textSize modifier
            //
            float yPos = m_graphics.GraphicsDevice.Viewport.Height - (m_project.getFontManager().getLineSpacing() / m_project.getFontManager().getTextScale());

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

                case FriendlierState.DiffPicker:
                    modeString = "performing diff";
                    break;

                default:
                    modeString = "free";
                    break;
            }

            float modeStringXPos = m_graphics.GraphicsDevice.Viewport.Width - modeString.Length * m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay) - (m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay) * 8);

            if (m_project.getSelectedBufferView().getFileBuffer() != null && m_project.getSelectedBufferView().getFileBuffer().getLineCount() > 0)
            {
                filePercent = (float)(m_project.getSelectedBufferView().getCursorPosition().Y) /
                              (float)(Math.Max(1, m_project.getSelectedBufferView().getFileBuffer().getLineCount() - 1));
            }

            string filePercentString = ((int)(filePercent * 100.0f)) + "%";
            float filePercentStringXPos = m_graphics.GraphicsDevice.Viewport.Width - filePercentString.Length * m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay) - (m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay) * 3);

            // Debug eye position
            //
            if (m_project.getViewMode() != Project.ViewMode.Formal)
            {
                string eyePosition = "[EyePosition] X " + m_eye.X + ",Y " + m_eye.Y + ",Z " + m_eye.Z;
                float xPos = m_graphics.GraphicsDevice.Viewport.Width - eyePosition.Length * m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay);
                spriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), eyePosition, new Vector2(0.0f, 0.0f), overlayColour, 0, Vector2.Zero, 1.0f, 0, 0);
            }

            // hardcode the font size to 1.0f so it looks nice
            //
            spriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), fileName, new Vector2(0.0f, (int)yPos), overlayColour, 0, Vector2.Zero, 1.0f, 0, 0);
            spriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), modeString, new Vector2((int)modeStringXPos, 0.0f), overlayColour, 0, Vector2.Zero, 1.0f, 0, 0);
            spriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), positionString, new Vector2((int)positionStringXPos, (int)yPos), overlayColour, 0, Vector2.Zero, 1.0f, 0, 0);
            spriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), filePercentString, new Vector2((int)filePercentStringXPos, (int)yPos), overlayColour, 0, Vector2.Zero, 1.0f, 0, 0);

            // Draw any temporary message
            //
            drawTemporaryMessage(gameTime, Color.HotPink);

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
        /// Draw the system CPU load and memory usage next to the FileBuffer
        /// </summary>
        /// <param name="gameTime"></param>
        protected void drawSystemLoad(GameTime gameTime, SpriteBatch spriteBatch)
        {
            Vector2 startPosition = Vector2.Zero;
            int linesHigh = 6;

            startPosition.X += m_graphics.GraphicsDevice.Viewport.Width - m_project.getFontManager().getCharWidth() * 8;
            startPosition.Y += ( m_graphics.GraphicsDevice.Viewport.Height / 2 ) - m_project.getFontManager().getLineSpacing() * linesHigh / 2;

            float height = m_project.getFontManager().getLineSpacing() * linesHigh;
            float width = m_project.getFontManager().getCharWidth() / 2;

            // Only fetch some new samples when this timespan has elapsed
            //
            TimeSpan mySpan = gameTime.TotalGameTime;

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

            //m_pannerSpriteBatch.Begin(SpriteSortMode.Texture, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.DepthRead, RasterizerState.CullNone /*, m_pannerEffect */ );

            // Draw background for CPU counter
            //
            Vector2 p1 = startPosition;
            Vector2 p2 = startPosition;

            p1.Y += height;
            p1.X += 1;
            m_drawingHelper.drawBox(spriteBatch, p1, p2, Color.DarkGray, 0.8f);

            // Draw CPU load over the top
            //
            p1 = startPosition;
            p2 = startPosition;

            p1.Y += height;
            p2.Y += height - (m_systemLoad * height / 100.0f);
            p1.X += 1;

            m_drawingHelper.drawBox(spriteBatch, p1, p2, Color.DarkGreen, 0.8f);

            // Draw background for Memory counter
            //
            startPosition.X += m_project.getFontManager().getCharWidth();
            p1 = startPosition;
            p2 = startPosition;

            p1.Y += height;
            p1.X += 1;

            m_drawingHelper.drawBox(spriteBatch, p1, p2, Color.DarkGray, 0.8f);

            // Draw Memory over the top
            //
            p1 = startPosition;
            p2 = startPosition;

            p1.Y += height;
            p2.Y += height - (height * m_memoryAvailable / m_physicalMemory);
            p1.X += 1;

            m_drawingHelper.drawBox(spriteBatch, p1, p2, Color.DarkOrange, 0.8f);
            //m_pannerSpriteBatch.End();
        }

        /// <summary>
        /// Draw the differ - it's two mini document overviews and we provide an overlay so that
        /// we know what position in the diff we're currently looking at.
        /// </summary>
        /// <param name="v"></param>
        protected void drawDiffer(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Don't draw the cursor if we're not the active window or if we're confirming 
            // something on the screen.
            //
            if (m_differ == null || !this.IsActive || m_state != FriendlierState.DiffPicker || m_differ.hasDiffs() ==false)
            {
                return;
            }

            // Start spritebatch
            //
            //m_pannerSpriteBatch.Begin(SpriteSortMode.Texture, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.DepthRead, RasterizerState.CullNone /*, m_pannerEffect */ );
            //m_pannerSpriteBatch.Draw(m_flatTexture, new Rectangle((int)v1.X, (int)v1.Y, 1, 100), Color.White);

            Color myColour = Color.White;

            m_drawingHelper.drawBox(spriteBatch, m_differ.getLeftBox(), m_differ.getLeftBoxEnd(), myColour, 0.5f);
            m_drawingHelper.drawBox(spriteBatch, m_differ.getRightBox(), m_differ.getRightBoxEnd(), myColour, 0.5f);

            // Modify alpha according to the type of the line
            //
            float alpha = 1.0f;

            // Draw LHS preview
            //
            foreach(DiffPreview dp in m_differ.getLhsDiffPreview())
            {
                if (dp.m_colour == m_differ.m_unchangedColour)
                {
                    alpha = 0.5f;
                }
                else
                {
                    alpha = 0.8f;
                }

                m_drawingHelper.drawLine(spriteBatch, dp.m_startPos, dp.m_endPos, dp.m_colour, alpha);
            }

            // Draw RHS preview
            //
            foreach(DiffPreview dp in m_differ.getRhsDiffPreview())
            {
                if (dp.m_colour == m_differ.m_unchangedColour)
                {
                    alpha = 0.5f;
                }
                else
                {
                    alpha = 0.8f;
                }

                m_drawingHelper.drawLine(spriteBatch, dp.m_startPos, dp.m_endPos, dp.m_colour, alpha);
            }

            // Now we want to render a position viewer box overlay
            //
            float startY = Math.Min(m_differ.getLeftBox().Y + m_differ.getYMargin(), m_differ.getRightBox().Y + m_differ.getYMargin());
            float endY = Math.Min(m_differ.getLeftBoxEnd().Y - m_differ.getYMargin(), m_differ.getRightBoxEnd().Y - m_differ.getYMargin());

            double diffPercent = ((double)m_diffPosition) / ((double)m_differ.getMaxDiffLength());
            double height = ((double)m_project.getSelectedBufferView().getBufferShowLength())/((double)m_differ.getMaxDiffLength());

            Vector2 topLeft = new Vector2(m_differ.getLeftBox().X - 10.0f, startY + ((endY - startY) * ((float)diffPercent)));
            Vector2 topRight = new Vector2(m_differ.getRightBoxEnd().X + 10.0f, startY + ((endY - startY) * ((float)diffPercent)));
            Vector2 bottomRight = topRight;
            bottomRight.Y += Math.Max(((float)height * (endY - startY)), 3.0f);

            // Now render the quad
            //
            m_drawingHelper.drawQuad(spriteBatch, topLeft, bottomRight, Color.LightYellow, 0.3f);
            
            //m_pannerSpriteBatch.End();
        }

        /// <summary>
        /// Draw a cursor and make it blink in position on a FileBuffer
        /// </summary>
        /// <param name="v"></param>
        protected void drawCursor(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Don't draw the cursor if we're not the active window or if we're confirming 
            // something on the screen.
            //
            if (!this.IsActive || m_confirmState != ConfirmState.None || m_state == FriendlierState.FindText || m_state == FriendlierState.GotoLine)
            {
                return;
            }

            // No cursor for tailing BufferViews
            //
            if (!m_project.getSelectedBufferView().isTailing())
            {
                double dTS = gameTime.TotalGameTime.TotalSeconds;
                int blinkRate = 4;

                // Test for when we're showing this
                //
                if (Convert.ToInt32(dTS * blinkRate) % 2 != 0)
                {
                    return;
                }

                // Blinks rate
                //
                Vector3 v1 = m_project.getSelectedBufferView().getCursorCoordinates();
                v1.Y += m_project.getSelectedBufferView().getLineSpacing();

                Vector3 v2 = m_project.getSelectedBufferView().getCursorCoordinates();
                v2.X += 1;

                m_drawingHelper.renderQuad(v1, v2, m_project.getSelectedBufferView().getHighlightColor(), spriteBatch);
            }
            // Draw any temporary highlight
            //
            if (m_clickHighlight.First != null  &&
                ((BufferView)m_clickHighlight.First) == m_project.getSelectedBufferView())
            {
                Highlight h = (Highlight)m_clickHighlight.Second;
                Vector3 h1 = m_project.getSelectedBufferView().getSpaceCoordinates(h.m_startHighlight.asScreenPosition());
                Vector3 h2 = m_project.getSelectedBufferView().getSpaceCoordinates(h.m_endHighlight.asScreenPosition());

                // Add some height here so we can see the highlight
                //
                h2.Y += m_project.getFontManager().getLineSpacing();

                m_drawingHelper.renderQuad(h1, h2, h.getColour(), spriteBatch);
            }
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

            // We are showing this in the OverlayFont
            //
            Vector3 startPosition = new Vector3((float)m_project.getFontManager().getOverlayFont().MeasureString("X").X * 20,
                                                (float)m_project.getFontManager().getOverlayFont().LineSpacing * 8,
                                                0.0f);


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

            // Overlay batch
            //
            m_overlaySpriteBatch.Begin();

            // Draw header line
            //
            m_overlaySpriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), line, new Vector2((int)startPosition.X, (int)(startPosition.Y - m_project.getSelectedBufferView().getLineSpacing() * 3)), Color.White, 0, lineOrigin, 1.0f, 0, 0);

            // If we're using this method to position a new window only then don't show the directory chooser part..
            //
            if (m_state == FriendlierState.PositionScreenNew || m_state == FriendlierState.PositionScreenCopy)
            {
                m_overlaySpriteBatch.End();
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
                            yPosition += m_project.getFontManager().getOverlayFont().LineSpacing /* * 1.5f */;
                            line = "...";
                        }

                        m_overlaySpriteBatch.DrawString(m_project.getFontManager().getOverlayFont(),
                             line,
                             new Vector2((int)startPosition.X, (int)(startPosition.Y + yPosition)),
                             (lineNumber == m_fileSystemView.getHighlightIndex() ? m_highlightColour : (lineNumber == endShowing ? Color.White : dirColour)),
                             0,
                             lineOrigin,
                             1.0f,
                             0, 0);

                        yPosition += m_project.getFontManager().getOverlayFont().LineSpacing /* * 1.5f */;
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
                m_overlaySpriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), line, new Vector2((int)startPosition.X, (int)startPosition.Y), (m_fileSystemView.getHighlightIndex() == 0 ? m_highlightColour : dirColour), 0, lineOrigin, 1.0f, 0, 0);

                yPosition += m_project.getFontManager().getOverlayFont().LineSpacing * 3.0f;

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
                            yPosition += m_project.getFontManager().getOverlayFont().LineSpacing;
                            line = "...";
                        }

                        m_overlaySpriteBatch.DrawString(m_project.getFontManager().getOverlayFont(),
                             line,
                             new Vector2(startPosition.X, startPosition.Y + yPosition),
                             (lineNumber == m_fileSystemView.getHighlightIndex() ? m_highlightColour : (lineNumber == endShowing ? Color.White : dirColour)),
                             0,
                             lineOrigin,
                             1.0f,
                             0, 0);

                        yPosition += m_project.getFontManager().getOverlayFont().LineSpacing;
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
                            yPosition += m_project.getFontManager().getLineSpacing();
                            line = "...";
                        }

                        m_overlaySpriteBatch.DrawString(m_project.getFontManager().getOverlayFont(),
                                                 line,
                                                 new Vector2((int)startPosition.X, (int)(startPosition.Y + yPosition)),
                                                 (lineNumber == m_fileSystemView.getHighlightIndex() ? m_highlightColour : (lineNumber == endShowing ? Color.White : m_itemColour)),
                                                 0,
                                                 lineOrigin,
                                                 1.0f,
                                                 0, 0);

                        yPosition += m_project.getFontManager().getOverlayFont().LineSpacing/* * 1.5f */;
                    }
                    lineNumber++;
                }
            }

            if (m_temporaryMessageEndTime > gameTime.TotalGameTime.TotalSeconds && m_temporaryMessage != "")
            {
                // Add any temporary message on to the end of the message
                //
                m_overlaySpriteBatch.DrawString(m_project.getFontManager().getOverlayFont(),
                                         m_temporaryMessage,
                                         new Vector2((int)startPosition.X, (int)(startPosition.Y - 30.0f)),
                                         Color.LightGoldenrodYellow,
                                         0,
                                         lineOrigin,
                                         1.0f,
                                         0,
                                         0);
            }

            // Close the SpriteBatch
            //
            m_overlaySpriteBatch.End();
        }

        /// <summary>
        /// How to draw a diff'd BufferView on the screen - we key on m_diffPosition rather
        /// than using the cursor.  Always start from the translated lhs window position.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="gameTime"></param>
        protected void drawDiffBuffer(BufferView view, GameTime gameTime)
        {
            // Only process for diff views
            //
            if (view != m_differ.getSourceBufferViewLhs() && view != m_differ.getSourceBufferViewRhs())
            {
                return;
            }

            int sourceLine = m_diffPosition;
            string line = "";
            Color colour = Color.White;
            Vector3 viewSpaceTextPosition = view.getPosition();
            float yPosition = 0;
            List<Pair<DiffResult, int>> diffList;

            // Get the diffList generated in the Differ object - this holds all the expanded
            // diff information which we'll need to adjust for (on the right hand side) if we're
            // to generate a meaningful side by side diff whilst we scroll through it.
            //
            if (view == m_differ.getSourceBufferViewLhs())
            {
                diffList = m_differ.getLhsDiff();
            }
            else
            {
                diffList = m_differ.getRhsDiff();
            }


            // Need to adjust the sourceLine by the number of padding lines in the diffList up to this
            // point - otherwise we lost alignment as we scroll through the document.
            //
            for (int j = 0; j < m_diffPosition; j++)
            {
                if (j < diffList.Count)
                {
                    if (diffList[j].First == DiffResult.Padding)
                    {
                        sourceLine--;
                    }
                }
            }

            // Now iterate down the view and pull in the lines as required
            //
            for(int i = 0; i < view.getBufferShowLength(); i++)
            {
                if ((i + m_diffPosition) < diffList.Count)
                {
                    switch (diffList[i + m_diffPosition].First)
                    {
                        case DiffResult.Unchanged:
                            colour = m_differ.m_unchangedColour;

                            if (sourceLine < view.getFileBuffer().getLineCount())
                            {
                                line = view.getFileBuffer().getLine(sourceLine++);
                            }
                            // print line
                            break;

                        case DiffResult.Deleted:
                            // print deleted line (colour change?)
                            colour = m_differ.m_deletedColour;

                            if (sourceLine < view.getFileBuffer().getLineCount())
                            {
                                line = view.getFileBuffer().getLine(sourceLine++);
                            }
                            break;

                        case DiffResult.Inserted:
                            // print inserted line (colour)
                            colour = m_differ.m_insertedColour;

                            if (sourceLine < view.getFileBuffer().getLineCount())
                            {
                                line = view.getFileBuffer().getLine(sourceLine++);
                            }
                            break;

                        case DiffResult.Padding:
                        default:
                            colour = m_differ.m_paddingColour;
                            line = "";
                            // add a padding line
                            break;
                    }
                        
                }
                else
                {
                    // Do something to handle blank lines beyond end of list
                    //
                    colour = m_differ.m_paddingColour;
                    line = "";
                }

                // Truncate the line as necessary
                //
                string drawLine = line.Substring(view.getBufferShowStartX(), Math.Min(line.Length - view.getBufferShowStartX(), view.getBufferShowWidth()));
                if (view.getBufferShowStartX() + view.getBufferShowWidth() < line.Length)
                {
                    drawLine += " [>]";
                }

                m_spriteBatch.DrawString(
                                m_project.getFontManager().getFont(),
                                drawLine,
                                new Vector2((int)viewSpaceTextPosition.X /* + m_project.getFontManager().getCharWidth() * xPos */, (int)(viewSpaceTextPosition.Y + yPosition)),
                                colour,
                                0,
                                Vector2.Zero,
                                m_project.getFontManager().getTextScale(),
                                0,
                                0);

                //sourceLine++;

                yPosition += m_project.getFontManager().getLineSpacing();

            }
        }

        /// <summary>
        /// Draw a BufferView in any state that we wish to - this means showing the lines of the
        /// file/buffer we want to see at the current cursor position with highlighting as required.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="gameTime"></param>
        protected void drawFileBuffer(BufferView view, GameTime gameTime)
        {
            Color bufferColour = view.getTextColour();

            if (m_state != FriendlierState.TextEditing && m_state != FriendlierState.GotoLine && m_state != FriendlierState.FindText && m_state != FriendlierState.DiffPicker)
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

                if (view == m_buildStdOutView)
                {
                    lines = view.getWrappedEndofBuffer(m_project.getStdOutLastLine());
                }
                else if (view == m_buildStdErrView)
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
                    m_spriteBatch.DrawString(m_project.getFontManager().getFont(), lines[i], new Vector2((int)viewSpaceTextPosition.X, (int)viewSpaceTextPosition.Y + yPosition),
                        (i < view.getLogRunTerminator() ? bufferColourLastRun : bufferColour), 0, Vector2.Zero, m_project.getFontManager().getTextScale(), 0, 0);
                    yPosition += m_project.getFontManager().getLineSpacing();
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

                                m_spriteBatch.DrawString(
                                    m_project.getFontManager().getFont(),
                                    subLineToHighlight,
                                    new Vector2((int)viewSpaceTextPosition.X + m_project.getFontManager().getCharWidth() * xPos, (int)(viewSpaceTextPosition.Y + yPosition)),
                                    bufferColour,
                                    0,
                                    Vector2.Zero,
                                    m_project.getFontManager().getTextScale(),
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

                                m_spriteBatch.DrawString(
                                    m_project.getFontManager().getFont(),
                                    subLineInHighlight,
                                    new Vector2((int)viewSpaceTextPosition.X + m_project.getFontManager().getCharWidth() * xPos, (int)(viewSpaceTextPosition.Y + yPosition)),
                                    highlightColour,
                                    0,
                                    Vector2.Zero,
                                    m_project.getFontManager().getTextScale(),
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

                            m_spriteBatch.DrawString(
                                m_project.getFontManager().getFont(),
                                remainder,
                                new Vector2((int)viewSpaceTextPosition.X + m_project.getFontManager().getCharWidth() * xPos, (int)(viewSpaceTextPosition.Y + yPosition)),
                                bufferColour,
                                0,
                                Vector2.Zero,
                                m_project.getFontManager().getTextScale(),
                                0,
                                0);
                        }
                    }
                    else  // draw the line without highlighting
                    {
                        m_spriteBatch.DrawString(
                            m_project.getFontManager().getFont(),
                            line,
                            new Vector2((int)viewSpaceTextPosition.X, (int)(viewSpaceTextPosition.Y + yPosition)),
                            bufferColour,
                            0,
                            Vector2.Zero,
                            m_project.getFontManager().getTextScale(),
                            0,
                            0);
                    }

                    yPosition += m_project.getFontManager().getLineSpacing();
                }
            }

            // Draw overlaid ID on this window if we're far enough away to use it
            //
            if (m_zoomLevel > 950.0f)
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
                m_spriteBatch.DrawString(m_project.getFontManager().getFont(), bufferId, new Vector2((int)viewSpaceTextPosition.X, (int)viewSpaceTextPosition.Y), seeThroughColour, 0, Vector2.Zero, m_project.getFontManager().getTextScale() * 16.0f, 0, 0);

                // Show a filename
                //
                string fileName = view.getFileBuffer().getShortFileName();
                m_spriteBatch.DrawString(m_project.getFontManager().getFont(), fileName, new Vector2((int)viewSpaceTextPosition.X, (int)viewSpaceTextPosition.Y), seeThroughColour, 0, Vector2.Zero, m_project.getFontManager().getTextScale() * 4.0f, 0, 0);
            }
        }

        /// <summary>
        /// Render some scrolling text to a texture.  This takes the current m_temporaryMessage and renders
        /// to a texture according to how much time has passed since the message was created.
        /// </summary>
        protected void renderTextScroller()
        {
            if (m_state != FriendlierState.TextEditing)
            {
                return;
            }
            if (m_temporaryMessage == "")
            {
                return;
            }

            // Speed - higher is faster
            //
            float speed = 120.0f;

            // Set the render target and clear the buffer
            //
            m_graphics.GraphicsDevice.SetRenderTarget(m_textScroller);
            m_graphics.GraphicsDevice.Clear(Color.Black);

            // Start with whole message showing and scroll it left
            //
            int newPosition = (int)((m_gameTime.TotalGameTime.TotalSeconds - m_temporaryMessageStartTime) * - speed);

            if ((newPosition + (int)(m_temporaryMessage.Length * m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay))) < 0)
            {
                // Set the temporary message to start again and adjust position/time 
                // by width of the textScroller.
                //
                m_temporaryMessageStartTime = m_gameTime.TotalGameTime.TotalSeconds + m_textScroller.Width / speed;
            }

            // xPosition holds the scrolling position of the text in the temporary message window
            int xPosition = 0;
            float delayScroll = 0.7f; // delay the scrolling by this amount so we can read it before it starts moving

            if (m_gameTime.TotalGameTime.TotalSeconds - m_temporaryMessageStartTime > delayScroll)
            {
                xPosition = (int)((m_gameTime.TotalGameTime.TotalSeconds - delayScroll - m_temporaryMessageStartTime) * -120.0f);
            }
            
            // Draw to the render target
            //
            m_spriteBatch.Begin();
            m_spriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), m_temporaryMessage, new Vector2((int)xPosition, 0), Color.Pink, 0, new Vector2(0, 0), 1.0f, 0, 0);
            m_spriteBatch.End();

            // Now reset the render target to the back buffer
            //
            m_graphics.GraphicsDevice.SetRenderTarget(null);
            m_textScrollTexture = (Texture2D)m_textScroller;
        }

        /// <summary>
        /// Draw a scroll bar for a BufferView
        /// </summary>
        /// <param name="view"></param>
        /// <param name="file"></param>
        protected void drawScrollbar(BufferView view)
        {
            Vector3 sbPos = view.getPosition();
            float height = view.getBufferShowLength() * m_project.getFontManager().getLineSpacing();

            Rectangle sbBackGround = new Rectangle(Convert.ToInt16(sbPos.X - m_project.getFontManager().getTextScale() * 30.0f),
                                                   Convert.ToInt16(sbPos.Y),
                                                   1,
                                                   Convert.ToInt16(height));

            // Draw scroll bar
            //
            m_spriteBatch.Draw(m_flatTexture, sbBackGround, Color.DarkCyan);

            // Draw viewing window
            //
            float start = view.getBufferShowStartY();

            // Override this for the diff view
            //
            if (m_differ != null && m_state == FriendlierState.DiffPicker)
            {
                start = m_diffPosition;
            }

            float length = 0;

            // Get the line count
            //
            if (view.getFileBuffer() != null)
            {
                // Make this work for diff view as well as normal view
                //
                if (m_differ != null && m_state == FriendlierState.DiffPicker)
                {
                    length = m_differ.getMaxDiffLength();
                }
                else
                {
                    length = view.getFileBuffer().getLineCount();
                }
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

                Rectangle sb = new Rectangle(Convert.ToInt16(sbPos.X - m_project.getFontManager().getTextScale() * 30.0f),
                                             Convert.ToInt16(sbPos.Y + scrollStart),
                                             1,
                                             Convert.ToInt16(scrollLength));

                // Draw scroll bar window position
                //
                m_spriteBatch.Draw(m_flatTexture, sb, Color.LightGoldenrodYellow);
            }

            // Draw a highlight overview
            //
            if (view.gotHighlight())
            {
                float highlightStart = ((float)view.getHighlightStart().Y) / length * height;
                float highlightEnd = ((float)view.getHighlightEnd().Y) / length * height;

                Rectangle hl = new Rectangle(Convert.ToInt16(sbPos.X - m_project.getFontManager().getTextScale() * 40.0f),
                                             Convert.ToInt16(sbPos.Y + highlightStart),
                                             1,
                                             Convert.ToInt16(highlightEnd - highlightStart));

                m_spriteBatch.Draw(m_flatTexture, hl, view.getHighlightColor());
            }
        }

        /*
        /// <summary>
        /// Renders a quad at a given position - we can wrap this within another spritebatch call
        /// </summary>
        /// <param name="topLeft"></param>
        /// <param name="bottomRight"></param>
        protected void renderQuad(Vector3 topLeft, Vector3 bottomRight, Color quadColour, bool ownSpriteBatch = false)
        {
            Vector3 bottomLeft = new Vector3(topLeft.X, bottomRight.Y, topLeft.Z);
            Vector3 topRight = new Vector3(bottomRight.X, topLeft.Y, bottomRight.Z);

            // We should be caching this rather than newing it all the time
            //
            if (!ownSpriteBatch)
            {
                m_spriteBatch.Begin(SpriteSortMode.Texture, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.DepthRead, RasterizerState.CullNone, m_basicEffect);
            }

            m_spriteBatch.Draw(m_flatTexture, new Rectangle(Convert.ToInt16(topLeft.X),
                                                 Convert.ToInt16(topLeft.Y),
                                                 Convert.ToInt16(bottomRight.X) - Convert.ToInt16(topLeft.X),
                                                 Convert.ToInt16(bottomRight.Y) - Convert.ToInt16(topLeft.Y)),
                                                 quadColour);

            if (!ownSpriteBatch)
            {
                m_spriteBatch.End();
            }
        }
        */

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
        /// This can be called from anywhere so let's ensure that we have a bit of locking
        /// around the checkExit code.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void OnExiting(Object sender, EventArgs args)
        {
            base.OnExiting(sender, args);

            // Stop the threads
            //
            if (m_kinectWorker != null || m_counterWorker != null)
            {
                checkExit(m_gameTime, true);
            }
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

            string truncFileName = m_project.estimateFileStringTruncation("", m_project.getSelectedBufferView().getFileBuffer().getFilepath(), 75);

            text += truncFileName + "\n\n";
            text += "File status        : " + (m_project.getSelectedBufferView().getFileBuffer().isWriteable() ? "Writeable " : "Read Only") + "\n";
            text += "File lines         : " + m_project.getSelectedBufferView().getFileBuffer().getLineCount() + "\n";
            text += "File created       : " + m_project.getSelectedBufferView().getFileBuffer().getCreationSystemTime().ToString() +"\n";
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
            drawTextScreen(gameTime, text, 75);
        }

        /// <summary>
        /// Format a screen of information text - this will allow for screen height and provide paging
        /// using m_textScreenPositionY
        /// </summary>
        /// <param name="text"></param>
        protected void drawTextScreen(GameTime gameTime, string text, int fixedWidth = 0, int highlight = -1)
        {
            Vector3 fp = m_project.getSelectedBufferView().getPosition();

            // Always start from 0 for offsets
            //
            float yPos = 0.0f;
            float xPos = 0.0f;

            // Split out the input line
            //
            string [] infoRows = text.Split('\n');

            // We need to store this value so that page up and page down work
            //
            m_textScreenLength = infoRows.Length;

            //  Position the information centrally
            //
            int longestRow = 0;
            for (int i = 0; i < m_textScreenLength; i++)
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
            int endLine = m_textScreenPositionY + Math.Min(infoRows.Length - m_textScreenPositionY, m_project.getSelectedBufferView().getBufferShowLength());

            // Modify by height of the screen to centralise
            //
            yPos += (m_graphics.GraphicsDevice.Viewport.Height / 2) - (m_project.getFontManager().getLineSpacing(FontManager.FontType.Overlay) * ( endLine - m_textScreenPositionY ) / 2);

            // Adjust xPos
            //
            xPos = (m_graphics.GraphicsDevice.Viewport.Width / 2) - (longestRow * m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay) / 2);

            m_overlaySpriteBatch.Begin();

            // hardcode the font size to 1.0f so it looks nice
            //

            for (int i = m_textScreenPositionY; i < endLine; i++)
            {
                // Always Always Always render a string on an integer - never on a float as it looks terrible
                //
                m_overlaySpriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), infoRows[i], new Vector2((int)xPos, (int)yPos), (highlight == i ? Color.LightBlue : Color.White), 0, Vector2.Zero, 1.0f, 0, 0);

                /*
                if (i == highlight)
                {
                    Logger.logMsg("BLUE");
                }
                else
                {
                    Logger.logMsg("WHITE");
                }
                */

                yPos += m_project.getFontManager().getLineSpacing(FontManager.FontType.Overlay);
            }

            // Draw a page header if we need to
            //
            yPos = m_project.getFontManager().getLineSpacing(FontManager.FontType.Overlay) * 3;

            double dPages = Math.Ceiling((float)m_textScreenLength/(float)m_project.getSelectedBufferView().getBufferShowLength());
            double cPage = Math.Ceiling((float)(m_textScreenPositionY + 1)/((float)m_project.getSelectedBufferView().getBufferShowLength()));

            if (dPages > 1)
            {
                string pageString = "---- Page " + cPage + " of " + dPages + " ----";

                // 3 character adjustment below
                //
                xPos = (m_graphics.GraphicsDevice.Viewport.Width / 2) - ((pageString.Length + 3) * m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay) / 2);

                // Always Always Always render a string on an integer - never on a float as it looks terrible
                //
                m_overlaySpriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), pageString, new Vector2((int)xPos, (int)yPos), Color.White);
            }
            m_overlaySpriteBatch.End();

        }

        /// <summary>
        /// Draw some overlay text at a given position
        /// </summary>
        /// <param name="xPos"></param>
        /// <param name="yPos"></param>
        /// <param name="text"></param>
        /// <param name="textColour"></param>
        protected void drawTextOverlay(int xPos, int yPos, string text, Color textColour)
        {
            m_overlaySpriteBatch.Begin();

            // Always Always Always render a string on an integer - never on a float as it looks terrible
            //
            m_overlaySpriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), text, new Vector2(xPos, yPos), textColour);

            m_overlaySpriteBatch.End();
        }

        /// <summary>
        /// Draw some text with specified colour and position on the overlay SpriteBatch
        /// </summary>
        /// <param name="screenPosition"></param>
        /// <param name="text"></param>
        /// <param name="textColour"></param>
        protected void drawTextOverlay(FilePosition position, string text, Color textColour)
        {
            int xPos = (int)((float)position.X * m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay));
            int yPos = (int)((float)position.Y * m_project.getFontManager().getLineSpacing(FontManager.FontType.Overlay));

            drawTextOverlay(xPos, yPos, text, textColour);
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
            float yPos = 5.5f * m_project.getFontManager().getLineSpacing(FontManager.FontType.Overlay);
            float xPos = 10 * m_project.getFontManager().getCharWidth(FontManager.FontType.Overlay);

            // Start the spritebatch
            //
            m_overlaySpriteBatch.Begin();

            if (m_editConfigurationItem) // Edit a single configuration item
            {
                string text = "Edit configuration item";

                m_overlaySpriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), text, new Vector2((int)xPos, (int)yPos), Color.White, 0, Vector2.Zero, 1.0f, 0, 0);
                yPos += m_project.getFontManager().getLineSpacing(FontManager.FontType.Overlay) * 2;

                m_overlaySpriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), m_project.getConfigurationItem(m_configPosition).Name, new Vector2((int)xPos, (int)yPos), m_itemColour, 0, Vector2.Zero, 1.0f, 0, 0);
                yPos += m_project.getFontManager().getLineSpacing(FontManager.FontType.Overlay);

                string configString = m_editConfigurationItemValue;
                if (configString.Length > m_project.getSelectedBufferView().getBufferShowWidth())
                {
                    configString = "[..]" + configString.Substring(configString.Length - m_project.getSelectedBufferView().getBufferShowWidth() + 4, m_project.getSelectedBufferView().getBufferShowWidth() - 4);
                }

                m_overlaySpriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), configString, new Vector2((int)xPos, (int)yPos), m_highlightColour, 0, Vector2.Zero, 1.0f, 0, 0);
            }
            else
            {
                string text = "Configuration Items";

                m_overlaySpriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), text, new Vector2((int)xPos, (int)yPos), Color.White, 0, Vector2.Zero, 1.0f, 0, 0);
                yPos += m_project.getFontManager().getLineSpacing(FontManager.FontType.Overlay) * 2;

                // Write all the configuration items out - if we're highlight one of them then change
                // the colour.
                //
                for (int i = 0; i < m_project.getConfigurationListLength(); i++)
                {
                    string configItem = m_project.estimateFileStringTruncation("", m_project.getConfigurationItem(i).Value, 60 - m_project.getConfigurationItem(i).Name.Length);
                    //string item = m_project.getConfigurationItem(i).Name + "  =  " + m_project.getConfigurationItem(i).Value;
                    string item = m_project.getConfigurationItem(i).Name + "  =  " + configItem;

                    item = m_project.estimateFileStringTruncation("", item, m_project.getSelectedBufferView().getBufferShowWidth());

                    /*
                    if (item.Length > m_project.getSelectedBufferView().getBufferShowWidth())
                    {
                        item = item.Substring(m_configXOffset, m_project.getSelectedBufferView().getBufferShowWidth());
                    }
                    */

                    m_overlaySpriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), item, new Vector2((int)xPos, (int)yPos), (i == m_configPosition ? m_highlightColour : m_itemColour), 0, Vector2.Zero, 1.0f, 0, 0);
                    yPos += m_project.getFontManager().getLineSpacing(FontManager.FontType.Overlay);
                }
            }

            m_overlaySpriteBatch.End();
        }

        /// <summary>
        /// Perform an external build
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected void doBuildCommand(GameTime gameTime, string overrideString = "")
        {

            if (m_buildProcess != null)
            {
                Logger.logMsg("Friendlier::doBuildCommand() - build in progress");
                setActiveBuffer(m_buildStdOutView);
                setTemporaryMessage("Checking build status", 3,  m_gameTime);
                return;
            }

            Logger.logMsg("Friendlier::doBuildCommand() - attempting to run build command");

            // Check that we can find the build command
            //
            try
            {
                //string[] commandList = m_project.getBuildCommand().Split(' ');
                string[] commandList = m_project.getConfigurationValue("BUILDCOMMAND").Split(' ');

                // Override the default build command
                //
                if (overrideString != "")
                {
                    commandList = overrideString.Split();
                }

                if (commandList.Length == 0)
                {
                    setTemporaryMessage("Build command not defined", 2, gameTime);
                }
                else
                {
                    // If the end of the build command is no a .exe or a .bat then assume we've got a 
                    // space in the file name somewhere.  This code fixes spaces in file names for us
                    // in the command list for the first argument but could (and should) be extended
                    // to all arguments that don't make any sense.
                    //
                    if (!File.Exists(commandList[0]))
                    {
                        if (commandList[0].Length > 5)
                        {
                            int pos = 0;

                            while (pos < commandList.Length)
                            {
                                string endCommand = commandList[pos].Substring(commandList[pos].Length - 4, 4).ToUpper();

                                if (endCommand == ".EXE" || endCommand == ".BAT")
                                {
                                    // Create a new command list and combine the first 'pos' commands into
                                    // a single correct one.
                                    //
                                    string [] newCommandList = new string[commandList.Length - pos];

                                    for (int i = 0; i < commandList.Length; i++)
                                    {
                                        if (i <= pos)
                                        {
                                            newCommandList[0] += commandList[i];

                                            if (i < pos)
                                            {
                                                newCommandList[0] += " ";
                                            }
                                        }
                                        else
                                        {
                                            newCommandList[i - pos] = commandList[i];
                                        }
                                    }

                                    // Now assigned command list from newCommandList
                                    commandList = newCommandList;
                                    break;
                                }
                                else
                                {
                                    pos++;
                                }
                            }

                            //if (commandList[0].Substring(commandList[0].Length - 4, 3).ToUpper() != "EXE
                        }
                    }

                    // We ensure that full path is given to build command at this time
                    //
                    if (!File.Exists(commandList[0]))
                    {
                        setTemporaryMessage("Build command not found : \"" + commandList[0] + "\"", 5, gameTime);
                    }
                    else
                    {
                        string buildDir = m_project.getConfigurationValue("BUILDDIRECTORY");
                        string buildStdOutLog = m_project.getConfigurationValue("BUILDSTDOUTLOG");
                        string buildStdErrLog = m_project.getConfigurationValue("BUILDSTDERRLOG");

                        // Check the build directory
                        //
                        if (!Directory.Exists(buildDir))
                        {
                            setTemporaryMessage("Build directory doesn't exist : \"" + buildDir + "\"", 2, gameTime);
                            return;
                        }

                        // Add a standard error view
                        //
                        if (!File.Exists(buildStdErrLog))
                        {
                            StreamWriter newStdErr = File.CreateText(buildStdErrLog);
                            newStdErr.Close();
                        }

                        m_buildStdErrView = m_project.findBufferView(buildStdErrLog);

                        if (m_buildStdErrView == null)
                        {
                            m_buildStdErrView = addNewFileBuffer(buildStdErrLog, true, true);
                        }
                        m_buildStdErrView.setTailColour(Color.Orange);
                        m_buildStdErrView.noHighlight();

                        //m_buildStdErrView.setReadOnlyColour(Color.DarkRed);

                        // Store the line length of the existing file
                        //
                        m_project.setStdErrLastLine(m_buildStdErrView.getFileBuffer().getLineCount());

                        // If the build log doesn't exist then create it
                        //
                        if (!File.Exists(buildStdOutLog))
                        {
                            StreamWriter newStdOut = File.CreateText(buildStdOutLog);
                            newStdOut.Close();
                        }

                        // Now ensure that the build log is visible on the screen somewhere
                        //
                        m_buildStdOutView = m_project.findBufferView(buildStdOutLog);

                        if (m_buildStdOutView == null)
                        {
                            m_buildStdOutView = addNewFileBuffer(buildStdOutLog, true, true);
                        }
                        m_buildStdOutView.noHighlight();

                        // Store the line length of the existing file
                        //
                        m_project.setStdOutLastLine(m_buildStdOutView.getFileBuffer().getLineCount());

                        // Move to that BufferView
                        //
                        setActiveBuffer(m_buildStdOutView);

                        // Build the argument list
                        //
                        ProcessStartInfo info = new ProcessStartInfo();
                        info.WorkingDirectory = buildDir;
                        //info.WorkingDirectory = "C:\\Q\\mingw\\bin";
                        //info.EnvironmentVariables.Add("PATH", "C:\\Q\\mingw\\bin");
                        //info.EnvironmentVariables.Add("TempPath", "C:\\Temp");
                        info.UseShellExecute = false;
                        info.FileName = m_project.getCommand(commandList);
                        info.WindowStyle = ProcessWindowStyle.Hidden;
                        info.CreateNoWindow = true;
                        //info.Arguments = m_project.getArguments() + (options == "" ? "" : " " + options);
                        info.Arguments = m_project.getArguments(commandList);
                        info.RedirectStandardOutput = true;
                        info.RedirectStandardError = true;

                        // Append the command to the stdout file
                        //
                        m_buildStdOutView.getFileBuffer().appendLine("Running command: " + string.Join(" ", commandList));
                        m_buildStdOutView.getFileBuffer().save();

                        m_buildProcess = new Process();
                        m_buildProcess.StartInfo = info;
                        m_buildProcess.OutputDataReceived += new DataReceivedEventHandler(logBuildStdOut);
                        m_buildProcess.ErrorDataReceived += new DataReceivedEventHandler(logBuildStdErr);
                        m_buildProcess.Exited += new EventHandler(buildCompleted);

                        m_buildProcess.EnableRaisingEvents = true;

                        Logger.logMsg("Friendlier::doBuildCommand() - working directory = " + info.WorkingDirectory);
                        Logger.logMsg("Friendlier::doBuildCommand() - filename = " + info.FileName);
                        Logger.logMsg("Friendlier::doBuildCommand() - arguments = " + info.Arguments);

                        // Start the external build command and check the logs
                        //
                        m_buildProcess.Start();
                        m_buildProcess.BeginOutputReadLine();
                        m_buildProcess.BeginErrorReadLine();

                        // Inform that we're starting the build
                        //
                        setTemporaryMessage("Starting build..", 4, gameTime);
                        startBanner(m_gameTime, "Build started", 5);

                        /*
                        // Handle any immediate exit error code
                        //
                        if (m_buildProcess.ExitCode != 0)
                        {
                            Logger.logMsg("Friendlier::doBuildCommand() - build process failed with code " + m_buildProcess.ExitCode);
                        }
                        else
                        {
                            Logger.logMsg("Friendlier::doBuildCommand() - started build command succesfully");
                        }
                         * */
                    }
                }
            }
            catch (Exception e)
            {
                Logger.logMsg("Can't run command " + e.Message);

                // Disconnect the file handlers and the exit handler
                //
                m_buildProcess.OutputDataReceived -= new DataReceivedEventHandler(logBuildStdOut);
                m_buildProcess.ErrorDataReceived -= new DataReceivedEventHandler(logBuildStdErr);
                m_buildProcess.Exited -= new EventHandler(buildCompleted);

                // Set an error message
                //
                setTemporaryMessage("Problem when running command - " + e.Message, 5, gameTime);

                // Dispose of the build process object
                //
                m_buildProcess = null;
            }
        }

        /// <summary>
        /// Write the stdout from the build process to a log file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void logBuildStdOut(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                Logger.logMsg("Friendlier::logBuildStdOut() - got null data");
                return;
            }

            string time = string.Format("{0:yyyyMMdd HH:mm:ss}", DateTime.Now);
            string logBody = "INF:" + time + ":" + e.Data;

            m_buildStdOutView.getFileBuffer().appendLine(logBody);

            // Save the log file
            //
            m_buildStdOutView.getFileBuffer().setReadOnly(false);
            m_buildStdOutView.getFileBuffer().save();
            m_buildStdOutView.getFileBuffer().setReadOnly(true);
#if WRITE_LOG_FILE

            System.IO.TextWriter logFile = new StreamWriter(m_project.getConfigurationValue("BUILDLOG"), true);
            logFile.WriteLine("INF:" + time + ":" + logBody);
            logFile.Flush();
            logFile.Close();
            logFile = null;
#endif

            // Ensure we're looking at the end of the file
            //
            m_buildStdOutView.setTailPosition();
        }

        /// <summary>
        /// Write stderr
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void logBuildStdErr(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                Logger.logMsg("Friendlier::logBuildStdErr() - got null data");
                return;
            }

            string time = string.Format("{0:yyyyMMdd HH:mm:ss}", DateTime.Now);
            string logBody = "ERR:" + time + ":" + (string)e.Data;

            m_buildStdErrView.getFileBuffer().appendLine(logBody);

            // Save the log file
            //
            m_buildStdErrView.getFileBuffer().setReadOnly(false);
            m_buildStdErrView.getFileBuffer().save();
            m_buildStdErrView.getFileBuffer().setReadOnly(true);

#if WRITE_LOG_FILE
            System.IO.TextWriter logFile = new StreamWriter(m_project.getConfigurationValue("BUILDLOG"), true);
            logFile.WriteLine("ERR:" + time + ":" + logBody);
            logFile.Flush();
            logFile.Close();
            logFile = null;
#endif

            // Ensure we're looking at the end of the file
            //
            m_buildStdErrView.setTailPosition();
        }

        /// <summary>
        /// Build completed callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buildCompleted(object sender, System.EventArgs e)
        {

            // If there was an issue with the build then move to the active buffer that holds the error logs
            //
            if (m_buildProcess.ExitCode != 0)
            {
                setActiveBuffer(m_buildStdErrView);
                setTemporaryMessage("Build failed with exit code " + m_buildProcess.ExitCode, 5, m_gameTime);
                m_buildStdErrView.setTailColour(Color.Red);

                startBanner(m_gameTime, "Build failed", 5);
            }
            else
            {
                setTemporaryMessage("Build completed successfully.", 3, m_gameTime);

                // Also colour the error log green
                //
                m_buildStdErrView.setTailColour(Color.Green);

                startBanner(m_gameTime, "Build completed", 5);
            }

            // Invalidate the build process
            //
            m_buildProcess = null;
        }

        /// <summary>
        /// Start drawing a zooming banner
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="bannerString"></param>
        /// <param name="seconds"></param>
        protected void startBanner(GameTime gameTime, string bannerString, float seconds)
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
        protected void drawBanner(GameTime gameTime)
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
            foreach(string banner in m_bannerStringList)
            {
                if (banner.Length > maxLength)
                {
                    maxLength = banner.Length;
                }
            }
            int xPosition = 0;
            int yPosition = 0;

            bool isText = false;

            m_spriteBatch.Begin(SpriteSortMode.Texture, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.DepthRead, RasterizerState.CullNone, m_basicEffect);

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

                m_spriteBatch.DrawString(m_project.getFontManager().getOverlayFont(), m_bannerString, new Vector2((int)xPosition, (int)yPosition), m_bannerColour, 0, new Vector2(0, 0), scale, 0, 0);
            }
            else
            {
                xPosition = (int)m_project.getEyePosition().X;
                yPosition = (int)m_project.getEyePosition().Y;

                xPosition += (int)(scale * (float)m_splashScreen.Width / 2.0f);
                yPosition += (int)(scale * (float)m_splashScreen.Height / 2.0f);

                m_spriteBatch.Draw(m_splashScreen, new Vector2((int)xPosition, (int)yPosition), null, Color.White, 0f, Vector2.Zero, scale, 0, 0);
            }

            m_spriteBatch.End();
        }

        /// <summary>
        /// Dragged files entering event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void friendlierDragEnter(object sender, System.Windows.Forms.DragEventArgs e)
        {
            Logger.logMsg("Friendlier::friendlierDragEnter() - dragging entered " + e.Data.GetType().ToString());


            //if (!e.Data.GetDataPresent(typeof(System.Windows.Forms.DataObject)))
            if (e.Data.GetType() != typeof(System.Windows.Forms.DataObject))
            {
                e.Effect = System.Windows.Forms.DragDropEffects.None;
                return;
            }

            // Effect is to "link" to this project
            //
            e.Effect = System.Windows.Forms.DragDropEffects.Link;
        }

        /// <summary>
        /// Drag and drop target function - do some file and directory adding
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void friendlierDragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            Logger.logMsg("Friendlier::friendlierDragEnter() - drop event fired of type " + e.Data.ToString());

            if (e.Data.GetType() == typeof(System.Windows.Forms.DataObject))
            {
                System.Windows.Forms.DataObject obj = (System.Windows.Forms.DataObject)e.Data.GetData(typeof(System.Windows.Forms.DataObject));
                string [] formats = e.Data.GetFormats();
                string[] files = (string[])e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop);

                //int filesAdded = 0;
                //int dirsAdded = 0;

                List<string> filesAdded = new List<string>();
                List<string> dirsAdded = new List<string>();

                BufferView newView = null;

                foreach (string newFile in files)
                {
                    
                    // Is this a directory or a file?
                    //
                    if (Directory.Exists(newFile))
                    {
                        Logger.logMsg("Friendlier::friendlierDragDrop() - adding directory = " + newFile);
                        addNewDirectory(newFile);
                        dirsAdded.Add(newFile);
                    }
                    else
                    {
                        Logger.logMsg("Friendlier::friendlierDragDrop() - adding file " + newFile);
                        newView = addNewFileBuffer(newFile);
                        filesAdded.Add(newFile);
                    }
                }

                // Always set to the last added BufferView
                //
                if (newView != null)
                {
                    setActiveBuffer(newView);
                }

                // Build an intelligible temporary message after we've done this work
                //
                string message = "";

                if (filesAdded.Count > 0)
                {
                    message = filesAdded.Count + " file";

                    if (filesAdded.Count > 1)
                    {
                        message += "s";
                    }

                    message += " added ";

                    foreach(string fi in filesAdded)
                    {
                        message += " " + fi;
                    }
                }

                if (dirsAdded.Count > 0)
                {
                    if (message != "")
                    {
                        message += ", ";
                    }

                    message += dirsAdded.Count + " ";

                    if (dirsAdded.Count == 1)
                    {
                        message += "directory";
                    }
                    else
                    {
                        message += "directories";
                    }

                    message += " added";

                    foreach (string di in dirsAdded)
                    {
                        message += " " + di;
                    }
                }

                // Set the temporary message if we've generated one
                //
                if (message != "")
                {
                    setTemporaryMessage(message, 5, m_gameTime);
                }
            }
        }

        /// <summary>
        /// Add directory full of files recursively
        /// </summary>
        /// <param name="dirPath"></param>
        protected void addNewDirectory(string dirPath)
        {
            Logger.logMsg("Friendlier::addNewDirectory() - adding directory " + dirPath);
        }

    }
}
