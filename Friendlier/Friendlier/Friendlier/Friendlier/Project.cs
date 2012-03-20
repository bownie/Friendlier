using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Xyglo
{

    /// <summary>
    /// Configuration item
    /// </summary>
    [DataContract(Name = "Friendlier", Namespace = "http://www.xyglo.com")]
    public class Configuration
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
    }

    /// <summary>
    /// A Friendlier project file
    /// </summary>
    [DataContract(Name = "Friendlier", Namespace = "http://www.xyglo.com")]
    public class Project
    {
        //////////// MEMBER VARIABLES ///////////////

        /// <summary>
        /// The name of our project
        /// </summary>
        [DataMember]
        public string m_projectName
        { get; set; }

        [DataMember]
        public string m_projectFile
        { get; set; }

        /// <summary>
        /// List of the FileBuffers attached to this project
        /// </summary>
        [DataMember()]
        protected List<FileBuffer> m_fileBuffers = new List<FileBuffer>();

        /// <summary>
        /// List of the BufferViews attached to this project
        /// </summary>
        [DataMember()]
        protected List<BufferView> m_bufferViews = new List<BufferView>();

        /// <summary>
        /// Length of the visible BufferView for this project
        /// </summary>
        [DataMember()]
        protected int m_bufferViewLength = 20;

        // Which view is currently selected in the project
        //
        [DataMember()]
        protected int m_selectedViewId = 0;

        /// <summary>
        /// When this project was initially created - when we persist this it should
        /// only ever take the initial value here.
        /// </summary>
        [DataMember()]
        protected DateTime m_creationTime = DateTime.Now;

        /// <summary>
        /// When this project is reconstructed this value with be updated on default construction
        /// </summary>
        [DataMember()]
        protected DateTime m_lastAccessTime;

        /// <summary>
        /// Our local configuration
        /// </summary>
        [DataMember]
        protected List<Configuration> m_configuration;

        /// <summary>
        /// Store where we last opened a file from
        /// </summary>
        protected string m_openDirectory = "";

        /// <summary>
        /// Store where we last saved a file to
        /// </summary>
        protected string m_saveDirectory;

        ////////// CONSTRUCTORS ///////////

        /// <summary>
        /// Default constructor 
        /// </summary>
        public Project()
        {
            m_projectName = "<unnamed>";
            m_lastAccessTime = DateTime.Now;

            // Build some configuation if we need to
            //
            buildInitialConfiguration();
        }


        /// <summary>
        /// Name constructor
        /// </summary>
        /// <param name="name"></param>
        public Project(string name, string projectFile)
        {
            m_projectName = name;
            m_lastAccessTime = DateTime.Now;
            m_projectFile = projectFile;

            // Build some configuation if we need to
            //
            buildInitialConfiguration();
        }

        ////////////// METHODS ////////////////

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

                // And for the moment populate from here
                //
                addConfigurationItem("BUILDCOMMAND", @"C:\QtSDK\mingw\bin\mingw32-make.exe -f D:\garderobe-build-desktop\Makefile");
                addConfigurationItem("BUILDDIRECTORY", @"D:\garderobe-build-desktop");
                addConfigurationItem("BUILDLOG", @"C:\temp\output.log");
                addConfigurationItem("AUTOINDENT", "TRUE");
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
        /// Remove a FileBuffer from our project - any BufferViews that are open against it
        /// are also removed.
        /// </summary>
        /// <param name="fb"></param>
        /// <returns></returns>
        public bool removeFileBuffer(FileBuffer fb)
        {
            // Firstly ensure that FileBuffer exists
            //
            FileBuffer result = m_fileBuffers.Find(item => item.getFilepath() == fb.getFilepath());

            if (result != null)
            {
                Logger.logMsg("Project::removeFileBuffer() - cannot find file to remove " + fb.getFilepath());
                return false;
            }

            // Create a removal list
            //
            List<BufferView> removeList = new List<BufferView>();

            foreach (BufferView bv in m_bufferViews)
            {
                if (bv.getFileBuffer().getFilepath() == fb.getFilepath())
                {
                    removeList.Add(bv);
                }
            }

            // Remove our list from the m_bufferViews
            //
            foreach (BufferView bv in removeList)
            {
                m_bufferViews.Remove(bv);
            }

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
        public BufferView addFileBuffer(string filePath, int fileIndex)
        {
            FileBuffer newFB = new FileBuffer(filePath);
            m_fileBuffers.Add(newFB);

            BufferView newBV = new BufferView(newFB, Vector3.Zero, 0, m_bufferViewLength, fileIndex);
            m_bufferViews.Add(newBV);

            return newBV;
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
        public BufferView addFileBufferRelative(string filePath, BufferView rootbv, BufferView.BufferPosition position)
        {
            FileBuffer newFB = new FileBuffer(filePath);
            m_fileBuffers.Add(newFB);
            int index = m_fileBuffers.IndexOf(newFB);

            BufferView newBV = new BufferView(rootbv, position);
            m_bufferViews.Add(newBV);

            return newBV;
        }

        /// <summary>
        /// Deserialise the xml file and create objects
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        static public Project dataContractDeserialise(string fileName)
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
            }
            catch (Exception e)
            {
                Logger.logMsg("Project::dataContractDeserialise() - caught an issue " + e.Message);

                // Use an empty project and clear up the existing project file
                //
                deserializedProject = new Project();
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
            FileStream writer = new FileStream(m_projectFile, FileMode.Create);

            System.Runtime.Serialization.DataContractSerializer x =
                new System.Runtime.Serialization.DataContractSerializer(this.GetType());
            x.WriteObject(writer, this);
            writer.Close();
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
        /// When we are ready we can load all the files
        /// </summary>
        public void loadFiles()
        {
            foreach (FileBuffer fb in m_fileBuffers)
            {
                fb.loadFile();
            }
        }

        /// <summary>
        /// After we have deserialised we need to connect up the objects - this helps
        /// to stop things breaking.  Also we need to initialise things that we haven't
        /// persisted such as commands in the FileBuffers etc.
        /// </summary>
        public void connectFloatingWorld()
        {
            // Fix our BufferViews
            //
            foreach(BufferView bv in m_bufferViews)
            {
                if (bv.getFileBuffer() == null)
                {
                    bv.setFileBuffer(m_fileBuffers[bv.getFileBufferIndex()]);
                }
            }

            // Fix our FileBuffers
            //
            foreach (FileBuffer fb in m_fileBuffers)
            {
                fb.initialiseAfterDeseralising();
            }
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
                    if (bv.getFileBuffer() == fb)
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
        public string getCommand()
        {
            string[] command = getConfigurationValue("BUILDCOMMAND").Split(' ');

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
        public string getArguments()
        {
            string[] command = getConfigurationValue("BUILDCOMMAND").Split(' ');

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
    }
}
