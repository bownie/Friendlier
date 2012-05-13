﻿using System;
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
    class ReformatTextCommand : Command
    {
        /// <summary>
        /// Reformattext constructor - note we have the jiggery pokery for the null FilePositions
        /// </summary>
        /// <param name="name"></param>
        /// <param name="buffer"></param>
        /// <param name="insertPosition"></param>
        /// <param name="text"></param>
        public ReformatTextCommand(string name, FileBuffer buffer, SyntaxManager syntaxManager, FilePosition? startPosition = null, FilePosition? endPosition = null)
        {
            m_name = name;
            m_fileBuffer = buffer;
            m_startPos = startPosition ?? new FilePosition(0, 0);
            m_endPos= endPosition ?? new FilePosition(0, 0);
            m_syntaxManager = syntaxManager;
        }

        /// <summary>
        /// Do this command
        /// </summary>
        public override FilePosition doCommand()
        {
            FilePosition fp = m_startPos;

            string fetchLine = m_fileBuffer.getLine(m_startPos.Y);

            // Special case for a closing brace
            //
            if (m_startPos == m_endPos && fetchLine.Length > 2 && fetchLine.Substring(0, 2) == "  " && fetchLine.Substring(fetchLine.Length - 1, 1) == "}")
            {
                Logger.logMsg("ReformatTextCommand::doCommand() - fixing brace");

                // Push original line to snippet
                //
                m_snippet.m_lines.Add(fetchLine);

                // Modify line and push to FileBuffer
                //
                fetchLine = fetchLine.Remove(0, 2);
                m_fileBuffer.setLine(m_startPos.Y, fetchLine);

                // Fix the cursor
                //
                fp.X = fetchLine.Length;
            }

            return fp;
        }

        /// <summary>
        /// Undo this command
        /// </summary>
        public override FilePosition undoCommand()
        {
            if (m_snippet.m_lines.Count() == 1)
            {
                string line = m_snippet.m_lines[0];
            }
            return m_startPos;
        }

        /// <summary>
        ///  Overridden dispose method
        /// </summary>
        public override void Dispose()
        {
            //m_originalText = "";
            m_fileBuffer = null;
        }

        /// <summary>
        /// Snippet for our text - the line can expand into a multi-line snippet
        /// </summary>
        [DataMember]
        TextSnippet m_snippet = SnippetFactory.getSnippet();

        /// <summary>
        /// The FileBuffer we're working on
        /// </summary>
        FileBuffer m_fileBuffer;

        /// <summary>
        /// Keep a local copy of the syntax manager associated with this command
        /// </summary>
        SyntaxManager m_syntaxManager;

    }
}