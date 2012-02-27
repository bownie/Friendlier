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
        [STAThread]// Setting this as single threaded for cut and paste of all things to work
        static void Main(string[] args)
        {
            // Build a list of Files/Entities/Fragments and label them and extact some basic information
            //

            // Build a tree from the relationships
            //
            //TreeBuilder tb = new TreeBuilder();
            //tb.topologicalSort();

            // Render the tree
            //
            //ModelBuilder mb = new ModelBuilder(tb);
            //mb.build();

            // Create a project or load one
            //


            string editFile = @"C:\devel\SubFriendly\Friendlier\Friendlier\Friendlier\Friendlier.cs";

            Project project = new Project();
            project.addFileBuffer(editFile);

            //editFile = @"C:\FinanceIT\FiRE\Server\scripts\appSchemaInstall.ksh";
            //editFile = @"C:\appSchemaInstall.ksh";

            Friendlier friendlier = new Friendlier();
            friendlier.loadProject(project);

            friendlier.Run();
        }
    }
#endif
}

