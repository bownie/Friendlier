using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Xyglo
{
    public class FileSystemObject
    {
        public enum FileSystemObjectType
        {
            None,
            Unknown,
            File,
            Directory,
            Link
        };

        protected FileSystemObjectType m_type;
        protected string m_name;

        public FileSystemObject(string name)
        {
            m_name = name;
            m_type = FileSystemObjectType.None;
            m_position = Vector3.Zero;
        }

        /// <summary>
        /// Position in 3D space
        /// </summary>
        Vector3 m_position;

        public Vector3 getPosition()
        {
            return m_position;
        }


    }
}
