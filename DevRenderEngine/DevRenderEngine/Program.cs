#region File Description
//-----------------------------------------------------------------------------
// Program.cs
//
// Copyright (C) Xyglo. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;

namespace Xyglo
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (DevRenderEngine game = new DevRenderEngine())
            {
                game.Run();
            }
        }
    }
#endif
}

