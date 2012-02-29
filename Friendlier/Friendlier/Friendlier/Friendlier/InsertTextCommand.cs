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
            if (m_fileBuffer.getLineCount() > 0 && m_startPos.Y < m_fileBuffer.getLineCount())
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
                // Inserting several lines in an empty buffer
                //
                if (m_fileBuffer.getLineCount() == 0)
                {
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
        [DataMember()]
        string m_originalText;

        /// <summary>
        /// Do we need a new line?
        /// </summary>
        [DataMember()]
        bool m_newLine;

        /// <summary>
        /// Snippet for our text - the line can expand into a multi-line snipper
        /// </summary>
        [DataMember()]
        TextSnippet m_snippet = SnippetFactory.getSnippet();

        /// <summary>
        /// The FileBuffer we're working on
        /// </summary>
        FileBuffer m_fileBuffer;
    }
}
