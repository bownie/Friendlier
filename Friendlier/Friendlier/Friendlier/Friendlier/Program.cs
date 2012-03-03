#region File Description
//-----------------------------------------------------------------------------
// Program.cs
//
// Copyright (C) Xyglo. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.IO;

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
            Project project;
            string xmlFile = @"C:\Temp\serialise.txt";

            if (File.Exists(xmlFile))
            {
                project = Project.dataContractDeserialise(xmlFile);
                project.loadFiles();
                project.connectFloatingWorld();
            }
            else
            {
                project = new Project("My First Project");
                // Create a project and load it
                //
                string editFile = @"C:\devel\SubFriendly\Friendlier\Friendlier\Friendlier\Friendlier.cs";
                string rbFile1 = @"C:\FinanceIT\FiRE\Server\scripts\appSchemaInstall.ksh";
                string rbFile2 = @"C:\appSchemaInstall.ksh";

                BufferView newBV = project.addFileBuffer(editFile, 0);
//                BufferView newBV = project.addFileBuffer(rbFile1, 0);
                //project.addFileBufferRelative(rbFile2, newBV, BufferView.BufferPosition.Right);
            }

            Friendlier friendlier = new Friendlier(project);
            friendlier.Run();
        }
    }
#endif
}

