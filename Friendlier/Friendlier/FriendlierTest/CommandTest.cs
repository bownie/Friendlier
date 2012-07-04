using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xyglo;

namespace FriendlierTest
{
    [TestClass]
    public class CommandsTest
    {
        /// <summary>
        /// Our test Project
        /// </summary>
        Project         m_project;

        /// <summary>
        /// Our test FileBuffer
        /// </summary>
        FileBuffer      m_fileBuffer;

        /// <summary>
        /// A File Position
        /// </summary>
        FilePosition    m_fp1;

        /// <summary>
        /// A screen position
        /// </summary>
        ScreenPosition  m_hl1;

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
            m_commands =  new List<Command>();
        }

        [TestMethod]
        public void testInsertLines()
        {
            string originalTextLine1 = "First test text";

            // Insert a single line of text
            //
            InsertTextCommand insertCommand = new InsertTextCommand(m_project, "Insert first line", m_fileBuffer, m_fp1, originalTextLine1, m_hl1, m_hl1);
            insertCommand.doCommand();

            // Push onto commands list
            //
            m_commands.Add(insertCommand);

            // Delete a single character at the beginning of the text
            //
            DeleteTextCommand deleteCommand = new DeleteTextCommand(m_project, "Delete single character", m_fileBuffer, m_fp1, m_fp1, m_hl1, m_hl1);
            deleteCommand.doCommand();

            // Push onto commands list
            //
            m_commands.Add(deleteCommand);

            // Asset this test
            //
            Assert.AreEqual(originalTextLine1.Substring(1, originalTextLine1.Length - 1), m_fileBuffer.getLine(0));

            // Move cursor to the end of the line
            //
            m_fp1.X = m_fileBuffer.getLine(0).Length;

            // Insert a new line
            //
            InsertTextCommand secondLineCommand = new InsertTextCommand(m_project, "Insert second line", m_fileBuffer, m_fp1, m_hl1, m_hl1, true);
            secondLineCommand.doCommand();

            // Push onto commands list
            //
            m_commands.Add(secondLineCommand);

            // Check line count
            //
            Assert.IsTrue(m_fileBuffer.getLineCount() == 2, "Line count should be 2 at this point");

            // Move to beginning of the second line
            m_fp1.X = 0;
            m_fp1.Y = 1;

            // Populate some more text
            //
            InsertTextCommand secondLinePopulateCommand = new InsertTextCommand(m_project, "Populate second line", m_fileBuffer, m_fp1, "This is the second line text.", m_hl1, m_hl1);
            secondLinePopulateCommand.doCommand();

            // Push onto commands list
            //
            m_commands.Add(secondLinePopulateCommand);
        }

        [TestMethod]
        public void testDeleteSelection()
        {
            string originalTextLine1 = "First test text";

            // Insert a single line of text
            //
            InsertTextCommand insertCommand = new InsertTextCommand(m_project, "Insert first line", m_fileBuffer, m_fp1, originalTextLine1, m_hl1, m_hl1);
            insertCommand.doCommand();

            m_fp1.X = 7;
            m_fp1.Y = 0;

            FilePosition fp2 = m_fp1;
            fp2.X = 12;

            DeleteTextCommand deleteSelection = new DeleteTextCommand(m_project, "Delete selection", m_fileBuffer, m_fp1, fp2, m_hl1, m_hl1);
            deleteSelection.doCommand();

            // Push onto commands list
            //
            m_commands.Add(deleteSelection);

            string result = m_fileBuffer.getLine(0);
            string compare = originalTextLine1.Remove(6, 5);

            Assert.AreEqual(compare, m_fileBuffer.getLine(0));
        }

        [TestMethod]
        public void testReplaceSelection()
        {
            string originalTextLine1 = "First test text";

            // Insert a single line of text
            //
            InsertTextCommand insertCommand = new InsertTextCommand(m_project, "Insert first line", m_fileBuffer, m_fp1, originalTextLine1, m_hl1, m_hl1);
            insertCommand.doCommand();

            string replaceText = "REPLACING";

            FilePosition fp2 = m_fp1;
            fp2.X = 12;

            ScreenPosition hl2 = m_hl1;
            hl2.X += 5;
            ReplaceTextCommand replaceCommand = new ReplaceTextCommand(m_project, "Replace selection", m_fileBuffer, m_fp1, fp2, replaceText, m_hl1, hl2);
            replaceCommand.doCommand();

            // Push onto commands list
            //
            m_commands.Add(replaceCommand);
        }

        [TestMethod]
        public void testUndoStack()
        {
            // Retrieve this from the command
            //
            ScreenPosition cursorPosition;

            if (m_commands.Count() > 0)
            {
                for (int i = m_commands.Count() - 1; i > 0; i--)
                {
                    cursorPosition = m_commands[i].undoCommand();
                }
            }

            // Check the cursor and the undo has been done correctly
            //
        }


    }
}
