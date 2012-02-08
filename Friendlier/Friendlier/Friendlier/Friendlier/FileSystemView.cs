using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;


namespace Xyglo
{
    
    public class FileSystemView 
    {
        protected string m_path;

        public string getPath()
        {
            return m_path;
        }

        // Reset to this directory and test for directory and file access at this level
        //
        public void setDirectory(string directory)
        {
            m_path = directory;
            scanDirectory();
        }

        //protected Hashtable m_directoryTree = new Hashtable();

        protected DirectoryInfo m_directoryInfo;
        protected FileInfo m_fileInfo;

        public DirectoryInfo getDirectoryInfo()
        {
            return m_directoryInfo;
        }

        float m_lineHeight;
        float m_charWidth;

        public FileSystemView(string path, Vector3 position, float lineHeight, float charWidth)
        {
            m_path = path;
            m_position = position;
            m_lineHeight = lineHeight;
            m_charWidth = charWidth;
            scanDirectory();
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

        protected void scanDirectory()
        {
            // Attempt to get Directory and File info for this path
            //
            m_directoryInfo = new DirectoryInfo(m_path);
            m_fileInfo = new FileInfo(m_path);
        }

        Vector3 m_position;

        public Vector3 getPosition()
        {
            return m_position;
        }

        public Vector3 getEyePosition()
        {
            Vector3 rV = m_position;
            rV.Y = -rV.Y; // invert Y
            rV.X += m_charWidth * m_bufferShowWidth / 2;
            rV.Y -= m_lineHeight * m_bufferShowLength / 2;
            rV.Z += 600.0f;
            return rV;
        }

        /// <summary>
        /// Length of visible buffer
        /// </summary>
        protected int m_bufferShowLength = 20;

        /// <summary>
        /// Get BufferShow length
        /// </summary>
        /// <returns></returns>
        public int getBufferShowLength()
        {
            return m_bufferShowLength;
        }

        /// <summary>
        /// Number of characters to show in a BufferView line
        /// </summary>
        protected int m_bufferShowWidth = 80;

        /// <summary>
        /// Accessor for BufferShowWidth
        /// </summary>
        /// <returns></returns>
        public int getBufferShowWidth()
        {
            return m_bufferShowWidth;
        }
    }
}
