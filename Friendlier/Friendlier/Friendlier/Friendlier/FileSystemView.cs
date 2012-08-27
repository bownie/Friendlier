#region File Description
//-----------------------------------------------------------------------------
// FileSystemView.cs
//
// Copyright (C) Xyglo Ltd. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;


namespace Xyglo
{
    /// <summary>
    /// Convenience Xyglo class to keep all the file and drive related stuff together
    /// </summary>
    public class FileSystemView 
    {
        ///////////////////// MEMBER VARIABLES //////////////////////

        /// <summary>
        /// Our current directory
        /// </summary>
        protected string m_path = null;

        /// <summary>
        /// Are we scanning at drive level?
        /// </summary>
        bool m_driveLevel = false;

        /// <summary>
        /// Directory information object
        /// </summary>
        protected DirectoryInfo m_directoryInfo;

        /// <summary>
        /// File information object
        /// </summary>
        protected FileInfo m_fileInfo;

        /// <summary>
        /// Project associated with this FileSystemView
        /// </summary>
        Project m_project;

        /// <summary>
        /// Index of the currently highlighted directory in a directory picker
        /// </summary>
        protected int m_directoryHighlight = 0;

        /// <summary>
        /// Position in 3D land
        /// </summary>
        Vector3 m_position;

        ///////////////////////// CONSTRUCTORS ////////////////////////
        public FileSystemView(string path, Vector3 position, Project project)
        {
            m_path = fixPathEnding(path);
            m_position = position;
            m_project = project;
            scanDirectory();
        }

        /// <summary>
        /// Set the project
        /// </summary>
        /// <param name="project"></param>
        public void setProject(Project project)
        {
            m_project = project;
        }


        ////////////////////////// METHODS ///////////////////////////

        /// <summary>
        /// Ensure that a path always ends in a backslash
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        protected string fixPathEnding(string path)
        {
            if (path.Length > 0)
            {
                if (path[path.Length - 1] != '\\')
                {
                    path += @"\";
                }
            }

            return path;
        }

        /// <summary>
        /// Return current directory
        /// </summary>
        /// <returns></returns>
        public string getPath()
        {
            return m_path;
        }

        /// <summary>
        /// Reset to this directory and test for directory and file access at this level
        /// </summary>
        /// <param name="directory"></param>
        public void setDirectory(string directory)
        {
            if (directory != null)
            {
                m_driveLevel = false;
                m_path = fixPathEnding(directory);
                scanDirectory();
            }
            else
            {
                m_driveLevel = true;
                m_directoryHighlight = 0;
            }
        }

        /// <summary>
        /// Take the drive letter of the current value m_directoryHighlight
        /// </summary>
        public void setHighlightedDrive()
        {
            int i = 0;    
            foreach (DriveInfo drive in getDrives())
            {
                if (!drive.IsReady)
                {
                    continue;
                }

                if (i++ == m_directoryHighlight)
                {
                    setDirectory(drive.Name);
                    m_directoryHighlight = 0;
                    return;
                }
            }
        }

        /// <summary>
        /// Are we scanning at drive level?
        /// </summary>
        /// <returns></returns>
        public bool atDriveLevel()
        {
            return m_driveLevel;
        }

        /// <summary>
        /// Getting drive info for active drives
        /// </summary>
        /// <returns></returns>
        public DriveInfo[] getDrives()
        {
            return DriveInfo.GetDrives();
        }

        /// <summary>
        /// Return the count of active drives
        /// </summary>
        /// <returns></returns>
        public int countActiveDrives()
        {
            int count = 0;
            foreach (DriveInfo drive in getDrives())
            {
                if (drive.IsReady)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Fetch the directory information - simple wrapper
        /// </summary>
        /// <returns></returns>
        public DirectoryInfo getDirectoryInfo()
        {
            return m_directoryInfo;
        }

        /// <summary>
        /// Fetch the sub directories of this node
        /// </summary>
        /// <returns></returns>
        public DirectoryInfo[] getSubDirectories()
        {
            return m_directoryInfo.GetDirectories();
        }

        /// <summary>
        /// Get the parent directory
        /// </summary>
        /// <returns></returns>
        public DirectoryInfo getParent()
        {
            return m_directoryInfo.Parent;
        }

        // Get total number of directories and files
        //
        public int getDirectoryLength()
        {
            return (m_directoryInfo.GetDirectories().Length + m_directoryInfo.GetFiles().Length);
        }

        protected void scanDirectory()
        {
            // Attempt to get Directory and File info for this path
            //
            m_directoryInfo = new DirectoryInfo(m_path);
            m_fileInfo = new FileInfo(m_path);
        }

        public Vector3 getPosition()
        {
            return m_position;
        }

        public Vector3 getEyePosition()
        {
            Vector3 rV = m_position;
            rV.Y = -rV.Y; // invert Y
            rV.X += m_project.getFontManager().getCharWidth(m_project.getSelectedBufferView().getViewSize()) * m_project.getSelectedBufferView().getBufferShowWidth() / 2;
            rV.Y -= m_project.getFontManager().getLineSpacing(m_project.getSelectedBufferView().getViewSize()) * m_project.getSelectedBufferView().getBufferShowLength() / 2;
            rV.Z += 600.0f;
            return rV;
        }

        /// <summary>
        /// Get the highlight index
        /// </summary>
        /// <returns></returns>
        public int getHighlightIndex()
        {
            return m_directoryHighlight;
        }

        /// <summary>
        /// Set highlight position
        /// </summary>
        /// <param name="directoryHighlight"></param>
        public void setHighlightIndex(int directoryHighlight)
        {
            m_directoryHighlight = directoryHighlight;
        }

        /// <summary>
        /// Increment highlight position
        /// </summary>
        /// <param name="inc"></param>
        public void incrementHighlightIndex(int inc)
        {
            m_directoryHighlight += inc;
        }

        /// <summary>
        /// Get the highlight position
        /// </summary>
        /// <returns></returns>
        public string getHighlightedFile()
        {
            string file = m_directoryInfo.FullName;

            // Now work out the directory or filename
            //
            return file;
        }

        List<string> m_fileHolder = new List<string>();

        public void directorySearch(string sDir, string filename) 
        {
            try	
            {
                foreach (string d in Directory.GetDirectories(sDir)) 
                {
                    foreach (string f in Directory.GetFiles(d, filename)) 
                    {
                        m_fileHolder.Add(f);
                    }
                    directorySearch(d, filename);
                }
            }
            catch (System.Exception excpt) 
            {
                Console.WriteLine(excpt.Message);
            }
        }

        public List<string> getSearchDirectories()
        {
            return m_fileHolder;
        }

        public void clearSearchDirectories()
        {
            m_fileHolder.Clear();
        }
    }
}
