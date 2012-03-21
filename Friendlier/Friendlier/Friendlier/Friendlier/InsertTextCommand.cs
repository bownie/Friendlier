using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Xyglo
{
    /// <summary>
    /// Take a FilePosition and some text and insert it into a FileBuffer at that point.
    /// </summary>
    [DataContract(Name = "Friendlier", Namespace = "http://www.xyglo.com")]
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
        public InsertTextCommand(string name, FileBuffer buffer, FilePosition insertPosition, bool newLine = true, string indent = "")
        {
            m_name = name;
            m_fileBuffer = buffer;
            m_startPos = insertPosition;
            m_newLine = newLine;
            m_indent = indent;
        }

        /// <summary>
        /// Do this command
        /// </summary>
        public override FilePosition doCommand()
        {
#if INSERT_COMMAND_DEBUG
            Logger.logMsg("InsertTextCommand::doCommand() - start position X = " + m_startPos.X + ", Y = " + m_startPos.Y);
#endif

            // Store the initial cursor position locally
            //
            FilePosition fp = m_startPos;

            // Fetch the line if it's available
            //
            string fetchLine = "";
            if (m_fileBuffer.getLineCount() > 0 && m_startPos.Y < m_fileBuffer.getLineCount())
            {
                fetchLine = m_fileBuffer.getLine(m_startPos.Y);
            }
            
            string firstLine = fetchLine.Substring(0, m_startPos.X);
            string secondLine = "";

            // Always as default set the original line to this
            //
            m_originalText = fetchLine;

            if (fetchLine.Length > 0)
            {
                secondLine = fetchLine.Substring(m_startPos.X, fetchLine.Length - m_startPos.X);
            }

            // Force a new line if we're inserting beyond the end of the current buffer
            //
            //if (m_startPos.Y >= m_fileBuffer.getLineCount())
            //{
                //m_newLine = true;
            //}

            // Set first line always like this
            //
            if (m_fileBuffer.getLineCount() > 0)
            {
                m_fileBuffer.setLine(m_startPos.Y, firstLine);
            }

            if (m_newLine) // If this is a newline then special case for a single newline only
            {
                // Inserting several lines in an empty buffer
                //
                if (m_fileBuffer.getLineCount() == 0)
                {
                    // Originally we had nothing - so put that back when we undo
                    //
                    m_originalText = "";

                    if (m_snippet.m_lines.Count == 0)
                    {
                        m_fileBuffer.insertLine(0, "");
                        m_fileBuffer.insertLine(1, "");

                        fp.Y = 1;
                    }
                    else
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
                }
                else
                {
                    // In this case we only ever want to insert one line - return has been hit
                    //

                    // Ensure we add any indent
                    //
                    if (m_indent != "")
                    {
                        secondLine = m_indent + secondLine;
                    }

                    m_fileBuffer.insertLine(m_startPos.Y + 1, secondLine);

                    // Adjust the new X cursor position according to if we're auto indenting or not
                    //
                    if (m_indent == "")
                    {
                        fp.X = 0; // Reset to zero on X
                    }
                    else
                    {
                        fp.X = m_indent.Length;
                    }

                    fp.Y++; // Increment Y
                }
            }
            else
            {
                // Add a line if there is none
                //
                if (m_fileBuffer.getLineCount() == 0)
                {
                    m_fileBuffer.insertLine(0, "");
                }

                
                if (m_snippet.m_lines.Count() == 1)
                {
                    m_fileBuffer.setLine(m_startPos.Y, firstLine + m_snippet.m_lines[0] + secondLine);
                    fp.X += m_snippet.m_lines[0].Length;
                }
                else
                {
                    // Append the first line onto the current line firstly
                    //
                    m_fileBuffer.appendLine(m_startPos.Y, m_snippet.m_lines[0]);

                    // Now insert additional lines in reverse snippet order so that they flow downwards
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
                // If we only have one line
                //
                if (m_fileBuffer.getLineCount() == 1)
                {
                    m_fileBuffer.setLine(0, m_originalText);
                }
                else
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
            }
            else
            {
                // We could have inserted multiple lines here - enusure that they are removed
                if (m_snippet.m_lines.Count() > 1)
                {
                    m_fileBuffer.deleteLines(m_startPos.Y + 1, m_snippet.m_lines.Count() - 1);
                }


                if (m_startPos.Y == 0 && m_originalText == "")
                {
                    m_fileBuffer.deleteLines(0, 1);
                    return (new FilePosition(0, 0));

                }
                else
                {
                    m_fileBuffer.setLine(m_startPos.Y, m_originalText);
                }

            }

#if INSERT_COMMAND_DEBUG
            Logger.logMsg("InsertTextCommand::undoCommand() - undo position X = " + m_startPos.X + ", Y = " + m_startPos.Y);
#endif

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
        [DataMember]
        string m_originalText;

        /// <summary>
        /// Do we need a new line?
        /// </summary>
        [DataMember]
        bool m_newLine;

        /// <summary>
        /// Snippet for our text - the line can expand into a multi-line snipper
        /// </summary>
        [DataMember]
        TextSnippet m_snippet = SnippetFactory.getSnippet();

        /// <summary>
        /// The FileBuffer we're working on
        /// </summary>
        FileBuffer m_fileBuffer;

        /// <summary>
        /// An indent for a new line
        /// </summary>
        protected string m_indent = "";
    }
}
