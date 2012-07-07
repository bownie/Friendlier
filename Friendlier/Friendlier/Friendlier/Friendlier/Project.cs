#region File Description
//-----------------------------------------------------------------------------
// Project.cs
//
// Copyright (C) Xyglo Ltd. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Text.RegularExpressions;

namespace Xyglo
{

    /// <summary>
    /// A Configuration item
    /// </summary>
    [DataContract(Name = "Friendlier", Namespace = "http://www.xyglo.com")]
    public class Configuration : IComparable
    {
        /// <summary>
        /// The name of this configuration item
        /// </summary>
        [DataMember]
        public string Name;

        /// <summary>
        /// The value of this configuration item
        /// </summary>
        [DataMember]
        public string Value;

        public Configuration(string name, string value)
        {
            Name = name;
            Value = value;
        }

        // Implement the generic CompareTo method with the Temperature 
        // class as the Type parameter. 
        //
        int IComparable.CompareTo(object obj)
        {
            Configuration config = (Configuration)(obj);

            // If other is not a valid object reference, this instance is greater.
            if (config == null) return 1;

            // The temperature comparison depends on the comparison of 
            // the underlying Double values. 
            return Name.CompareTo(config.Name);
        }


    }

    /// <summary>
    /// A Friendlier project file - contains a list of BufferViews and FileBuffers and these
    /// two could be (but don't have to be) related.   We also expose some interfaces for doing
    /// things to FileBuffers and BufferViews.
    /// </summary>
    [DataContract(Name = "Friendlier", Namespace = "http://www.xyglo.com")]
    public class Project
    {

        /// <summary>
        /// Define a viewing mode for this project - this decides what should be shown
        /// and when during working with the project i.e. banners, medals etc.
        /// </summary>
        public enum ViewMode
        {
            Formal,
            Coloured,
            Fun
        }

        //////////// MEMBER VARIABLES ///////////////

        /// <summary>
        /// The name of our project
        /// </summary>
        [DataMember]
        public string m_projectName
        { get; set; }

        /// <summary>
        /// Project file location
        /// </summary>
        [DataMember]
        public string m_projectFile
        { get; set; }

        /// <summary>
        /// List of the FileBuffers attached to this project
        /// </summary>
        [DataMember]
        protected List<FileBuffer> m_fileBuffers = new List<FileBuffer>();

        /// <summary>
        /// List of the BufferViews attached to this project
        /// </summary>
        [DataMember]
        protected List<BufferView> m_bufferViews = new List<BufferView>();

        /// <summary>
        /// List of generic views - for the moment we only push DiffViews on to them and
        /// we cannot serialise this list as it's an abstract class.
        /// </summary>
        [NonSerialized]
        protected List<XygloView> m_views = new List<XygloView>();

        /// <summary>
        /// The LHS BufferView of a diff
        /// </summary>
        [DataMember]
        protected int m_leftDiffBufferViewIndex = -1;

        /// <summary>
        /// The RHS BufferView of a diff
        /// </summary>
        [DataMember]
        protected int m_rightDiffBufferViewIndex = -1;

        /// <summary>
        /// Length of the visible BufferView for this project
        /// </summary>
        [DataMember]
        protected int m_bufferViewLength = 20;

        /// <summary>
        /// Which BufferView is currently selected in the project - to be superceded
        /// </summary>
        [DataMember]
        protected int m_selectedViewId = 0;

        /// <summary>
        /// The generic view id which will supercede the m_selectedViewId eventually
        /// </summary>
        [DataMember]
        protected int m_selectedXygloViewId = 0;

        /// <summary>
        /// When this project was initially created - when we persist this it should
        /// only ever take the initial value here.
        /// </summary>
        [DataMember]
        protected DateTime m_creationTime = DateTime.Now;

        /// <summary>
        /// When this project is reconstructed this value will be updated
        /// </summary>
        [DataMember]
        public DateTime m_lastAccessTime;

        /// <summary>
        /// Last time this project was persisted
        /// </summary>
        [DataMember]
        protected DateTime m_lastWriteTime;

        /// <summary>
        /// How long has this project been active for (total in-app time)
        /// </summary>
        [DataMember]
        public TimeSpan m_activeTime
        { get; set; }

        /// <summary>
        /// Our local configuration
        /// </summary>
        [DataMember]
        protected List<Configuration> m_configuration;

        /// <summary>
        /// Store where we last opened a file from
        /// </summary>
        [DataMember]
        protected string m_openDirectory = @"C:\";

        /// <summary>
        /// Store where we last saved a file to
        /// </summary>
        [DataMember]
        protected string m_saveDirectory;

        /// <summary>
        /// Is this software licenced?
        /// </summary>
        [IgnoreDataMember]
        protected bool m_licenced = false;

        /// <summary>
        /// Eye position to be persistently stored
        /// </summary>
        [DataMember]
        protected Vector3 m_eyeSavePosition = new Vector3();

        /// <summary>
        /// Target eye position to be persistently stored
        /// </summary>
        [DataMember]
        protected Vector3 m_targetSavePosition = new Vector3();

        /// <summary>
        /// Buffer view colour index
        /// </summary>
        [DataMember]
        protected int m_bvColourIndex = 0;

        /// <summary>
        /// List of buffer view colours that we cycle through
        /// </summary>
        [DataMember]
        protected Color[] m_bvColours = { Color.DeepSkyBlue, Color.IndianRed, Color.OrangeRed, Color.GreenYellow, Color.HotPink, Color.LavenderBlush };

        /// <summary>
        /// Viewing mode of the project
        /// </summary>
        [DataMember]
        protected ViewMode m_viewMode = ViewMode.Formal;

        /// <summary>
        /// Is the project full screen?
        /// </summary>
        [DataMember]
        protected bool m_fullScreen = false;

        /// <summary>
        /// Window size
        /// </summary>
        [DataMember]
        protected Vector2 m_windowSize;

        /// <summary>
        /// Window position
        /// </summary>
        [DataMember]
        protected Vector2 m_windowPosition;

        /// <summary>
        /// Font manager passed in and set from Friendlier
        /// </summary>
        [NonSerialized]
        protected FontManager m_fontManager;

        /// <summary>
        /// When this is instantiated it makes a language specific syntax handler to
        /// provide syntax highlighting, suggestions for autocompletes and also indent
        /// levels.
        /// </summary>
        [NonSerialized]
        protected SyntaxManager m_syntaxManager;

        /// <summary>
        /// Tab spaces defined in the project
        /// </summary>
        [NonSerialized]
        protected string m_tab;

        /// <summary>
        /// The last line in the Standard Output log from the previous run
        /// </summary>
        [DataMember]
        protected int m_stdOutLastLine;

        /// <summary>
        /// The last line in the Standard Error log from the previous run
        /// </summary>
        [DataMember]
        protected int m_stdErrLastLine;

        /// <summary>
        /// An external file that defines the project - could be a project file or a build/Makefile
        /// that provides information about the whole project.
        /// </summary>
        [DataMember]
        protected string m_externalProjectDefinitionFile = "";

        /// <summary>
        /// Define a base directory for our project - searching for files will occur from this 
        /// directory.
        /// </summary>
        [DataMember]
        protected string m_externalProjectBaseDirectory = "";

        /// <summary>
        /// History of all searches for this project
        /// </summary>
        [DataMember]
        protected List<string> m_searchHistory = new List<string>();

        ////////// CONSTRUCTORS ///////////

        /// <summary>
        /// Default constructor 
        /// </summary>
        public Project()
        {
            m_projectName = "<unnamed>";
        }

        /// <summary>
        /// Default constructor 
        /// </summary>
        public Project(FontManager fontManager)
        {
            m_projectName = "<unnamed>";
            m_fontManager = fontManager;
            initialise();
        }


        /// <summary>
        /// Name constructor
        /// </summary>
        /// <param name="name"></param>
        public Project(FontManager fontManager, string name, string projectFile)
        {
            m_projectName = name;
            m_projectFile = projectFile;
            m_fontManager = fontManager;
            initialise();
        }

        ////////////// METHODS ////////////////

