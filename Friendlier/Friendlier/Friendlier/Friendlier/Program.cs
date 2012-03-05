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
            string projectFile = Project.getUserDataPath() + "default_project.xml";

            Logger.logMsg(" DIR = " + projectFile);

            if (File.Exists(projectFile))
            {
                project = Project.dataContractDeserialise(projectFile);
                project.loadFiles();
                project.connectFloatingWorld();
            }
            else
            {
                project = new Project("New Project", projectFile);
            }

            Friendlier friendlier = new Friendlier(project);
            friendlier.Run();
        }
    }
#endif
}

