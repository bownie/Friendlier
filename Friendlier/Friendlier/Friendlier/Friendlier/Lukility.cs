#region File Description
//-----------------------------------------------------------------------------
// Lukility.cs
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
    /// This is purely for stuff that we want to play around with or prove.  It's where Luke's
    /// tests sit and the poor answers for them.
    /// </summary>
    public class Lukility
    {
        public bool isPowerOfTwo(int n)
        {
            float floatN = (float)n;

            while (floatN > 1.0f)
            {
                floatN /= 2.0f;
            }

            return (floatN == 1.0f);
        }
    }
}
