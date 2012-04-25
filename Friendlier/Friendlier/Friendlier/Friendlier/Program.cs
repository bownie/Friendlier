#region File Description
//-----------------------------------------------------------------------------
// Program.cs
//
// Copyright (C) Xyglo. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.IO;
using System.Windows.Forms;

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
#if OUTER_EXCEPTION_HANDLING
            try
            {
#endif
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

                bool checkValidity = Registration.checkRegistry();

                // Check for software registration
                //
                if (checkValidity)
                {
                    Logger.logMsg("Friendlier - licence key passed validation");
                }
                //            else
                //          {
                //            Logger.logMsg("Friendlier - licence key failed validation");

                // Test for drop dead and exit if it's passed
                //
                if (DateTime.Now > VersionInformation.getDropDead())
                {
                    Logger.logMsg("Registration::checkRegistry() - drop dead date has passed.  Please licence this software to continue to save files.");
                    checkValidity = false;
                }
                else
                {
                    checkValidity = true; // just override for the moment
                }
                //      }
                //Logger.logMsg("SHHH = " + Registration.generate("me", "rich@xyglo.com", VersionInformation.getProductName(), VersionInformation.getProductVersion()));


                // Create a FontManager
                //
                FontManager fontManager = new FontManager();

                // Create a project or load one
                //
                Project project;
                string projectFile = Project.getUserDataPath() + "default_project.xml";

                //Logger.logMsg(" DIR = " + projectFile);

                if (File.Exists(projectFile))
                {
                    project = Project.dataContractDeserialise(fontManager, projectFile);
                    //project.loadFiles();
                    //project.connectFloatingWorld();
                }
                else
                {
                    project = new Project(fontManager, "New Project", projectFile);
                }

                //Logger.logMsg("FILE BUFFER ROOT = " + project.getFileBufferRoot());

                // Set the licencing state
                //
                project.setLicenced(checkValidity);

                Friendlier friendlier = new Friendlier(project);
                friendlier.Run();
#if OUTER_EXCEPTION_HANDLING
            }
            catch (Exception e)
            {
                MessageBox.Show("Friendlier encountered a problem: " + e.Message);
            }
#endif
        }
    }
#endif
}

