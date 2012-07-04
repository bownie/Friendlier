#region File Description
//-----------------------------------------------------------------------------
// FileSystemFile.cs
//
// Copyright (C) Xyglo Ltd. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xyglo
{
    /// <summary>
    /// Not used
    /// </summary>
    public class FileSystemFile : FileSystemObject
    {
        public FileSystemFile() : base ("")
        {
            this.m_name = "";
            this.m_type = FileSystemObject.FileSystemObjectType.File;
        }

        public FileSystemFile(string name) : base (name)
        {
            this.m_type = FileSystemObject.FileSystemObjectType.File;
        }

     

    }
}
