#region File Description
//-----------------------------------------------------------------------------
// Undoable.cs
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
    /// Interface that we want all of our Commands to implement.  Not currently used or even close
    /// to being finished by the looks of things.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal interface IUndoable<T>
    {
        T State { get; }
    }
}