        /// <summary>
        /// Interface for initialising the font manager via the Project
        /// </summary>
        /// <param name="contentManager"></param>
        /// <param name="fontFamily"></param>
        /// <param name="aspectRatio"></param>
        /// <param name="processor"></param>
        public void initialiseFonts(Microsoft.Xna.Framework.Content.ContentManager contentManager, string fontFamily, float aspectRatio, string processor = "")
        {
            m_fontManager.initialise(contentManager, fontFamily, aspectRatio, processor);
        }

        /// <summary>
        /// Initialise this project
        /// </summary>
        protected void initialise()
        {
            // Update this
            //
            m_lastAccessTime = DateTime.Now;
            
            // Initialise
            //
            m_activeTime = TimeSpan.Zero;

            // Build some configuation if we need to
            //
            buildInitialConfiguration();

            // Build the syntax manager - for the moment we force it to Cpp
            //
            m_syntaxManager = new CppSyntaxManager(this);

            // Default tab to two spaces here
            //
            m_tab = "  ";
        }

        /// <summary>
        /// Return the next colour we should use for a BufferView
        /// </summary>
        /// <returns></returns>
        public Color getNewFileBufferColour()
        {
            Color rC = m_bvColours[m_bvColourIndex];

            m_bvColourIndex = (m_bvColourIndex + 1) % m_bvColours.Length;

            // Set alpha
            //
            rC.A = 150;

            return rC;
        }

        /// <summary>
        /// Set the licenced status for this project
        /// </summary>
        /// <param name="value"></param>
        public void setLicenced(bool value)
        {
            m_licenced = value;
        }

        /// <summary>
        /// Is this licenced?
        /// </summary>
        public bool getLicenced()
        {
            return m_licenced;
        }

        /// <summary>
        /// Creation time of this project
        /// </summary>
        /// <returns></returns>
        public DateTime getCreationTime()
        {
            return m_creationTime;
        }

        /// <summary>
        /// Return the configuration 
        /// </summary>
        /// <returns></returns>
        public int getConfigurationListLength()
        {
            if (m_configuration == null)
            {
                return 0;
            }
            else
            {
                return m_configuration.Count;
            }
        }

        /// <summary>
        /// Build the configuration object from scratch
        /// </summary>
        public void buildInitialConfiguration()
        {
            if (m_configuration == null)
            {
                m_configuration = new List<Configuration>();
            }

            // Main build command
            //
            //if (addCheckConfigurationItem("BUILDCOMMAND", @"C:\QtSDK\mingw\bin\mingw32-make.exe -f D:\garderobe-build-desktop\Makefile"))
            if (addCheckConfigurationItem("BUILDCOMMAND", ""))
            {
                Logger.logMsg("Project::buildInitialConfiguration - added BUILDCOMMAND config");
            }

            // Second build command (qmake, make clean etc)
            //if (addCheckConfigurationItem("ALTERNATEBUILDCOMMAND", @"C:\Q\Desktop\Qt\4.7.4\mingw\bin\qmake.exe -makefile C:\newdir\test.pro"))
            if (addCheckConfigurationItem("ALTERNATEBUILDCOMMAND", ""))
            {
                Logger.logMsg("Project::buildInitialConfiguration - added ALTERNATEBUILDCOMMAND config");
            }

            // Where the build occurs
            //
            //if (addCheckConfigurationItem("BUILDDIRECTORY", @"D:\garderobe-build-desktop"))
            if (addCheckConfigurationItem("BUILDDIRECTORY", ""))
            {
                Logger.logMsg("Project::buildInitialConfiguration - added BUILDIRECTORY config");
            }

            string stdOutLog = getUserDataPath() + "stdout.log";
            string stdErrLog = getUserDataPath() + "stderr.log";

            // Build Stdout log location
            //
            if (addCheckConfigurationItem("BUILDSTDOUTLOG", stdOutLog))
            {
                Logger.logMsg("Project::buildInitialConfiguration - added BUILDSTDOUTLOG config");
            }

            // Build Stderr log location
            //
            if (addCheckConfigurationItem("BUILDSTDERRLOG", stdErrLog))
            {
                Logger.logMsg("Project::buildInitialConfiguration - added BUILDSTDERRLOG config");
            }

            // Autoindent on?
            //
            if (addCheckConfigurationItem("AUTOINDENT", "TRUE"))
            {
                Logger.logMsg("Project::buildInitialConfiguration - added AUTOINDENT config");
            }

            // Should diffing centre the screen on both BufferViews?
            //
            if (addCheckConfigurationItem("DIFFCENTRE", "FALSE"))
            {
                Logger.logMsg("Project::buildInitialConfiguration - added DIFFCENTRE config");
            }

            // Confirmation on quit
            //
            if (addCheckConfigurationItem("CONFIRMQUIT", "TRUE"))
            {
                Logger.logMsg("Project::buildInitialConfiguration - added DIFFCENTRE config");
            }

            // Confirmation on quit
            //
            if (addCheckConfigurationItem("SYNTAXHIGHLIGHT", "TRUE"))
            {
                Logger.logMsg("Project::buildInitialConfiguration - added SYNTAXHIGHLIGHT config");
            }

            // Sort the config to within an inch of its life
            //
            m_configuration.Sort();

            // Initialise m_views if we've persisted none (more than likely)
            //
            if (m_views == null)
            {
                m_views = new List<XygloView>();
            }

        }

        /// <summary>
        /// Add an item to the configuration if it doesn't exist
        /// </summary>
        /// <param name="item"></param>
        /// <param name="value"></param>
        protected bool addConfigurationItem(string item, string value)
        {
            foreach (Configuration config in m_configuration)
            {
                if (config.Name == item)
                {
                    return false; // already exists
                }
            }

            // Add the item and return
            //
            m_configuration.Add(new Configuration(item, value));
            m_configuration.Sort();
            return true;
        }

        /// <summary>
        /// Add an item if it doesn't already exist
        /// </summary>
        /// <param name="item"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected bool addCheckConfigurationItem(string item, string value)
        {
            if(m_configuration.Exists(val => val.Name == item))
            {
                return false;
            }

            // Add the item and return
            //
            m_configuration.Add(new Configuration(item, value));
            m_configuration.Sort();
            return true;
        }


        /// <summary>
        /// Remove a configuration item
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected bool deleteConfigurationItem(string item)
        {
            Configuration foundConfig = null;

            foreach (Configuration config in m_configuration)
            {
                if (config.Name == item)
                {
                    foundConfig = config;
                    break;
                }
            }

            if (foundConfig == null)
            {
                return false;
            }
            else
            {
                m_configuration.Remove(foundConfig);
                m_configuration.Sort();
                return true;
            }
        }

        /// <summary>
        /// Return a configuration item
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public Configuration getConfigurationItem(int number)
        {
            if (number >= 0 && number < m_configuration.Count)
            {
                m_configuration.Sort();
                return m_configuration[number];
            }
           
            return null;
        }

        /// <summary>
        /// Get a configuration value by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string getConfigurationValue(string name)
        {
            foreach (Configuration config in m_configuration)
            {
                if (config.Name == name)
                {
                    return config.Value;
                }
            }

            throw new Exception("No configuration item for " + name);
        }

        /// <summary>
        /// Update an existing configuration item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool updateConfigurationItem(string item, string value)
        {
            Configuration foundConfig = null;

            foreach (Configuration config in m_configuration)
            {
                if (config.Name == item)
                {
                    foundConfig = config;
                    break;
                }
            }

            if (foundConfig == null)
            {
                return false;
            }
            else
            {
                foundConfig.Value = value;
                return true;
            }
        }


        /// <summary>
        /// Return the selected BufferView id
        /// </summary>
        /// <returns></returns>
        public int getSelectedBufferViewId()
        {
            return m_selectedViewId;
        }

        /// <summary>
        /// Set selected buffer view index
        /// </summary>
        /// <param name="index"></param>
        public void setSelectedBufferViewId(int index)
        {
            m_selectedViewId = index;
        }

        /// <summary>
        /// Return the list of Views - these are called generic for the moment until m_bufferViews are
        /// integrated with them.
        /// </summary>
        /// <returns></returns>
        public List<XygloView> getGenericViews()
        {
            return m_views;
        }

