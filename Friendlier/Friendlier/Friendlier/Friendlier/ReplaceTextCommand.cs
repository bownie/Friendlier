using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xyglo
{
    public class ReplaceTextCommand : Command
    {
        public ReplaceTextCommand(string name, FileBuffer buffer, FilePosition start, FilePosition end, string text)
        {
            m_name = name;
            m_fileBuffer = buffer;
            m_startPos = start;
            m_endPos = end;
            m_text = text;

            if (m_text != null && m_text.Split(m_splitCharacter).Count() > 1)
            {
                // Populate snippet with multiple lines
                //
                for (int i = 0; i < m_text.Split(m_splitCharacter).Count(); i++)
                {
                    m_writeSnippet.m_lines.Add(m_text.Split(m_splitCharacter)[i]);
                }
            }
            else
            {
                m_writeSnippet.m_lines.Add(m_text);  // populate one line
            }

            // Correct start and end positions
            //
            positionOrder();
        }


        /// <summary>
        /// Do this command
        /// </summary>
        public override FilePosition doCommand()
        {
            string newLine, bufLine;

            // Are we deleting on the same line?
            //
            if (m_startPos.Y == m_endPos.Y)
            {
                string line = m_fileBuffer.getLine(m_startPos.Y);

                // Add this line to snippet as we're only editing a single line
                //
                m_saveSnippet.setSnippetSingle(line);

                if (m_startPos.X == m_endPos.X) // deletion at cursor
                {
                    if (m_startPos.X < line.Length)
                    {
                        string editLine = line.Substring(0, m_startPos.X) +
                                          line.Substring(m_startPos.X + 1, line.Length - (m_startPos.X + 1));


                        m_fileBuffer.setLine(m_startPos.Y, editLine);
                    }
                    else
                    {
                        if (m_fileBuffer.getLineCount() > m_startPos.Y)
                        {
                            // Append next line to current
                            //
                            m_fileBuffer.appendLine(m_startPos.Y, m_fileBuffer.getLine(m_startPos.Y + 1));

                            // Remove next
                            m_fileBuffer.deleteLines(m_startPos.Y + 1, 1);
                            m_saveSnippet.incrementLinesDeleted(1);
                        }
                    }
                }
                else
                {
                    bufLine = line.Substring(0, m_startPos.X) +
                              line.Substring(m_endPos.X, line.Length - m_endPos.X);

                    if (bufLine == "")
                    {
                        m_fileBuffer.deleteLines(m_startPos.Y, 1);
                        m_saveSnippet.incrementLinesDeleted(1);
                    }
                    else
                    {
                        m_fileBuffer.setLine(m_startPos.Y, bufLine);
                    }
                }
            }
            else  // Multi-line delete
            {
                // Clear the snippet and then append all the lines we're going to delete
                //
                //
                m_saveSnippet.m_lines.Clear();

                for (int i = m_startPos.Y; i < m_endPos.Y; i++)
                {
                    bufLine = m_fileBuffer.getLine(i);

                    // Add the whole bufLine to snippet
                    //
                    Logger.logMsg("adding to snippet = " + bufLine);

                    m_saveSnippet.m_lines.Add(bufLine);

                    if (i == m_startPos.Y)
                    {
                        newLine = bufLine.Substring(m_startPos.X, bufLine.Length - m_startPos.X);
                    }
                    else if (i == m_endPos.Y)
                    {
                        newLine = bufLine.Substring(0, m_endPos.X);
                    }
                    else
                    {
                        newLine = bufLine;
                    }
                }

                // Cut and append the current line and the last line and save to bufLine
                //
                bufLine = m_fileBuffer.getLine(m_startPos.Y).Substring(0, m_startPos.X);
                bufLine += m_fileBuffer.getLine(m_endPos.Y).Substring(m_endPos.X, m_fileBuffer.getLine(m_endPos.Y).Length - m_endPos.X);

                // Push the end line onto the snippet so that is also restored by an undo
                //
                m_saveSnippet.m_lines.Add(m_fileBuffer.getLine(m_endPos.Y));

                // Delete all the remaining lines
                //
                m_fileBuffer.deleteLines(m_startPos.Y + 1, m_endPos.Y - m_startPos.Y);
                m_saveSnippet.incrementLinesDeleted(Convert.ToInt16(m_endPos.Y - m_startPos.Y));

                // Set the current line to our buffer
                //
                m_fileBuffer.setLine(m_startPos.Y, bufLine);

            }

            // Now do the insert of the text
            //
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
                Logger.logMsg("writing " + m_text + " (length " + m_text.Length + ")");
            }


            return m_startPos;
        }

        /// <summary>
        /// Undo this command
        /// </summary>
        public override FilePosition undoCommand()
        {
            Logger.logMsg("m_linesDeleted = " + m_saveSnippet.getLinesDeleted());

            // If we need to re-insert a line then do so
            //
            for (int i = 0; i < m_saveSnippet.getLinesDeleted(); i++)
            {
                Logger.logMsg("Inserted line at " + m_startPos.Y);
                m_fileBuffer.insertLine(m_startPos.Y, "dummy");
            }

            Logger.logMsg("snippet line count = " + m_saveSnippet.m_lines.Count);

            // Now overwrite all the lines
            //
            int snippetLine = 0;
            for (int i = m_startPos.Y; i < m_startPos.Y + m_saveSnippet.m_lines.Count; i++)
            {
                Logger.logMsg("overwriting = " + snippetLine);
                m_fileBuffer.setLine(i, m_saveSnippet.m_lines[snippetLine++]);
            }

            // Return the start position for undo
            //
            return m_startPos;
        }

        /// <summary>
        /// Dispose of current TextSnippet - clear it and return it
        /// </summary>
        public override void Dispose()
        {
            SnippetFactory.returnSnippet(m_saveSnippet);
            Logger.logMsg("ReplaceTextCommand Dispose()");
        }

        /// <summary>
        /// We have a Save Snippet for the lines we're deleting
        /// </summary>
        TextSnippet m_saveSnippet = SnippetFactory.getSnippet();

        /// <summary>
        /// There is a Write Snippet for the lines we want to insert
        /// </summary>
        TextSnippet m_writeSnippet = SnippetFactory.getSnippet();

        /// <summary>
        /// The FileBuffer
        /// </summary>
        FileBuffer m_fileBuffer;

        /// <summary>
        /// Text we're inserting with this command - also used for undo purposes for new lines
        /// </summary>
        string m_text;

        /// <summary>
        /// Do we need a new line?
        /// </summary>
        bool m_newLine = false;
    }
}
