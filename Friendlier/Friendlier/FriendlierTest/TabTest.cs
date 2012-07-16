using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xyglo;
using Microsoft.Xna.Framework;

namespace TestProject1
{
    /// <summary>
    /// Some unit tests to ensure that tabs are being handled properly by the FileBuffers and the
    /// ScreenPositions.
    /// </summary>
    [TestClass]
    public class TabTest
    {
        /// <summary>
        /// Our test Project
        /// </summary>
        Project m_project;

        /// <summary>
        /// Our test FileBuffer
        /// </summary>
        FileBuffer m_fileBuffer;

        /// <summary>
        /// A File Position
        /// </summary>
        FilePosition m_fp1;

        /// <summary>
        /// A screen position
        /// </summary>
        ScreenPosition m_hl1;

        /// <summary>
        /// Keep a track of the ScreenPosition
        /// </summary>
        ScreenPosition m_cursorPosition;

        /// <summary>
        /// List of commands so that we can test undo/redo
        /// </summary>
        List<Command> m_commands;

        [TestInitialize]
        public void initialise()
        {
            // Init Project and FileBuffer
            //
            m_project = new Project();
            m_fileBuffer = new FileBuffer();

            // Init positions
            //
            m_fp1 = new FilePosition(0, 0);
            m_hl1 = new ScreenPosition(-1, -1);

            // Initialise the commands list
            //
            m_commands = new List<Command>();
        }

        [TestMethod]
        public void DoTabTest()
        {
            // Create command, run and add to the m_commands list.  Storing the cursor position.
            //
            InsertTextCommand textCommand = new InsertTextCommand(m_project, "Insert something with Tabs", m_fileBuffer, m_fp1, "\t\t\t\thello there\t\t\t\t", m_hl1, m_hl1);
            m_cursorPosition = textCommand.doCommand();
            m_commands.Add(textCommand);


        }
    }
}
