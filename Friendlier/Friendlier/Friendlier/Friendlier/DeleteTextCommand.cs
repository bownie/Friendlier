using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Xyglo
{
    [DataContract(Name = "Friendlier", Namespace = "http://www.xyglo.com")]
    public class DeleteTextCommand : Command
    {
        public DeleteTextCommand(Project project, string name, FileBuffer buffer, FilePosition start, FilePosition end, ScreenPosition startHighlight, ScreenPosition endHighlight)
        {
            m_name = name;
            m_fileBuffer = buffer;
            m_startPos = start;
            m_screenPosition = new ScreenPosition(start);
            m_endPos = end;
            m_project = project;
            m_highlightStart = startHighlight;
            m_highlightEnd = endHighlight;

            // Correct start and end positions
            //
            positionOrder();
        }

        /// <summary>
        /// Do this command
        /// </summary>
        public override ScreenPosition doCommand()
        {
            string newLine, bufLine;

            // Are we deleting on the same line?
            //
            if (m_startPos.Y == m_endPos.Y)
            {
                string line = m_fileBuffer.getLine(m_startPos.Y);

                // Add this line to snippet as we're only editing a single line
                //
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
                        if (m_fileBuffer.getLineCount() - 1 > m_startPos.Y)
                        {
                            // Append next line to current
                            //
                            m_fileBuffer.appendToLine(m_startPos.Y, m_fileBuffer.getLine(m_startPos.Y + 1));

                            // Remove next
                            m_fileBuffer.deleteLines(m_startPos.Y + 1, 1);
                            m_snippet.incrementLinesDeleted(1);
                        }
                        else
                        {
                            throw new Exception("DeleteTextCommand - nothing to delete");
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
                        m_snippet.incrementLinesDeleted(1);
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
                m_snippet.setLinesDeleted(0);

                for (int i = m_startPos.Y; i < m_endPos.Y; i++)
                {
                    bufLine = m_fileBuffer.getLine(i);

#if DELETE_COMMAND_DEBUG
                    // Add the whole bufLine to snippet
                    //
                    Logger.logMsg("DeleteTextCommand:doCommand() - adding a line to snippet - " + bufLine);
#endif

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

#if DELETE_COMMAND_DEBUG
                Logger.logMsg("DeleteTextCommand:doCommand() - snippet contains " + m_snippet.m_lines.Count());
#endif
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
                m_snippet.incrementLinesDeleted(Convert.ToInt16(m_endPos.Y - m_startPos.Y));

                // Set the current line to our buffer
                //
                m_fileBuffer.setLine(m_startPos.Y, bufLine);

            }

#if DELETE_COMMAND_DEBUG
            Logger.logMsg("DeleteTextCommand:doCommand() - snippet position is " + m_snippet.getSnippetFactoryPosition());
#endif
            return new ScreenPosition(m_startPos);
        }

        /// <summary>
        /// Undo this command
        /// </summary>
        public override ScreenPosition undoCommand()
        {
#if DELETE_COMMAND_DEBUG
            Logger.logMsg("DeleteTextCommand::undoCommand() - Lines to add " + m_snippet.getLinesDeleted());
#endif
            // If we need to re-insert a line then do so
            //
            for (int i = 0; i < m_snippet.getLinesDeleted(); i++)
            {
#if DELETE_COMMAND_DEBUG
                Logger.logMsg("DeleteTextCommand::undoCommand() - inserted line at Y position = " + m_startPos.Y);
#endif
                m_fileBuffer.insertLine(m_startPos.Y, "dummy");
            }

#if DELETE_COMMAND_DEBUG
            Logger.logMsg("DeleteTextCommand::undoCommand() - now overwriting " + m_snippet.m_lines.Count() + " lines");
#endif
            // Now overwrite all the lines
            //
            int snippetLine = 0;
            for (int i = m_startPos.Y; i < m_startPos.Y + m_snippet.m_lines.Count; i++)
            {
#if DELETE_COMMAND_DEBUG
                Logger.logMsg("DeleteTextCommand::undoCommand() - overwriting line " + i + " with " + snippetLine);
#endif
                m_fileBuffer.setLine(i, m_snippet.m_lines[snippetLine++]);
            }

            // Return the start position for undo
            //
            return m_screenPosition;
        }

        /// <summary>
        /// Dispose of current TextSnippet - clear it and return it
        /// </summary>
        public override void Dispose()
        {
            Logger.logMsg("DeleteTextCommand:Dispose() - returning snippet");

            // At the moment this isn't working properly
            //
            //SnippetFactory.returnSnippet(m_snippet);
        }

        [DataMember]
        TextSnippet m_snippet = SnippetFactory.getSnippet();

        FileBuffer m_fileBuffer;
    }
}
