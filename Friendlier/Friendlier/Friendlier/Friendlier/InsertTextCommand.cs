using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xyglo
{
    /// <summary>
    /// Take a FilePosition and some text and insert it into a FileBuffer at that point.
    /// </summary>
    class InsertTextCommand : Command
    {
        /// <summary>
        /// Insert text constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="buffer"></param>
        /// <param name="insertPosition"></param>
        /// <param name="text"></param>
        public InsertTextCommand(string name, FileBuffer buffer, FilePosition insertPosition, string text)
        {
            m_name = name;
            m_fileBuffer = buffer;
            m_startPos = insertPosition;
            m_text = text;
        }

        /// <summary>
        /// New line constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="buffer"></param>
        /// <param name="insertPosition"></param>
        /// <param name="newLine"></param>
        public InsertTextCommand(string name, FileBuffer buffer, FilePosition insertPosition, bool newLine = true)
        {
            m_name = name;
            m_fileBuffer = buffer;
            m_startPos = insertPosition;
            m_newLine = newLine;
        }

        /// <summary>
        /// Do this command
        /// </summary>
        public override FilePosition doCommand()
        {
            // Fetch and store the line
            //
            string fetchLine = m_fileBuffer.getLine(m_startPos.Y);

            // Store the initial cursor position locally
            //
            FilePosition fp = m_startPos;

            if (m_newLine)
            {
                // Store the whole line in the text field for undo purposes
                //
                m_text = fetchLine;
                fp.Y++;

                if (m_startPos.X == 0)
                {
                    m_fileBuffer.insertLine(m_startPos.Y, "");
                }
                else if (fetchLine.Length == m_startPos.X)
                {
                    m_fileBuffer.insertLine(m_startPos.Y + 1, "");

                    // Reset to zero on X
                    //
                    fp.X = 0;
                }
                else
                {
                    // Split line and create new one
                    //
                    string firstLine = fetchLine.Substring(0, m_startPos.X);
                    string secondLine = fetchLine.Substring(m_startPos.X + 1, fetchLine.Length - (m_startPos.X + 1));

                    // Set first
                    //
                    m_fileBuffer.setLine(m_startPos.Y, firstLine);

                    // Insert second
                    //
                    m_fileBuffer.insertLine(m_startPos.Y + 1, secondLine);

                    // Reset to zero on X
                    //
                    fp.X = 0;
                }
            }
            else
            {
                // Insert the text at the cursor position
                //
                m_fileBuffer.setLine(m_startPos.Y, fetchLine.Insert(m_startPos.X, m_text));

                fp.X += m_text.Length;
                //Console.WriteLine("Writing " + m_text + " (length " + m_text.Length + ")");
            }

            return fp;
        }

        /// <summary>
        /// Undo this command
        /// </summary>
        public override FilePosition undoCommand()
        {
            // Did we insert a line or some text?
            if (m_newLine)
            {
                // Check to see whether the new line has something on it - if so we need to reconstruct the original
                // line and then delete the new one
                if (m_fileBuffer.getLine(m_startPos.Y + 1).Length > 0)
                {
                    m_fileBuffer.setLine(m_startPos.Y, m_text);
                }

                // Delete the new line now
                //
                m_fileBuffer.deleteLines(m_startPos.Y + 1, 1);
            }
            else
            {
                string fetchLine = m_fileBuffer.getLine(m_startPos.Y);
                m_fileBuffer.setLine(m_startPos.Y, fetchLine.Remove(m_startPos.X, m_text.Length));
            }
            // Return the start position when undoing
            //
            return m_startPos;
        }

        /// <summary>
        ///  Overridden dispose method
        /// </summary>
        public override void Dispose()
        {
            m_text = "";
            m_fileBuffer = null;
        }

        /// <summary>
        /// Text we're inserting with this command - also used for undo purposes for new lines
        /// </summary>
        string m_text;

        /// <summary>
        /// Do we need a new line?
        /// </summary>
        bool m_newLine;

        //TextSnippet m_snippet = SnippetFactory.getSnippet();

        /// <summary>
        /// The FileBuffer we're working on
        /// </summary>
        FileBuffer m_fileBuffer;
    }
}