        /// <summary>
        /// Return the list of BufferViews
        /// </summary>
        /// <returns></returns>
        public List<BufferView> getBufferViews()
        {
            return m_bufferViews;
        }

        /// <summary>
        /// Return the list of FileBuffers
        /// </summary>
        /// <returns></returns>
        public List<FileBuffer> getFileBuffers()
        {
            return m_fileBuffers;
        }

        /// <summary>
        /// Return non-null FileBuffers
        /// </summary>
        /// <returns></returns>
        public List<FileBuffer> getNonNullFileBuffers()
        {
            List<FileBuffer> rL = new List<FileBuffer>();

            foreach (FileBuffer fb in m_fileBuffers)
            {
                if (fb.getFilepath() != null && fb.getFilepath() != "")
                {
                    rL.Add(fb);
                }
            }
            return rL;
        }

        /// <summary>
        /// Try and find a BufferView for a given filename
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public BufferView getBufferView(string filename)
        {
            List<BufferView> bvL = m_bufferViews.Where(item => item.getFileBuffer().getFilepath().ToUpper() == filename.ToUpper()).ToList();

            if (bvL.Count() > 0)
            {
                return bvL[0];
            }
            return null;
        }

        /// <summary>
        /// Get a FileBuffer for a given filepath
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public FileBuffer getFileBuffer(string filename)
        {
            List<FileBuffer> fbL = m_fileBuffers.Where(item => item.getFilepath().ToUpper() == filename.ToUpper()).ToList();

            if (fbL.Count() > 0)
            {
                return fbL[0];
            }

            return null;

        }

        /// <summary>
        /// Doing the same as above with an IEnumerable
        /// </summary>
        /// <returns></returns>
        public List<FileBuffer> getNonNullFileBuffersIEnumerable()
        {
            return m_fileBuffers.Where(item => ( item.getFilepath() != null && item.getFilepath() != "")).ToList();
        }

        /// <summary>
        /// Clear down this project
        /// </summary>
        public void clear()
        {
            m_fileBuffers.Clear();
            m_bufferViews.Clear();
        }

        /// <summary>
        /// Add an existing FileBuffer and return the index of it
        /// </summary>
        /// <param name="fb"></param>
        /// <returns></returns>
        public int addFileBuffer(FileBuffer fb)
        {
            m_fileBuffers.Add(fb);
            return m_fileBuffers.IndexOf(fb);
        }

        /// <summary>
        /// Remove a BufferView - we don't remove FileBuffers as part of this removal
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
        public bool removeBufferView(BufferView view)
        {
            return m_bufferViews.Remove(view);
        }

