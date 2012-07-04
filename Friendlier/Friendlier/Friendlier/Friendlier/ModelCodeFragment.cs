#region File Description
//-----------------------------------------------------------------------------
// ModelCodeFragment.cs
//
// Copyright (C) Xyglo Ltd. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Xyglo
{
    /// <summary>
    /// Defines a CodeFragment for our rendered Model - for the moment we assume this to
    /// be the same as a FileBuffer for rendering purposes.
    /// </summary>
    class ModelCodeFragment : ModelItem
    {
        public ModelCodeFragment(string name):base(name)
        {
            m_itemType = ItemType.CodeFragment;
            m_dimensions.X = 0.4f;
        }

        public ModelCodeFragment(string name, Vector3 pos)
            : base(name)
        {
            m_itemType = ItemType.CodeFragment;
            m_position = pos;
        }
    }
}
