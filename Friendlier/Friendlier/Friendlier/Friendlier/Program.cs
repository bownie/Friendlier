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
            //using (Friendlier game = new Friendlier())
            //{
                //game.Run();
            //}

            // File parser - extract relationships
            //


            // Build a list of Files/Entities/Fragments and label them and extact some basic information
            //

            // Build a tree from the relationships
            //
            TreeBuilder tb = new TreeBuilder();
            tb.topologicalSort();

            // Render the tree
            //
            //ModelBuilder mb = new ModelBuilder(tb);
            //mb.build();

            string editFile = @"C:\devel\SubFriendly\Friendlier\Friendlier\Friendlier\Friendlier.cs";
            //editFile = @"C:\FinanceIT\FiRE\Server\scripts\appSchemaInstall.ksh";

            Friendlier friendlier = new Friendlier(editFile);
            friendlier.Run();

        }
    }
#endif
}