        /// <summary>
        /// Remove a FileBuffer by filePath
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool removeFileBuffer(string filePath)
        {
            // Ensure that slashes are matching when we check the path
            //
            FileBuffer result = m_fileBuffers.Find(item => item.getFilepath().Replace(@"\\", @"\").ToUpper() == filePath.Replace(@"\\", @"\").ToUpper());

            if (result == null)
            {
                Logger.logMsg("Project::removeFileBuffer() - cannot find file " + filePath + " to remove");
                return false;
            }

            return removeFileBuffer(result);
        }

        /// <summary>
        /// Find a BufferView that is non-active currently and also doesn't have the indicated
        /// FileBuffer in it - so that we can do a delete on it.
        /// </summary>
        /// <param name="fileBuffer"></param>
        /// <returns></returns>
        public int findNonActiveBufferView(FileBuffer fileBuffer)
        {
            if (m_bufferViews[m_selectedViewId].getFileBuffer() == fileBuffer)
            {
                int returnId = -1;

                for (int i = 0; i < m_bufferViews.Count; i++)
                {
                    if (i != m_selectedViewId)
                    {
                        if (m_bufferViews[i].getFileBuffer() != fileBuffer)
                        {
                            returnId = i;
                        }
                    }
                }

                return returnId;
            }
            else
            {
                return m_selectedViewId;
            }
        }

        /// <summary>
        /// Remove a FileBuffer from our project - any BufferViews that are open against it
        /// are also removed.
        /// </summary>
        /// <param name="fb"></param>
        /// <returns></returns>
        public bool removeFileBuffer(FileBuffer fb)
        {
            // Firstly ensure that FileBuffer exists
            //
            FileBuffer result = m_fileBuffers.Find(item => item.getFilepath().Replace(@"\\", @"\").ToUpper() == fb.getFilepath().Replace(@"\\", @"\").ToUpper());

            if (result == null)
            {
                Logger.logMsg("Project::removeFileBuffer() - cannot find file to remove " + fb.getFilepath());
                return false;
            }

            // Now ensure that we've got the right index for an active BufferView
            //
            int returnId = findNonActiveBufferView(fb);

            if (returnId == -1)
            {
                Logger.logMsg("Project::removeFileBuffer() - can't remove this FileBuffer");
            }
            else
            {
                m_selectedViewId = returnId;
            }

            // Create a removal list
            //
            List<BufferView> removeList = m_bufferViews.Where(item => item.getFileBuffer().getFilepath().Replace(@"\\", @"\").ToUpper() == fb.getFilepath().Replace(@"\\", @"\").ToUpper()).ToList();

            // Can we find an active BufferView which isn't involved in this FileBuffer removal?  If not
            // we can't remove anything yet.
            //
            List<BufferView> remainingList = m_bufferViews.Except(removeList).ToList();

            if (remainingList.Count == 0)
            {
                Logger.logMsg("Project::removeFileBuffer - can't remove this FileBuffer as there are no other BufferViews available to switch to");
                return false;
            }

            // Save this as we need to reassign the m_selectedViewId after deletion
            //
            BufferView activeBufferView = m_bufferViews[m_selectedViewId];

            // If a currently active view is visible then we need to find a new BufferView
            //
            if (m_bufferViews.Contains(activeBufferView))
            {
                // At this point we know we have another BufferView remaining and that
                // this list is safe.
                //
                activeBufferView = remainingList[0];
            };

            // Remove our list from the m_bufferViews
            //
            foreach (BufferView bv in removeList)
            {
                m_bufferViews.Remove(bv);
            }

            // Now we need to update the m_selectedViewId
            //
            m_selectedViewId = m_bufferViews.IndexOf(activeBufferView);

            // Now remove the FileBuffer
            //
            return m_fileBuffers.Remove(fb);
        }

        /// <summary>
        /// Return the project file
        /// </summary>
        /// <returns></returns>
        public string getProjectFile()
        {
            return m_projectFile;
        }

        /// <summary>
        /// Add a pre-existing View to the collection
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
        public int addGenericView(XygloView view)
        {
            m_views.Add(view);
            return m_views.IndexOf(view);
        }

        /// <summary>
        /// Add an existing BufferView and return the index of it
        /// </summary>
        /// <param name="bv"></param>
        /// <returns></returns>
        public int addBufferView(BufferView bv)
        {
            m_bufferViews.Add(bv);
            return m_bufferViews.IndexOf(bv);
        }

        /// <summary>
        /// Add a FileBuffer to this project and ensure that we set the index of the FileBuffer so
        /// that we can reconnect it after deserialisation.
        /// </summary>
        /// <param name="text"></param>
        public BufferView addFileBuffer(FontManager fontManager, string filePath, int fileIndex)
        {
            FileBuffer newFB = new FileBuffer(filePath);
            m_fileBuffers.Add(newFB);

            BufferView newBV = new BufferView(fontManager, newFB, Vector3.Zero, 0, m_bufferViewLength, fileIndex);
            m_bufferViews.Add(newBV);

            return newBV;
        }

        /// <summary>
        /// Return a m_fileBuffer index value for the FileBuffer
        /// </summary>
        /// <param name="fb"></param>
        /// <returns></returns>
        public int getFileIndex(FileBuffer fb)
        {
            for (int i = 0; i < m_fileBuffers.Count(); i++)
            {
                if (fb == m_fileBuffers[i])
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Add a new FileBuffer without an index
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public FileBuffer addFileBuffer(string filePath)
        {
            FileBuffer newFB = new FileBuffer(filePath);
            m_fileBuffers.Add(newFB);
            return newFB;
        }

        /// <summary>
        /// Add a FileBuffer and BufferView relative to an existing position
        /// </summary>
        /// <param name="rootbv"></param>
        /// <param name="position"></param>
        /// <param name="text"></param>
        public BufferView addFileBufferRelative(FontManager fontManager, string filePath, BufferView rootbv, BufferView.ViewPosition position)
        {
            FileBuffer newFB = new FileBuffer(filePath);
            m_fileBuffers.Add(newFB);
            int index = m_fileBuffers.IndexOf(newFB);

            BufferView newBV = new BufferView(fontManager, rootbv, position);
            m_bufferViews.Add(newBV);

            return newBV;
        }

        /// <summary>
        /// Deserialise the xml file and create objects
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        static public Project dataContractDeserialise(FontManager fontManager, string fileName)
        {
            Logger.logMsg("Project::dataContractDeserialise() - deserializing an instance of the Project object.");
            FileStream fs = new FileStream(fileName, FileMode.Open);
            XmlDictionaryReader reader =
                XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas());
            DataContractSerializer ser = new DataContractSerializer(typeof(Project));

            // Deserialize the data and read it from the instance.
            //
            Project deserializedProject;
            try
            {
                deserializedProject = (Project)ser.ReadObject(reader, true);
                deserializedProject.setFontManager(fontManager);
            }
            catch (Exception e)
            {
                Logger.logMsg("Project::dataContractDeserialise() - caught an issue " + e.Message);

                // Use an empty project and clear up the existing project file
                //
                deserializedProject = new Project(fontManager);
                BufferView bv = new BufferView();
                deserializedProject.addBufferView(bv);
                reader.Close();
                fs.Close();
                //File.Delete(fileName);  
            }
            finally
            {
                // Close everything
                //
                reader.Close();
                fs.Close();
            }

            return deserializedProject;
        }

        /// <summary>
        /// Serialise this project to XML
        /// </summary>
        public void dataContractSerialise()
        {
            // Before serialisation we store how long we've been active for
            // and reset the last access time
            //
            DateTime snapshot = DateTime.Now;
            m_activeTime += snapshot - m_lastAccessTime;
            m_lastWriteTime = snapshot;

            FileStream writer = new FileStream(m_projectFile, FileMode.Create);

            System.Runtime.Serialization.DataContractSerializer x =
                new System.Runtime.Serialization.DataContractSerializer(this.GetType());
            x.WriteObject(writer, this);
            writer.Close();
        }

        /// <summary>
        /// Method we can use to make some backups of the project file before we serialise it
        /// </summary>
        public void manageSerialisations()
        {
            int versionsToMaintain = 5;
            List<string> filesExist = new List<string>();

            if (!File.Exists(m_projectFile))
            {
                Logger.logMsg("Project::manageSerialisations() - nothing to do for first pass");
                return;
            }
            
            Logger.logMsg("Project::manageSerialisations() - maintaining file versions");

            try
            {

                // Test for which versions exist
                //
                for (int i = 1; i <= versionsToMaintain; i++)
                {
                    // Pad out a file name
                    //
                    string testFilePath = m_projectFile + "." + i.ToString().PadLeft(4, '0');

                    // If the file exists then 
                    if (File.Exists(testFilePath))
                    {
                        filesExist.Add(testFilePath);
                    }
                }

                // Shift versions if any exist up one number and remove the oldest
                //
                if (filesExist.Count > 0)
                {
                    // Remove/Move in reverse order
                    //
                    for (int i = filesExist.Count; i > 0; i--)
                    {
                        if (i == versionsToMaintain)
                        {
                            File.Delete(filesExist[i - 1]);
                        }
                        else
                        {
                            File.Move(filesExist[i - 1], m_projectFile + "." + (i + 1).ToString().PadLeft(4, '0'));
                        }
                    }
                }

                // Copy file to the first position
                //
                File.Copy(m_projectFile, m_projectFile + ".0001");
            }
            catch (Exception e)
            {
                Logger.logMsg("Project::manageSerialisations() - failed to manage versions with error " + e.Message);
            }
        }

        /// <summary>
        /// XML Serialise - we don't use this
        /// </summary>
        public void xmlSerialise()
        {
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(this.GetType());
            x.Serialize(Console.Out, this);
            //Console.WriteLine();
            //Console.ReadLine();
        }

        /// <summary>
        /// When we are ready we can load all the files that have BufferViews
        /// </summary>
        public void loadFiles()
        {
            Logger.logMsg("Project::loadFiles() - starting");
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start(); 

            // We always need a Syntax Manager when handling files
            //
            if (m_syntaxManager == null)
            {
                // Build the syntax manager - for the moment we force it to Cpp
                //
                m_syntaxManager = new CppSyntaxManager(this);
            }

            // Only load files by active FileBuffers
            //
            foreach (BufferView bv in m_bufferViews)
            {
                Logger.logMsg("Project::loadFiles() - loading " + bv.getFileBuffer().getFilepath(), true);
                bv.getFileBuffer().loadFile(m_syntaxManager);
                Logger.logMsg("Project::loadFiles() - completed loading " + bv.getFileBuffer().getFilepath(), true);

                // Don't generate any highlighting for tailing files as we consider those as outputs
                //
                if (!bv.isTailing())
                {
                    Logger.logMsg("Project::loadFiles() - generating highlighting for " + bv.getFileBuffer().getFilepath(), true);
                    if (getConfigurationValue("SYNTAXHIGHLIGHT").ToUpper() == "TRUE")
                    {
                        m_syntaxManager.generateAllHighlighting(bv.getFileBuffer());
                    }
                }

            }

            // Also reset our access timer
            //
            m_lastAccessTime = DateTime.Now;

            sw.Stop();
            Logger.logMsg("Project::loadFiles() - completed in  " + sw.ElapsedMilliseconds + " ms");
        }


        /// <summary>
        /// Find the Standard Output View
        /// </summary>
        /// <returns></returns>
        public BufferView getStdOutView()
        {
            foreach (BufferView bv in m_bufferViews)
            {
                if (bv.getFileBuffer().getFilepath() == getConfigurationValue("BUILDSTDOUTLOG"))
                {
                    return bv;
                }
            }

            return null;
        }


        /// <summary>
        /// Find the Standard Error View
        /// </summary>
        /// <returns></returns>
        public BufferView getStdErrView()
        {
            foreach (BufferView bv in m_bufferViews)
            {
                if (bv.getFileBuffer().getFilepath() == getConfigurationValue("BUILDSTDERRLOG"))
                {
                    return bv;
                }
            }

            return null;
        }

        /// <summary>
        /// After we have deserialised we need to connect up the objects - this helps
        /// to stop things breaking.  Also we need to initialise things that we haven't
        /// persisted such as commands in the FileBuffers etc.
        /// </summary>
        public void connectFloatingWorld()
        {
            List<BufferView> removeList = new List<BufferView>();

            // Fix our BufferViews
            //
            foreach(BufferView bv in m_bufferViews)
            {
                if (bv.getFileBuffer() == null)
                {
                    if (bv.getFileBufferIndex() < m_fileBuffers.Count)
                    {
                        bv.setFileBuffer(m_fileBuffers[bv.getFileBufferIndex()]);
                    }
                    else
                    {
                        Logger.logMsg(
                            "Project::connectFloatingWorld() - got out of scope FileBuffer reference - removing BufferView");
                        removeList.Add(bv);
                    }
                }

                bv.setFontManager(m_fontManager);
            }

            // Remove the BufferViews which are no longer valid
            //
            foreach(BufferView bv in removeList)
            {
                m_bufferViews.Remove(bv);
            }

            // Ensure that the current BufferView is valid
            //
            if (m_selectedViewId < 0 || m_selectedViewId >= m_bufferViews.Count)
            {
                m_selectedViewId = 0;
            }

            // Fix our FileBuffers
            //
            foreach (FileBuffer fb in m_fileBuffers)
            {
                fb.initialiseAfterDeseralising();
            }


        }

        /// <summary>
        /// Return our font manager instance
        /// </summary>
        /// <returns></returns>
        public FontManager getFontManager()
        {
            return m_fontManager;
        }

        /// <summary>
        /// Set the font manager
        /// </summary>
        /// <param name="fontManager"></param>
        public void setFontManager(FontManager fontManager)
        {
            m_fontManager = fontManager;
        }


        /// <summary>
        /// Return the application directory string
        /// </summary>
        /// <returns></returns>
        static public string getUserDataPath()
        {
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            dir = System.IO.Path.Combine(dir, @"Xyglo\Friendlier\");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Return the log path
        /// </summary>
        /// <returns></returns>
        static public string getLogPath()
        {
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            dir = System.IO.Path.Combine(dir, @"Xyglo\Friendlier\Log\");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Set the selected view in the project by a view reference
        /// </summary>
        /// <param name="view"></param>
        public void setSelectedView(BufferView view)
        {
            int i = 0;
            foreach(BufferView bv in m_bufferViews)
            {
                if (view == bv)
                {
                    m_selectedViewId = i;
                    break;
                }
                ++i;
            }
        }

        /// <summary>
        /// Simple accessor for selected BufferView
        /// </summary>
        /// <returns></returns>
        public BufferView getSelectedBufferView()
        {
            if (m_selectedViewId < m_bufferViews.Count())
            {
                return m_bufferViews[m_selectedViewId];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Set index from BufferView
        /// </summary>
        /// <param name="view"></param>
        public void setSelectedBufferView(BufferView view)
        {
            m_selectedViewId = m_bufferViews.IndexOf(view);
        }

        /// <summary>
        /// Set a View selection - differs to BufferView selection for the moment
        /// </summary>
        /// <param name="view"></param>
        public void setSelectedView(XygloView view)
        {
            if (view == null)
            {
                m_selectedXygloViewId = -1;
            }
            else
            {
                m_selectedXygloViewId = m_views.IndexOf(view);
            }
        }

        /// <summary>
        /// Set a BufferView from a FileBuffer
        /// </summary>
        /// <param name="fb"></param>
        /// <returns></returns>
        public BufferView setSelectedBufferView(FileBuffer fb)
        {
            foreach (BufferView bv in m_bufferViews)
            {
                if (bv.getFileBuffer() == fb)
                {
                    m_selectedViewId = m_bufferViews.IndexOf(bv);
                    return bv;
                }
            }

            return null;
        }

        /// <summary>
        /// Attempt to find a FileBuffer from a filename
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public FileBuffer findFileBuffer(string filename)
        {
            foreach (FileBuffer fb in m_fileBuffers)
            {
                if (fb.getFilepath() == filename)
                {
                    return fb;
                }
            }

            return null;
        }

        /// <summary>
        /// Return an associated BufferView for a filename if one exists
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public BufferView findBufferView(string filename)
        {
            FileBuffer fb = findFileBuffer(filename);

            if (fb != null)
            {
                foreach(BufferView bv in m_bufferViews)
                {
                    FileBuffer bvFB = bv.getFileBuffer();
                    if (bvFB.getFilepath() == fb.getFilepath())
                    {
                        return bv;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get just the command string only from the m_buildCommand
        /// </summary>
        /// <returns></returns>
        public string getCommand(string []command)
        {
            //string[] command = getConfigurationValue("BUILDCOMMAND").Split(' ');

            if (command.Length > 0)
            {
                return command[0];
            }

            return "";
        }

        /// <summary>

        /// Get the argument list only from the m_buildCommand
        /// </summary>
        /// <returns></returns>
        public string getArguments(string []command)
        {
            //string[] command = getConfigurationValue("BUILDCOMMAND").Split(' ');

            string retArgs = "";
            int i = 0;
            foreach (string arg in command)
            {
                if (i++ > 0)
                {
                    retArgs += arg + " ";
                }
            }

            return retArgs;
        }

        /// <summary>
        /// Return total lines of FileBuffers
        /// </summary>
        /// <returns></returns>
        public int getFilesTotalLines()
        {
            int total = 0;

            foreach (FileBuffer fb in m_fileBuffers)
            {
                total += fb.getLineCount();
            }

            return total;
        }

        /// <summary>
        /// Set the eye position prior to persistance
        /// </summary>
        /// <param name="eye"></param>
        public void setEyePosition(Vector3 eye)
        {
            m_eyeSavePosition = eye;
        }

        /// <summary>
        /// Return the eye position - allow for defaults
        /// </summary>
        public Vector3 getEyePosition()
        {
            if (m_eyeSavePosition.Z == 0)
            {
                m_eyeSavePosition.Z = 500.0f;
            }
            return m_eyeSavePosition;
        }

        /// <summary>
        /// Set the target position prior to persistance
        /// </summary>
        /// <param name="eye"></param>
        public void setTargetPosition(Vector3 target)
        {
            m_targetSavePosition = target;
        }

        /// <summary>
        /// Return the target position
        /// </summary>
        public Vector3 getTargetPosition()
        {
            return m_targetSavePosition;
        }

        /// <summary>
        /// Set the background colours either on or off
        /// </summary>
        public void setBackgroundColours(bool state)
        {
            Color bgColour = Color.DeepSkyBlue;
            bgColour.A = 190;

            foreach(BufferView bv in m_bufferViews)
            {
                if (state == false)
                {
                    bv.setBackgroundColour(Color.Black);
                }
                else
                {
                    if (!bv.isReadOnly())
                    {
                        bv.setBackgroundColour(bgColour);
                    }
                }
            }
        }

        /// <summary>
        /// What is our viewing mode - for colouring purposes
        /// </summary>
        /// <returns></returns>
        public ViewMode getViewMode()
        {
            return m_viewMode;
        }

        public void setViewMode(ViewMode viewMode)
        {
            m_viewMode = viewMode;
        }

        /// <summary>
        /// Check the full screen state
        /// </summary>
        /// <returns></returns>
        public bool isFullScreen()
        {
            return m_fullScreen;
        }

        /// <summary>
        /// Set the full screen state
        /// </summary>
        /// <param name="state"></param>
        public void setFullScreen(bool state)
        {
            m_fullScreen = state;
        }

        /// <summary>
        /// Get the window size
        /// </summary>
        /// <returns></returns>
        public Vector2 getWindowSize()
        {
            return m_windowSize;
        }

        /// <summary>
        /// Set the window size
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void setWindowSize(float width, float height)
        {
            m_windowSize.X = width;
            m_windowSize.Y = height;
        }

        /// <summary>
        /// Set the window size
        /// </summary>
        /// <param name="size"></param>
        public void setWindowSize(Vector2 size)
        {
            m_windowSize = size;
        }

        /// <summary>
        /// Get the window position
        /// </summary>
        /// <returns></returns>
        public Vector2 getWindowPostion()
        {
            return m_windowPosition;
        }

        /// <summary>
        /// Set the window position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void setWindowPostion(float x, float y)
        {
            m_windowPosition.X = x;
            m_windowPosition.Y = y;
        }

        /// <summary>
        /// Set the window position
        /// </summary>
        /// <param name="pos"></param>
        public void setWindowPosition(Vector2 pos)
        {
            m_windowPosition = pos;
        }

        /// <summary>
        /// Get the root directory of all the FileBuffers - does this in our own inimitable way.
        /// If we supply a prefix it will look for all roots prefixed with this.  This is horrible.
        /// </summary>
        /// <returns></returns>
        public string getFileBufferRoot(string prefix = "")
        {
            string rS = "";
            prefix = prefix.ToUpper(); // Always upper case in this method

            // Firstly strip out any null paths which might exist for unsaved FileBuffers
            //
            List<FileBuffer> rL = new List<FileBuffer>();

            // Length of the longest file path
            //
            int longLength = 0;

            // Store the id of the longest file path
            //
            int longId = 0;

            // Test initial drive letter
            //
            string testDriveLetter = "";
            string driveLetter = "";

            foreach (FileBuffer fb in getNonNullFileBuffers())
            {
                // Store the drive letter
                //
                if (fb.getFilepath().Length > 0)
                {
                    testDriveLetter = fb.getFilepath().ToUpper().Substring(0, 1);
                    if (driveLetter == "" && testDriveLetter != "")
                    {
                        driveLetter = testDriveLetter;
                    }
                    else
                    {
                        if (driveLetter != testDriveLetter)
                        {
                            return ""; // we have differnet drives here
                        }

                    }
                }


                // Store the length of the longest string
                if (fb.getFilepath().Length > longLength)
                {
                    longLength = fb.getFilepath().Length;
                    longId = rL.Count();
                }
                rL.Add(fb);
                Logger.logMsg("Project::getFileBufferRoot() - added FileBuffer " + fb.getFilepath());
            }

            // Check to see if the prefix has excluded everything
            //
            if (prefix.Length > longLength)
            {
                Logger.logMsg("Project::getFileBufferRoot() - prefix " + prefix + " has excluded all potential FileBuffers");
                return rS;
            }

            // Prepend prefix to test return string
            //
            rS += prefix;

            // Test 
            bool foundDifference = false;
            bool matchedAny = false;
            int lastBackSlash = 0;
            for (int i = prefix.Length; i < longLength; i++)
            {
                rS += rL[longId].getFilepath().ToUpper()[i];

                // Store location of last backslash
                //
                if (rL[longId].getFilepath()[i] == '\\')
                {
                    lastBackSlash = i;
                }

                for (int j = 0; j < rL.Count; j++)
                {
                    if (j != longId)
                    {
                        string check = rL[j].getFilepath().ToUpper();

                        if (rS != check.Substring(0, Math.Min(rS.Length, check.Length)))
                        {
                            foundDifference = true;
                            break;
                        }
                        else
                        {
                            matchedAny = true;
                        }
                    }
                }

                if (foundDifference)
                {
                    break;
                }
            }

            // Test for failure to match any prefixes
            //
            if (!matchedAny)
            {
                Logger.logMsg("Project::getFileBufferRoot() - failed to match any prefixes");
                return "";
            }

            // Truncate up to last backslash
            //
            if (lastBackSlash > 0)
            {
                rS = rS.Substring(0, lastBackSlash + 1);
            }

            return rS;
        }

        /// <summary>
        /// Return the syntax manager hanlde
        /// </summary>
        /// <returns></returns>
        public SyntaxManager getSyntaxManager()
        {
            return m_syntaxManager;
        }

        /// <summary>
        /// Get a tab character(s)
        /// </summary>
        /// <returns></returns>
        public string getTab()
        {
            return m_tab;
        }

        /// <summary>
        /// Set the tab string
        /// </summary>
        /// <param name="tabString"></param>
        public void setTab(string tabString)
        {
            m_tab = tabString;
        }

        /// <summary>
        /// This might do as good a job as any complex algorithm
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public string estimateFileStringTruncation(string root, string fileName, int maxLength)
        {
            string rs = root + fileName;

            if (rs.Length < maxLength)
            {
                return rs;

            }
            int halfLength = (maxLength / 2) - 2;

            if (halfLength > 0)
            {
                rs = rs.Substring(0, halfLength) + "..." + rs.Substring(rs.Length - halfLength, halfLength);
            }

            return rs;
        }

        /*
        /// <summary>
        /// Build a sensible length file string from the passed arguments - to ensure that 
        /// the string fits on a page
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public string buildFileString(string root, string fileName, int maxLength)
        {
            if (root.Length + fileName.Length < maxLength)
            {
                return root + fileName;
            }
            else
            {
                string tryRoot = root;
                string tryFile = fileName;
                int lastLength = tryRoot.Length + tryFile.Length;

                while (lastLength > maxLength)
                {
                    if (tryRoot.Length > fileName.Length)
                    {
                        string newPath = truncateTrailingDirectory(tryRoot);

                        if (newPath != tryRoot)
                        {
                            tryRoot = newPath + "...";
                        }
                    }
                    else // fileName is longer
                    {
                        string newFile = truncateLeadingDirectory(tryFile);

                        if (newFile != tryFile)
                        {
                            tryFile = "..." + newFile;
                        }
                    }

                    if (tryRoot.Length + tryFile.Length < lastLength)
                    {
                        lastLength = tryRoot.Length + tryFile.Length;
                    }
                    else
                    {
                        // Do something radical
                        //
                        string umm = "???";
                    }
                }

                return tryRoot + tryFile;

            }

        }
        */

        /// <summary>
        /// Truncate the trailing directory from a path
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        public string truncateTrailingDirectory(string dirPath)
        {
            int forwardSlashes = dirPath.Count(item => item == '/');
            int backSlashes = dirPath.Count(item => item == '\\');

            // First we try with forward slashes
            //
            if (forwardSlashes > 0)
            {
                int lastSlash = dirPath.LastIndexOf('/');

                if (lastSlash > 0)
                {
                    return dirPath.Substring(0, lastSlash - 1);
                }
            }
            else if (backSlashes > 0) // try backslashes
            {
                int lastSlash = dirPath.LastIndexOf('\\');

                if (lastSlash > 0)
                {
                    return dirPath.Substring(0, lastSlash - 1);
                }
            }

            return dirPath;
        }

        /// <summary>
        /// Truncate a leading directory
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        public string truncateLeadingDirectory(string dirPath)
        {
            int forwardSlashes = dirPath.Count(item => item == '/');
            int backSlashes = dirPath.Count(item => item == '\\');

            if (forwardSlashes > 0)
            {
                int firstSlash = dirPath.IndexOf('/');

                if (firstSlash > 0)
                {
                    return dirPath.Substring(firstSlash + 1, dirPath.Length - (firstSlash + 1));
                }
            }
            else if (backSlashes > 0) // try backslashes
            {
                int firstSlash = dirPath.IndexOf('\\');

                if (firstSlash > 0)
                {
                    return dirPath.Substring(firstSlash + 1, dirPath.Length - (firstSlash + 1));
                }
            }

            return dirPath;
        }

        /// <summary>
        /// Testing whether arrived in bounding sphere
        /// </summary>
        protected BoundingSphere m_testArrived = new BoundingSphere();

        /// <summary>
        /// Test result
        /// </summary>
        protected ContainmentType m_testResult;

        /// <summary>
        /// Search our active BufferViews and see if the current eye position is over anything new
        /// </summary>
        /// <param name="eyePosition"></param>
        /// <returns></returns>
        public BufferView testNearBufferView(Vector3 eyePosition)
        {
            Vector3 testPosition;
            foreach (BufferView bv in m_bufferViews)
            {
                // Get the eye position from the BufferView but ignore the Z component
                //
                testPosition = bv.getEyePosition();
                testPosition.Z = eyePosition.Z;

                m_testArrived.Center = testPosition;
                m_testArrived.Radius = 200.0f; // scale this by the bufferview sizes
                m_testArrived.Contains(ref eyePosition, out m_testResult);

                if (m_testResult == ContainmentType.Contains)
                {
                    return bv;
                }
            }

            return null;
        }

        /// <summary>
        /// Get the maximum extent of the project in terms of screen coverage
        /// </summary>
        /// <returns></returns>
        public BoundingBox getBoundingBox()
        {
            BoundingBox rB = new BoundingBox();

            if (m_bufferViews.Count > 0)
            {
                rB = m_bufferViews.First().getBoundingBox();

                foreach (BufferView bv in m_bufferViews)
                {
                    if (bv.getBoundingBox().Min.X <= rB.Min.X &&
                        bv.getBoundingBox().Min.Y <= rB.Min.Y &&
                        bv.getBoundingBox().Min.Z <= rB.Min.Z)
                    {
                        rB.Min = bv.getBoundingBox().Min;
                    }

                    if (bv.getBoundingBox().Max.X >= rB.Max.X &&
                        bv.getBoundingBox().Max.Y >= rB.Max.Y &&
                        bv.getBoundingBox().Max.Z >= rB.Max.Z)
                    {
                        rB.Max = bv.getBoundingBox().Max;
                    }
                }
            }

            return rB;
        }


        /// <summary>
        /// Check to see if a Ray passes through one of our BufferViews.  If it does then we
        /// work out whereabouts on the BufferView this hits - we return a ScreenPosition but
        /// this refers not a screen relative position but to a Screen (expanded tabs) position
        /// within the file.
        /// </summary>
        /// <param name="ray"></param>
        /// <returns></returns>
        public Pair<BufferView, Pair<ScreenPosition, ScreenPosition>> testRayIntersection(Ray ray)
        {
#if TEST_RAY
            // Some test code
            //
            //Ray testRay = new Ray(new Vector3(0, 0, 500), new Vector3(0, 0, -1));
            //Plane newPlane = new Plane(new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0));
            //float? testIntersects = testRay.Intersects(newPlane);

            if (testIntersects != null)
            {
                Logger.logMsg("Got intersect");
            }

            float? planeIntersection = ray.Intersects(newPlane);

            if (planeIntersection != null)
            {
                Logger.logMsg("Got PLANE intersect");
            }

#endif // TEST_RAY

            // Return BufferView
            //
            BufferView rBV = null;
            ScreenPosition rFP = new ScreenPosition();
            ScreenPosition rSP = new ScreenPosition();

            // Hypotenuse of our ray - distance to where ray hits the bounding box of the BufferView
            //
            float? hyp = null;

            foreach (BufferView bv in m_bufferViews)
            {
                float? testBB = bv.getBoundingBox().Intersects(ray);

                BoundingBox bb = bv.getBoundingBox();
                if (bb.Max.Z == bb.Min.Z)
                {
                    bb.Max.Z++;
                }
                hyp = ray.Intersects(bb);

                if (hyp != null)
                {
                    rBV = bv;
                    break;
                }
            }

            // If we've found one then work out where abouts on the BufferView we're clicking
            //
            if (rBV != null && hyp != null)
            {
                Vector3 intersectPos = new Vector3();

                double dHyp = (double)hyp; // convert this here

                intersectPos.X = (float)(ray.Position.X + dHyp * (Math.Atan(((double)(-ray.Direction.X)) / ((double)(ray.Direction.Z)))));
                intersectPos.Y = (float)(ray.Position.Y + dHyp * (Math.Atan((((double)-ray.Direction.Y)) / ((double)(ray.Direction.Z)))));
                intersectPos.Z = 0.0f;

                // Now convert the bufferview screen position to a cursor position.
                // Firstly remove the BufferView positional offset.
                //
                intersectPos.X -= rBV.getTopLeft().X;
                intersectPos.Y -= rBV.getTopLeft().Y;

                // This leaves the relative position within the BufferView adjusting for offsets
                //
                rFP.X = (int)(intersectPos.X / getFontManager().getCharWidth()) + rBV.getBufferShowStartX();
                rFP.Y = (int)(intersectPos.Y / getFontManager().getLineSpacing()) + rBV.getBufferShowStartY();

                // We also want to store the ScreenPosition at this point and return it 
                // as we have no way of calculating externally (easily) the relative position
                // on the screen considering wrappedlines (tailing buffers).
                //
                rSP.X = (int)(intersectPos.X / getFontManager().getCharWidth());
                rSP.Y = (int)(intersectPos.Y / getFontManager().getLineSpacing());

                Logger.logMsg("Project::testRayIntersection() - got FilePosition of X = " + rFP.X + ", Y = " + rFP.Y);

                if (rBV.isTailing())
                {
                    int wrappedLine = rFP.Y - rBV.getBufferShowStartY();
                    int fileLine = rBV.convertWrappedLineToFileLine(wrappedLine);

                    if (fileLine != -1)
                    {
                        Logger.logMsg("GET REAL LINE = " + rBV.getFileBuffer().getLine(fileLine));

                        // Set the return file position to the correct row - nice bug here, we were
                        // doing all the hard work and then forgetting to set the return value..
                        //
                        rFP.Y = fileLine;
                    }
                }
            }

            return new Pair<BufferView, Pair<ScreenPosition, ScreenPosition>>(rBV, new Pair<ScreenPosition, ScreenPosition>(rFP, rSP));
        }

        /// <summary>
        /// Get a filename and line, column value from a string taken from a build standard error file.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public List<Pair<string, Pair<int, int>>> getFileNamesFromText(string text)
        {
            List<Pair<string, Pair<int, int>>> rL = new List<Pair<string, Pair<int, int>>>();

            // Firstly try and find a FileName
            //
            Regex fileName = new Regex(@"[A-Za-z]+[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+");
            Regex lineNumber = new Regex(@"[A-Za-z]+[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+:[0-9]+");
            MatchCollection matches = fileName.Matches(text);

            foreach (Match m in matches)
            {
                Pair<int, int> lineColumn = new Pair<int,int>();

                // Default line and column to 0
                //
                lineColumn.First = 0;
                lineColumn.Second = 0;

                // Test and set line if we find it
                //
                MatchCollection lineMatches = lineNumber.Matches(text);

                int line = -1;
                foreach (Match l in lineMatches)
                {
                    line = Convert.ToInt16(l.ToString().Split(':')[1]);
                }

                if (line != -1)
                {
                    lineColumn.First = line;
                }

                // Push onto return collection
                //
                rL.Add(new Pair<string, Pair<int, int>>(m.ToString(), lineColumn));
            }

            return rL;
        }

        /// <summary>
        /// Send this method some text and it'll try to work out a FileBuffer for it and a ScreenPosition in
        /// that FileBuffer if possible.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public Pair<FileBuffer, ScreenPosition> findFileBufferFromText(string text, ModelBuilder model)
        {
            Pair<FileBuffer, ScreenPosition> rF = new Pair<FileBuffer, ScreenPosition>();

            // Default first 
            //
            rF.First = null;
            ScreenPosition sP = new ScreenPosition();

            // Get the list of potentials
            //
            List<Pair<string, Pair<int, int>>> matches = getFileNamesFromText(text);

            foreach (Pair<string, Pair<int, int>> m in matches)
            {
                // Try and find match in existing FileBuffers
                //
                foreach(FileBuffer fb in m_fileBuffers)
                {
                    if (fb.getFilepath().ToUpper().Contains(m.First.ToUpper()))
                    {
                        rF.First = fb; // set filename
                        sP.Y = m.Second.First;  // set line
                        sP.X = m.Second.Second; // set column

                        rF.Second = sP;
                        break;
                    }
                }
            }

            return rF;
        }


        /// <summary>
        /// Convert a File line to a Screen line with spaces in it
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public string fileToScreen(string line)
        {
            return line.Replace("\t", getTab());
        }

        /// <summary>
        /// Turn a FilePosition to ScreenPosition X coordinate allowing for tabs
        /// </summary>
        /// <param name="line"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public int fileToScreen(string line, int filePositionX)
        {
            return line.Substring(0, filePositionX).Replace("\t", getTab()).Length;
        }

        /// <summary>
        /// Accept a file name 
        /// </summary>
        /// <param name="fileLine"></param>
        /// <param name="screenPositionX"></param>
        /// <returns></returns>
        public int screenToFile(string fileLine, int screenPositionX)
        {
            string screenString = fileLine.Replace("\t", getTab());
            int rP = 0; // return position (in file)
            int sP = 0; // screen position

            // Test for single char tabs
            if (screenString.Length == fileLine.Length)
            {
                return screenPositionX;
            }

            // Return position will be at most the screenPositionX or 
            // the screenString length if it's shorter (error).
            //
            while (sP < Math.Min(screenPositionX, screenString.Length))
            {
                if (fileLine[rP] == '\t')
                {
                    sP += getTab().Length;
                }
                else
                {
                    sP++;
                }
                rP++;
            }

            Logger.logMsg("Project::screenToFile() - rP " + rP);
            return rP;
        }

        /// <summary>
        /// Set the directory where we are opening files from.
        /// </summary>
        /// <param name="directory"></param>
        public void setOpenDirectory(string directory)
        {
            m_openDirectory = directory;
        }

        /// <summary>
        /// Get the directory where files are being opened from
        /// </summary>
        /// <returns></returns>
        public string getOpenDirectory()
        {
            return m_openDirectory;

        }

        /// <summary>
        /// Set stdout line
        /// </summary>
        /// <param name="line"></param>
        public void setStdOutLastLine(int line)
        {
            m_stdOutLastLine = line;
        }

        /// <summary>
        /// Set last stderr line
        /// </summary>
        /// <param name="line"></param>
        public void setStdErrLastLine(int line)
        {
            m_stdErrLastLine = line;
        }

        /// <summary>
        /// /// Last line standard output was written to on
        /// </summary>
        /// <returns></returns>
        public int getStdOutLastLine()
        {
            return m_stdOutLastLine;
        }

        /// <summary>
        /// Last line standard error was written to on
        /// </summary>
        /// <returns></returns>
        public int getStdErrLastLine()
        {
            return m_stdErrLastLine;
        }

        /// <summary>
        /// External project definition file - Makefile or .pro file for example
        /// </summary>
        /// <returns></returns>
        public string getExternalProjectDefinitionFile()
        {
            return m_externalProjectDefinitionFile;
        }

        /// <summary>
        /// External project base directory
        /// </summary>
        /// <returns></returns>
        public string getExternalProjectBaseDirectory()
        {
            return m_externalProjectBaseDirectory;
        }

        /// <summary>
        /// Set the external project definition file
        /// </summary>
        /// <param name="file"></param>
        public void setExternalProjectDefinitionFile(string file)
        {
            m_externalProjectDefinitionFile = file;
        }

        /// <summary>
        /// Set the external project base directory
        /// </summary>
        /// <param name="directory"></param>
        public void setExternalProjectBaseDirectory(string directory)
        {
            m_externalProjectBaseDirectory = directory;
        }

        /// <summary>
        /// Find some room for a specified rectangle somewhere in the vicinity of the
        /// supplied bufferview.
        /// </summary>
        /// <param name="bb"></param>
        /// <returns></returns>
        public Vector3 getFreePosition(BufferView bv, float width, float height)
        {
            // Scale factor
            //
            int factor = 1;

            // Initial BoundingBox setup
            //
            Vector3 newPosition = bv.calculateRelativePositionVector(BufferView.ViewPosition.Right, factor);

            // maxPosition of BoundingBox
            //
            Vector3 bbSize = new Vector3(width, height, 0);
            Vector3 maxPosition = newPosition + bbSize;

            // New BoundingBox
            //
            BoundingBox newBB = new BoundingBox(newPosition, maxPosition);

            while (checkBufferViewOverlaps(newBB))
            {
                newPosition = bv.calculateRelativePositionVector(BufferView.ViewPosition.Right, factor);
                newBB.Min = newPosition;
                newBB.Max = newPosition + bbSize;

                if (!checkBufferViewOverlaps(newBB))
                {
                    break;
                }

                newPosition = bv.calculateRelativePositionVector(BufferView.ViewPosition.Above, factor);
                newBB.Min = newPosition;
                newBB.Max = newPosition + bbSize;

                if (!checkBufferViewOverlaps(newBB))
                {
                    break;
                }

                newPosition = bv.calculateRelativePositionVector(BufferView.ViewPosition.Left, factor);
                newBB.Min = newPosition;
                newBB.Max = newPosition + bbSize;

                if (!checkBufferViewOverlaps(newBB))
                {
                    break;
                }

                newPosition = bv.calculateRelativePositionVector(BufferView.ViewPosition.Below, factor);
                newBB.Min = newPosition;
                newBB.Max = newPosition + bbSize;

                if (!checkBufferViewOverlaps(newBB))
                {
                    break;
                }

                factor++;
            }

            return newPosition;
        }

        /// <summary>
        /// Calculate a good position for a new BufferView close to the existing one.
        /// </summary>
        /// <param name="bv"></param>
        /// <returns></returns>
        public Vector3 getBestBufferViewPosition(BufferView bv)
        {
            // Setup the new BoundingBox as a copy of the original
            //
            BoundingBox newBB = bv.getBoundingBox();

            // Store the size of it
            //
            Vector3 bbSize = newBB.Max - newBB.Min;

            // Our new potential position
            //
            Vector3 newPosition = new Vector3();

            // Scale factor
            //
            int factor = 1;

            while (checkBufferViewOverlaps(newBB))
            {
                newPosition = bv.calculateRelativePositionVector(BufferView.ViewPosition.Right, factor);
                newBB.Min = newPosition;
                newBB.Max = newPosition + bbSize;

                if (!checkBufferViewOverlaps(newBB))
                {
                    break;
                }

                newPosition = bv.calculateRelativePositionVector(BufferView.ViewPosition.Above, factor);
                newBB.Min = newPosition;
                newBB.Max = newPosition + bbSize;

                if (!checkBufferViewOverlaps(newBB))
                {
                    break;
                }

                newPosition = bv.calculateRelativePositionVector(BufferView.ViewPosition.Left, factor);
                newBB.Min = newPosition;
                newBB.Max = newPosition + bbSize;

                if (!checkBufferViewOverlaps(newBB))
                {
                    break;
                }

                newPosition = bv.calculateRelativePositionVector(BufferView.ViewPosition.Below, factor);
                newBB.Min = newPosition;
                newBB.Max = newPosition + bbSize;

                if (!checkBufferViewOverlaps(newBB))
                {
                    break;
                }

                factor++;
            }

            return newPosition;
        }

        /// <summary>
        /// Check for BufferView intersections
        /// </summary>
        /// <param name="bv1"></param>
        /// <param name="bv2"></param>
        /// <returns></returns>
        public bool checkBufferViewOverlap(BufferView bv1, BufferView bv2)
        {
            return bv1.getBoundingBox().Intersects(bv2.getBoundingBox());
        }

        /// <summary>
        /// Check project for BufferView collisions with this BoundingBox
        /// </summary>
        /// <param name="bb"></param>
        /// <returns></returns>
        public bool checkBufferViewOverlaps(BoundingBox bb)
        {
            foreach (BufferView bv in m_bufferViews)
            {
                if (bv.getBoundingBox().Intersects(bb))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Set the LHS of a diff
        /// </summary>
        /// <param name="view"></param>
        public void setLHSDiff(BufferView view)
        {
            m_leftDiffBufferViewIndex = m_bufferViews.IndexOf(view);
        }

        /// <summary>
        /// Set the RHS of a diff
        /// </summary>
        /// <param name="view"></param>
        public void setRHSDiff(BufferView view)
        {
            m_rightDiffBufferViewIndex = m_bufferViews.IndexOf(view);
        }

        /// <summary>
        /// RHS diff
        /// </summary>
        /// <returns></returns>
        public int getRHSDiff()
        {
            return m_rightDiffBufferViewIndex;
        }

        /// <summary>
        /// LHS diff
        /// </summary>
        /// <returns></returns>
        public int getLHSDiff()
        {
            return m_leftDiffBufferViewIndex;
        }

        /// <summary>
        /// Set RHS by index
        /// </summary>
        /// <param name="index"></param>
        public void setRHSDiff(int index)
        {
            m_rightDiffBufferViewIndex = index;
        }

        /// <summary>
        /// Set LHS by index
        /// </summary>
        /// <param name="index"></param>
        public void setLHSDiff(int index)
        {
            m_leftDiffBufferViewIndex = index;
        }
    }

    /// <summary>
    /// http://www.extensionmethod.net/Details.aspx?ID=152
    /// </summary>
    public static class LinqExtensions
    {
        /*
        public static IEnumerable<IEnumerable<T>> Transpose<T>(this IEnumerable<IEnumerable<T>> values)
        {
            if (values.Count() == 0)
                return values;
            if (values.First().Count() == 0)
                return Transpose(values.Skip(1));

            var x = values.First().First();
            var xs = values.First().Skip(1);
            var xss = values.Skip(1);
         * 
            return
                new[] {new[] {x}
        .Concat(xss.Select(ht => ht.First()))}
                .Concat(new[] { xs }
                .Concat(xss.Select(ht => ht.Skip(1)))
                .Transpose());
        }*/

        /// <summary>
        /// http://stackoverflow.com/questions/2070356/find-common-prefix-of-strings
        /// 
        /// Use:
        /// 
        /// string[] xs = new[] { "h:/a/b/c", "h:/a/b/d", "h:/a/b/e", "h:/a/c" };
        /// string x = string.Join("\\", xs.Select(s => s.Split('\\').AsEnumerable()).Transpose().TakeWhile(s => s.All(d => d == s.First())).Select(s => s.First())); 
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> Transpose<T>(this IEnumerable<IEnumerable<T>> source)
        {
            var enumerators = source.Select(e => e.GetEnumerator()).ToArray();
            try
            {
                while (enumerators.All(e => e.MoveNext()))
                {
                    yield return enumerators.Select(e => e.Current).ToArray();
                }
            }
            finally
            {
                Array.ForEach(enumerators, e => e.Dispose());
            }
        }
    }
}
