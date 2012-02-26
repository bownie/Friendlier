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

            if (text != null && text.Split(m_splitCharacter).Count() > 1)
            {
                // Populate snippet with multiple lines
                //
                for (int i = 0; i < text.Split(m_splitCharacter).Count(); i++)
                {
                    m_snippet.m_lines.Add(text.Split(m_splitCharacter)[i]);
                }
            }
            else
            {
                m_snippet.m_lines.Add(text);  // populate one line
            }
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
            // Store the initial cursor position locally
            //
            FilePosition fp = m_startPos;

            // Fetch the line if it's available
            //
            string fetchLine = "";
            if (m_fileBuffer.getLineCount() > 0)
            {
                fetchLine = m_fileBuffer.getLine(m_startPos.Y);
            }

            string firstLine = fetchLine.Substring(0, m_startPos.X);
            string secondLine = "";

            if (fetchLine.Length > 0)
            {
                secondLine = fetchLine.Substring(m_startPos.X, fetchLine.Length - m_startPos.X);
            }

            // Force a new line if we're inserting beyond the end of the current buffer
            //
            if (m_startPos.Y >= m_fileBuffer.getLineCount())
            {
                m_newLine = true;
            }

            // Set first line always like this
            //
            if (m_fileBuffer.getLineCount() > 0)
            {
                m_fileBuffer.setLine(m_startPos.Y, firstLine);
            }

            if (m_newLine) // If this is a newline then special case for a single newline only
            {
                // Insert second
                //
                if (m_fileBuffer.getLineCount() == 0)
                {
                    // Insert lines in reverse snippet order
                    //
                    for (int i = m_snippet.m_lines.Count() - 1; i >= 0; i--)
                    {
                        m_fileBuffer.insertLine(m_startPos.Y, m_snippet.m_lines[i]);

                        if (i != 0)
                        {
                            fp.Y++; // incremement once per inserted line
                        }
                    }
                    fp.X = m_snippet.m_lines.Last<string>().Length;
                }
                else
                {
                    // In this case we only ever want to insert one line
                    //
                    m_fileBuffer.insertLine(m_startPos.Y + 1, secondLine);
                    m_originalText = fetchLine; // store original line

                    fp.X = 0; // Reset to zero on X
                    fp.Y++; // Increment Y
                }
            }
            else
            {
                m_fileBuffer.appendLine(m_startPos.Y, m_snippet.m_lines[0]);
                if (m_snippet.m_lines.Count() == 1)
                {
                    m_fileBuffer.appendLine(m_startPos.Y, secondLine);
                    fp.X += m_snippet.m_lines[0].Length;
                }
                else
                {
                    // Insert lines in reverse snippet order
                    //
                    for (int i = m_snippet.m_lines.Count() - 1; i > 0; i--)
                    {
                        m_fileBuffer.insertLine(m_startPos.Y + 1, m_snippet.m_lines[i]);
                        fp.Y++; // incremement once per inserted line
                    }


                    fp.X = m_snippet.m_lines.Last<string>().Length;

                    // Append the end
                    m_fileBuffer.appendLine(m_startPos.Y + m_snippet.m_lines.Count(), secondLine);
                }
            }

            /*
            // If this buffer is empty then insert a line and set it
            //
            if (m_fileBuffer.getLineCount() == 0)
            {
                // Insert lines in reverse snippet order
                //
                for (int i = m_snippet.m_lines.Count() - 1; i >= 0; i--)
                {
                    m_fileBuffer.insertLine(m_startPos.Y, m_snippet.m_lines[i]);
                }
            }
            else
            {
                // Fetch and store the line
                //
                string fetchLine = "";

                if (m_startPos.Y < m_fileBuffer.getLineCount())
                {
                    fetchLine = m_fileBuffer.getLine(m_startPos.Y);
                }
                else
                {
                    m_newLine = true;
                }

                if (m_newLine)
                {
                    // Store the whole line in the text field for undo purposes
                    //
                    m_text = fetchLine;
                    fp.Y++;


                    if (m_startPos.X == 0) // if we're inserting at line beginning
                    {
                        // Insert lines in reverse snippet order
                        //
                        for (int i = m_snippet.m_lines.Count() - 1; i >= 0; i--)
                        {
                            m_fileBuffer.insertLine(m_startPos.Y, m_snippet.m_lines[i]);
                        }
                    }
                    else if (fetchLine.Length == m_startPos.X)  // or at line end
                    {
                        // Insert lines in reverse snippet order
                        //
                        for (int i = m_snippet.m_lines.Count() - 1; i >= 0; i--)
                        {
                            m_fileBuffer.insertLine(m_startPos.Y + 1, m_snippet.m_lines[i]);
                        }
                        //m_fileBuffer.insertLine(m_startPos.Y + 1, "");

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
                    m_fileBuffer.setLine(m_startPos.Y, fetchLine.Insert(m_startPos.X, m_snippet.m_lines[0]));

                    if (m_snippet.m_lines.Count() > 1)
                    {

                        // Insert the rest of the lines in reverse snippet order
                        //
                        for (int i = m_snippet.m_lines.Count() - 1; i > 0; i--)
                        {
                            m_fileBuffer.insertLine(m_startPos.Y + 1, m_snippet.m_lines[i]);
                        }
                    }

                    fp.X += m_text.Length;
                    Logger.logMsg("InsertTextCommand::FilePosition() - writing " + m_text + " (length " + m_text.Length + ")");
                }
                
            }
            */
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
                    m_fileBuffer.setLine(m_startPos.Y, m_originalText);
                }

                // Delete the new line now
                //
                m_fileBuffer.deleteLines(m_startPos.Y + 1, 1);
            }
            else
            {
                string fetchLine = m_fileBuffer.getLine(m_startPos.Y);
                m_fileBuffer.setLine(m_startPos.Y, fetchLine.Remove(m_startPos.X, m_originalText.Length));
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
            m_originalText = "";
            m_fileBuffer = null;
        }

        /// <summary>
        /// We use this only for undo as the original position will only be one line
        /// </summary>
        string m_originalText;

        /// <summary>
        /// Do we need a new line?
        /// </summary>
        bool m_newLine;

        /// <summary>
        /// Snippet for our text - the line can expand into a multi-line snipper
        /// </summary>
        TextSnippet m_snippet = SnippetFactory.getSnippet();

        /// <summary>
        /// The FileBuffer we're working on
        /// </summary>
        FileBuffer m_fileBuffer;
    }
}
