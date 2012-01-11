using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xyglo
{
    public class DeleteTextCommand : Command
    {
        public DeleteTextCommand(string name, FileBuffer buffer, FilePosition start, FilePosition end)
        {
            m_name = name;
            m_fileBuffer = buffer;
            m_startPos = start;
            m_endPos = end;

            // Correct start and end positions
            //
            positionOrder();
        }

        /// <summary>
        /// Do this command
        /// </summary>
        public override void doCommand()
        {
            string newLine, bufLine;

            // Are we deleting on the same line?
            //
            if (m_startPos.Y == m_endPos.Y)
            {
                string line = m_fileBuffer.getLine(m_startPos.Y);

                // Add this line to snippet as we're only editing a single line
                //
                //m_snippet.m_lines.Add(line);
                m_snippet.setSnippetSingle(line);

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
                            m_snippet.m_linesDeleted++;
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
                        m_snippet.m_linesDeleted++;
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
                m_snippet.m_lines.Clear();

                for (int i = m_startPos.Y; i < m_endPos.Y; i++)
                {
                    bufLine = m_fileBuffer.getLine(i);

                    // Add the whole bufLine to snippet
                    //
                    Console.WriteLine("ADDING TO SNIPPET = " + bufLine);

                    m_snippet.m_lines.Add(bufLine);

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
                m_snippet.m_lines.Add(m_fileBuffer.getLine(m_endPos.Y));

                // Delete all the remaining lines
                //
                m_fileBuffer.deleteLines(m_startPos.Y + 1, m_endPos.Y - m_startPos.Y);
                m_snippet.m_linesDeleted += Convert.ToInt16(m_endPos.Y - m_startPos.Y);

                // Set the current line to our buffer
                //
                m_fileBuffer.setLine(m_startPos.Y, bufLine);

            }
        }

        /// <summary>
        /// Undo this command
        /// </summary>
        public override void undoCommand()
        {
            Console.WriteLine("m_linesDeleted = " + m_snippet.m_linesDeleted);

            // If we need to re-insert a line then do so
            //
            for (int i = 0; i < m_snippet.m_linesDeleted; i++)
            {
                Console.WriteLine("INSERT LINE at " + m_startPos.Y);
                m_fileBuffer.insertLine(m_startPos.Y, "dummy");
            }

            Console.WriteLine("SNIPPET LINE COUNT = " + m_snippet.m_lines.Count);

            // Now overwrite all the lines
            //
            int snippetLine = 0;
            for (int i = m_startPos.Y; i < m_startPos.Y + m_snippet.m_lines.Count; i++)
            {
                Console.WriteLine("OVERWRITING = " + snippetLine);
                m_fileBuffer.setLine(i, m_snippet.m_lines[snippetLine++]);
            }
        }

        /// <summary>
        /// Dispose of current TextSnippet - clear it and return it
        /// </summary>
        public override void Dispose()
        {
            //m_snippet.clear();
            SnippetFactory.returnSnippet(m_snippet);
            Console.WriteLine("DeleteTextCommand Dispose()");
        }

        TextSnippet m_snippet = SnippetFactory.getSnippet();
        FileBuffer m_fileBuffer;

    }
}
